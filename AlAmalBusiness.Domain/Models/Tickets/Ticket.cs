using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Tickets
{
    // A support ticket a member of staff raised, and the workflow on it.
    // Ported from the CRMS Tickets app; reshaped on 2026-09-19 so that
    // raising and solving are different jobs — see AppRoles for who does
    // which, and TicketService for the rules.
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

        // Both optional and purely descriptive.
        public int? CategoryId { get; set; }
        public TicketCategory? Category { get; set; }
        public int? ProcedureId { get; set; }
        public TicketProcedure? Procedure { get; set; }

        // Why the ticket was raised, in the raiser's own words. Free text
        // since 2026-09-19: it replaced a per-procedure TicketReasons picker
        // that an admin had to keep filled, and whose entries never said
        // enough on their own.
        public string? Reason { get; set; }

        // The one field that routes a ticket: true means it belongs to the
        // insurance desk (TInsurance) alone — kept out of the support agent's
        // queue and out of the raising manager's department view, worked and
        // closed by that desk. Its creator still follows it.
        //
        // Only a procedure with AllowsInsurance may carry it, which in
        // practice means "Open invoice"; TicketService refuses it anywhere
        // else. Fixed at creation; there is no edit.
        //
        // This replaced a PaymentWays? PaymentMethod column on 2026-09-19.
        // Cash vs Insurance was never really a payment fact here — it was
        // being used as a routing flag, and only one of its two values meant
        // anything, so the other half was noise on every form.
        public bool IsInsurance { get; set; }

        [Required]
        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        // The creator's department at the moment they raised it, from their
        // own token. Null when their account had none. Since 2026-09-19 this
        // is load-bearing rather than descriptive: it is what a TManager's
        // view of "my department's tickets" filters on.
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
