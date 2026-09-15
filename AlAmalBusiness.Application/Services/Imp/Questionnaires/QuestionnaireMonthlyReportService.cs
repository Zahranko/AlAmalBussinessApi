using AlAmalBusiness.Application.DTOs.Email;
using AlAmalBusiness.Application.DTOs.Questionnaires;
using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.Questionnaires;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.Questionnaires;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Questionnaires
{
    public class QuestionnaireMonthlyReportService : IQuestionnaireMonthlyReportService
    {
        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // The report reads every department, so it runs as an unrestricted
        // actor — who receives what is decided by recipient department below,
        // not by this.
        private static readonly QuestionnaireActor ReportActor = new("system", CanViewAll: true, DepartmentId: 0);

        private readonly IQuestionnaireRepo _repo;
        private readonly IQuestionnaireService _questionnaires;
        private readonly IQuestionnaireExcelReportService _excel;
        private readonly IUserRepo _users;
        private readonly IEmailQueue _email;
        private readonly ILogger<QuestionnaireMonthlyReportService> _logger;
        private readonly string? _consoleBaseUrl;

        public QuestionnaireMonthlyReportService(
            IQuestionnaireRepo repo,
            IQuestionnaireService questionnaires,
            IQuestionnaireExcelReportService excel,
            IUserRepo users,
            IEmailQueue email,
            ILogger<QuestionnaireMonthlyReportService> logger,
            IConfiguration config)
        {
            _repo = repo;
            _questionnaires = questionnaires;
            _excel = excel;
            _users = users;
            _email = email;
            _logger = logger;
            var baseUrl = config["Email:ConsoleBaseUrl"]?.TrimEnd('/');
            _consoleBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl;
        }

        public async Task<MonthlyReportSendResult> SendScheduledAsync(int year, int month)
        {
            if (await _repo.HasReportRunAsync(year, month))
                return Skip(year, month, "This month's report was already sent.");

            // Don't burn the month when nothing can actually go out.
            if (!_email.IsEnabled)
                return Skip(year, month, "Email is not configured.");

            var (reports, sources) = await BuildReportsAsync(year, month, null);

            var run = await _repo.TryClaimReportRunAsync(year, month);
            if (run == null)
                return Skip(year, month, "Another process already sent this month's report.");

            var result = await EnqueueAsync(reports, sources, year, month);

            run.CompletedAt = AppClock.Now;
            run.EmailsQueued = result.EmailsQueued;
            await _repo.SaveChangesAsync();

            return result;
        }

        public async Task<MonthlyReportSendResult> SendNowAsync(int year, int month)
        {
            var (reports, sources) = await BuildReportsAsync(year, month, null);
            var result = await EnqueueAsync(reports, sources, year, month);
            if (!_email.IsEnabled)
            {
                result.Skipped = true;
                result.SkipReason = "Email is not configured — nothing was sent.";
            }
            return result;
        }

        public async Task<string> RenderPreviewAsync(int year, int month, int? departmentId)
        {
            var (reports, _) = await BuildReportsAsync(year, month, departmentId);
            if (reports.Count == 0)
                return "<p dir=\"rtl\" style=\"font-family:Tahoma,Arial,sans-serif\">لا توجد استبيانات لهذا الشهر.</p>";

            var html = new StringBuilder("<!doctype html><meta charset=\"utf-8\"><body style=\"margin:0;background:#e5e7eb\">");
            foreach (var report in reports)
            {
                var recipients = await _users.GetActiveEmailsInRoleAsync(AppRoles.QManager, report.DepartmentId);
                html.Append("<div style=\"font-family:Tahoma,Arial,sans-serif;font-size:13px;padding:14px 20px;background:#111827;color:#f9fafb\">");
                html.Append($"<b>Subject:</b> {WebUtility.HtmlEncode(QuestionnaireMonthlyEmailTemplate.Subject(report))}<br>");
                html.Append($"<b>To:</b> {(recipients.Count == 0 ? "<i>no active QManager with an email in this department — would be skipped</i>" : WebUtility.HtmlEncode(string.Join(", ", recipients)))}<br>");
                html.Append($"<b>Attachments:</b> {WebUtility.HtmlEncode(string.Join(", ", report.Questionnaires.Select(q => FileName(q.Slug, year, month))))}");
                html.Append("</div>");
                html.Append(QuestionnaireMonthlyEmailTemplate.Html(report, _consoleBaseUrl));
            }
            html.Append("</body>");
            return html.ToString();
        }

        public async Task<(string FileName, byte[] Content)?> BuildAttachmentAsync(int questionnaireId, int year, int month)
        {
            var (from, to) = MonthBounds(year, month);
            var data = await _questionnaires.GetExportDataAsync(questionnaireId, ReportActor, from, to);
            return data == null ? null : (FileName(data.Stats.Slug, year, month), _excel.Build(data));
        }

        // ---------- building ----------

        // What one questionnaire's attachment is built from, kept from building
        // the email so the attachment doesn't recompute it.
        private sealed record AttachmentSource(QuestionnaireStatsResponse Stats, List<QuestionnaireMonthRow> Months);

        private async Task<(List<DepartmentMonthlyReport> Reports, Dictionary<int, AttachmentSource> Sources)> BuildReportsAsync(
            int year, int month, int? departmentId)
        {
            var (from, to) = MonthBounds(year, month);
            var monthStart = from.ToDateTime(TimeOnly.MinValue);
            var monthEndExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var sources = new Dictionary<int, AttachmentSource>();

            var summaries = await _repo.GetSummariesAsync(departmentId, from, to);

            // A questionnaire belongs in the report if it is live, or if it
            // still collected answers during the month before being switched off.
            // One created after the month ended has nothing to say about it.
            var included = summaries
                .Where(s => (s.IsActive || s.SubmissionCount > 0) && s.CreatedDate < monthEndExclusive)
                .ToList();
            if (included.Count == 0) return (new List<DepartmentMonthlyReport>(), sources);

            // Loaded once for every included questionnaire, then assembled in
            // memory: the month rows, the questions, and every rating up to the
            // month's end split into this month and before. This used to be
            // two stats calls (six queries) per questionnaire for the email,
            // then seven more per attachment.
            var ids = included.Select(s => s.Id).ToList();
            var months = await _repo.GetMonthlyAsync(ids, QuestionnaireService.HistoryStart, monthEndExclusive);
            var monthsByQuestionnaire = months.ToLookup(m => m.QuestionnaireId);
            var questionsByQuestionnaire = (await _repo.GetQuestionsAsync(ids)).ToLookup(q => q.QuestionnaireId);
            var ratings = await _repo.GetRatingCountsSplitAsync(ids, monthStart, monthEndExclusive);
            var thisMonthRatings = ratings.Where(r => r.InPeriod).ToLookup(r => r.QuestionnaireId);
            var beforeRatings = ratings.Where(r => !r.InPeriod).ToLookup(r => r.QuestionnaireId);

            var reports = new List<DepartmentMonthlyReport>();
            foreach (var department in included.GroupBy(s => new { s.DepartmentId, s.DepartmentName }).OrderBy(g => g.Key.DepartmentName))
            {
                var report = new DepartmentMonthlyReport
                {
                    DepartmentId = department.Key.DepartmentId,
                    DepartmentName = department.Key.DepartmentName ?? "-",
                    Year = year,
                    Month = month,
                    Totals = QuestionnaireTrendBuilder.Build(
                        department.SelectMany(s => monthsByQuestionnaire[s.Id]), year, month, QuestionnaireService.TrendMonths)
                };

                foreach (var q in department.OrderByDescending(s => s.IsActive).ThenBy(s => s.Title))
                {
                    var questions = questionsByQuestionnaire[q.Id].ToList();
                    var thisMonth = QuestionnaireService.BuildStats(
                        q.Id, q.Title, q.Slug, q.DepartmentName, q.IsActive, from, to, q.SubmissionCount,
                        questions, thisMonthRatings[q.Id]);
                    // Everything before the month — only its per-question
                    // averages are read, so the submission count is irrelevant.
                    var before = QuestionnaireService.BuildStats(
                        q.Id, q.Title, q.Slug, q.DepartmentName, q.IsActive, null, from.AddDays(-1), 0,
                        questions, beforeRatings[q.Id]);
                    var beforeById = before.Questions.ToDictionary(x => x.Id);
                    sources[q.Id] = new AttachmentSource(thisMonth, monthsByQuestionnaire[q.Id].ToList());

                    report.Questionnaires.Add(new QuestionnaireMonthlyReport
                    {
                        Id = q.Id,
                        Title = q.Title,
                        Slug = q.Slug,
                        IsActive = q.IsActive,
                        Trend = QuestionnaireTrendBuilder.Build(monthsByQuestionnaire[q.Id], year, month, QuestionnaireService.TrendMonths),
                        Questions = thisMonth.Questions
                            .Where(x => !x.IsArchived)
                            .Select(x =>
                            {
                                beforeById.TryGetValue(x.Id, out var b);
                                return new QuestionCompareRow
                                {
                                    Text = x.Text,
                                    AnswersThisMonth = x.AnswerCount,
                                    AverageThisMonth = x.AverageRating,
                                    AverageBefore = b?.AverageRating,
                                    SatisfiedThisMonth = x.SatisfactionPercent,
                                    SatisfiedBefore = b?.SatisfactionPercent
                                };
                            })
                            .ToList()
                    });
                }

                reports.Add(report);
            }

            return (reports, sources);
        }

        private async Task<MonthlyReportSendResult> EnqueueAsync(
            List<DepartmentMonthlyReport> reports, Dictionary<int, AttachmentSource> sources, int year, int month)
        {
            var result = new MonthlyReportSendResult { Year = year, Month = month };

            foreach (var report in reports)
            {
                var entry = new MonthlyReportDepartmentResult
                {
                    DepartmentName = report.DepartmentName,
                    Questionnaires = report.Questionnaires.Count
                };
                result.Departments.Add(entry);

                try
                {
                    entry.Recipients = await _users.GetActiveEmailsInRoleAsync(AppRoles.QManager, report.DepartmentId);
                    if (entry.Recipients.Count == 0)
                    {
                        _logger.LogInformation("Monthly questionnaire report {Year}-{Month}: no active QManager with an email in {Department}, skipped.",
                            year, month, report.DepartmentName);
                        continue;
                    }

                    // Built once per department and shared by every recipient's copy.
                    var attachments = new List<EmailAttachment>();
                    foreach (var q in report.Questionnaires)
                    {
                        if (!sources.TryGetValue(q.Id, out var source)) continue;
                        var data = await QuestionnaireService.BuildExportDataAsync(_repo, source.Stats, source.Months);
                        attachments.Add(new EmailAttachment(FileName(q.Slug, year, month), _excel.Build(data), XlsxContentType));
                    }

                    var subject = QuestionnaireMonthlyEmailTemplate.Subject(report);
                    var html = QuestionnaireMonthlyEmailTemplate.Html(report, _consoleBaseUrl);
                    var text = QuestionnaireMonthlyEmailTemplate.Text(report);

                    foreach (var to in entry.Recipients)
                    {
                        if (_email.Enqueue(new EmailMessage(to, subject, html, text, attachments)))
                            entry.EmailsQueued++;
                        else
                            _logger.LogWarning("Monthly questionnaire report {Year}-{Month}: email to {To} was not queued.", year, month, to);
                    }
                }
                catch (Exception ex)
                {
                    // One department's failure must not stop the others' reports.
                    _logger.LogError(ex, "Monthly questionnaire report {Year}-{Month}: failed for {Department}.", year, month, report.DepartmentName);
                }

                result.EmailsQueued += entry.EmailsQueued;
            }

            return result;
        }

        // ---------- helpers ----------

        private static (DateOnly From, DateOnly To) MonthBounds(int year, int month)
        {
            var from = new DateOnly(year, month, 1);
            return (from, from.AddMonths(1).AddDays(-1));
        }

        private static string FileName(string slug, int year, int month) => $"questionnaire-{slug}-{year:D4}-{month:D2}.xlsx";

        private static MonthlyReportSendResult Skip(int year, int month, string reason) =>
            new() { Year = year, Month = month, Skipped = true, SkipReason = reason };
    }
}
