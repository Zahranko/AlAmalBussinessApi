using System;

namespace AlAmalBusiness.Application.DTOs.Feedback.Response
{
    // One entry on the message's timeline.
    public class FeedbackHistoryResponse
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        // Set on Forwarded entries only.
        public string? FromDepartmentName { get; set; }
        public string? ToDepartmentName { get; set; }
        public string? ActorName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
