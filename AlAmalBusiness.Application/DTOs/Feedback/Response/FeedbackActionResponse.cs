namespace AlAmalBusiness.Application.DTOs.Feedback.Response
{
    // Result wrapper for the workflow actions that return the full detail
    // (status change, assign, note) — mirrors LeadActionResponse's shape.
    //
    // NotFound is kept apart from a plain failure so the controller can answer
    // 404 for a message the caller may not see, rather than 409 with a message
    // confirming that the id exists.
    public class FeedbackActionResponse
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Error { get; set; }
        public FeedbackDetailResponse? Feedback { get; set; }
    }
}
