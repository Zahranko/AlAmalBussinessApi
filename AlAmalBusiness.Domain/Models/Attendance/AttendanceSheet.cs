using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Attendance
{
    // One department's attendance for one month: a row per employee, a cell
    // per day holding a code. Unique per (Year, Month, DepartmentId) — a
    // month is opened for several departments at once, one sheet each.
    public class AttendanceSheet
    {
        [Key]
        public int Id { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        // The shared Departments lookup, so scoping is a plain column
        // comparison against the token, as in feedback.
        public int DepartmentId { get; set; }
        public Departments? Department { get; set; }

        // The month's target. Each row's shortage is its total minus this.
        public decimal RequiredHours { get; set; } = 208;

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;

        public List<AttendanceRow> Rows { get; set; } = new();
    }

    // One employee on one sheet. Number and name are typed (or carried over
    // from the department's previous sheet), not an FK: most staff have no
    // account here.
    public class AttendanceRow
    {
        [Key]
        public int Id { get; set; }

        public int SheetId { get; set; }
        public AttendanceSheet? Sheet { get; set; }

        [MaxLength(20)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string EmployeeName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;
        public DateTime? UpdatedAt { get; set; }

        public List<AttendanceEntry> Entries { get; set; } = new();
    }

    // One filled cell: the code on that day. A blank day has no row.
    public class AttendanceEntry
    {
        public int RowId { get; set; }
        public AttendanceRow? Row { get; set; }

        // 1..days in the sheet's month.
        public int Day { get; set; }

        public int CodeId { get; set; }
        public AttendanceCode? Code { get; set; }
    }
}
