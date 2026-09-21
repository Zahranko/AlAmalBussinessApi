using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Questionnaires;
using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using AlAmalBusiness.Application.Services.Interface.Questionnaires;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.Questionnaires;
using AlAmalBusiness.Domain.Models.Questionnaires;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Questionnaires
{
    public class QuestionnaireService : IQuestionnaireService
    {
        private readonly IQuestionnaireRepo _repo;
        private readonly IDepartmentRepo _departmentRepo;

        private const int MaxQuestions = 50;
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private static readonly Regex SlugPattern = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

        // Paths the public site itself answers, which a questionnaire must not
        // shadow.
        private static readonly HashSet<string> ReservedSlugs = new(StringComparer.Ordinal)
        {
            "api", "index", "assets", "static", "admin", "favicon", "robots"
        };

        public QuestionnaireService(IQuestionnaireRepo repo, IDepartmentRepo departmentRepo)
        {
            _repo = repo;
            _departmentRepo = departmentRepo;
        }

        // ---------- list ----------

        public async Task<QuestionnaireListResponse> GetListAsync(QuestionnaireActor actor, DateOnly? from, DateOnly? to)
        {
            if (from.HasValue && to.HasValue && from > to)
                (from, to) = (to, from);

            var rows = await _repo.GetSummariesAsync(VisibleDepartments(actor), from, to);

            var totalAnswers = rows.Sum(r => r.TotalAnswers);

            return new QuestionnaireListResponse
            {
                From = from,
                To = to,
                QuestionnaireCount = rows.Count,
                ActiveCount = rows.Count(r => r.IsActive),
                TotalSubmissions = rows.Sum(r => r.SubmissionCount),
                AverageRating = totalAnswers == 0
                    ? null
                    : Round2(rows.Sum(r => (r.AverageRating ?? 0) * r.TotalAnswers) / totalAnswers),
                SatisfactionPercent = Percent(rows.Sum(r => r.PositiveAnswers), totalAnswers),
                Questionnaires = rows.Select(r => new QuestionnaireSummaryResponse
                {
                    Id = r.Id,
                    Title = r.Title,
                    Slug = r.Slug,
                    DepartmentId = r.DepartmentId,
                    DepartmentName = r.DepartmentName,
                    IsActive = r.IsActive,
                    CreatedDate = r.CreatedDate,
                    QuestionCount = r.QuestionCount,
                    SubmissionCount = r.SubmissionCount,
                    AverageRating = r.AverageRating.HasValue ? Round2(r.AverageRating.Value) : null,
                    SatisfactionPercent = Percent(r.PositiveAnswers, r.TotalAnswers),
                    LastSubmissionDate = r.LastSubmissionDate
                }).ToList()
            };
        }

        public async Task<QuestionnaireDetailResponse?> GetDetailAsync(int id, QuestionnaireActor actor)
        {
            var questionnaire = await _repo.GetDetailAsync(id);
            if (questionnaire == null || !CanSee(questionnaire.DepartmentId, actor))
                return null;

            var detail = ToDetail(questionnaire);

            // Which questions are past changing their type. One query, and
            // only for the edit form's benefit — the rule itself is enforced
            // in UpdateAsync, whatever the form chooses to grey out.
            var answered = await _repo.GetAnsweredQuestionIdsAsync(detail.Questions.Select(q => q.Id));
            foreach (var question in detail.Questions)
                question.HasAnswers = answered.Contains(question.Id);

            return detail;
        }

        // ---------- results ----------

        public async Task<QuestionnaireStatsResponse?> GetStatsAsync(int id, QuestionnaireActor actor, DateOnly? from, DateOnly? to)
        {
            var questionnaire = await _repo.GetDetailAsync(id);
            if (questionnaire == null || !CanSee(questionnaire.DepartmentId, actor))
                return null;

            if (from.HasValue && to.HasValue && from > to)
                (from, to) = (to, from);

            var counts = await _repo.GetRatingCountsAsync(id, from, to);
            // Sequential, like every other pair of reads here: one scoped
            // DbContext does not allow two queries at once.
            var textCounts = await _repo.GetTextAnswerCountsAsync(id, from, to);
            var submissionCount = await _repo.CountSubmissionsAsync(id, from, to);

            return BuildStats(
                questionnaire.Id, questionnaire.Title, questionnaire.Slug, questionnaire.Department?.Name,
                questionnaire.IsActive, from, to, submissionCount,
                questionnaire.Questions.Select(q => new QuestionnaireQuestionRow
                {
                    Id = q.Id,
                    QuestionnaireId = q.QuestionnaireId,
                    Text = q.Text,
                    Type = q.Type,
                    IsRequired = q.IsRequired,
                    DisplayOrder = q.DisplayOrder,
                    IsArchived = q.IsArchived
                }),
                counts,
                textCounts);
        }

        // The results numbers from parts already in hand. GetStatsAsync loads
        // one questionnaire's parts; the monthly report loads every
        // questionnaire's in a few queries and calls this per questionnaire,
        // so both read the same arithmetic.
        internal static QuestionnaireStatsResponse BuildStats(
            int id, string title, string slug, string? departmentName, bool isActive,
            DateOnly? from, DateOnly? to, int submissionCount,
            IEnumerable<QuestionnaireQuestionRow> allQuestions, IEnumerable<QuestionRatingCountRow> ratingCounts,
            IEnumerable<QuestionTextCountRow>? textCounts = null)
        {
            var counts = ratingCounts.ToList();
            var byQuestion = counts.ToLookup(c => c.QuestionId);
            // Absent (the monthly report doesn't read them) is the same as
            // zero everywhere below.
            var written = (textCounts ?? Enumerable.Empty<QuestionTextCountRow>())
                .ToDictionary(t => t.QuestionId, t => t.Count);

            var questions = allQuestions
                // Live questions in page order, then the archived ones that
                // still have something to show for the period — which for a
                // text question means somebody wrote something, not a rating.
                .OrderBy(q => q.DisplayOrder)
                .Where(q => !q.IsArchived || byQuestion[q.Id].Any() || written.ContainsKey(q.Id))
                .OrderBy(q => q.IsArchived)
                .ThenBy(q => q.DisplayOrder)
                .Select(q =>
                {
                    // A text answer has no score: no average, no satisfied
                    // share, an empty distribution. Its one number is how
                    // many people wrote something, and the answers
                    // themselves are read separately.
                    if (q.Type == QuestionType.Text)
                    {
                        return new QuestionStatsResponse
                        {
                            Id = q.Id,
                            Text = q.Text,
                            Type = nameof(QuestionType.Text),
                            IsArchived = q.IsArchived,
                            AnswerCount = written.TryGetValue(q.Id, out var n) ? n : 0
                        };
                    }

                    var distribution = ToDistribution(byQuestion[q.Id]);
                    return new QuestionStatsResponse
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = nameof(QuestionType.Rating),
                        IsArchived = q.IsArchived,
                        AnswerCount = Total(distribution),
                        AverageRating = Average(distribution),
                        SatisfactionPercent = Percent(distribution.Good + distribution.VeryGood, Total(distribution)),
                        Distribution = distribution
                    };
                })
                .ToList();

            var overall = ToDistribution(counts);

            return new QuestionnaireStatsResponse
            {
                Id = id,
                Title = title,
                Slug = slug,
                DepartmentName = departmentName,
                IsActive = isActive,
                From = from,
                To = to,
                SubmissionCount = submissionCount,
                AverageRating = Average(overall),
                SatisfactionPercent = Percent(overall.Good + overall.VeryGood, Total(overall)),
                Distribution = overall,
                Questions = questions
            };
        }

        public async Task<PagedResultDTO<QuestionnaireSubmissionResponse>?> GetSubmissionsAsync(
            int id, QuestionnaireActor actor, DateOnly? from, DateOnly? to, bool contactOnly, int page, int pageSize)
        {
            var questionnaire = await _repo.GetDetailAsync(id);
            if (questionnaire == null || !CanSee(questionnaire.DepartmentId, actor))
                return null;

            if (from.HasValue && to.HasValue && from > to)
                (from, to) = (to, from);

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

            var (items, total) = await _repo.PageSubmissionsAsync(id, from, to, contactOnly, page, pageSize);

            return new PagedResultDTO<QuestionnaireSubmissionResponse>
            {
                Items = items.Select(s => new QuestionnaireSubmissionResponse
                {
                    Id = s.Id,
                    CreatedDate = s.CreatedDate,
                    Name = s.Name,
                    PhoneNumber = s.PhoneNumber,
                    Notes = s.Notes,
                    AverageRating = s.AverageRating.HasValue ? Round2(s.AverageRating.Value) : null
                }).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        // ---------- export ----------

        // A hard ceiling on response rows in one workbook: the API runs in a
        // 1 GB shared pool and ClosedXML builds the whole file in memory.
        private const int ExportCap = 20000;

        // How many months a trend chart shows, and where "all history" starts.
        internal const int TrendMonths = 12;
        internal static readonly DateTime HistoryStart = new(2000, 1, 1);

        public async Task<QuestionnaireExportData?> GetExportDataAsync(int id, QuestionnaireActor actor, DateOnly? from, DateOnly? to)
        {
            // Goes through GetStatsAsync so the export can never widen what the
            // screen already scoped — null for another department's id.
            var stats = await GetStatsAsync(id, actor, from, to);
            if (stats == null) return null;

            var months = await _repo.GetMonthlyAsync(new[] { id }, HistoryStart, TrendEndExclusive(stats));
            return await BuildExportDataAsync(_repo, stats, months);
        }

        // The trend always ends at the period's last month (today for "all
        // time") and looks back over the whole history before it.
        private static DateOnly TrendEndDate(QuestionnaireStatsResponse stats) =>
            stats.To ?? DateOnly.FromDateTime(AppClock.Today);

        internal static DateTime TrendEndExclusive(QuestionnaireStatsResponse stats) =>
            TrendEndDate(stats).AddDays(1).ToDateTime(TimeOnly.MinValue);

        // The workbook's data for stats already built and that questionnaire's
        // month rows up to TrendEndExclusive(stats): only the per-response rows
        // are read here. Shared with the monthly report's attachments.
        internal static async Task<QuestionnaireExportData> BuildExportDataAsync(
            IQuestionnaireRepo repo, QuestionnaireStatsResponse stats, IEnumerable<QuestionnaireMonthRow> months)
        {
            var (submissions, answers) = await repo.GetExportRowsAsync(stats.Id, stats.From, stats.To, ExportCap);
            var bySubmission = answers.ToLookup(a => a.SubmissionId);
            var endDate = TrendEndDate(stats);

            return new QuestionnaireExportData
            {
                Stats = stats,
                Trend = QuestionnaireTrendBuilder.Build(months, endDate.Year, endDate.Month, TrendMonths),
                ExportCap = ExportCap,
                Truncated = stats.SubmissionCount > submissions.Count,
                Responses = submissions.Select(s => new QuestionnaireExportResponseRow
                {
                    CreatedDate = s.CreatedDate,
                    Name = s.Name,
                    PhoneNumber = s.PhoneNumber,
                    Notes = s.Notes,
                    AverageRating = s.AverageRating.HasValue ? Round2(s.AverageRating.Value) : null,
                    Ratings = bySubmission[s.Id]
                        .Where(a => a.Rating.HasValue)
                        .ToDictionary(a => a.QuestionId, a => a.Rating!.Value),
                    Texts = bySubmission[s.Id]
                        .Where(a => a.Text != null)
                        .ToDictionary(a => a.QuestionId, a => a.Text!)
                }).ToList()
            };
        }

        // One text question's written answers, newest first. Scoped through
        // the same 404 as everything else, and refused for a rating
        // question — there is nothing to read there.
        public async Task<QuestionTextAnswersResponse?> GetTextAnswersAsync(
            int id, int questionId, QuestionnaireActor actor, DateOnly? from, DateOnly? to, int page, int pageSize)
        {
            var questionnaire = await _repo.GetDetailAsync(id);
            if (questionnaire == null || !CanSee(questionnaire.DepartmentId, actor))
                return null;

            // Must be a text question OF THIS questionnaire: an id from
            // another one would otherwise read a department the caller
            // cannot see.
            var question = questionnaire.Questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null || question.Type != QuestionType.Text)
                return null;

            if (from.HasValue && to.HasValue && from > to)
                (from, to) = (to, from);

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

            var (items, total) = await _repo.PageTextAnswersAsync(questionId, from, to, page, pageSize);

            return new QuestionTextAnswersResponse
            {
                QuestionId = question.Id,
                QuestionText = question.Text,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                Items = items.Select(a => new QuestionTextAnswerResponse
                {
                    SubmissionId = a.SubmissionId,
                    Text = a.Text,
                    Name = a.Name,
                    CreatedDate = a.CreatedDate
                }).ToList()
            };
        }

        // ---------- manage ----------

        public async Task<QuestionnaireActionResponse> CreateAsync(SaveQuestionnaireDTO request, QuestionnaireActor actor)
        {
            var (fields, error) = await ValidateAsync(request, actor, existing: null);
            if (error != null) return Failed(error);

            var questionnaire = new Questionnaire
            {
                Title = fields!.Title,
                Slug = fields.Slug,
                Description = fields.Description,
                DepartmentId = fields.DepartmentId,
                IsActive = request.IsActive,
                CreatedById = actor.UserId,
                CreatedDate = AppClock.Now
            };

            var order = 1;
            foreach (var q in fields.Questions)
                questionnaire.Questions.Add(new QuestionnaireQuestion
                {
                    Text = q.Text,
                    Type = q.Type,
                    IsRequired = q.IsRequired,
                    DisplayOrder = order++
                });

            _repo.Add(questionnaire);
            await _repo.SaveChangesAsync();

            return await ReloadAsync(questionnaire.Id, actor);
        }

        public async Task<QuestionnaireActionResponse> UpdateAsync(int id, SaveQuestionnaireDTO request, QuestionnaireActor actor)
        {
            var questionnaire = await _repo.GetForEditAsync(id);
            if (questionnaire == null || !CanSee(questionnaire.DepartmentId, actor))
                return NotFound();

            var (fields, error) = await ValidateAsync(request, actor, questionnaire);
            if (error != null) return Failed(error);

            // Every id sent must be one of this questionnaire's live questions —
            // otherwise a crafted body could pull another questionnaire's
            // question (and its answers) in here.
            var live = questionnaire.Questions.Where(q => !q.IsArchived).ToDictionary(q => q.Id);
            var sentIds = request.Questions.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToList();

            if (sentIds.Count != sentIds.Distinct().Count() || sentIds.Any(qid => !live.ContainsKey(qid)))
                return Failed("قائمة الأسئلة تغيّرت منذ فتحها. أعد تحميل الصفحة وحاول مرة أخرى.");

            questionnaire.Title = fields!.Title;
            questionnaire.Slug = fields.Slug;
            questionnaire.Description = fields.Description;
            questionnaire.DepartmentId = fields.DepartmentId;
            questionnaire.IsActive = request.IsActive;

            // Which questions have been answered, over ALL of them and not
            // just the ones being removed: two rules need it now — an
            // answered question is archived rather than deleted, and an
            // answered question may not change what it asks for (below).
            // Asking only about the removed ones let a type change through.
            var removed = live.Values.Where(q => !sentIds.Contains(q.Id)).ToList();
            var answered = await _repo.GetAnsweredQuestionIdsAsync(live.Keys);
            foreach (var question in removed)
            {
                if (answered.Contains(question.Id))
                    question.IsArchived = true;
                else
                    _repo.RemoveQuestion(question);
            }

            var order = 1;
            for (var i = 0; i < request.Questions.Count; i++)
            {
                var field = fields.Questions[i];
                var sent = request.Questions[i];

                if (sent.Id.HasValue)
                {
                    var question = live[sent.Id.Value];

                    // Renaming a question keeps its answers; changing what
                    // it ASKS FOR cannot. Ratings would sit under a text
                    // question and prose under an averaged one, so a
                    // question that has been answered keeps its type —
                    // remove it and add a new one instead, which archives
                    // the old answers where they still make sense.
                    if (question.Type != field.Type && answered.Contains(question.Id))
                        return Failed("لا يمكن تغيير نوع سؤال تمت الإجابة عليه. احذفه وأضف سؤالاً جديداً بدلاً من ذلك.");

                    question.Text = field.Text;
                    question.Type = field.Type;
                    question.IsRequired = field.IsRequired;
                    question.DisplayOrder = order++;
                }
                else
                {
                    questionnaire.Questions.Add(new QuestionnaireQuestion
                    {
                        Text = field.Text,
                        Type = field.Type,
                        IsRequired = field.IsRequired,
                        DisplayOrder = order++
                    });
                }
            }

            await _repo.SaveChangesAsync();

            return await ReloadAsync(id, actor);
        }

        public async Task<QuestionnaireActionResponse> DeleteAsync(int id, QuestionnaireActor actor)
        {
            var questionnaire = await _repo.GetForEditAsync(id);
            if (questionnaire == null || !CanSee(questionnaire.DepartmentId, actor))
                return NotFound();

            if (await _repo.HasSubmissionsAsync(id))
                return Failed("لا يمكن حذف استبيان تمت الإجابة عليه. يمكنك إيقافه بدلاً من ذلك.");

            _repo.Remove(questionnaire);
            await _repo.SaveChangesAsync();

            return new QuestionnaireActionResponse { Success = true };
        }

        // ---------- public (anonymous) ----------

        public async Task<PublicQuestionnaireResponse?> GetPublicAsync(string slug)
        {
            var normalized = NormalizeSlug(slug);
            if (normalized == null) return null;

            var questionnaire = await _repo.GetActiveBySlugAsync(normalized);
            if (questionnaire == null || questionnaire.Questions.Count == 0)
                return null;

            return new PublicQuestionnaireResponse
            {
                Title = questionnaire.Title,
                Slug = questionnaire.Slug,
                Description = questionnaire.Description,
                DepartmentName = questionnaire.Department?.Name,
                Questions = questionnaire.Questions
                    .Select(q => new PublicQuestionResponse
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = q.Type.ToString(),
                        IsRequired = q.IsRequired
                    })
                    .ToList()
            };
        }

        public async Task<QuestionnaireSubmittedResponse> SubmitAsync(string slug, SubmitQuestionnaireDTO request, SubmissionContext context)
        {
            var normalized = NormalizeSlug(slug);
            var questionnaire = normalized == null ? null : await _repo.GetActiveBySlugAsync(normalized);
            if (questionnaire == null || questionnaire.Questions.Count == 0)
                return new QuestionnaireSubmittedResponse { NotFound = true, Error = "هذا الاستبيان غير متاح حالياً" };

            var answers = request.Answers ?? new List<SubmitAnswerDTO>();
            var questions = questionnaire.Questions.ToDictionary(q => q.Id);

            if (answers.Any(a => !questions.ContainsKey(a.QuestionId)))
                return Rejected("تم تحديث الاستبيان. أعد تحميل الصفحة وحاول مرة أخرى.");

            if (answers.Select(a => a.QuestionId).Distinct().Count() != answers.Count)
                return Rejected("إجابات مكررة لنفس السؤال");

            var sent = answers.ToDictionary(a => a.QuestionId);
            // What actually gets stored: an entry per question that was
            // answered. A skipped optional text question produces no row at
            // all, so "how many answered this" stays a plain count.
            var toSave = new List<QuestionnaireAnswer>();

            foreach (var question in questionnaire.Questions)
            {
                sent.TryGetValue(question.Id, out var answer);

                if (question.Type == QuestionType.Text)
                {
                    var text = Truncate(Clean(answer?.Text), 2000);
                    if (text == null)
                    {
                        // Required means required; optional means the box may
                        // be left empty, and an empty box is not an answer.
                        if (question.IsRequired)
                            return Rejected("يرجى الإجابة على جميع الأسئلة");
                        continue;
                    }

                    toSave.Add(new QuestionnaireAnswer { QuestionId = question.Id, Text = text });
                    continue;
                }

                // Every rating question still has to be answered, with a
                // rating that exists — a partial scale would skew the
                // per-question averages against each other.
                if (answer?.Rating == null)
                    return Rejected("يرجى الإجابة على جميع الأسئلة");

                if (!Enum.IsDefined(answer.Rating.Value))
                    return Rejected("تقييم غير صحيح");

                toSave.Add(new QuestionnaireAnswer { QuestionId = question.Id, Rating = answer.Rating.Value });
            }

            // Both optional; a phone, when given, has to be a plausible number.
            var phone = NormalizePhone(request.PhoneNumber);
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && phone == null)
                return Rejected("رقم الهاتف غير صحيح");

            var submission = new QuestionnaireSubmission
            {
                QuestionnaireId = questionnaire.Id,
                Name = Truncate(Clean(request.Name), 100),
                PhoneNumber = phone,
                Notes = Truncate(Clean(request.Notes), 2000),
                SubmittedFromIp = context.IpAddress,
                UserAgent = Truncate(context.UserAgent, 400),
                CreatedDate = AppClock.Now
            };

            foreach (var answer in toSave)
                submission.Answers.Add(answer);

            _repo.AddSubmission(submission);
            await _repo.SaveChangesAsync();

            return new QuestionnaireSubmittedResponse { Success = true };
        }

        // ---------- validation ----------

        private sealed record ValidFields(
            string Title, string Slug, string? Description, int DepartmentId, List<ValidQuestion> Questions);

        // A question as the service will save it: cleaned text, its type,
        // and whether it must be answered (always true for a rating).
        private sealed record ValidQuestion(string Text, QuestionType Type, bool IsRequired);

        private async Task<(ValidFields? Fields, string? Error)> ValidateAsync(
            SaveQuestionnaireDTO request, QuestionnaireActor actor, Questionnaire? existing)
        {
            var title = Clean(request.Title);
            if (title == null)
                return (null, "عنوان الاستبيان مطلوب");

            var slug = NormalizeSlug(request.Slug);
            if (slug == null)
                return (null, "الرابط يجب أن يتكوّن من أحرف إنجليزية صغيرة وأرقام وشرطات فقط (مثال: radiology)");

            if (ReservedSlugs.Contains(slug))
                return (null, "هذا الرابط محجوز، اختر رابطاً آخر");

            if (await _repo.IsSlugExist(slug, existing?.Id ?? 0))
                return (null, "هذا الرابط مستخدم لاستبيان آخر");

            var departmentId = await ResolveDepartmentAsync(request, actor, existing);
            if (departmentId.Error != null)
                return (null, departmentId.Error);

            var questions = request.Questions ?? new List<SaveQuestionDTO>();
            if (questions.Count == 0)
                return (null, "أضف سؤالاً واحداً على الأقل");

            if (questions.Count > MaxQuestions)
                return (null, $"الحد الأقصى {MaxQuestions} سؤالاً");

            var texts = questions.Select(q => Clean(q.Text)).ToList();
            if (texts.Any(t => t == null))
                return (null, "لا يمكن ترك نص سؤال فارغاً");

            if (questions.Any(q => !Enum.IsDefined(q.Type)))
                return (null, "نوع السؤال غير صحيح");

            // At least one rating question: an all-text questionnaire has no
            // average, no satisfaction and nothing on the dashboard — which
            // is a form, not a questionnaire, and this feature's whole
            // reporting side would read as empty.
            if (questions.All(q => q.Type != QuestionType.Rating))
                return (null, "أضف سؤال تقييم واحداً على الأقل إلى جانب الأسئلة النصية");

            var valid = questions
                .Select((q, i) => new ValidQuestion(
                    texts[i]!,
                    q.Type,
                    // A rating question is always required; only a text one
                    // takes the flag.
                    q.Type == QuestionType.Rating || q.IsRequired))
                .ToList();

            return (new ValidFields(title, slug, Clean(request.Description), departmentId.Id, valid), null);
        }

        // A QManager's questionnaires live in a department they can read: the
        // one they work in by default, or any other they have been granted —
        // seeing three departments' results but only ever being able to create
        // in one would make the grant half a feature. Anything outside the
        // grant is refused, so this can't be widened from the request body.
        // An Admin picks any department, and may only pick an active one,
        // except to keep the one it already had.
        private async Task<(int Id, string? Error)> ResolveDepartmentAsync(
            SaveQuestionnaireDTO request, QuestionnaireActor actor, Questionnaire? existing)
        {
            if (!actor.CanViewAll)
            {
                if (actor.DepartmentIds.Count == 0)
                    return (0, "حسابك غير مرتبط بقسم. تواصل مع مسؤول النظام.");

                // Nothing picked keeps the old behavior: their own department.
                if (!request.DepartmentId.HasValue || request.DepartmentId <= 0)
                    return actor.DepartmentId > 0
                        ? (actor.DepartmentId, null)
                        : (actor.DepartmentIds[0], null);

                if (!actor.DepartmentIds.Contains(request.DepartmentId.Value))
                    return (0, "القسم المختار غير متاح لحسابك");

                return (request.DepartmentId.Value, null);
            }

            if (!request.DepartmentId.HasValue || request.DepartmentId <= 0)
                return (0, "القسم مطلوب");

            var department = await _departmentRepo.GetDepartmentByIdAsync(request.DepartmentId.Value);
            if (department == null)
                return (0, "القسم المختار غير موجود");

            if (!department.IsActive && existing?.DepartmentId != department.Id)
                return (0, "القسم المختار غير مفعّل");

            return (department.Id, null);
        }

        // Lowercase latin letters, digits and single inner hyphens, 2-60 long.
        // Null when the value can't be a slug.
        private static string? NormalizeSlug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var slug = value.Trim().Trim('/').ToLowerInvariant();
            if (slug.Length < 2 || slug.Length > 60 || !SlugPattern.IsMatch(slug))
                return null;

            return slug;
        }

        // ---------- visibility ----------

        // Same rule as the feedback inbox: only an Admin is unrestricted; a
        // QManager sees the departments on their token. Null = no
        // restriction, empty = nothing.
        private static List<int>? VisibleDepartments(QuestionnaireActor actor) =>
            actor.CanViewAll ? null : actor.DepartmentIds.ToList();

        private static bool CanSee(int departmentId, QuestionnaireActor actor) =>
            actor.CanViewAll || actor.DepartmentIds.Contains(departmentId);

        private async Task<QuestionnaireActionResponse> ReloadAsync(int id, QuestionnaireActor actor) =>
            new() { Success = true, Questionnaire = await GetDetailAsync(id, actor) };

        // ---------- helpers ----------

        private static RatingDistributionResponse ToDistribution(IEnumerable<QuestionRatingCountRow> rows)
        {
            var d = new RatingDistributionResponse();
            foreach (var row in rows)
            {
                switch (row.Rating)
                {
                    case QuestionRating.VeryGood: d.VeryGood += row.Count; break;
                    case QuestionRating.Good: d.Good += row.Count; break;
                    case QuestionRating.Mid: d.Mid += row.Count; break;
                    case QuestionRating.Bad: d.Bad += row.Count; break;
                    case QuestionRating.VeryBad: d.VeryBad += row.Count; break;
                }
            }
            return d;
        }

        private static int Total(RatingDistributionResponse d) => d.VeryGood + d.Good + d.Mid + d.Bad + d.VeryBad;

        // Null, not 0, when nothing was answered — "no answers yet" is not
        // "rated as badly as possible".
        private static double? Average(RatingDistributionResponse d)
        {
            var total = Total(d);
            if (total == 0) return null;

            var sum = d.VeryGood * 5 + d.Good * 4 + d.Mid * 3 + d.Bad * 2 + d.VeryBad * 1;
            return Round2((double)sum / total);
        }

        private static double? Percent(int part, int total) =>
            total == 0 ? null : Math.Round(part * 100.0 / total, 1);

        private static double Round2(double value) => Math.Round(value, 2);

        // Separators dropped, a leading + kept; 7-15 digits or it isn't a phone.
        // Null for blank or implausible input.
        private static string? NormalizePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var trimmed = value.Trim();
            var digits = Regex.Replace(trimmed, @"\D", string.Empty);
            if (digits.Length < 7 || digits.Length > 15) return null;

            return trimmed.StartsWith('+') ? $"+{digits}" : digits;
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

        private static QuestionnaireActionResponse NotFound() => new() { Success = false, NotFound = true };

        private static QuestionnaireActionResponse Failed(string error) => new() { Success = false, Error = error };

        private static QuestionnaireSubmittedResponse Rejected(string error) => new() { Success = false, Error = error };

        private static QuestionnaireDetailResponse ToDetail(Questionnaire q) => new()
        {
            Id = q.Id,
            Title = q.Title,
            Slug = q.Slug,
            Description = q.Description,
            DepartmentId = q.DepartmentId,
            DepartmentName = q.Department?.Name,
            IsActive = q.IsActive,
            CreatedDate = q.CreatedDate,
            Questions = q.Questions
                .Where(x => !x.IsArchived)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new QuestionResponse
                {
                    Id = x.Id,
                    Text = x.Text,
                    Type = x.Type.ToString(),
                    IsRequired = x.IsRequired,
                    DisplayOrder = x.DisplayOrder
                })
                .ToList()
        };
    }
}
