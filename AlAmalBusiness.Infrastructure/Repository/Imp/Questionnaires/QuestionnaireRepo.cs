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
        public async Task<List<QuestionnaireSummaryRow>> GetSummariesAsync(int? restrictToDepartmentId, DateOnly? from, DateOnly? to)
        {
            var questionnaires = _context.Questionnaires.AsNoTracking();
            if (restrictToDepartmentId.HasValue)
                questionnaires = questionnaires.Where(q => q.DepartmentId == restrictToDepartmentId.Value);

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
            if (restrictToDepartmentId.HasValue)
                submissions = submissions.Where(s => s.Questionnaire!.DepartmentId == restrictToDepartmentId.Value);

            var counts = await submissions
                .GroupBy(s => s.QuestionnaireId)
                .Select(g => new { QuestionnaireId = g.Key, Count = g.Count(), Last = g.Max(s => s.CreatedDate) })
                .ToDictionaryAsync(x => x.QuestionnaireId);

            var ratings = await (
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in submissions on a.SubmissionId equals s.Id
                group a by s.QuestionnaireId into g
                select new
                {
                    QuestionnaireId = g.Key,
                    Average = g.Average(a => (double)(int)a.Rating),
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
                group a by new { a.QuestionId, a.Rating } into g
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
                    AverageRating = s.Answers.Average(a => (double?)(int)a.Rating)
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
                    AverageRating = s.Answers.Average(a => (double?)(int)a.Rating)
                })
                .ToListAsync();

            var answers = await (
                from a in _context.QuestionnaireAnswers.AsNoTracking()
                join s in newest on a.SubmissionId equals s.Id
                select new QuestionnaireAnswerExportRow { SubmissionId = a.SubmissionId, QuestionId = a.QuestionId, Rating = a.Rating })
                .ToListAsync();

            return (submissions, answers);
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
