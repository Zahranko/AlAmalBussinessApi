using System;

namespace AlAmalBusiness.Application.DTOs.Feedback.Response
{
    // What an anonymous submit returns — the reference number and nothing
    // else. The stored record is never echoed back to an unauthenticated
    // caller.
    public class FeedbackCreatedResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
