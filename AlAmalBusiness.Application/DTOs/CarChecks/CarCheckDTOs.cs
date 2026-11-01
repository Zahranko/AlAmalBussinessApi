using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.CarChecks
{
    // A car of the managed list — create/update body (IsActive in the same
    // body, no status endpoint).
    public class SaveSecurityCarDTO
    {
        [Required, StringLength(100)]
        public string? Name { get; set; }

        [StringLength(30)]
        public string? PlateNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class SecurityCarResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public bool IsActive { get; set; }
        public int SheetCount { get; set; }
        public int? LatestSheetId { get; set; }
        public int? LatestYear { get; set; }
        public int? LatestMonth { get; set; }
    }

    public class OpenCarCheckSheetDTO
    {
        public int CarId { get; set; }

        [Range(2000, 2100)]
        public int Year { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }
    }

    public class CarCheckSheetResponse
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        // "9-2026", as the paper sheet heads it.
        public string Title { get; set; } = string.Empty;
        public int DaysInMonth { get; set; }
        public int FilledDays { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        // Closed = read-only for a SecurityManager; only an Admin reopens.
        public bool IsClosed { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? ClosedByName { get; set; }
    }

    // One car's sheets, newest month on top.
    public class CarSheetsResponse
    {
        public SecurityCarResponse Car { get; set; } = new();
        public List<CarCheckSheetResponse> Sheets { get; set; } = new();
    }

    // One day of the sheet, request and response alike. Each check is true
    // (✔), false (✘) or null (not filled). A day sent with nothing filled is
    // cleared.
    public class CarCheckDayDTO
    {
        public int Day { get; set; }
        public bool? EngineOil { get; set; }
        public bool? BrakeOil { get; set; }
        public bool? PowerSteeringOil { get; set; }
        public bool? RadiatorWater { get; set; }
        public bool? BodyDamage { get; set; }
        public string? Notes { get; set; }
        public string? EmployeeName { get; set; }
    }

    // The whole sheet in one read: one entry per day of the month, blank days
    // included, so the grid never has to fill gaps.
    public class CarCheckGridResponse
    {
        public CarCheckSheetResponse Sheet { get; set; } = new();
        public List<CarCheckDayDTO> Days { get; set; } = new();
    }

    public class CarCheckResult<T>
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public bool Conflict { get; set; }
        public string? Error { get; set; }
        public T? Item { get; set; }
    }
}
