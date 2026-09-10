using AlAmalBusiness.DbContext.Infrastructure;
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

        public async Task<PatientFeedback> CreateAsync(PatientFeedback feedback)
        {
            await _context.Feedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();
            return feedback;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
