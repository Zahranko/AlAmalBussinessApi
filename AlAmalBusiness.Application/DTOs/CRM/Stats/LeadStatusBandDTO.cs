using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.CRM.Stats
{
    public class LeadStatusBandDTO
    {
        public int Total { get; set; }
        public List<LeadStatusCountDTO> Statuses { get; set; } = new();
    }

    public class LeadStatusCountDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
