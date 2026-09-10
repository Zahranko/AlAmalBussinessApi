using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.Feedback;
using AlAmalBusiness.Domain.Models.Feedback;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Feedback
{
    public class FeedbackHistoryRepo : IFeedbackHistoryRepo
    {
        private readonly AppDbContext _context;

        public FeedbackHistoryRepo(AppDbContext context)
        {
            _context = context;
        }

        // Queued against the same context the feedback itself was read from,
        // so IPatientFeedbackRepo.SaveChangesAsync commits both together.
        public void Add(FeedbackHistory history) => _context.FeedbackHistories.Add(history);

        public Task<List<FeedbackHistory>> GetByFeedbackAsync(int feedbackId) =>
            _context.FeedbackHistories
                .Include(h => h.Actor)
                .AsNoTracking()
                .Where(h => h.FeedbackId == feedbackId)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();
    }
}
