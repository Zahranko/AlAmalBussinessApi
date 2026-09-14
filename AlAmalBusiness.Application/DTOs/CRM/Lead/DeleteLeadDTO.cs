using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead
{
    public class DeleteLeadDTO
    {
        // Optional — shown in the deleted-leads list and on the timeline.
        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
