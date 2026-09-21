using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Questionnaires;
using AlAmalBusiness.Domain.Models.Questionnaires;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Questionnaires
{
    public class QuestionnaireRepo : IQuestionnaireRepo
    {
        private readonly AppDbContext _context;

        public QuestionnaireRepo(AppDbContext context)
        {
            _context = context;
        }

        // Three grouped queries, merged in memory: the questionnaires in scope,
        // their submission counts, and their answer averages. None of them
        // returns an answer row.
        public async Task<List<QuestionnaireSummaryRow>> GetSummariesAsync(List<int>? restrictToDepartmentIds, DateOnly? from, DateOnly? to)
        {
            var questionnaires = _context.Questionnaires.AsNoTracking();
            // Null is unrestricted (Admin); an empty set is "may read
            // nothing" and must filter to no rows, never to everything.
            if (restrictToDepartmentIds is { } readable)
            {
                questionnaires = readable.Count == 0
                    ? questionnaires.Where(q => false)
                    : questionnaires.Where(q => readable.Contains(q.DepartmentId));
            }

            var rows = await questionnaires
                .OrderBy(q => q.Title)
                .Select(q => new QuestionnaireSummaryRow
                {
                    Id = q.Id,
                    Title = q.Title,
                    Slug = q.Slug,
                    DepartmentId = q.DepartmentId,
                    DepartmentName = q.Department!.Name,
                    IsActive = q.IsActive,
                    CreatedDate = q.CreatedDate,
                    QuestionCount = q.Questions.Count(x => !x.IsArchived)
                })
                .ToListAsync();

            if (rows.Count == 0)
                return rows;

            var submissions = InPeriod(_context.QuestionnaireSubmissions.AsNoTracking(), from, to);
            if (restrictToDepartmentIds is { } readableSubs)
            {
                submissions = readableSubs.Count == 0
                    ? submissions.Where(s => false)
                    : submissions.Where(s => readableSubs.Contains(s.Questionnaire!.DepartmentId));
            }

            var counts = await submissions
                .GroupBy(s => s.QuestionnaireId)
                .Select(g => new { QuestionnaireId = g.Key, Count = g.Count(), Last = g.Max(s => s.CreatedDate) })
                .ToDictionaryAsync(x => x.QuestionnaireId);

            var ratings = await (
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in submissions on a.SubmissionId equals s.Id
                // Rating answers only. A text answer has no score, and
                // counting it would drag every average and every satisfied
                // share towards a number nobody gave.
                where a.Rating != null
                group a by s.QuestionnaireId into g
                select new
                {
                    QuestionnaireId = g.Key,
                    Average = g.Average(a => (double)(int)a.Rating!.Value),
                    Positive = g.Count(a => a.Rating == QuestionRating.Good || a.Rating == QuestionRating.VeryGood),
                    Total = g.Count()
                })
                .ToDictionaryAsync(x => x.QuestionnaireId);

            foreach (var row in rows)
            {
                if (counts.TryGetValue(row.Id, out var c))
                {
                    row.SubmissionCount = c.Count;
                    row.LastSubmissionDate = c.Last;
                }

                if (ratings.TryGetValue(row.Id, out var r))
                {
                    row.AverageRating = r.Average;
                    row.PositiveAnswers = r.Positive;
                    row.TotalAnswers = r.Total;
                }
            }

            return rows;
        }

        public Task<Questionnaire?> GetForEditAsync(int id) =>
            _context.Questionnaires
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

        // Single row, so Include'ing the department name is fine here.
        // Archived questions come along too: the results screen still shows
        // what they collected. The service filters them for the edit form.
        public Task<Questionnaire?> GetDetailAsync(int id) =>
            _context.Questionnaires
                .Include(q => q.Department)
                .Include(q => q.Questions.OrderBy(x => x.DisplayOrder))
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id);

        public Task<Questionnaire?> GetActiveBySlugAsync(string slug) =>
            _context.Questionnaires
                .Include(q => q.Department)
                .Include(q => q.Questions.Where(x => !x.IsArchived).OrderBy(x => x.DisplayOrder))
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Slug == slug && q.IsActive);

        public Task<bool> IsSlugExist(string slug, int excludeId) =>
            _context.Questionnaires.AnyAsync(q => q.Slug == slug && q.Id != excludeId);

        public async Task<HashSet<int>> GetAnsweredQuestionIdsAsync(IEnumerable<int> questionIds)
        {
            var ids = questionIds.ToList();
            if (ids.Count == 0) return new HashSet<int>();

            var answered = await _context.QuestionnaireAnswers.AsNoTracking()
                .Where(a => ids.Contains(a.QuestionId))
                .Select(a => a.QuestionId)
                .Distinct()
                .ToListAsync();

            return answered.ToHashSet();
        }

        public Task<bool> HasSubmissionsAsync(int questionnaireId) =>
            _context.QuestionnaireSubmissions.AnyAsync(s => s.QuestionnaireId == questionnaireId);

        public Task<List<QuestionRatingCountRow>> GetRatingCountsAsync(int questionnaireId, DateOnly? from, DateOnly? to)
        {
            var submissions = InPeriod(
                _context.QuestionnaireSubmissions.AsNoTracking().Where(s => s.QuestionnaireId == questionnaireId),
                from, to);

            return (
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in submissions on a.SubmissionId equals s.Id
                where a.Rating != null
                group a by new { a.QuestionId, Rating = a.Rating!.Value } into g
                select new QuestionRatingCountRow
                {
                    QuestionId = g.Key.QuestionId,
                    Rating = g.Key.Rating,
                    Count = g.Count()
                })
                .ToListAsync();
        }

        public Task<int> CountSubmissionsAsync(int questionnaireId, DateOnly? from, DateOnly? to) =>
            InPeriod(
                _context.QuestionnaireSubmissions.AsNoTracking().Where(s => s.QuestionnaireId == questionnaireId),
                from, to)
            .CountAsync();

        public async Task<(List<QuestionnaireSubmissionRow> Items, int TotalCount)> PageSubmissionsAsync(
            int questionnaireId, DateOnly? from, DateOnly? to, bool contactOnly, int page, int pageSize)
        {
            var q = InPeriod(
                _context.QuestionnaireSubmissions.AsNoTracking().Where(s => s.QuestionnaireId == questionnaireId),
                from, to);

            // "Left something": a name, a phone, or a note.
            if (contactOnly)
                q = q.Where(s => s.Name != null || s.PhoneNumber != null || s.Notes != null);

            var total = await q.CountAsync();

            // The per-response average is a correlated subquery over that
            // response's own answers — one statement, no answer rows returned.
            var items = await q
                .OrderByDescending(s => s.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new QuestionnaireSubmissionRow
                {
                    Id = s.Id,
                    CreatedDate = s.CreatedDate,
                    Name = s.Name,
                    PhoneNumber = s.PhoneNumber,
                    Notes = s.Notes,
                    AverageRating = s.Answers.Where(a => a.Rating != null).Average(a => (double?)(int)a.Rating!.Value)
                })
                .ToListAsync();

            return (items, total);
        }

        public async Task<(List<QuestionnaireSubmissionRow> Submissions, List<QuestionnaireAnswerExportRow> Answers)> GetExportRowsAsync(
            int questionnaireId, DateOnly? from, DateOnly? to, int max)
        {
            var newest = InPeriod(
                    _context.QuestionnaireSubmissions.AsNoTracking().Where(s => s.QuestionnaireId == questionnaireId),
                    from, to)
                .OrderByDescending(s => s.CreatedDate)
                .Take(max);

            var submissions = await newest
                .Select(s => new QuestionnaireSubmissionRow
                {
                    Id = s.Id,
                    CreatedDate = s.CreatedDate,
                    Name = s.Name,
                    PhoneNumber = s.PhoneNumber,
                    Notes = s.Notes,
                    AverageRating = s.Answers.Where(a => a.Rating != null).Average(a => (double?)(int)a.Rating!.Value)
                })
                .ToListAsync();

            var answers = await (
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in newest on a.SubmissionId equals s.Id
                select new QuestionnaireAnswerExportRow
                {
                    SubmissionId = a.SubmissionId,
                    QuestionId = a.QuestionId,
                    Rating = a.Rating,
                    Text = a.Text
                })
                .ToListAsync();

            return (submissions, answers);
        }

        public async Task<List<QuestionnaireMonthRow>> GetMonthlyAsync(IReadOnlyCollection<int> questionnaireIds, DateTime from, DateTime toExclusive)
        {
            if (questionnaireIds.Count == 0) return new List<QuestionnaireMonthRow>();
            var ids = questionnaireIds.ToList();

            var submissions = _context.QuestionnaireSubmissions.AsNoTracking()
                .Where(s => ids.Contains(s.QuestionnaireId) && s.CreatedDate >= from && s.CreatedDate < toExclusive);

            var counts = await submissions
                .GroupBy(s => new { s.QuestionnaireId, s.CreatedDate.Year, s.CreatedDate.Month })
                .Select(g => new { g.Key.QuestionnaireId, g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var ratings = await (
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in submissions on a.SubmissionId equals s.Id
                where a.Rating != null
                group a by new { s.QuestionnaireId, s.CreatedDate.Year, s.CreatedDate.Month } into g
                select new
                {
                    g.Key.QuestionnaireId,
                    g.Key.Year,
                    g.Key.Month,
                    Answers = g.Count(),
                    Sum = g.Sum(a => (int)a.Rating!.Value),
                    Positive = g.Count(a => a.Rating == QuestionRating.Good || a.Rating == QuestionRating.VeryGood)
                })
                .ToListAsync();

            var byKey = ratings.ToDictionary(r => (r.QuestionnaireId, r.Year, r.Month));

            return counts
                .Select(c =>
                {
                    byKey.TryGetValue((c.QuestionnaireId, c.Year, c.Month), out var r);
                    return new QuestionnaireMonthRow
                    {
                        QuestionnaireId = c.QuestionnaireId,
                        Year = c.Year,
                        Month = c.Month,
                        Submissions = c.Count,
                        AnswerCount = r?.Answers ?? 0,
                        RatingSum = r?.Sum ?? 0,
                        Positive = r?.Positive ?? 0
                    };
                })
                .ToList();
        }

        public async Task<List<QuestionnaireQuestionRow>> GetQuestionsAsync(IReadOnlyCollection<int> questionnaireIds)
        {
            if (questionnaireIds.Count == 0) return new List<QuestionnaireQuestionRow>();
            var ids = questionnaireIds.ToList();

            return await _context.QuestionnaireQuestions.AsNoTracking()
                .Where(q => ids.Contains(q.QuestionnaireId))
                .OrderBy(q => q.DisplayOrder)
                .Select(q => new QuestionnaireQuestionRow
                {
                    Id = q.Id,
                    QuestionnaireId = q.QuestionnaireId,
                    Text = q.Text,
                    Type = q.Type,
                    IsRequired = q.IsRequired,
                    DisplayOrder = q.DisplayOrder,
                    IsArchived = q.IsArchived
                })
                .ToListAsync();
        }

        public async Task<List<QuestionnairePeriodRatingRow>> GetRatingCountsSplitAsync(
            IReadOnlyCollection<int> questionnaireIds, DateTime periodStart, DateTime periodEndExclusive)
        {
            if (questionnaireIds.Count == 0) return new List<QuestionnairePeriodRatingRow>();
            var ids = questionnaireIds.ToList();

            return await (
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in _context.QuestionnaireSubmissions.AsNoTracking() on a.SubmissionId equals s.Id
                where ids.Contains(s.QuestionnaireId) && s.CreatedDate < periodEndExclusive && a.Rating != null
                group a by new { s.QuestionnaireId, a.QuestionId, Rating = a.Rating!.Value, InPeriod = s.CreatedDate >= periodStart } into g
                select new QuestionnairePeriodRatingRow
                {
                    QuestionnaireId = g.Key.QuestionnaireId,
                    QuestionId = g.Key.QuestionId,
                    Rating = g.Key.Rating,
                    InPeriod = g.Key.InPeriod,
                    Count = g.Count()
                })
                .ToListAsync();
        }

        // How many people wrote something, per text question. An empty box
        // is stored as no answer at all, so this is a plain count.
        public Task<List<QuestionTextCountRow>> GetTextAnswerCountsAsync(int questionnaireId, DateOnly? from, DateOnly? to)
        {
            var submissions = InPeriod(
                _context.QuestionnaireSubmissions.AsNoTracking().Where(s => s.QuestionnaireId == questionnaireId),
                from, to);

            return (
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in submissions on a.SubmissionId equals s.Id
                where a.Text != null
                group a by a.QuestionId into g
                select new QuestionTextCountRow { QuestionId = g.Key, Count = g.Count() })
                .ToListAsync();
        }

        // One text question's answers, newest first, a page at a time — the
        // per-question page reads these on demand rather than the stats
        // endpoint carrying every paragraph anyone ever typed.
        public async Task<(List<QuestionTextAnswerRow> Items, int TotalCount)> PageTextAnswersAsync(
            int questionId, DateOnly? from, DateOnly? to, int page, int pageSize)
        {
            var submissions = InPeriod(_context.QuestionnaireSubmissions.AsNoTracking(), from, to);

            var answers =
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in submissions on a.SubmissionId equals s.Id
                where a.QuestionId == questionId && a.Text != null
                select new QuestionTextAnswerRow
                {
                    SubmissionId = a.SubmissionId,
                    Text = a.Text!,
                    Name = s.Name,
                    CreatedDate = s.CreatedDate
                };

            var total = await answers.CountAsync();
            var items = await answers
                .OrderByDescending(a => a.CreatedDate)
                .ThenByDescending(a => a.SubmissionId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public Task<bool> HasReportRunAsync(int year, int month) =>
            _context.QuestionnaireReportRuns.AnyAsync(r => r.Year == year && r.Month == month);

        public async Task<QuestionnaireReportRun?> TryClaimReportRunAsync(int year, int month)
        {
            if (await _context.QuestionnaireReportRuns.AnyAsync(r => r.Year == year && r.Month == month))
                return null;

            var run = new QuestionnaireReportRun { Year = year, Month = month, StartedAt = AppClock.Now };
            _context.QuestionnaireReportRuns.Add(run);
            try
            {
                await _context.SaveChangesAsync();
                return run;
            }
            catch (DbUpdateException)
            {
                // Another process claimed it between the check and the insert.
                _context.Entry(run).State = EntityState.Detached;
                return null;
            }
        }

        public void Add(Questionnaire questionnaire) => _context.Questionnaires.Add(questionnaire);

        public void Remove(Questionnaire questionnaire) => _context.Questionnaires.Remove(questionnaire);

        public void RemoveQuestion(QuestionnaireQuestion question) => _context.QuestionnaireQuestions.Remove(question);

        public void AddSubmission(QuestionnaireSubmission submission) => _context.QuestionnaireSubmissions.Add(submission);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        // CreatedDate is a local DateTime, so the bounds are day boundaries:
        // from 00:00 on From up to (not including) 00:00 the day after To.
        private static IQueryable<QuestionnaireSubmission> InPeriod(IQueryable<QuestionnaireSubmission> q, DateOnly? from, DateOnly? to)
        {
            if (from.HasValue)
            {
                var start = from.Value.ToDateTime(TimeOnly.MinValue);
                q = q.Where(s => s.CreatedDate >= start);
            }

            if (to.HasValue)
            {
                var endExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                q = q.Where(s => s.CreatedDate < endExclusive);
            }

            return q;
        }
    }
}
