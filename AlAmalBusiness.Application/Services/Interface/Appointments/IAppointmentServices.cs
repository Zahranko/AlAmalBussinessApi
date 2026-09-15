using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.DTOs.Appointments.Response;
using AlAmalBusiness.Application.DTOs.Feedback;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Appointments
{
    // The anonymous side: what the public appointment page reads and submits.
    public interface IAppointmentService
    {
        Task<AppointmentFormOptionsResponse> GetFormOptionsAsync();

        // Saves the request, then queues the notification email to every
        // active address on the emails list (best-effort).
        Task<AppointmentCreatedResponse> SubmitAsync(CreateAppointmentRequestDTO request, SubmissionContext context);
    }

    // The admin side: the three lists behind the appointment page.
    public interface IAppointmentListService
    {
        Task<List<AppointmentListItemDTO>> GetProceduresAsync();
        Task<AppointmentListResponse<AppointmentListItemDTO>> CreateProcedureAsync(AppointmentListItemDTO dto);
        Task<AppointmentListResponse<AppointmentListItemDTO>> UpdateProcedureAsync(int id, AppointmentListItemDTO dto);

        Task<List<AppointmentListItemDTO>> GetReferralSourcesAsync();
        Task<AppointmentListResponse<AppointmentListItemDTO>> CreateReferralSourceAsync(AppointmentListItemDTO dto);
        Task<AppointmentListResponse<AppointmentListItemDTO>> UpdateReferralSourceAsync(int id, AppointmentListItemDTO dto);

        Task<List<AppointmentEmailDTO>> GetEmailsAsync();
        Task<AppointmentListResponse<AppointmentEmailDTO>> CreateEmailAsync(AppointmentEmailDTO dto);
        Task<AppointmentListResponse<AppointmentEmailDTO>> UpdateEmailAsync(int id, AppointmentEmailDTO dto);
    }
}
