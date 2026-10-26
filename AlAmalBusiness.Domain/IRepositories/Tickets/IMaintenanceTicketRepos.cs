using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models.Tickets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Tickets
{
    public interface IMaintenanceTicketRepo
    {
        Task<MaintenanceTicket> CreateAsync(MaintenanceTicket ticket);

        // Tracked and with no Includes, for the workflow actions to mutate.
        Task<MaintenanceTicket?> GetByIdAsync(int id);

        Task<MaintenanceTicketDetailRow?> GetDetailAsync(int id);

        Task<(List<MaintenanceTicketListRow> Items, int TotalCount)> PageTicketsAsync(MaintenanceTicketListQuery query);

        Task SaveChangesAsync();
    }

    public interface IMaintenanceTicketHistoryRepo
    {
        // Queued only — saved together with the change it describes.
        void Add(MaintenanceTicketHistory history);

        Task<List<TicketHistoryRow>> GetByTicketAsync(int ticketId);
    }

    // The maintenance queue's tabs — the support queue's minus Unassigned,
    // which with no claim step only ever meant "open".
    public enum MaintenanceQueueScope
    {
        // Every open request.
        Open,
        // Closed by the caller. Every status.
        Mine,
        // Fixed or not, most recently closed first.
        Closed
    }

    public class MaintenanceTicketListQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Matches the title, the location, a public sender's name, or "#id".
        public string? Search { get; set; }
        // An explicit status always wins over what the scope would show.
        public TicketStatus? Status { get; set; }

        // ---- set by the service, never by the caller ----

        public MaintenanceQueueScope Scope { get; set; } = MaintenanceQueueScope.Open;

        // Narrows to requests closed by this user (Scope = Mine).
        public string? AssignedToId { get; set; }

        // Narrows to requests this user raised. When set, the scope is
        // ignored — the created-by-me list shows every status.
        public string? CreatedById { get; set; }

        // MSupport/Admin read every request; anyone else only their own,
        // which the service enforces by setting CreatedById instead.
        public bool SeesEverything { get; set; }
        public string? ViewerId { get; set; }
    }

    // Flat projection, same reasoning as TicketListRow: no Include, no
    // AspNetUsers rows, no free text in a list.
    public class MaintenanceTicketListRow
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Location { get; set; }
        public TicketStatus Status { get; set; }
        public MaintenancePriority Priority { get; set; }
        public string? CreatedById { get; set; }
        // The account's username, or the typed name on a public request.
        public string? CreatedByName { get; set; }
        public string? RequesterName { get; set; }
        public string? RequesterPhone { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class MaintenanceTicketDetailRow : MaintenanceTicketListRow
    {
        public string? Details { get; set; }
        public string? Resolution { get; set; }
    }
}
