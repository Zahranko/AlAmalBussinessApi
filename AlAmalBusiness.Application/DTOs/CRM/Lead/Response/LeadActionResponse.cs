namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    // Result wrapper for actions that return the full lead detail (claim,
    // follow-up, reopen, admin full-edit) — mirrors CreateLeadResponse's shape.
    public class LeadActionResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public LeadDetailResponse? Lead { get; set; }
    }
}
