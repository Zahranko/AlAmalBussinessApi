using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Feedback
{
    public class UpdateFeedbackStatusDTO
    {
        [Required]
        [EnumDataType(typeof(FeedbackStatus))]
        public FeedbackStatus Status { get; set; }

        // Why it moved. Optional — the timeline records the change either way.
        [StringLength(2000)]
        public string? Note { get; set; }
    }
}
