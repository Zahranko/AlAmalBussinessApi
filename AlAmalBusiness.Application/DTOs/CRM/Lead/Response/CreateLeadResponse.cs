namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    public class CreateLeadResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public LeadListItemResponse? Lead { get; set; }
    }
}
