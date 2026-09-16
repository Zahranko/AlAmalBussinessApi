using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.Appointments
{
    // Flat row the inbox query projects into straight from SQL — the columns
    // the list shows and nothing else, with related names read through the
    // navigations in the same SELECT. Same reasoning as FeedbackListRow:
    // Include'ing the department and the AspNetUsers row (password hash,
    // security stamp) plus the Details column for every row is a lot of
    // network for a dozen scalars.
    public class AppointmentListRow
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? PhoneCountryCode { get; set; }
        public string? PhoneNumber { get; set; }
        public AppointmentStatus Status { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int ReferralSourceId { get; set; }
        public string? ReferralSourceName { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
