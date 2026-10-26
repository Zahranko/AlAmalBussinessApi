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
