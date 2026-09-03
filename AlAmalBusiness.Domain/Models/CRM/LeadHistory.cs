using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.CRM
{
    public class LeadHistory
    {
        [Key]
        public int Id { get; set; }

        public int LeadId { get; set; }
        public Lead? Lead { get; set; }

        [Required]
        public string? ActorId { get; set; }
        public User? Actor { get; set; }

        public LeadActions Type { get; set; }
        public LeadStatus? ResultingStatus { get; set; }
        public DateTime? ActionDate { get; set; }

        public int? DoctorId { get; set; }
        public Doctors? Doctor { get; set; }

        public int? ClosedReasonId { get; set; }
        public ClosedReason? ClosedReason { get; set; }

        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
