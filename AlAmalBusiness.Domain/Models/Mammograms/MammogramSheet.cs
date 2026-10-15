using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Mammograms
{
    // One month of the mammogram follow-up register — the "9-2026" tab of the
    // Excel workbook it replaces. There is exactly one per (Year, Month),
    // enforced by a unique index, and every record in it is a visit made in
    // that month.
    public class MammogramSheet
    {
        [Key]
        public int Id { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;

        public List<MammogramRecord> Records { get; set; } = new();
    }
}
