using System.Collections.Generic;
using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.Appointments
{
    // The inbox's filter set, passed controller -> service -> repository as
    // one object so adding a filter doesn't ripple through three signatures.
    // Mirrors FeedbackListQuery.
    public class AppointmentListQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        public AppointmentStatus? Status { get; set; }
        public int? DepartmentId { get; set; }
        public int? ReferralSourceId { get; set; }

        // Inclusive bounds on the day the request arrived. Unlike feedback —
        // which filters on the visit date the patient typed — an appointment
        // request has no patient-supplied date, so the only date it has is
        // the one it came in on.
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        // Matches the patient's name or phone.
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
