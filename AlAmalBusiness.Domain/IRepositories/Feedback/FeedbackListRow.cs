using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.Feedback
{
    // Flat row the inbox query projects into straight from SQL — the columns
    // the list shows and nothing else, with related names read through the
    // navigations in the same SELECT. Same reasoning as LeadListRow: Include'ing
    // the department and the AspNetUsers row (password hash, security stamp)
    // plus the Details LOB for every row is a lot of network for a dozen
    // scalars, and production talks to the database over the wire.
    public class FeedbackListRow
    {
        public int Id { get; set; }
        public string? ReferenceNumber { get; set; }
        public FeedbackType Type { get; set; }
        public FeedbackStatus Status { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneCountryCode { get; set; }
        public string? PhoneNumber { get; set; }
        public DateOnly VisitDate { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
