using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Tickets;
using AlAmalBusiness.Domain.Models.Tickets;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Tickets
{
    public class TicketRepo : ITicketRepo
    {
        private readonly AppDbContext _context;

        public TicketRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Ticket> CreateAsync(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public Task<Ticket?> GetByIdAsync(int id) =>
            _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);

        // Projections every read goes through — see TicketListRow. Names come
        // through the navigations inside the same SELECT (LEFT JOINs), no
        // Include, no tracking, and the list leaves the free text behind.
        private static readonly Expression<Func<Ticket, TicketListRow>> ToRow = t => new TicketListRow
        {
            Id = t.Id,
            Title = t.Title,
            Name = t.Name,
            Status = t.Status,
            Type = t.Type,
            CategoryId = t.CategoryId,
            CategoryName = t.Category!.Name,
            ProcedureId = t.ProcedureId,
            ProcedureName = t.Procedure!.Name,
            IsInsurance = t.IsInsurance,
            SourceUrl = t.SourceUrl,
            CreatedById = t.CreatedById,
            CreatedByName = t.CreatedBy!.UserName,
            DepartmentId = t.DepartmentId,
            DepartmentName = t.Department!.Name,
            AssignedToId = t.AssignedToId,
            AssignedToName = t.AssignedTo!.UserName,
            ClosedAt = t.ClosedAt,
            CreatedDate = t.CreatedDate
        };

        public Task<TicketDetailRow?> GetDetailAsync(int id) =>
            _context.Tickets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TicketDetailRow
                {
                    Id = t.Id,
                    Title = t.Title,
                    Name = t.Name,
                    Status = t.Status,
                    Type = t.Type,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category!.Name,
                    ProcedureId = t.ProcedureId,
                    ProcedureName = t.Procedure!.Name,
                    IsInsurance = t.IsInsurance,
                    SourceUrl = t.SourceUrl,
                    CreatedById = t.CreatedById,
                    CreatedByName = t.CreatedBy!.UserName,
                    DepartmentId = t.DepartmentId,
                    DepartmentName = t.Department!.Name,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo!.UserName,
                    ClosedAt = t.ClosedAt,
                    CreatedDate = t.CreatedDate,
                    Reason = t.Reason,
                    PatientId = t.PatientId,
                    Resolution = t.Resolution
                })
                .FirstOrDefaultAsync();

        public async Task<(List<TicketListRow> Items, int TotalCount)> PageTicketsAsync(TicketListQuery query)
        {
            var q = _context.Tickets.AsNoTracking();

            // The restrictions the service resolved from the caller come first;
            // nothing the caller asked for below can widen them.
            var isQueue = query.CreatedById == null;
            if (!isQueue)
            {
                q = q.Where(t => t.CreatedById == query.CreatedById);
            }
            else
            {
                // An insurance ticket belongs to the insurance desk alone, so
                // the queue is split on it: the support team's side is every
                // other ticket (Cash or no payment method), the desk's side is
                // Insurance. Someone who works both sides gets no filter.
                // The arms OR together, so holding two roles widens the queue
                // rather than narrowing it. Admin short-circuits the lot.
                if (!query.SeesEverything)
                {
                    var viewerId = query.ViewerId;
                    var departmentId = query.SeesDepartmentId;
                    var support = query.SeesSupportQueue;
                    var insurance = query.SeesInsurance;

                    q = q.Where(t =>
                        // Always: what the caller raised themselves.
                        t.CreatedById == viewerId
                        // The support agent: everything not flagged insurance.
                        || (support && !t.IsInsurance)
                        // The insurance desk: only the flagged ones.
                        || (insurance && t.IsInsurance)
                        // A manager: their own department, flagged ones
                        // excepted — those belong to the desk alone.
                        || (departmentId != null && !t.IsInsurance && t.DepartmentId == departmentId));
                }
            }

            if (query.CategoryId.HasValue)
                q = q.Where(t => t.CategoryId == query.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                // Title is the MRN and Name is who the ticket is about — the
                // two things someone hunting for a particular ticket actually
                // knows. The free text is deliberately not searched: it is
                // unbounded prose and would make this a scan.
                q = q.Where(t => t.Title.Contains(term) || (t.Name != null && t.Name.Contains(term)));
            }

            // An explicit status wins over the scope's own status rule, as in
            // the original: "Unassigned" plus "Failed" is a legitimate question.
            var closedOnly = false;
            if (isQueue)
            {
                switch (query.Scope)
                {
                    case TicketQueueScope.Mine:
                        q = q.Where(t => t.AssignedToId == query.AssignedToId);
                        break;
                    case TicketQueueScope.Unassigned:
                        q = q.Where(t => t.AssignedToId == null);
                        if (!query.Status.HasValue) q = q.Where(t => t.Status == TicketStatus.Open);
                        break;
                    case TicketQueueScope.Closed:
                        if (!query.Status.HasValue) q = q.Where(t => t.Status != TicketStatus.Open);
                        closedOnly = true;
                        break;
                    default:
                        if (!query.Status.HasValue) q = q.Where(t => t.Status == TicketStatus.Open);
                        break;
                }
            }

            if (query.Status.HasValue)
            {
                q = q.Where(t => t.Status == query.Status.Value);
                closedOnly = query.Status.Value != TicketStatus.Open;
            }

            var totalCount = await q.CountAsync();

            // A closed-only view reads by when each ticket was closed, most
            // recent first; everything else by when it was raised — newest
            // first for a list somebody reads, oldest first for a queue
            // somebody works down one ticket at a time (OldestFirst). The
            // second ordering has to happen HERE rather than in the caller:
            // the page is cut after the sort, so a client re-sorting its own
            // page would be re-sorting the newest 50 and never see the
            // oldest ticket at all.
            var ordered = closedOnly
                ? q.OrderByDescending(t => t.ClosedAt).ThenByDescending(t => t.CreatedDate)
                : query.OldestFirst
                    ? q.OrderBy(t => t.CreatedDate).ThenBy(t => t.Id)
                    : q.OrderByDescending(t => t.CreatedDate);

            var items = await ordered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToRow)
                .ToListAsync();

            return (items, totalCount);
        }

        // ---------- dashboard ----------

        // Grouped queries over the period, none of which returns a ticket
        // row, the same shape the feedback and appointment dashboards use.
        // The one exception is the first-response pass at the end, which
        // returns at most one small tuple per ticket because "who answered
        // first" cannot be grouped without knowing which entry was first.
        public async Task<TicketStatsRows> GetStatsAsync(TicketStatsQuery query)
        {
            var scoped = _context.Tickets.AsNoTracking();

            // CreatedDate is local (AppClock), so the bounds are day
            // boundaries: from 00:00 on From up to, but not including, 00:00
            // the day after To.
            if (query.From.HasValue)
            {
                var from = query.From.Value.ToDateTime(TimeOnly.MinValue);
                scoped = scoped.Where(t => t.CreatedDate >= from);
            }

            if (query.To.HasValue)
            {
                var toExclusive = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
                scoped = scoped.Where(t => t.CreatedDate < toExclusive);
            }

            var rows = new TicketStatsRows();

            rows.Statuses = await scoped
                .GroupBy(t => t.Status)
                .Select(g => new TicketStatusCountRow { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            rows.InsuranceCount = await scoped.CountAsync(t => t.IsInsurance);

            rows.Departments = await scoped
                .GroupBy(t => new { t.DepartmentId, Name = t.Department!.Name })
                .Select(g => new TicketNamedCountRow
                {
                    Id = g.Key.DepartmentId,
                    Name = g.Key.Name,
                    Total = g.Count(),
                    OpenCount = g.Count(t => t.Status == TicketStatus.Open)
                })
                .ToListAsync();

            rows.Procedures = await scoped
                .Where(t => t.ProcedureId != null)
                .GroupBy(t => new { t.ProcedureId, Name = t.Procedure!.Name })
                .Select(g => new TicketNamedCountRow
                {
                    Id = g.Key.ProcedureId,
                    Name = g.Key.Name,
                    Total = g.Count(),
                    OpenCount = g.Count(t => t.Status == TicketStatus.Open)
                })
                .ToListAsync();

            rows.Categories = await scoped
                .Where(t => t.CategoryId != null)
                .GroupBy(t => new { t.CategoryId, Name = t.Category!.Name })
                .Select(g => new TicketNamedCountRow
                {
                    Id = g.Key.CategoryId,
                    Name = g.Key.Name,
                    Total = g.Count(),
                    OpenCount = g.Count(t => t.Status == TicketStatus.Open)
                })
                .ToListAsync();

            rows.AvgResolutionMinutes = await scoped
                .Where(t => t.ClosedAt != null)
                .Select(t => (double?)EF.Functions.DateDiffMinute(t.CreatedDate, t.ClosedAt!.Value))
                .AverageAsync();

            // Min() over a nullable projection answers null on an empty set,
            // where MinAsync would throw. "Nothing is waiting" has to read as
            // null, not as an age of zero.
            rows.OldestOpenAt = await scoped
                .Where(t => t.Status == TicketStatus.Open)
                .Select(t => (DateTime?)t.CreatedDate)
                .MinAsync();

            rows.Closers = await scoped
                .Where(t => t.AssignedToId != null && t.ClosedAt != null)
                .GroupBy(t => new { t.AssignedToId, Name = t.AssignedTo!.UserName })
                .Select(g => new TicketCloserRow
                {
                    ActorId = g.Key.AssignedToId,
                    ActorName = g.Key.Name,
                    ClosedCount = g.Count(),
                    SuccessCount = g.Count(t => t.Status == TicketStatus.Success),
                    FailedCount = g.Count(t => t.Status == TicketStatus.Failed),
                    AvgResolutionMinutes = g.Average(t => (double?)EF.Functions.DateDiffMinute(t.CreatedDate, t.ClosedAt!.Value))
                })
                .ToListAsync();

            // Who answered each ticket first, and how long they took. A
            // correlated first-row-per-ticket (OUTER APPLY), so it is one
            // query returning four small columns per ticket rather than a
            // history table scan brought into memory. The creator's own
            // entries don't count as a response to themselves.
            var firstResponses = await scoped
                .Select(t => new
                {
                    t.CreatedDate,
                    First = _context.TicketHistories
                        .Where(h => h.TicketId == t.Id
                            && h.Type != TicketActions.Created
                            && h.ActorId != t.CreatedById)
                        .OrderBy(h => h.CreatedAt)
                        .Select(h => new { h.ActorId, ActorName = h.Actor!.UserName, h.CreatedAt })
                        .FirstOrDefault()
                })
                .ToListAsync();

            rows.FirstResponses = firstResponses
                .Where(x => x.First != null)
                .Select(x => new TicketFirstResponseRow
                {
                    ActorId = x.First!.ActorId,
                    ActorName = x.First.ActorName,
                    Minutes = (x.First.CreatedAt - x.CreatedDate).TotalMinutes
                })
                .ToList();

            return rows;
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }

    public class TicketHistoryRepo : ITicketHistoryRepo
    {
        private readonly AppDbContext _context;

        public TicketHistoryRepo(AppDbContext context)
        {
            _context = context;
        }

        // Queued against the same context the ticket itself was read from, so
        // ITicketRepo.SaveChangesAsync commits both together.
        public void Add(TicketHistory history) => _context.TicketHistories.Add(history);

        public Task<List<TicketHistoryRow>> GetByTicketAsync(int ticketId) =>
            _context.TicketHistories
                .AsNoTracking()
                .Where(h => h.TicketId == ticketId)
                .OrderBy(h => h.CreatedAt)
                .ThenBy(h => h.Id)
                .Select(h => new TicketHistoryRow
                {
                    Id = h.Id,
                    Type = h.Type,
                    ActorName = h.Actor!.UserName,
                    Note = h.Note,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();
    }

    public class TicketCategoryRepo : ITicketCategoryRepo
    {
        private readonly AppDbContext _context;

        public TicketCategoryRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<TicketCategory>> GetAllAsync() =>
            _context.TicketCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

        public Task<List<TicketCategory>> GetActiveAsync() =>
            _context.TicketCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

        public Task<TicketCategory?> GetByIdAsync(int id) =>
            _context.TicketCategories.FirstOrDefaultAsync(c => c.Id == id);

        public async Task CreateAsync(TicketCategory category)
        {
            _context.TicketCategories.Add(category);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<bool> IsNameExist(string name, int excludeId) =>
            _context.TicketCategories.AnyAsync(c => c.Name == name && c.Id != excludeId);
    }

    public class TicketProcedureRepo : ITicketProcedureRepo
    {
        private readonly AppDbContext _context;

        public TicketProcedureRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<TicketProcedure>> GetAllAsync() =>
            _context.TicketProcedures.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

        public Task<List<TicketProcedure>> GetActiveAsync() =>
            _context.TicketProcedures.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

        public Task<TicketProcedure?> GetByIdAsync(int id) =>
            _context.TicketProcedures.FirstOrDefaultAsync(p => p.Id == id);

        public async Task CreateAsync(TicketProcedure procedure)
        {
            _context.TicketProcedures.Add(procedure);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<bool> IsNameExist(string name, int excludeId) =>
            _context.TicketProcedures.AnyAsync(p => p.Name == name && p.Id != excludeId);
    }
}
