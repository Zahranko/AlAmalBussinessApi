namespace AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response
{
    public class ClosedReasonResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public ClosedReasonDTO? ClosedReason { get; set; }
    }
}
