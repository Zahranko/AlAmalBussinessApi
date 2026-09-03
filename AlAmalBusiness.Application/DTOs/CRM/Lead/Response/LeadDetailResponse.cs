using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    public class LeadDetailResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        public string? Description { get; set; }
        public LeadStatus Status { get; set; }
        public PaymentWays? PaymentWay { get; set; }
        public bool HasDoctor { get; set; }
        public int? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? ClinicSignature { get; set; }
        public int ReferalId { get; set; }
        public string? ReferalName { get; set; }
        public int ProcedureId { get; set; }
        public string? ProcedureName { get; set; }
        public string? CreatedByName { get; set; }
        public string? ClaimedByName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ClosedReason { get; set; }
        public List<LeadHistoryResponse> History { get; set; } = new();
        public List<LeadCallResponse> Calls { get; set; } = new();
    }
}
