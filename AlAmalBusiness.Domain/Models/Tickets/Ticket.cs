using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Tickets
{
    // A support ticket a member of staff raised, and the workflow on it.
    // Ported from the CRMS Tickets app with its business rules; what changed
    // is where names and departments come from, and who an insurance ticket
    // belongs to.
    //
    // CreatedById/AssignedToId are real AspNetUsers FKs read back through the
    // navigations — the original had no user table of its own and
    // snapshotted usernames, which this app has no reason to. DepartmentId is
    // the creator's department at the moment they raised it (from their own
    // token), pointing at the shared Departments lookup instead of the
    // original's copied department name.
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketType Type { get; set; } = TicketType.General;

        // Only meaningful for Type == CertaCure — see TicketType.
        public string? PatientId { get; set; }
        public string? SourceUrl { get; set; }

        // All three optional and purely descriptive; a reason can only be
        // picked alongside the procedure it belongs to (TicketService).
        public int? CategoryId { get; set; }
        public TicketCategory? Category { get; set; }
        public int? ProcedureId { get; set; }
        public TicketProcedure? Procedure { get; set; }
        public int? ReasonId { get; set; }
        public TicketReason? Reason { get; set; }

        // Optional, and the one field that routes a ticket: Insurance means it
        // belongs to the insurance desk (TInsurance) alone — kept out of the
        // support queue, worked and closed by that desk. Cash, or nothing, is
        // an ordinary queue ticket. Fixed at creation; there is no edit.
        //
        // The original gated this behind a HasInvoice checkbox and a separate
        // accept/decline review step; both were dropped on 2026-09-17.
        public PaymentWays? PaymentMethod { get; set; }

        [Required]
        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        // Null when the creator's account had no department.
        public int? DepartmentId { get; set; }
        public Departments? Department { get; set; }

        // Who closed it. There is no claim step.
        public string? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }

        // Why it failed. Only set on a Failed close; cleared on reopen.
        public string? Resolution { get; set; }

        // Stamped on Success/Failed and cleared on reopen, so it always
        // describes the row as it stands.
        public DateTime? ClosedAt { get; set; }

        // Jordan time, like every business timestamp (AppClock).
        public DateTime CreatedDate { get; set; } = AppClock.Now;
    }
}
