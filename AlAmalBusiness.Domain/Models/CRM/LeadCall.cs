using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.CRM
{
    public class LeadCall
    {
        [Key]
        public int Id { get; set; }

        public int LeadId { get; set; }
        public Lead? Lead { get; set; }

        [Required]
        public string? ActorId { get; set; }
        public User? Actor { get; set; }

        public DateTime Date { get; set; }
        public string? Note { get; set; }
        public bool IsDone { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
