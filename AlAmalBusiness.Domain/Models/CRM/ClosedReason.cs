using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Domain.Models.CRM
{
    public class ClosedReason
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    }
}
