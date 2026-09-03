using System;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    public class LeadCallResponse
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Note { get; set; }
        public bool IsDone { get; set; }
        public string? ActorName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
