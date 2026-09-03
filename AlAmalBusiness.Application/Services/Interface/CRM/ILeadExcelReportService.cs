using AlAmalBusiness.Application.DTOs.CRM.Stats;

namespace AlAmalBusiness.Application.Services.Interface.CRM
{
    public interface ILeadExcelReportService
    {
        byte[] Build(HospitalManagerStatsDTO stats);
        byte[] BuildDoctorLeads(DoctorLeadExportDTO export);
    }
}
