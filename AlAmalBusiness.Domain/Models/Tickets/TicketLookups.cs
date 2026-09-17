using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Tickets
{
    // The ticket app's three admin-maintained lists. Each is retired with
    // IsActive, never deleted, so a ticket that picked an entry keeps it.
    // They are the ticket app's own, deliberately not the CRM's Procedures —
    // the two are maintained for different teams, the same call the
    // appointment page made about its referral sources.

    public class TicketCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class TicketProcedure
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    // "Why this ticket was raised." Every reason belongs to one procedure:
    // the New ticket form's reason picker cascades off the chosen procedure,
    // so a reason without one could never be picked. For the same reason a
    // name is unique per procedure, not globally.
    public class TicketReason
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int ProcedureId { get; set; }
        public TicketProcedure? Procedure { get; set; }
    }
}
