using AlAmalBusiness.Domain.Models.Attendance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Attendance
{
    public interface IAttendanceCodeRepo
    {
        Task<List<AttendanceCode>> GetAllAsync();

        // Tracked.
        Task<AttendanceCode?> GetByIdAsync(int id);
        Task<bool> IsCodeExist(string code, int excludeId);

        Task CreateAsync(AttendanceCode code);
        Task SaveChangesAsync();
    }

    public interface IAttendanceSheetRepo
    {
        // Newest month first, then department order — one query, counts
        // included, no row data.
        Task<List<AttendanceSheetRow>> GetAllAsync(AttendanceSheetQuery query);
        Task<AttendanceSheetRow?> GetRowAsync(int id);

        // Tracked.
        Task<AttendanceSheet?> GetByIdAsync(int id);

        // Which of these departments already have that month open.
        Task<List<int>> GetOpenDepartmentIdsAsync(int year, int month, List<int> departmentIds);

        // The employees of the department's latest sheet before that month,
        // in order — what a newly opened sheet starts with.
        Task<List<(string Number, string Name)>> GetLatestRosterAsync(int departmentId, int year, int month);

        Task<bool> HasEntriesAsync(int id);

        void Add(AttendanceSheet sheet);
        void Remove(AttendanceSheet sheet);
        Task SaveChangesAsync();
    }

    public interface IAttendanceRowRepo
    {
        // The grid: every row of the sheet in order, with its cells.
        Task<List<AttendanceGridRow>> GetGridAsync(int sheetId);

        // Tracked with their entries, for a grid save.
        Task<List<AttendanceRow>> GetForEditAsync(int sheetId);

        // Tracked.
        Task<AttendanceRow?> GetByIdAsync(int id);

        void Add(AttendanceRow row);
        void Remove(AttendanceRow row);
        Task SaveChangesAsync();
    }

    public class AttendanceSheetQuery
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? DepartmentId { get; set; }

        // Set by the service, never by the caller: null = unrestricted
        // (Admin), empty = nothing at all.
        public List<int>? RestrictToDepartmentIds { get; set; }
    }

    public class AttendanceSheetRow
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public decimal RequiredHours { get; set; }
        public int EmployeeCount { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AttendanceGridRow
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<AttendanceCell> Cells { get; set; } = new();
    }

    public class AttendanceCell
    {
        public int Day { get; set; }
        public int CodeId { get; set; }
    }
}
