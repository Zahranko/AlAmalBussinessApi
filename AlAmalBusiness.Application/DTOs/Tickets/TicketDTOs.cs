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

        // Who the ticket is about. Optional — a ticket raised by hand often
        // has no one particular in it.
        [StringLength(200, ErrorMessage = "الاسم 200 حرف كحد أقصى")]
        public string? Name { get; set; }

        [EnumDataType(typeof(TicketType))]
        public TicketType Type { get; set; } = TicketType.General;

        // Only meaningful for Type == CertaCure — see TicketType.
        [StringLength(64)]
        public string? PatientId { get; set; }

        [StringLength(1000)]
        public string? SourceUrl { get; set; }

        public int? CategoryId { get; set; }
        public int? ProcedureId { get; set; }

        // Why it was raised, in the raiser's words — free text, several
        // lines allowed. Was a picked TicketReasons id until 2026-09-19.
        //
        // 4000 since 2026-09-20: the CertaCure extension prepends the list of
        // items to delete to whatever the raiser typed, so this one field now
        // carries what it and the old description carried between them.
        [StringLength(4000, ErrorMessage = "السبب 4000 حرف كحد أقصى")]
        public string? Reason { get; set; }

        // Hands the ticket to the insurance desk alone. Only allowed on a
        // procedure whose AllowsInsurance is set — "Open invoice" — and
        // refused with a 409 anywhere else, so the form must only offer the
        // checkbox once that procedure is chosen.
        public bool IsInsurance { get; set; }
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
        // Sees everything and may work either side. Admin alone.
        bool IsAdmin,
        // The support agent: receives and solves every ticket that isn't
        // flagged insurance, from every department. Admin, TSupport.
        bool CanSupport,
        // The insurance desk: the same rights, over the flagged ones only.
        // Nobody else sees those. Admin, TInsurance.
        bool CanInsurance,
        // Reads every ticket raised inside their own department, but solves
        // none of them — a supervisor's view. Admin, TManager.
        bool CanReadDepartment,
        // The actor's own department: stamped on a ticket they raise, and
        // what CanReadDepartment reads. Null when their account has none,
        // which leaves a manager seeing only what they raised themselves.
        int? DepartmentId);
}
