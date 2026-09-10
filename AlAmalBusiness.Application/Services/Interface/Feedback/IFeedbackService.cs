using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Feedback.Response;
using AlAmalBusiness.Domain.IRepositories.Feedback;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Feedback
{
    public interface IFeedbackService
    {
        // Anonymous — the public patient form.
        Task<FeedbackCreatedResponse> SubmitAsync(CreateFeedbackDTO request, SubmissionContext context);

        Task<PagedResultDTO<FeedbackListItemResponse>> GetPagedAsync(FeedbackListQuery query, FeedbackActor actor);

        // Null when the message doesn't exist or sits outside the caller's
        // department — the two are answered the same way on purpose.
        Task<FeedbackDetailResponse?> GetDetailAsync(int id, FeedbackActor actor);

        Task<FeedbackDetailResponse?> GetByReferenceAsync(string referenceNumber, FeedbackActor actor);

        Task<FeedbackActionResponse> ChangeStatusAsync(int id, UpdateFeedbackStatusDTO request, FeedbackActor actor);

        Task<FeedbackActionResponse> AssignAsync(int id, AssignFeedbackDTO request, FeedbackActor actor);

        // Moves a misrouted message to another department. Carries no record
        // back: the mover is normally scoped to their own department, so the
        // moment it leaves they can no longer read it.
        Task<FeedbackActionResponse> ForwardAsync(int id, ForwardFeedbackDTO request, FeedbackActor actor);

        Task<FeedbackActionResponse> AddNoteAsync(int id, AddFeedbackNoteDTO request, FeedbackActor actor);

        // The manager/admin dashboard. Scoped the same way the inbox is: a
        // manager gets their own department's numbers, an admin every
        // department's, optionally narrowed to one.
        Task<FeedbackStatsResponse> GetStatsAsync(FeedbackStatsQuery query, FeedbackActor actor);
    }
}
