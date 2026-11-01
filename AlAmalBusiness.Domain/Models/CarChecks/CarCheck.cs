using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.CarChecks
{
    // One of the hospital's vehicles the security team checks daily (the
    // ambulance first). A managed list: retired with IsActive, never deleted
    // from under its sheets.
    public class SecurityCar
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PlateNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = AppClock.Now;

        public List<CarCheckSheet> Sheets { get; set; } = new();
    }

    // One car's check sheet for one month — the paper "جدول تشييك سيارة":
    // a row per day of the month. Unique per (CarId, Year, Month).
    public class CarCheckSheet
    {
        [Key]
        public int Id { get; set; }

        public int CarId { get; set; }
        public SecurityCar? Car { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;

        // A closed month is read-only for everyone but an Admin, who alone
        // may reopen it. Any SecurityManager may close one.
        public bool IsClosed { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? ClosedById { get; set; }
        public User? ClosedBy { get; set; }

        public List<CarCheckDay> Days { get; set; } = new();
    }

    // One filled day. Each check is ✔ (true), ✘ (false) or not filled
    // (null); a day with nothing filled has no row at all.
    public class CarCheckDay
    {
        public int SheetId { get; set; }
        public CarCheckSheet? Sheet { get; set; }

        // 1..days in the sheet's month.
        public int Day { get; set; }

        // زيت الماتور
        public bool? EngineOil { get; set; }
        // زيت البريك
        public bool? BrakeOil { get; set; }
        // زيت البور
        public bool? PowerSteeringOil { get; set; }
        // مياه الرديتر
        public bool? RadiatorWater { get; set; }
        // خدوش او ضربات خارجية
        public bool? BodyDamage { get; set; }

        // ملاحظات
        [MaxLength(500)]
        public string? Notes { get; set; }

        // اسم الموظف — typed, not an FK: the guards who do the check mostly
        // have no account here.
        [MaxLength(100)]
        public string? EmployeeName { get; set; }

        public string? UpdatedById { get; set; }
        public User? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = AppClock.Now;
    }
}
