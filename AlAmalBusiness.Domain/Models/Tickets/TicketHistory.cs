using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Tickets
{
    // Append-only timeline entry, the ticket counterpart of
    // AppointmentHistory. The actor is a real FK — no username snapshot.
    public class TicketHistory
    {
        [Key]
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        [Required]
        public string? ActorId { get; set; }
        public User? Actor { get; set; }

        public TicketActions Type { get; set; }

        // The comment itself, a failure reason, or a decline reason.
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;
    }
}
