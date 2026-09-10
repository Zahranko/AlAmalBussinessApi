using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Feedback
{
    // Moving a misrouted message to the department it should have gone to.
    public class ForwardFeedbackDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "القسم مطلوب")]
        public int DepartmentId { get; set; }

        [StringLength(2000)]
        public string? Note { get; set; }
    }
}
