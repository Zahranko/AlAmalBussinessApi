using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Tickets;
using AlAmalBusiness.Application.DTOs.Tickets.Response;
using AlAmalBusiness.Domain.IRepositories.Tickets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Tickets
{
    public interface ITicketService
    {
        Task<TicketFormOptionsResponse> GetFormOptionsAsync();

        // Saves the ticket, then queues the email to the support team — or, for
        // an Insurance ticket, to the insurance desk (best-effort).
        Task<TicketActionResponse> CreateAsync(CreateTicketDTO request, TicketActor actor);

        // ---------- lists ----------

        // The queue: the support team's tickets and/or the insurance desk's,
        // whichever the actor works, narrowed by the scope tab.
        Task<PagedResultDTO<TicketListItemResponse>> GetQueueAsync(TicketListQuery query, TicketQueueScope scope, TicketActor actor);

        // The tickets the actor raised, every status, Insurance ones included.
        Task<PagedResultDTO<TicketListItemResponse>> GetCreatedByMeAsync(TicketListQuery query, TicketActor actor);

        // The admin dashboard. No actor: the endpoint is Admin-only, and an
        // admin reads every department and both sides of the queue.
        Task<TicketStatsResponse> GetStatsAsync(TicketStatsQuery query);

        // Null when the ticket doesn't exist or isn't the actor's to see —
        // answered the same way on purpose.
        Task<TicketDetailResponse?> GetDetailAsync(int id, TicketActor actor);

        // ---------- workflow ----------

        Task<TicketActionResponse> CommentAsync(int id, CommentTicketDTO request, TicketActor actor);
        Task<TicketActionResponse> CloseAsync(int id, CloseTicketDTO request, TicketActor actor);
        Task<TicketActionResponse> ReopenAsync(int id, TicketActor actor);
    }

    // The admin side: the three lists behind the New ticket form.
    public interface ITicketListService
    {
        Task<List<TicketListItemDTO>> GetCategoriesAsync();
        Task<TicketListResponse<TicketListItemDTO>> CreateCategoryAsync(TicketListItemDTO dto);
        Task<TicketListResponse<TicketListItemDTO>> UpdateCategoryAsync(int id, TicketListItemDTO dto);

        Task<List<TicketProcedureItemDTO>> GetProceduresAsync();
        Task<TicketListResponse<TicketProcedureItemDTO>> CreateProcedureAsync(TicketProcedureItemDTO dto);
        Task<TicketListResponse<TicketProcedureItemDTO>> UpdateProcedureAsync(int id, TicketProcedureItemDTO dto);
    }
}
