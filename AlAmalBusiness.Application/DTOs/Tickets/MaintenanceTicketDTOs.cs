using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Tickets
{
    // The New maintenance request form. Comment and close bodies are the
    // support tickets' own CommentTicketDTO / CloseTicketDTO.
    public class CreateMaintenanceTicketDTO
    {
        [Required(ErrorMessage = "عنوان الطلب مطلوب")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "العنوان بين حرفين و200 حرف")]
        public string? Title { get; set; }

        // Optional here; an undefined value is refused by the service.
        public MaintenanceSite? Site { get; set; }

        [StringLength(200, ErrorMessage = "الموقع 200 حرف كحد أقصى")]
        public string? Location { get; set; }

        [StringLength(4000, ErrorMessage = "التفاصيل 4000 حرف كحد أقصى")]
        public string? Details { get; set; }

        // Missing = Normal.
        public MaintenancePriority? Priority { get; set; }
    }

    // The public request page (ments.<domain>). Nobody signs in there, so it
    // says who is asking; length rules are the service's (Arabic messages,
    // one place).
    public class PublicMaintenanceTicketDTO
    {
        public string? RequesterName { get; set; }
        public string? RequesterPhone { get; set; }
        public string? Title { get; set; }
        // Required on the public page.
        public MaintenanceSite? Site { get; set; }
        public string? Location { get; set; }
        public string? Details { get; set; }
        // Missing = Normal; a value outside the enum is refused by the service.
        public MaintenancePriority? Priority { get; set; }
    }

    // Who is acting, from the caller's own validated token.
    public record MaintenanceTicketActor(
        string UserId,
        bool IsAdmin,
        // The maintenance team: reads and solves every request. Admin, MSupport.
        bool CanSupport,
        // Stamped on a request they raise. Descriptive only.
        int? DepartmentId);
}
