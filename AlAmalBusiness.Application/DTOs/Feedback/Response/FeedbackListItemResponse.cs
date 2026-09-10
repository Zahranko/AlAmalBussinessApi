using System;

namespace AlAmalBusiness.Application.DTOs.Feedback.Response
{
    // One row of the staff inbox.
    public class FeedbackListItemResponse
    {
        public int Id { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public DateOnly VisitDate { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
