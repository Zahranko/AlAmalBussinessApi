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

        // Set by the service, never by the caller: null means "no
        // restriction" (an admin), any value narrows the list to that one
        // department — the caller's own.
        public int? RestrictToDepartmentId { get; set; }
    }
}
