using System.Collections.Generic;
using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Appointments
{
    // The staff inbox's four write actions. Each mirrors its feedback
    // counterpart exactly — same rules, same reasons.

    public class UpdateAppointmentStatusDTO
    {
        [Required]
        [EnumDataType(typeof(AppointmentStatus))]
        public AppointmentStatus Status { get; set; }

        // Why it moved. Optional — the timeline records the change either way.
        [StringLength(2000)]
        public string? Note { get; set; }
    }

    // Assignment is deliberately only "me" or "nobody": an owner takes a
    // request on, it is never handed to someone else. Taking a user id from
    // the body would let any caller pin a request on a colleague.
    public class AssignAppointmentDTO
    {
        public bool AssignToMe { get; set; }
    }

    // Moving a misrouted request to the department it should have gone to.
    public class ForwardAppointmentDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "القسم مطلوب")]
        public int DepartmentId { get; set; }

        [StringLength(2000)]
        public string? Note { get; set; }
    }

    // A standalone timeline comment, posted from the box under the history.
    public class AddAppointmentNoteDTO
    {
        [Required(ErrorMessage = "Write something first.")]
        [StringLength(2000, MinimumLength = 1)]
        public string? Note { get; set; }
    }

    // Who is acting, resolved in the controller from the caller's own
    // validated token — see FeedbackActor for why it is passed in rather
    // than read from IHttpContextAccessor inside the service.
    public record AppointmentActor(
        string UserId,
        // True for Admin alone: sees every department's requests. Everyone
        // else, AManager included, is narrowed to DepartmentIds below.
        bool CanViewAll,
        // The department the actor works in (AppClaims.DepartmentId).
        int DepartmentId,
        // Every department the actor may read — see FeedbackActor.
        IReadOnlyList<int> DepartmentIds);
}
