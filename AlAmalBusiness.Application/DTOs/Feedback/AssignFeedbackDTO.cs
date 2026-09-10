namespace AlAmalBusiness.Application.DTOs.Feedback
{
    // Assignment is deliberately only "me" or "nobody" — same reasoning as
    // Lead's Claim: an owner takes a message on, it is never handed to
    // someone else. Taking a user id from the request body would let any
    // caller pin a message on a colleague.
    public class AssignFeedbackDTO
    {
        public bool AssignToMe { get; set; }
    }
}
