using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead
{
    public class CreateLeadDTO
    {
        // Lengths match the Leads columns (AppDbContext), so an over-long value
        // is a 400 here instead of a truncation 500 from the database.
        [Required]
        [StringLength(200)]
        public string? Name { get; set; }
        [Required]
        [StringLength(10)]
        public string? CountryKey { get; set; }
        [StringLength(32)]
        public string? PhoneNum { get; set; }
        [StringLength(100)]
        public string? NickName { get; set; }
        [Required]
        [StringLength(8000)]
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
