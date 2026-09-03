namespace AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response
{
    public class DoctorResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DoctorDTO? Doctor { get; set; }
    }
}
