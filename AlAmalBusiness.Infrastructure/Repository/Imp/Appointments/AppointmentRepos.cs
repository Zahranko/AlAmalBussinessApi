using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using AlAmalBusiness.Domain.Models.Appointments;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Appointments
{
    public class AppointmentRequestRepo : IAppointmentRequestRepo
    {
        private readonly AppDbContext _context;

        public AppointmentRequestRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AppointmentRequest> CreateAsync(AppointmentRequest request)
        {
            _context.AppointmentRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public Task<AppointmentRequest?> GetByIdAsync(int id) =>
            _context.AppointmentRequests.FirstOrDefaultAsync(a => a.Id == id);

        // The detail view is a single row, so Include'ing the three names it
        // needs is fine here — it is the list query (below) that must never
        // pull whole AspNetUsers rows.
        public Task<AppointmentRequest?> GetDetailAsync(int id) =>
            _context.AppointmentRequests
                .Include(a => a.Department)
                .Include(a => a.ReferralSource)
                .Include(a => a.AssignedTo)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

        // Projection the inbox reads through — see AppointmentListRow. Names
        // come through the navigations inside the same SELECT (LEFT JOINs),
        // no Include, no tracking, and the Details column is left behind.
        private static readonly System.Linq.Expressions.Expression<Func<AppointmentRequest, AppointmentListRow>> ToRow = a => new AppointmentListRow
        {
            Id = a.Id,
            FullName = a.FullName,
            PhoneCountryCode = a.PhoneCountryCode,
            PhoneNumber = a.PhoneNumber,
            Status = a.Status,
            DepartmentId = a.DepartmentId,
            DepartmentName = a.Department!.Name,
            ReferralSourceId = a.ReferralSourceId,
            ReferralSourceName = a.ReferralSource!.Name,
            AssignedToId = a.AssignedToId,
            AssignedToName = a.AssignedTo!.UserName,
            CompletedAt = a.CompletedAt,
            CreatedDate = a.CreatedDate
        };

        public async Task<(List<AppointmentListRow> Items, int TotalCount)> PageRequestsAsync(AppointmentListQuery query)
        {
            var q = _context.AppointmentRequests.AsNoTracking();

            // Applied before anything the caller asked for: this is the
            // department scoping the service resolved from the caller's
            // roles, not a filter they can widen.
            // The scoping the service resolved from the caller's token. An
            // empty set means they may read nothing, so it yields no rows.
            if (query.RestrictToDepartmentIds is { } readable)
            {
                q = readable.Count == 0
                    ? q.Where(a => false)
                    : q.Where(a => readable.Contains(a.DepartmentId));
            }

            if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status.Value);
            if (query.DepartmentId.HasValue) q = q.Where(a => a.DepartmentId == query.DepartmentId.Value);
            if (query.ReferralSourceId.HasValue) q = q.Where(a => a.ReferralSourceId == query.ReferralSourceId.Value);

            // CreatedDate is a local DateTime, so the bounds are day
            // boundaries: from 00:00 on FromDate up to (not including) 00:00
            // the day after ToDate.
            if (query.FromDate.HasValue)
            {
                var from = query.FromDate.Value.ToDateTime(TimeOnly.MinValue);
                q = q.Where(a => a.CreatedDate >= from);
            }

            if (query.ToDate.HasValue)
            {
                var toExclusive = query.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                q = q.Where(a => a.CreatedDate < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();

                // Same split as lead and feedback search: a digits-and-
                // punctuation term is tried against the phone alone, anything
                // else against the name alone. Still a scan, with fewer LIKEs
                // per row.
                if (term.All(c => char.IsDigit(c) || c is '+' or '-' or ' ' or '(' or ')'))
                {
                    // Matched leading-zero-tolerantly: numbers are stored with
                    // the national zero stripped, but a searcher types the
                    // number the way a patient says it.
                    var phone = term.TrimStart('0');
                    if (phone.Length == 0) phone = term;

                    q = q.Where(a => a.PhoneNumber.Contains(phone));
                }
                else
                {
                    q = q.Where(a => a.FullName.Contains(term));
                }
            }

            var totalCount = await q.CountAsync();

            var items = await q
                .OrderByDescending(a => a.CreatedDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToRow)
                .ToListAsync();

            return (items, totalCount);
        }

        // ---------- dashboard ----------

        // Six aggregate queries, none of which returns a request row. They
        // all start from the same scoped, date-bounded set: the scoping the
        // service resolved from the caller's roles, then the period, then an
        // admin's optional single-department narrowing.
        public async Task<AppointmentStatsRows> GetStatsAsync(AppointmentStatsQuery query)
        {
            var scoped = _context.AppointmentRequests.AsNoTracking();

            if (query.RestrictToDepartmentIds is { } readable)
            {
                scoped = readable.Count == 0
                    ? scoped.Where(a => false)
                    : scoped.Where(a => readable.Contains(a.DepartmentId));
            }

            if (query.DepartmentId.HasValue)
                scoped = scoped.Where(a => a.DepartmentId == query.DepartmentId.Value);

            if (query.From.HasValue)
            {
                var from = query.From.Value.ToDateTime(TimeOnly.MinValue);
                scoped = scoped.Where(a => a.CreatedDate >= from);
            }

            if (query.To.HasValue)
            {
                var toExclusive = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                scoped = scoped.Where(a => a.CreatedDate < toExclusive);
            }

            var rows = new AppointmentStatsRows();

            rows.Statuses = await scoped
                .GroupBy(a => a.Status)
                .Select(g => new AppointmentStatusCountRow { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            rows.Sources = await scoped
                .GroupBy(a => new { a.ReferralSourceId, Name = a.ReferralSource!.Name })
                .Select(g => new AppointmentSourceRow
                {
                    ReferralSourceId = g.Key.ReferralSourceId,
                    Name = g.Key.Name,
                    Count = g.Count()
                })
                .ToListAsync();

            rows.AvgBookingMinutes = await scoped
                .Where(a => a.CompletedAt != null)
                .Select(a => (double?)EF.Functions.DateDiffMinute(a.CreatedDate, a.CompletedAt!.Value))
                .AverageAsync();

            // MinAsync would throw on an empty set; Min() over a nullable
            // projection answers null instead, which is what "nothing is
            // waiting" has to read as.
            var oldestOpen = await scoped
                .Where(a => a.Status == AppointmentStatus.New || a.Status == AppointmentStatus.Contacted)
                .Select(a => (DateTime?)a.CreatedDate)
                .MinAsync();

            if (oldestOpen.HasValue)
                rows.OldestOpenMinutes = (AppClock.Now - oldestOpen.Value).TotalMinutes;

            // Who booked things, from the timeline rather than from
            // AssignedToId: the assignee is who picked it up, which is not
            // always who booked it.
            rows.Bookers = await (
                from h in _context.AppointmentHistories.AsNoTracking()
                join a in scoped on h.AppointmentId equals a.Id
                where h.Type == AppointmentActions.StatusChanged && h.ToStatus == AppointmentStatus.Scheduled
                group new { h, a } by new { h.ActorId, ActorName = h.Actor!.UserName } into g
                select new AppointmentBookerRow
                {
                    ActorId = g.Key.ActorId,
                    ActorName = g.Key.ActorName,
                    ScheduledCount = g.Count(),
                    AvgMinutes = g.Average(x => (double?)EF.Functions.DateDiffMinute(x.a.CreatedDate, x.h.CreatedAt))
                })
                .ToListAsync();

            rows.Departments = await scoped
                .GroupBy(a => new { a.DepartmentId, Name = a.Department!.Name })
                .Select(g => new AppointmentDepartmentRow
                {
                    DepartmentId = g.Key.DepartmentId,
                    Name = g.Key.Name,
                    Total = g.Count(),
                    NewCount = g.Count(a => a.Status == AppointmentStatus.New),
                    ContactedCount = g.Count(a => a.Status == AppointmentStatus.Contacted),
                    ScheduledCount = g.Count(a => a.Status == AppointmentStatus.Scheduled),
                    CancelledCount = g.Count(a => a.Status == AppointmentStatus.Cancelled),
                    AvgMinutes = g.Average(a => a.CompletedAt == null
                        ? (double?)null
                        : EF.Functions.DateDiffMinute(a.CreatedDate, a.CompletedAt.Value))
                })
                .ToListAsync();

            return rows;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }

    public class AppointmentHistoryRepo : IAppointmentHistoryRepo
    {
        private readonly AppDbContext _context;

        public AppointmentHistoryRepo(AppDbContext context)
        {
            _context = context;
        }

        // Queued against the same context the request itself was read from,
        // so IAppointmentRequestRepo.SaveChangesAsync commits both together.
        public void Add(AppointmentHistory history) => _context.AppointmentHistories.Add(history);

        public Task<List<AppointmentHistory>> GetByAppointmentAsync(int appointmentId) =>
            _context.AppointmentHistories
                .Include(h => h.Actor)
                .AsNoTracking()
                .Where(h => h.AppointmentId == appointmentId)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();
    }

    public class AppointmentReferralSourceRepo : IAppointmentReferralSourceRepo
    {
        private readonly AppDbContext _context;

        public AppointmentReferralSourceRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<AppointmentReferralSource>> GetAllAsync() =>
            _context.AppointmentReferralSources.AsNoTracking().OrderBy(r => r.Name).ToListAsync();

        public Task<List<AppointmentReferralSource>> GetActiveAsync() =>
            _context.AppointmentReferralSources.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();

        public Task<AppointmentReferralSource?> GetByIdAsync(int id) =>
            _context.AppointmentReferralSources.FirstOrDefaultAsync(r => r.Id == id);

        public async Task CreateAsync(AppointmentReferralSource source)
        {
            _context.AppointmentReferralSources.Add(source);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<bool> IsNameExist(string name, int excludeId) =>
            _context.AppointmentReferralSources.AnyAsync(r => r.Name == name && r.Id != excludeId);
    }
}
