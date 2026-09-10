using AlAmalBusiness.Domain.Models.Feedback;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Feedback
{
    public interface IFeedbackHistoryRepo
    {
        // Queued only — the caller saves it together with whatever it changed
        // on the feedback itself, so a timeline entry can never be written
        // without the change it describes.
        void Add(FeedbackHistory history);

        Task<List<FeedbackHistory>> GetByFeedbackAsync(int feedbackId);
    }
}
