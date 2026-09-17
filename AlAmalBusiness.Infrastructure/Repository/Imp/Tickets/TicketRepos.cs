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
        // Include, no tracking, and the list leaves Description behind.
        private static readonly Expression<Func<Ticket, TicketListRow>> ToRow = t => new TicketListRow
        {
            Id = t.Id,
            Title = t.Title,
            Status = t.Status,
            Type = t.Type,
            CategoryId = t.CategoryId,
            CategoryName = t.Category!.Name,
            ProcedureId = t.ProcedureId,
            ProcedureName = t.Procedure!.Name,
            ReasonId = t.ReasonId,
            ReasonName = t.Reason!.Name,
            PaymentMethod = t.PaymentMethod,
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
                    Status = t.Status,
                    Type = t.Type,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category!.Name,
                    ProcedureId = t.ProcedureId,
                    ProcedureName = t.Procedure!.Name,
                    ReasonId = t.ReasonId,
                    ReasonName = t.Reason!.Name,
                    PaymentMethod = t.PaymentMethod,
                    SourceUrl = t.SourceUrl,
                    CreatedById = t.CreatedById,
                    CreatedByName = t.CreatedBy!.UserName,
                    DepartmentId = t.DepartmentId,
                    DepartmentName = t.Department!.Name,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo!.UserName,
                    ClosedAt = t.ClosedAt,
                    CreatedDate = t.CreatedDate,
                    Description = t.Description,
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
                if (query.IncludeGeneral && !query.IncludeInsurance)
                    q = q.Where(t => t.PaymentMethod == null || t.PaymentMethod != PaymentWays.Insurance);
                else if (query.IncludeInsurance && !query.IncludeGeneral)
                    q = q.Where(t => t.PaymentMethod == PaymentWays.Insurance);
                else if (!query.IncludeGeneral && !query.IncludeInsurance)
                    q = q.Where(t => false);
            }

            if (query.CategoryId.HasValue)
                q = q.Where(t => t.CategoryId == query.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                q = q.Where(t => t.Title.Contains(term));
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
            // recent first; everything else by when it was raised.
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

    public class TicketReasonRepo : ITicketReasonRepo
    {
        private readonly AppDbContext _context;

        public TicketReasonRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<TicketReason>> GetAllAsync() =>
            _context.TicketReasons.AsNoTracking().Include(r => r.Procedure).OrderBy(r => r.Name).ToListAsync();

        // Active reasons under an active procedure: a reason whose procedure
        // was retired can't be reached from the form's cascade anyway.
        public Task<List<TicketReason>> GetActiveAsync() =>
            _context.TicketReasons.AsNoTracking()
                .Where(r => r.IsActive && r.Procedure!.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

        public Task<TicketReason?> GetByIdAsync(int id) =>
            _context.TicketReasons.Include(r => r.Procedure).FirstOrDefaultAsync(r => r.Id == id);

        public async Task CreateAsync(TicketReason reason)
        {
            _context.TicketReasons.Add(reason);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<bool> IsNameExist(string name, int procedureId, int excludeId) =>
            _context.TicketReasons.AnyAsync(r => r.Name == name && r.ProcedureId == procedureId && r.Id != excludeId);
    }
}
