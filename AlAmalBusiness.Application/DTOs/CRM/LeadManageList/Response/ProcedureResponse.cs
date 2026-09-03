namespace AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response
{
    public class ProcedureResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public ProcedureDTO? Procedure { get; set; }
    }
}
