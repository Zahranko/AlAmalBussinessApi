using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.DTOs.Appointments.Response;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Appointments
{
    public interface IAppointmentService
    {
        // ---------- anonymous (the public appointment page) ----------

        Task<AppointmentFormOptionsResponse> GetFormOptionsAsync();

        // Saves the request, then queues the notification email to every
        // active AManager of its department (best-effort).
        Task<AppointmentCreatedResponse> SubmitAsync(CreateAppointmentRequestDTO request, SubmissionContext context);

        // ---------- staff inbox ----------

        Task<PagedResultDTO<AppointmentListItemResponse>> GetPagedAsync(AppointmentListQuery query, AppointmentActor actor);

        // Null when the request doesn't exist or sits outside the caller's
        // department — the two are answered the same way on purpose.
        Task<AppointmentDetailResponse?> GetDetailAsync(int id, AppointmentActor actor);

        Task<AppointmentActionResponse> ChangeStatusAsync(int id, UpdateAppointmentStatusDTO request, AppointmentActor actor);

        Task<AppointmentActionResponse> AssignAsync(int id, AssignAppointmentDTO request, AppointmentActor actor);

        // Moves a misrouted request to another department. Carries no record
        // back: the mover is normally scoped to their own department, so the
        // moment it leaves they can no longer read it.
        Task<AppointmentActionResponse> ForwardAsync(int id, ForwardAppointmentDTO request, AppointmentActor actor);

        Task<AppointmentActionResponse> AddNoteAsync(int id, AddAppointmentNoteDTO request, AppointmentActor actor);

        // The manager/admin dashboard. Scoped the same way the inbox is.
        Task<AppointmentStatsResponse> GetStatsAsync(AppointmentStatsQuery query, AppointmentActor actor);
    }

    // The admin side: the one list left behind the appointment page.
    public interface IAppointmentListService
    {
        Task<List<AppointmentListItemDTO>> GetReferralSourcesAsync();
        Task<AppointmentListResponse<AppointmentListItemDTO>> CreateReferralSourceAsync(AppointmentListItemDTO dto);
        Task<AppointmentListResponse<AppointmentListItemDTO>> UpdateReferralSourceAsync(int id, AppointmentListItemDTO dto);
    }
}
