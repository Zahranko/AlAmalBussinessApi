using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Tickets
{
    // A maintenance request (a broken AC, a leaking tap, a dead socket) and
    // the workflow on it. Added 2026-10-21 beside the support tickets, with
    // the same raise -> comment -> close/reopen workflow and none of their
    // routing: no insurance desk, no department view, no CertaCure fields.
    // MRaiser raises and follows their own; MSupport receives and solves
    // every one (see AppRoles).
    //
    // Its own table rather than a kind flag on Ticket on purpose: every rule
    // in TicketService, the extension's queue and the TicketHub groups read
    // Tickets, and a maintenance request must never surface in any of them.
    // Statuses and timeline kinds are the support tickets' own enums.
    public class MaintenanceTicket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        // Where the problem is — a room, a floor, a ward — in free text.
        public string? Location { get; set; }

        // What is wrong, in the raiser's words. Several lines allowed.
        public string? Details { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Open;

        [Required]
        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        // The raiser's department at the moment they raised it, from their
        // own token. Descriptive only — nothing here is department-scoped.
        public int? DepartmentId { get; set; }
        public Departments? Department { get; set; }

        // Who closed it. There is no claim step, as with support tickets.
        public string? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }

        // Why it could not be fixed. Only set on a Failed close; cleared on reopen.
        public string? Resolution { get; set; }

        // Stamped on Success/Failed and cleared on reopen.
        public DateTime? ClosedAt { get; set; }

        // Jordan time, like every business timestamp (AppClock).
        public DateTime CreatedDate { get; set; } = AppClock.Now;
    }

    // Append-only timeline entry, the maintenance counterpart of
    // TicketHistory. Uses TicketActions (Created, Commented, Success, Failed,
    // Reopened — never Delayed/Undelayed, there is no queue parking here).
    public class MaintenanceTicketHistory
    {
        [Key]
        public int Id { get; set; }

        public int TicketId { get; set; }
        public MaintenanceTicket? Ticket { get; set; }

        [Required]
        public string? ActorId { get; set; }
        public User? Actor { get; set; }

        public TicketActions Type { get; set; }

        // The comment itself, or a failure reason.
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;
    }
}
