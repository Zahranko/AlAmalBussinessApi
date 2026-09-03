using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead
{
    public class LeadCallDTO
    {
        [Required]
        public DateTime Date { get; set; }

        [MaxLength(2000)]
        public string? Note { get; set; }
    }
}
