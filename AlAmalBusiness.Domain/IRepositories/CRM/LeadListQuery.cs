using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public class LeadListQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? Search { get; set; }
        public LeadStatus? Status { get; set; }
        public string? ClaimedByUserId { get; set; }
        public string? CreatedByUserId { get; set; }
        public int? DoctorId { get; set; }
        public bool UnclaimedOnly { get; set; }
        public bool TodayOnly { get; set; }
        public DateTime? ExactDate { get; set; }

        public bool OnlyCompleted { get; set; }
        public bool ExcludeCompletedByDefault { get; set; }
    }
}
