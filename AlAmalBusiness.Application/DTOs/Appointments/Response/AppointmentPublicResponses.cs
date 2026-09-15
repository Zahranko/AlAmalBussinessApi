using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Appointments.Response
{
    // Both pickers the public page needs, in one round trip. Active entries
    // only, and only id + name — nothing an anonymous caller doesn't need.
    public class AppointmentFormOptionsResponse
    {
        public List<AppointmentOptionResponse> Procedures { get; set; } = new();
        public List<AppointmentOptionResponse> ReferralSources { get; set; } = new();
    }

    public class AppointmentOptionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // What an anonymous submit returns — never the stored record.
    public class AppointmentCreatedResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
