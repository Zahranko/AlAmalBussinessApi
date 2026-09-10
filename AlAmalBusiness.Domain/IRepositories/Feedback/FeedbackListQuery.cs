using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.Feedback
{
    // The inbox's filter set, passed controller -> service -> repository as one
    // object so adding a filter doesn't ripple through three signatures.
    public class FeedbackListQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        public FeedbackType? Type { get; set; }
        public FeedbackStatus? Status { get; set; }
        public int? DepartmentId { get; set; }

        // Inclusive bounds on the visit date.
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        // Matches reference number, patient name, or phone.
        public string? Search { get; set; }

        // Set by the service, never by the caller: null means "no restriction"
        // (an admin or a manager), any value narrows the list to that one
        // department — the caller's own. A department id nobody's messages
        // carry therefore yields an empty inbox, which is the safe direction.
        public int? RestrictToDepartmentId { get; set; }
    }
}
