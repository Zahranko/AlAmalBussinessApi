using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.CRM.LeadEditRequest
{
    // What an employee proposes: every base field of the lead, the same set
    // and the same lengths as AdminUpdateLeadDTO, so an approval can be applied
    // by exactly the code an admin's own edit goes through.
    public class CreateLeadEditRequestDTO
    {
        [Required, StringLength(200)]
        public string? Name { get; set; }
        [Required, StringLength(10)]
        public string? CountryKey { get; set; }
        [StringLength(32)]
        public string? PhoneNum { get; set; }
        [StringLength(100)]
        public string? NickName { get; set; }
        [Required, StringLength(8000)]
        public string? Description { get; set; }
        public PaymentWays? PaymentWay { get; set; }
        public int? DoctorId { get; set; }
        [Required]
        public int ReferalId { get; set; }
        [Required]
        public int ProcedureId { get; set; }

        // Why — shown to the admin beside the changes.
        [StringLength(500)]
        public string? Note { get; set; }
    }

    public class ReviewLeadEditRequestDTO
    {
        [StringLength(500)]
        public string? Note { get; set; }
    }

    public class LeadEditChangeResponse
    {
        // Stable key for a client to translate ("name", "phone", "doctor"...).
        public string Field { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? From { get; set; }
        public string? To { get; set; }
    }

    public class LeadEditRequestResponse
    {
        public int Id { get; set; }
        public int LeadId { get; set; }
        public string? LeadName { get; set; }
        public LeadStatus LeadStatus { get; set; }
        public string? RequestedByName { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? Note { get; set; }
        public LeadEditRequestStatus Status { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }

        // Proposed vs the lead as it is now. Empty on a pending request means
        // the lead already matches (someone made the same correction since).
        public List<LeadEditChangeResponse> Changes { get; set; } = new();
    }
}
