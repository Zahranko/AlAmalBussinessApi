using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Feedback
{
    // Append-only timeline entry, the feedback counterpart of LeadHistory.
    public class FeedbackHistory
    {
        [Key]
        public int Id { get; set; }

        public int FeedbackId { get; set; }
        public PatientFeedback? Feedback { get; set; }

        [Required]
        public string? ActorId { get; set; }
        public User? Actor { get; set; }

        public FeedbackActions Type { get; set; }

        // Only set on StatusChanged entries — lets the timeline read
        // "New -> In review" without recomputing it from neighbouring rows.
        public FeedbackStatus? FromStatus { get; set; }
        public FeedbackStatus? ToStatus { get; set; }

        // Only set on Forwarded entries. Names rather than ids, and snapshots
        // on purpose: renaming a department later must not silently rewrite
        // what the timeline says happened.
        public string? FromDepartmentName { get; set; }
        public string? ToDepartmentName { get; set; }

        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
