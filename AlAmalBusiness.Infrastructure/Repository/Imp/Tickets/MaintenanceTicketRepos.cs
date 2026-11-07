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
    public class MaintenanceTicketRepo : IMaintenanceTicketRepo
    {
        private readonly AppDbContext _context;

        public MaintenanceTicketRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MaintenanceTicket> CreateAsync(MaintenanceTicket ticket)
        {
            _context.MaintenanceTickets.Add(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public Task<MaintenanceTicket?> GetByIdAsync(int id) =>
            _context.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);

        private static readonly Expression<Func<MaintenanceTicket, MaintenanceTicketListRow>> ToRow = t => new MaintenanceTicketListRow
        {
            Id = t.Id,
            Title = t.Title,
            Site = t.Site,
            Location = t.Location,
            Status = t.Status,
            Priority = t.Priority,
            CreatedById = t.CreatedById,
            // A public request has no account behind it — show who typed it.
            CreatedByName = t.CreatedById != null ? t.CreatedBy!.UserName : t.RequesterName,
            RequesterName = t.RequesterName,
            RequesterPhone = t.RequesterPhone,
            DepartmentId = t.DepartmentId,
            DepartmentName = t.Department!.Name,
            AssignedToId = t.AssignedToId,
            AssignedToName = t.AssignedTo!.UserName,
            ClosedAt = t.ClosedAt,
            CreatedDate = t.CreatedDate
        };

        public Task<MaintenanceTicketDetailRow?> GetDetailAsync(int id) =>
            _context.MaintenanceTickets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new MaintenanceTicketDetailRow
                {
                    Id = t.Id,
                    Title = t.Title,
                    Site = t.Site,
                    Location = t.Location,
                    Status = t.Status,
                    Priority = t.Priority,
                    CreatedById = t.CreatedById,
                    CreatedByName = t.CreatedById != null ? t.CreatedBy!.UserName : t.RequesterName,
                    RequesterName = t.RequesterName,
                    RequesterPhone = t.RequesterPhone,
                    DepartmentId = t.DepartmentId,
                    DepartmentName = t.Department!.Name,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo!.UserName,
                    ClosedAt = t.ClosedAt,
                    CreatedDate = t.CreatedDate,
                    Details = t.Details,
                    Resolution = t.Resolution
                })
                .FirstOrDefaultAsync();

        public async Task<(List<MaintenanceTicketListRow> Items, int TotalCount)> PageTicketsAsync(MaintenanceTicketListQuery query)
        {
            var q = _context.MaintenanceTickets.AsNoTracking();

            // The restriction the service resolved comes first; nothing the
            // caller asked for below can widen it.
            var isQueue = query.CreatedById == null;
            if (!isQueue)
                q = q.Where(t => t.CreatedById == query.CreatedById);
            else if (!query.SeesEverything)
                q = q.Where(t => t.CreatedById == query.ViewerId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                var idText = term.TrimStart('#').Trim();
                int? id = int.TryParse(idText, out var parsed) && parsed > 0 ? parsed : null;
                q = q.Where(t =>
                    (id != null && t.Id == id)
                    || t.Title.Contains(term)
                    || (t.Location != null && t.Location.Contains(term))
                    || (t.RequesterName != null && t.RequesterName.Contains(term)));
            }

            var closedOnly = false;
            if (isQueue)
            {
                switch (query.Scope)
                {
                    case MaintenanceQueueScope.Mine:
                        q = q.Where(t => t.AssignedToId == query.AssignedToId);
                        break;
                    case MaintenanceQueueScope.Closed:
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

            var ordered = closedOnly
                ? q.OrderByDescending(t => t.ClosedAt).ThenByDescending(t => t.CreatedDate)
                : q.OrderByDescending(t => t.CreatedDate);

            var items = await ordered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToRow)
                .ToListAsync();

            return (items, totalCount);
        }

        // TicketRepo.GetStatsAsync over the maintenance tables. Every figure
        // is about the requests RAISED in the period, so one raised inside it
        // but closed after still counts as the period's.
        public async Task<MaintenanceTicketStatsRows> GetStatsAsync(TicketStatsQuery query)
        {
            var scoped = _context.MaintenanceTickets.AsNoTracking();

            // CreatedDate is local (AppClock): 00:00 on From up to, but not
            // including, 00:00 the day after To.
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

            var rows = new MaintenanceTicketStatsRows();

            rows.Statuses = await scoped
                .GroupBy(t => t.Status)
                .Select(g => new TicketStatusCountRow { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var sites = await scoped
                .GroupBy(t => t.Site)
                .Select(g => new { Site = g.Key, Total = g.Count(), OpenCount = g.Count(t => t.Status == TicketStatus.Open) })
                .ToListAsync();
            rows.Sites = sites.Select(s => (s.Site, s.Total, s.OpenCount)).ToList();

            var priorities = await scoped
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key, Total = g.Count(), OpenCount = g.Count(t => t.Status == TicketStatus.Open) })
                .ToListAsync();
            rows.Priorities = priorities.Select(p => (p.Priority, p.Total, p.OpenCount)).ToList();

            // Public requests carry no department; they are left out here
            // rather than shown as a nameless row.
            rows.Departments = await scoped
                .Where(t => t.DepartmentId != null)
                .GroupBy(t => new { t.DepartmentId, Name = t.Department!.Name })
                .Select(g => new TicketNamedCountRow
                {
                    Id = g.Key.DepartmentId,
                    Name = g.Key.Name,
                    Total = g.Count(),
                    OpenCount = g.Count(t => t.Status == TicketStatus.Open)
                })
                .ToListAsync();

            rows.AvgResolutionMinutes = await scoped
                .Where(t => t.ClosedAt != null)
                .Select(t => (double?)EF.Functions.DateDiffMinute(t.CreatedDate, t.ClosedAt!.Value))
                .AverageAsync();

            // Min() over a nullable projection answers null on an empty set.
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

            // Who answered each request first — a comment or a close by
            // anyone but the raiser. A public request has no raiser account
            // (CreatedById null), so any signed-in actor's entry counts.
            var firstResponses = await scoped
                .Select(t => new
                {
                    t.CreatedDate,
                    First = _context.MaintenanceTicketHistories
                        .Where(h => h.TicketId == t.Id
                            && h.Type != TicketActions.Created
                            && h.ActorId != null
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

    public class MaintenanceTicketHistoryRepo : IMaintenanceTicketHistoryRepo
    {
        private readonly AppDbContext _context;

        public MaintenanceTicketHistoryRepo(AppDbContext context)
        {
            _context = context;
        }

        public void Add(MaintenanceTicketHistory history) => _context.MaintenanceTicketHistories.Add(history);

        public Task<List<TicketHistoryRow>> GetByTicketAsync(int ticketId) =>
            _context.MaintenanceTicketHistories
                .AsNoTracking()
                .Where(h => h.TicketId == ticketId)
                .OrderBy(h => h.CreatedAt)
                .ThenBy(h => h.Id)
                .Select(h => new TicketHistoryRow
                {
                    Id = h.Id,
                    Type = h.Type,
                    // No actor only on a public request's Created entry.
                    ActorName = h.ActorId != null ? h.Actor!.UserName : h.Ticket!.RequesterName,
                    Note = h.Note,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();
    }
}
