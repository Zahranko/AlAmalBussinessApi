using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.CRM
{
    // The admin recycle bin for leads. A delete never removes the Lead row —
    // it sets Lead.IsDeleted (hidden everywhere by the global query filter)
    // and records who/when/why here, so a restore brings back the same id
    // with its whole timeline and calls intact. One row per currently-deleted
    // lead; the row is removed on restore (the Deleted/Restored timeline
    // entries keep the record after that).
    public class DeletedLead
    {
        [Key]
        public int Id { get; set; }

        public int LeadId { get; set; }
        public Lead? Lead { get; set; }

        [Required]
        public string? DeletedById { get; set; }
        public User? DeletedBy { get; set; }

        public DateTime DeletedAt { get; set; } = AppClock.Now;

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
