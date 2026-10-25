using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Tickets;
using AlAmalBusiness.Application.DTOs.Tickets.Response;
using AlAmalBusiness.Domain.IRepositories.Tickets;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Tickets
{
    public interface IMaintenanceTicketService
    {
        // Saves the request, then queues the email to the maintenance team
        // (best-effort).
        Task<MaintenanceTicketActionResponse> CreateAsync(CreateMaintenanceTicketDTO request, MaintenanceTicketActor actor);

        // The anonymous public page: validates, saves with no account behind
        // it, then the same email. Answers the request number only.
        Task<MaintenancePublicSubmitResponse> SubmitPublicAsync(PublicMaintenanceTicketDTO request, SubmissionContext context);

        // The queue: every request for the maintenance team, the caller's own
        // for anyone else, narrowed by the scope tab.
        Task<PagedResultDTO<MaintenanceTicketListItemResponse>> GetQueueAsync(
            MaintenanceTicketListQuery query, MaintenanceQueueScope scope, MaintenanceTicketActor actor);

        Task<PagedResultDTO<MaintenanceTicketListItemResponse>> GetCreatedByMeAsync(
            MaintenanceTicketListQuery query, MaintenanceTicketActor actor);

        // Null when it doesn't exist or isn't the actor's to see.
        Task<MaintenanceTicketDetailResponse?> GetDetailAsync(int id, MaintenanceTicketActor actor);

        Task<MaintenanceTicketActionResponse> CommentAsync(int id, CommentTicketDTO request, MaintenanceTicketActor actor);
        Task<MaintenanceTicketActionResponse> CloseAsync(int id, CloseTicketDTO request, MaintenanceTicketActor actor);
        Task<MaintenanceTicketActionResponse> ReopenAsync(int id, MaintenanceTicketActor actor);
    }
}
