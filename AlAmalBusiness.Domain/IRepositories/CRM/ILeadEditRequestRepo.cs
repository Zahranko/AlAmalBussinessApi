using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models.CRM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface ILeadEditRequestRepo
    {
        void Add(LeadEditRequest request);
        Task<LeadEditRequest?> GetTrackedAsync(int id);
        Task<LeadEditRequestRow?> GetRowAsync(int id);
        Task<int?> GetPendingIdForLeadAsync(int leadId);
        Task<(List<LeadEditRequestRow> Items, int Total)> PageAsync(
            LeadEditRequestStatus? status, string? requestedById, int page, int pageSize);
        Task<int> CountPendingAsync();
        Task SaveChangesAsync();
    }

    // Projected for lists and the review screen: the request's proposed
    // values with their lookup names, and the lead's current values beside
    // them, so the diff needs no second query.
    public class LeadEditRequestRow
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public string? LeadName { get; set; }
        public LeadStatus LeadStatus { get; set; }
        public bool LeadDeleted { get; set; }
        public string? RequestedById { get; set; }
        public string? RequestedByName { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? Note { get; set; }
        public LeadEditRequestStatus Status { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }

        public LeadEditValues Proposed { get; set; } = new();
        public LeadEditValues Current { get; set; } = new();
    }

    public class LeadEditValues
    {
        public string? Name { get; set; }
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        public string? Description { get; set; }
        public PaymentWays? PaymentWay { get; set; }
        public int? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public int ReferalId { get; set; }
        public string? ReferalName { get; set; }
        public int ProcedureId { get; set; }
        public string? ProcedureName { get; set; }
    }
}
