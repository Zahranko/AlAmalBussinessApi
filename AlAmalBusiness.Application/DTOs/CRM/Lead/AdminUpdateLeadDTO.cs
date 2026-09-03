using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead
{
    // Admin-only: edits every base field of an existing lead, regardless of
    // status — a data-correction tool, not a workflow action. Every field is
    // authoritative (unlike FollowUpLeadDTO's "null = don't touch" fields).
    public class AdminUpdateLeadDTO
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        public PaymentWays PaymentWay { get; set; }
        public bool HasDoctor { get; set; } = false;
        public int? DoctorId { get; set; }
        [Required]
        public int ReferalId { get; set; }
        [Required]
        public int ProcedureId { get; set; }
    }
}
