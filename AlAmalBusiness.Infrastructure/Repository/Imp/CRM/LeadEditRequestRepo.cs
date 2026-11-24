using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CRM
{
    public class LeadEditRequestRepo : ILeadEditRequestRepo
    {
        private readonly AppDbContext _context;

        public LeadEditRequestRepo(AppDbContext context)
        {
            _context = context;
        }

        public void Add(LeadEditRequest request) => _context.LeadEditRequests.Add(request);

        public Task<LeadEditRequest?> GetTrackedAsync(int id) =>
            _context.LeadEditRequests.FirstOrDefaultAsync(r => r.Id == id);

        public Task<LeadEditRequestRow?> GetRowAsync(int id) =>
            Rows().Where(r => r.Id == id).FirstOrDefaultAsync();

        public Task<int?> GetPendingIdForLeadAsync(int leadId) =>
            _context.LeadEditRequests
                .Where(r => r.LeadId == leadId && r.Status == LeadEditRequestStatus.Pending)
                .OrderByDescending(r => r.Id)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();

        public async Task<(List<LeadEditRequestRow> Items, int Total)> PageAsync(
            LeadEditRequestStatus? status, string? requestedById, int page, int pageSize)
        {
            var q = Rows();
            if (status.HasValue) q = q.Where(r => r.Status == status.Value);
            if (requestedById != null) q = q.Where(r => r.RequestedById == requestedById);

            var total = await q.CountAsync();
            // Pending: oldest first, so nobody's request waits behind newer
            // ones. Decided: newest first, the way a history reads.
            var ordered = status == LeadEditRequestStatus.Pending
                ? q.OrderBy(r => r.RequestedAt).ThenBy(r => r.Id)
                : q.OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Id);
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        // A request on a lead that was deleted since doesn't count: it can't
        // be approved until the lead is restored.
        public Task<int> CountPendingAsync() =>
            _context.LeadEditRequests.CountAsync(r => r.Status == LeadEditRequestStatus.Pending && !r.Lead!.IsDeleted);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        // One projection for every read: no Include, no AspNetUsers rows.
        // Leads' global query filter makes the join to a deleted lead drop the
        // row, which is the wanted outcome — a deleted lead has nothing to edit.
        private IQueryable<LeadEditRequestRow> Rows() =>
            _context.LeadEditRequests
                .AsNoTracking()
                .Select(r => new LeadEditRequestRow
                {
                    Id = r.Id,
                    LeadId = r.LeadId,
                    LeadName = r.Lead!.Name,
                    LeadStatus = r.Lead!.Status,
                    LeadDeleted = r.Lead!.IsDeleted,
                    RequestedById = r.RequestedById,
                    RequestedByName = r.RequestedBy!.FullName ?? r.RequestedBy!.UserName,
                    RequestedAt = r.RequestedAt,
                    Note = r.Note,
                    Status = r.Status,
                    ReviewedByName = r.ReviewedBy!.FullName ?? r.ReviewedBy!.UserName,
                    ReviewedAt = r.ReviewedAt,
                    ReviewNote = r.ReviewNote,
                    Proposed = new LeadEditValues
                    {
                        Name = r.Name,
                        CountryKey = r.CountryKey,
                        PhoneNum = r.PhoneNum,
                        NickName = r.NickName,
                        Description = r.Description,
                        PaymentWay = r.PaymentWay,
                        DoctorId = r.DoctorId,
                        DoctorName = r.Doctor!.Name,
                        ReferalId = r.ReferalId,
                        ReferalName = r.Referal!.Name,
                        ProcedureId = r.ProcedureId,
                        ProcedureName = r.Procedure!.Name,
                    },
                    Current = new LeadEditValues
                    {
                        Name = r.Lead!.Name,
                        CountryKey = r.Lead!.CountryKey,
                        PhoneNum = r.Lead!.PhoneNum,
                        NickName = r.Lead!.NickName,
                        Description = r.Lead!.Description,
                        PaymentWay = r.Lead!.PaymentWay,
                        DoctorId = r.Lead!.DoctorId,
                        DoctorName = r.Lead!.Doctor!.Name,
                        ReferalId = r.Lead!.ReferalId,
                        ReferalName = r.Lead!.Referal!.Name,
                        ProcedureId = r.Lead!.ProcedureId,
                        ProcedureName = r.Lead!.Procedure!.Name,
                    },
                });
    }
}
