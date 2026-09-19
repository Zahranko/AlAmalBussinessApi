using System.Collections.Generic;
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

        // Set by the service from the caller's token, never by the caller:
        // null means "no restriction", which is Admin only. Otherwise it is
        // every department the caller may read — their own plus whatever
        // UserDepartments grants them — and an EMPTY list means they may read
        // nothing. Empty must never be treated as "no restriction"; the repo
        // filters it to no rows on purpose, which is the safe direction.
        public List<int>? RestrictToDepartmentIds { get; set; }
    }
}
