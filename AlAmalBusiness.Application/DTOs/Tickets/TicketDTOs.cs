using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Tickets
{
    // The New ticket form.
    public class CreateTicketDTO
    {
        [Required(ErrorMessage = "عنوان التذكرة مطلوب")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "العنوان بين حرفين و200 حرف")]
        public string? Title { get; set; }

        [StringLength(4000)]
        public string? Description { get; set; }

        [EnumDataType(typeof(TicketType))]
        public TicketType Type { get; set; } = TicketType.General;

        // Only meaningful for Type == CertaCure — see TicketType.
        [StringLength(64)]
        public string? PatientId { get; set; }

        [StringLength(1000)]
        public string? SourceUrl { get; set; }

        public int? CategoryId { get; set; }
        public int? ProcedureId { get; set; }
        public int? ReasonId { get; set; }

        // Insurance hands the ticket to the insurance desk alone; see
        // Ticket.PaymentMethod.
        [EnumDataType(typeof(PaymentWays))]
        public PaymentWays? PaymentMethod { get; set; }
    }

    // A plain timeline comment.
    public class CommentTicketDTO
    {
        [Required(ErrorMessage = "اكتب تعليقاً أولاً.")]
        [StringLength(2000, MinimumLength = 1)]
        public string? Note { get; set; }
    }

    // Closing: Success, or Failed with a reason. The outcome is the enum
    // itself; Open is refused by the service rather than by model binding so
    // it gets the same readable 409 as every other rule.
    public class CloseTicketDTO
    {
        [Required]
        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus Outcome { get; set; }

        // Required only for Failed — conditional on Outcome, so the service
        // checks it.
        [StringLength(2000)]
        public string? Reason { get; set; }
    }

    // Who is acting, resolved in the controller from the caller's own
    // validated token — see AppointmentActor for why it is passed in rather
    // than read from IHttpContextAccessor inside the service.
    public record TicketActor(
        string UserId,
        // Works the support queue — every ticket that isn't Insurance: sees,
        // comments, closes. Admin, TManager, TEmployee.
        bool CanWork,
        // May reopen a closed support-queue ticket. Admin, TManager.
        bool CanReopen,
        // The insurance desk — the same three rights over Insurance tickets,
        // reopening included, since nobody else can see them. Admin, TInsurance.
        bool CanInsurance,
        // The actor's own department, stamped on a ticket they raise. Null
        // when their account has none.
        int? DepartmentId);
}
