using System;

namespace AlAmalBusiness.Application.DTOs.CRM.Stats
{
    // What gets remembered for a user's last Hospital-Manager date-range
    // request — restored when they hit stats/export again with no query string.
    public class HospitalManagerFilterCacheDTO
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
