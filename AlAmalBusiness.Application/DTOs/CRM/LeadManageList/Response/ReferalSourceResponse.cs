namespace AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response
{
    public class ReferalSourceResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public ReferalSourceDTO? ReferalSource { get; set; }
    }
}
