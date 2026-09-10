using AlAmalBusiness.Domain.Models.Feedback;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Feedback
{
    public interface IPatientFeedbackRepo
    {
        // Tracked, for the workflow actions to mutate.
        Task<PatientFeedback?> GetByIdAsync(int id);

        // Detail view — the message plus its department and assignee names.
        Task<PatientFeedback?> GetDetailAsync(int id);

        Task<PatientFeedback?> GetByReferenceAsync(string referenceNumber);

        Task<bool> IsReferenceExist(string referenceNumber);

        Task<(List<FeedbackListRow> Items, int TotalCount)> PageFeedbacksAsync(FeedbackListQuery query);

        // The dashboard's numbers, aggregated in SQL — never a page-through.
        Task<FeedbackStatsRows> GetStatsAsync(FeedbackStatsQuery query);

        Task<PatientFeedback> CreateAsync(PatientFeedback feedback);

        Task SaveChangesAsync();
    }
}
