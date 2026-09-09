namespace AlAmalBusiness.Application.DTOs.CRM.Lead
{
    // What gets remembered for a user's last Lead list request — restored
    // verbatim when they hit the endpoint again with no query string.
    public class LeadFilterCacheDTO
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Scope { get; set; }
        public int? DoctorId { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? ClaimedByUserId { get; set; }
        // A single day to restrict the list to, matched against CreatedDate
        // (LeadListQuery.ExactDate). Time component is ignored.
        public DateTime? Date { get; set; }
    }
}
