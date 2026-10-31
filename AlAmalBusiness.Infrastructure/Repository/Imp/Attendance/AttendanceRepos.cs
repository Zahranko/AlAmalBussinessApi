using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.Attendance;
using AlAmalBusiness.Domain.Models.Attendance;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Attendance
{
    public class AttendanceCodeRepo : IAttendanceCodeRepo
    {
        private readonly AppDbContext _context;

        public AttendanceCodeRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<AttendanceCode>> GetAllAsync() =>
            _context.AttendanceCodes.AsNoTracking().OrderBy(c => c.Code).ToListAsync();

        public Task<AttendanceCode?> GetByIdAsync(int id) =>
            _context.AttendanceCodes.FirstOrDefaultAsync(c => c.Id == id);

        public Task<bool> IsCodeExist(string code, int excludeId) =>
            _context.AttendanceCodes.AnyAsync(c => c.Code == code && c.Id != excludeId);

        public async Task CreateAsync(AttendanceCode code)
        {
            _context.AttendanceCodes.Add(code);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }

    public class AttendanceSheetRepo : IAttendanceSheetRepo
    {
        private readonly AppDbContext _context;

        public AttendanceSheetRepo(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<AttendanceSheetRow> Rows(IQueryable<AttendanceSheet> sheets) =>
            sheets.AsNoTracking().Select(s => new AttendanceSheetRow
            {
                Id = s.Id,
                Year = s.Year,
                Month = s.Month,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department!.Name,
                RequiredHours = s.RequiredHours,
                EmployeeCount = s.Rows.Count(),
                CreatedByName = s.CreatedBy!.UserName,
                CreatedAt = s.CreatedAt
            });

        public Task<List<AttendanceSheetRow>> GetAllAsync(AttendanceSheetQuery query)
        {
            var q = _context.AttendanceSheets.AsQueryable();

            // Empty is "nothing", not "no filter" — see AttendanceSheetQuery.
            if (query.RestrictToDepartmentIds is { } readable)
                q = readable.Count == 0
                    ? q.Where(s => false)
                    : q.Where(s => readable.Contains(s.DepartmentId));

            if (query.Year.HasValue) q = q.Where(s => s.Year == query.Year.Value);
            if (query.Month.HasValue) q = q.Where(s => s.Month == query.Month.Value);
            if (query.DepartmentId.HasValue) q = q.Where(s => s.DepartmentId == query.DepartmentId.Value);

            // Departments in the admin's attendance order (Settings ->
            // Attendance), name only to break ties.
            return Rows(q
                    .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
                    .ThenBy(s => s.Department!.AttendanceOrder).ThenBy(s => s.Department!.Name))
                .ToListAsync();
        }

        public Task<AttendanceSheetRow?> GetRowAsync(int id) =>
            Rows(_context.AttendanceSheets.Where(s => s.Id == id)).FirstOrDefaultAsync();

        public Task<AttendanceSheet?> GetByIdAsync(int id) =>
            _context.AttendanceSheets.FirstOrDefaultAsync(s => s.Id == id);

        public Task<List<int>> GetOpenDepartmentIdsAsync(int year, int month, List<int> departmentIds) =>
            _context.AttendanceSheets
                .Where(s => s.Year == year && s.Month == month && departmentIds.Contains(s.DepartmentId))
                .Select(s => s.DepartmentId)
                .ToListAsync();

        public async Task<List<(string Number, string Name)>> GetLatestRosterAsync(int departmentId, int year, int month)
        {
            var key = year * 12 + month;
            var latestId = await _context.AttendanceSheets
                .Where(s => s.DepartmentId == departmentId && s.Year * 12 + s.Month < key)
                .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
            if (latestId == null) return new();

            var rows = await _context.AttendanceRows.AsNoTracking()
                .Where(r => r.SheetId == latestId.Value)
                .OrderBy(r => r.DisplayOrder).ThenBy(r => r.Id)
                .Select(r => new { r.EmployeeNumber, r.EmployeeName })
                .ToListAsync();

            return rows.Select(r => (r.EmployeeNumber, r.EmployeeName)).ToList();
        }

        public Task<bool> HasEntriesAsync(int id) =>
            _context.AttendanceEntries.AnyAsync(e => e.Row!.SheetId == id);

        public void Add(AttendanceSheet sheet) => _context.AttendanceSheets.Add(sheet);
        public void Remove(AttendanceSheet sheet) => _context.AttendanceSheets.Remove(sheet);
        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }

    public class AttendanceRowRepo : IAttendanceRowRepo
    {
        private readonly AppDbContext _context;

        public AttendanceRowRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<AttendanceGridRow>> GetGridAsync(int sheetId) =>
            _context.AttendanceRows.AsNoTracking()
                .Where(r => r.SheetId == sheetId)
                .OrderBy(r => r.DisplayOrder).ThenBy(r => r.Id)
                .Select(r => new AttendanceGridRow
                {
                    Id = r.Id,
                    EmployeeNumber = r.EmployeeNumber,
                    EmployeeName = r.EmployeeName,
                    DisplayOrder = r.DisplayOrder,
                    Cells = r.Entries.Select(e => new AttendanceCell { Day = e.Day, CodeId = e.CodeId }).ToList()
                })
                .AsSplitQuery()
                .ToListAsync();

        // A sheet is a department's staff (tens of rows, ~30 cells each), so
        // loading all of it for a save is cheaper than a query per row.
        public Task<List<AttendanceRow>> GetForEditAsync(int sheetId) =>
            _context.AttendanceRows
                .Where(r => r.SheetId == sheetId)
                .Include(r => r.Entries)
                .AsSplitQuery()
                .ToListAsync();

        public Task<AttendanceRow?> GetByIdAsync(int id) =>
            _context.AttendanceRows.FirstOrDefaultAsync(r => r.Id == id);

        public void Add(AttendanceRow row) => _context.AttendanceRows.Add(row);
        public void Remove(AttendanceRow row) => _context.AttendanceRows.Remove(row);
        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
