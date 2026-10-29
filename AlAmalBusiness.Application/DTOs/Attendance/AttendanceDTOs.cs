using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Attendance
{
    // One code of the attendance codes list — create/update body and response
    // alike, IsActive in the same body (no status endpoint).
    public class AttendanceCodeDTO
    {
        public int Id { get; set; }

        [Required, StringLength(10)]
        public string? Code { get; set; }

        [StringLength(100)]
        public string? Name { get; set; }

        [Range(0, 24)]
        public decimal Hours { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // Opens one month for several departments at once: one sheet each. A
    // department already open for that month is skipped and named back.
    public class OpenAttendanceSheetsDTO
    {
        [Range(2000, 2100)]
        public int Year { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        [Required, MinLength(1)]
        public List<int>? DepartmentIds { get; set; }

        // Defaults to 208.
        [Range(1, 744)]
        public decimal? RequiredHours { get; set; }
    }

    public class OpenAttendanceSheetsResponse
    {
        public List<AttendanceSheetResponse> Opened { get; set; } = new();

        // Departments that already had this month open.
        public List<string> AlreadyOpen { get; set; } = new();
    }

    public class UpdateAttendanceSheetDTO
    {
        [Range(1, 744)]
        public decimal RequiredHours { get; set; }
    }

    public class AttendanceSheetResponse
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        // "10-2026".
        public string Title { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int DaysInMonth { get; set; }
        public decimal RequiredHours { get; set; }
        public int EmployeeCount { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // The whole grid in one read: the sheet, the codes its dropdowns offer
    // (every active code plus any retired one still on a day), and the rows.
    public class AttendanceGridResponse
    {
        public AttendanceSheetResponse Sheet { get; set; } = new();
        public List<AttendanceCodeDTO> Codes { get; set; } = new();
        public List<AttendanceRowResponse> Rows { get; set; } = new();
    }

    public class AttendanceRowResponse
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        // One entry per day of the month, index 0 = day 1: the code id, or
        // null for a blank day.
        public List<int?> Days { get; set; } = new();

        // The sum of every day's code hours.
        public decimal TotalHours { get; set; }

        // TotalHours - RequiredHours: negative while short of the target.
        public decimal Shortage { get; set; }
    }

    // One row of a grid save: Id for an existing row, none for a new one.
    // ClientKey is echoed back so the result can be matched to the row that
    // sent it. Days is a full replace of the row's cells, index 0 = day 1;
    // a shorter list leaves the remaining days blank.
    //
    // No data annotations, as in the mammogram grid: AttendanceService checks
    // every rule so one bad row can't 400 the whole batch.
    public class SaveAttendanceRowDTO
    {
        public int? Id { get; set; }
        public string? ClientKey { get; set; }
        public string? EmployeeNumber { get; set; }
        public string? EmployeeName { get; set; }
        public int? DisplayOrder { get; set; }
        public List<int?>? Days { get; set; }
    }

    public class AttendanceRowResult
    {
        public string? ClientKey { get; set; }
        public int? Id { get; set; }
        public AttendanceRowResponse? Row { get; set; }
        public string? Error { get; set; }
    }

    // Built from the caller's token in the controller, never from the body.
    public record AttendanceActor(
        string UserId,
        // Admin alone: every department.
        bool CanViewAll,
        int DepartmentId,
        IReadOnlyList<int> DepartmentIds);

    // {Success, NotFound, Conflict, Error, Item}: 404 / 409 / 400 / 200.
    public class AttendanceResult<T>
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public bool Conflict { get; set; }
        public string? Error { get; set; }
        public T? Item { get; set; }
    }
}
