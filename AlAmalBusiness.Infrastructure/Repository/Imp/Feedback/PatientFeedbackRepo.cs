using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Feedback;
using AlAmalBusiness.Domain.Models.Feedback;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Feedback
{
    public class PatientFeedbackRepo : IPatientFeedbackRepo
    {
        private readonly AppDbContext _context;

        public PatientFeedbackRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<PatientFeedback?> GetByIdAsync(int id) =>
            _context.Feedbacks.FirstOrDefaultAsync(f => f.Id == id);

        public Task<PatientFeedback?> GetDetailAsync(int id) =>
            DetailQuery().FirstOrDefaultAsync(f => f.Id == id);

        public Task<PatientFeedback?> GetByReferenceAsync(string referenceNumber) =>
            DetailQuery().FirstOrDefaultAsync(f => f.ReferenceNumber == referenceNumber);

        public Task<bool> IsReferenceExist(string referenceNumber) =>
            _context.Feedbacks.AnyAsync(f => f.ReferenceNumber == referenceNumber);

        // The detail view is a single row, so Include'ing the two names it
        // needs is fine here — it is the list query (below) that must never
        // pull whole AspNetUsers rows.
        private IQueryable<PatientFeedback> DetailQuery() =>
            _context.Feedbacks
                .Include(f => f.Department)
                .Include(f => f.AssignedTo)
                .AsNoTracking();

        // Projection the inbox reads through — see FeedbackListRow. Names come
        // through the navigations inside the same SELECT (LEFT JOINs), no
        // Include, no tracking, and the Details column is left behind.
        private static readonly System.Linq.Expressions.Expression<Func<PatientFeedback, FeedbackListRow>> ToRow = f => new FeedbackListRow
        {
            Id = f.Id,
            ReferenceNumber = f.ReferenceNumber,
            Type = f.Type,
            Status = f.Status,
            FirstName = f.FirstName,
            LastName = f.LastName,
            PhoneCountryCode = f.PhoneCountryCode,
            PhoneNumber = f.PhoneNumber,
            VisitDate = f.VisitDate,
            DepartmentId = f.DepartmentId,
            DepartmentName = f.Department!.Name,
            AssignedToId = f.AssignedToId,
            AssignedToName = f.AssignedTo!.UserName,
            ResolvedAt = f.ResolvedAt,
            CreatedDate = f.CreatedDate
        };

        public async Task<(List<FeedbackListRow> Items, int TotalCount)> PageFeedbacksAsync(FeedbackListQuery query)
        {
            var q = _context.Feedbacks.AsNoTracking();

            // Applied before anything the caller asked for: this is the
            // department scoping the service resolved from the caller's roles,
            // not a filter they can widen.
            if (query.RestrictToDepartmentId.HasValue)
                q = q.Where(f => f.DepartmentId == query.RestrictToDepartmentId.Value);

            if (query.Type.HasValue) q = q.Where(f => f.Type == query.Type.Value);
            if (query.Status.HasValue) q = q.Where(f => f.Status == query.Status.Value);
            if (query.DepartmentId.HasValue) q = q.Where(f => f.DepartmentId == query.DepartmentId.Value);
            if (query.FromDate.HasValue) q = q.Where(f => f.VisitDate >= query.FromDate.Value);
            if (query.ToDate.HasValue) q = q.Where(f => f.VisitDate <= query.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();

                // Phone is matched leading-zero-tolerantly, the way lead search
                // is: numbers are stored with the national zero stripped, but
                // a searcher types the number the way a patient says it.
                var phone = term.TrimStart('0');

                q = q.Where(f =>
                    f.ReferenceNumber.Contains(term) ||
                    f.FirstName!.Contains(term) ||
                    f.LastName!.Contains(term) ||
                    f.PhoneNumber!.Contains(phone));
            }

            var totalCount = await q.CountAsync();

            var items = await q
                .OrderByDescending(f => f.CreatedDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToRow)
                .ToListAsync();

            return (items, totalCount);
        }

        // ---------- dashboard ----------

        // Six aggregate queries, none of which returns a message row. They all
        // start from the same scoped, date-bounded set: the scoping the
        // service resolved from the caller's roles, then the period, then an
        // admin's optional single-department narrowing.
        public async Task<FeedbackStatsRows> GetStatsAsync(FeedbackStatsQuery query)
        {
            var scoped = _context.Feedbacks.AsNoTracking();

            if (query.RestrictToDepartmentId.HasValue)
                scoped = scoped.Where(f => f.DepartmentId == query.RestrictToDepartmentId.Value);

            if (query.DepartmentId.HasValue)
                scoped = scoped.Where(f => f.DepartmentId == query.DepartmentId.Value);

            // CreatedDate is a local DateTime, so the bounds are day
            // boundaries: everything from 00:00 on From up to (not including)
            // 00:00 the day after To.
            if (query.From.HasValue)
            {
                var from = query.From.Value.ToDateTime(TimeOnly.MinValue);
                scoped = scoped.Where(f => f.CreatedDate >= from);
            }

            if (query.To.HasValue)
            {
                var toExclusive = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                scoped = scoped.Where(f => f.CreatedDate < toExclusive);
            }

            var rows = new FeedbackStatsRows();

            rows.Statuses = await scoped
                .GroupBy(f => f.Status)
                .Select(g => new FeedbackStatusCountRow { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            rows.Types = await scoped
                .GroupBy(f => f.Type)
                .Select(g => new FeedbackTypeCountRow { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            rows.AvgResolutionMinutes = await scoped
                .Where(f => f.ResolvedAt != null)
                .Select(f => (double?)EF.Functions.DateDiffMinute(f.CreatedDate, f.ResolvedAt!.Value))
                .AverageAsync();

            // MinAsync would throw on an empty set; Min() over a nullable
            // projection answers null instead, which is what "nothing is
            // waiting" has to read as.
            var oldestOpen = await scoped
                .Where(f => f.Status == FeedbackStatus.New || f.Status == FeedbackStatus.InReview)
                .Select(f => (DateTime?)f.CreatedDate)
                .MinAsync();

            if (oldestOpen.HasValue)
                rows.OldestOpenMinutes = (DateTime.Now - oldestOpen.Value).TotalMinutes;

            // Who closed things out, from the timeline rather than from
            // AssignedToId: the assignee is who picked it up, which is not
            // always who resolved it.
            rows.Resolvers = await (
                from h in _context.FeedbackHistories.AsNoTracking()
                join f in scoped on h.FeedbackId equals f.Id
                where h.Type == FeedbackActions.StatusChanged && h.ToStatus == FeedbackStatus.Resolved
                group new { h, f } by new { h.ActorId, ActorName = h.Actor!.UserName } into g
                select new FeedbackResolverRow
                {
                    ActorId = g.Key.ActorId,
                    ActorName = g.Key.ActorName,
                    ResolvedCount = g.Count(),
                    AvgMinutes = g.Average(x => (double?)EF.Functions.DateDiffMinute(x.f.CreatedDate, x.h.CreatedAt))
                })
                .ToListAsync();

            rows.Departments = await scoped
                .GroupBy(f => new { f.DepartmentId, Name = f.Department!.Name })
                .Select(g => new FeedbackDepartmentRow
                {
                    DepartmentId = g.Key.DepartmentId,
                    Name = g.Key.Name,
                    Total = g.Count(),
                    NewCount = g.Count(f => f.Status == FeedbackStatus.New),
                    InReviewCount = g.Count(f => f.Status == FeedbackStatus.InReview),
                    ResolvedCount = g.Count(f => f.Status == FeedbackStatus.Resolved),
                    ArchivedCount = g.Count(f => f.Status == FeedbackStatus.Archived),
                    AvgMinutes = g.Average(f => f.ResolvedAt == null
                        ? (double?)null
                        : EF.Functions.DateDiffMinute(f.CreatedDate, f.ResolvedAt.Value))
                })
                .ToListAsync();

            return rows;
        }

        public async Task<PatientFeedback> CreateAsync(PatientFeedback feedback)
        {
            await _context.Feedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();
            return feedback;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
