using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Mammograms
{
    // A saved custom report over the register (added 2026-10-19): the
    // console's report builder — filters, counts and breakdowns on any
    // column, the COUNTIFS/pivot work the team did in Excel. The definition
    // is the builder's own JSON, opaque here: the counting happens in the
    // browser over the rows of the chosen period, so the API only stores and
    // hands back what was built. Shared by everyone with the role.
    public class MammogramReport
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Definition { get; set; } = "{}";

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = AppClock.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
