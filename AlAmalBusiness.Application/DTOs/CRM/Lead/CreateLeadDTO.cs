using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead
{
    public class CreateLeadDTO
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        [Required]
        public string? Description { get; set; }
        public PaymentWays? PaymentWay { get; set; }
        public bool HasDoctor { get; set; } = false;
        public int? DoctorId { get; set; }
        [Required]
        public int ReferalId { get; set; }
        [Required]
        public int ProcedureId { get; set; }
    }
}
