using AlAmalBusiness.Application.DTOs.Attendance;
using AlAmalBusiness.Application.Services.Interface.Attendance;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.Attendance;
using AlAmalBusiness.Domain.Models.Attendance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Attendance
{
    // Monthly attendance sheets: one per department per month, a row per
    // employee, a code per day. Each row's total is the sum of its codes'
    // hours and its shortage is that total minus the sheet's RequiredHours
    // (208 unless the sheet says otherwise).
    //
    // Scoping is the feedback rule: Admin reads every department, everyone
    // else the departments on their own token, and a sheet outside that set
    // answers 404, not 403.
    public class AttendanceService : IAttendanceService
    {
        private const decimal DefaultRequiredHours = 208;

        // A department is tens of people; this bounds one request.
        private const int MaxRowsPerSave = 1000;

        private readonly IAttendanceSheetRepo _sheets;
        private readonly IAttendanceRowRepo _rows;
        private readonly IAttendanceCodeRepo _codes;
        private readonly IDepartmentRepo _departments;

        public AttendanceService(
            IAttendanceSheetRepo sheets,
            IAttendanceRowRepo rows,
            IAttendanceCodeRepo codes,
            IDepartmentRepo departments)
        {
            _sheets = sheets;
            _rows = rows;
            _codes = codes;
            _departments = departments;
        }

        // ---------- sheets ----------

        public async Task<List<AttendanceSheetResponse>> GetSheetsAsync(int? year, int? month, int? departmentId, AttendanceActor actor)
        {
            var rows = await _sheets.GetAllAsync(new AttendanceSheetQuery
            {
                Year = year,
                Month = month,
                DepartmentId = departmentId,
                RestrictToDepartmentIds = VisibleDepartments(actor)
            });
            return rows.Select(ToResponse).ToList();
        }

        public async Task<AttendanceResult<OpenAttendanceSheetsResponse>> OpenSheetsAsync(OpenAttendanceSheetsDTO dto, AttendanceActor actor)
        {
            if (dto.Month is < 1 or > 12) return Failed<OpenAttendanceSheetsResponse>("Month must be between 1 and 12.");
            if (dto.Year is < 2000 or > 2100) return Failed<OpenAttendanceSheetsResponse>("Year isn't valid.");

            var required = dto.RequiredHours ?? DefaultRequiredHours;
            if (required is <= 0 or > 744) return Failed<OpenAttendanceSheetsResponse>("Required hours must be between 1 and 744.");

            var ids = (dto.DepartmentIds ?? new()).Distinct().ToList();
            if (ids.Count == 0) return Failed<OpenAttendanceSheetsResponse>("Pick at least one department.");

            // A department outside the caller's reach reads the same as one
            // that doesn't exist — the 404 rule, in words.
            var departments = (await _departments.GetActiveDepartmentsAsync()).ToDictionary(d => d.Id);
            foreach (var id in ids)
                if (!departments.ContainsKey(id) || !CanSee(id, actor))
                    return Failed<OpenAttendanceSheetsResponse>($"Department {id} doesn't exist or isn't one of yours.");

            var open = await _sheets.GetOpenDepartmentIdsAsync(dto.Year, dto.Month, ids);
            var toOpen = ids.Where(id => !open.Contains(id)).ToList();

            if (toOpen.Count == 0)
                return Conflicted<OpenAttendanceSheetsResponse>($"{Title(dto.Year, dto.Month)} is already open for every department picked.");

            var created = new List<AttendanceSheet>();
            foreach (var departmentId in toOpen)
            {
                var sheet = new AttendanceSheet
                {
                    Year = dto.Year,
                    Month = dto.Month,
                    DepartmentId = departmentId,
                    RequiredHours = required,
                    CreatedById = actor.UserId,
                    CreatedAt = AppClock.Now
                };

                // The department's staff carry over from its previous sheet,
                // so a month starts with its names and only the days to fill.
                var roster = await _sheets.GetLatestRosterAsync(departmentId, dto.Year, dto.Month);
                var order = 0;
                foreach (var (number, name) in roster)
                    sheet.Rows.Add(new AttendanceRow
                    {
                        EmployeeNumber = number,
                        EmployeeName = name,
                        DisplayOrder = ++order,
                        CreatedAt = AppClock.Now
                    });

                _sheets.Add(sheet);
                created.Add(sheet);
            }
            await _sheets.SaveChangesAsync();

            var opened = new List<AttendanceSheetResponse>();
            foreach (var sheet in created)
                opened.Add(ToResponse((await _sheets.GetRowAsync(sheet.Id))!));

            return Ok(new OpenAttendanceSheetsResponse
            {
                Opened = opened,
                AlreadyOpen = open.Select(id => departments[id].Name ?? id.ToString()).ToList()
            });
        }

        public async Task<AttendanceResult<AttendanceSheetResponse>> UpdateSheetAsync(int id, UpdateAttendanceSheetDTO dto, AttendanceActor actor)
        {
            var sheet = await _sheets.GetByIdAsync(id);
            if (sheet == null || !CanSee(sheet.DepartmentId, actor)) return Missing<AttendanceSheetResponse>();
            if (dto.RequiredHours is <= 0 or > 744) return Failed<AttendanceSheetResponse>("Required hours must be between 1 and 744.");

            sheet.RequiredHours = dto.RequiredHours;
            await _sheets.SaveChangesAsync();
            return Ok(ToResponse((await _sheets.GetRowAsync(id))!));
        }

        // A sheet opened for the wrong month or department by mistake. Once a
        // day is filled it is the record and stays.
        public async Task<AttendanceResult<bool>> DeleteSheetAsync(int id, AttendanceActor actor)
        {
            var sheet = await _sheets.GetByIdAsync(id);
            if (sheet == null || !CanSee(sheet.DepartmentId, actor)) return Missing<bool>();
            if (await _sheets.HasEntriesAsync(id))
                return Conflicted<bool>("Only a sheet with no days filled can be deleted — clear its days first.");

            _sheets.Remove(sheet);
            await _sheets.SaveChangesAsync();
            return Ok(true);
        }

        // ---------- grid ----------

        public async Task<AttendanceGridResponse?> GetGridAsync(int id, AttendanceActor actor)
        {
            var sheet = await _sheets.GetRowAsync(id);
            if (sheet == null || !CanSee(sheet.DepartmentId, actor)) return null;

            var codes = (await _codes.GetAllAsync()).ToDictionary(c => c.Id);
            var rows = await _rows.GetGridAsync(id);
            var days = DateTime.DaysInMonth(sheet.Year, sheet.Month);

            var used = rows.SelectMany(r => r.Cells).Select(c => c.CodeId).ToHashSet();

            return new AttendanceGridResponse
            {
                Sheet = ToResponse(sheet),
                Codes = codes.Values
                    .Where(c => c.IsActive || used.Contains(c.Id))
                    .Select(AttendanceCodeService.ToDto)
                    .ToList(),
                Rows = rows.Select(r => ToRow(r.Id, r.EmployeeNumber, r.EmployeeName, r.DisplayOrder,
                    r.Cells.Select(c => (c.Day, c.CodeId)), days, sheet.RequiredHours, codes)).ToList()
            };
        }

        public async Task<AttendanceResult<List<AttendanceRowResult>>> SaveRowsAsync(int sheetId, List<SaveAttendanceRowDTO> rows, AttendanceActor actor)
        {
            var sheet = await _sheets.GetByIdAsync(sheetId);
            if (sheet == null || !CanSee(sheet.DepartmentId, actor)) return Missing<List<AttendanceRowResult>>();
            if (rows.Count == 0) return Ok(new List<AttendanceRowResult>());
            if (rows.Count > MaxRowsPerSave)
                return Failed<List<AttendanceRowResult>>($"At most {MaxRowsPerSave} rows can be saved at once.");

            var days = DateTime.DaysInMonth(sheet.Year, sheet.Month);
            var codes = (await _codes.GetAllAsync()).ToDictionary(c => c.Id);

            var existing = (await _rows.GetForEditAsync(sheetId)).ToDictionary(r => r.Id);

            // Who holds each employee number right now, updated as the batch
            // applies, so two rows of one save can't end up sharing one.
            var numbers = existing.Values.ToDictionary(r => r.EmployeeNumber, r => r, StringComparer.OrdinalIgnoreCase);
            var nextOrder = existing.Values.Select(r => r.DisplayOrder).DefaultIfEmpty(0).Max();

            var results = new List<(AttendanceRowResult Result, AttendanceRow? Row)>();
            foreach (var dto in rows)
            {
                var result = new AttendanceRowResult { ClientKey = dto.ClientKey, Id = dto.Id };

                AttendanceRow? row = null;
                if (dto.Id is { } id && !existing.TryGetValue(id, out row))
                {
                    result.Error = "This row no longer exists — it may have been deleted.";
                    results.Add((result, null));
                    continue;
                }

                var error = Check(dto, row, days, codes, numbers, out var number, out var name, out var cells);
                if (error != null)
                {
                    result.Error = error;
                    results.Add((result, null));
                    continue;
                }

                if (row == null)
                {
                    row = new AttendanceRow
                    {
                        SheetId = sheet.Id,
                        DisplayOrder = dto.DisplayOrder ?? ++nextOrder,
                        CreatedAt = AppClock.Now
                    };
                    _rows.Add(row);
                }
                else
                {
                    numbers.Remove(row.EmployeeNumber);
                    if (dto.DisplayOrder.HasValue) row.DisplayOrder = dto.DisplayOrder.Value;
                    row.UpdatedAt = AppClock.Now;
                }

                row.EmployeeNumber = number;
                row.EmployeeName = name;
                numbers[number] = row;
                ApplyCells(row, cells);

                results.Add((result, row));
            }

            // A row that failed was never touched (Check runs before any
            // copy), so this commits only the good ones.
            await _rows.SaveChangesAsync();

            return Ok(results.Select(x =>
            {
                if (x.Row != null)
                {
                    x.Result.Id = x.Row.Id;
                    x.Result.Row = ToRow(x.Row.Id, x.Row.EmployeeNumber, x.Row.EmployeeName, x.Row.DisplayOrder,
                        x.Row.Entries.Select(e => (e.Day, e.CodeId)), days, sheet.RequiredHours, codes);
                }
                return x.Result;
            }).ToList());
        }

        public async Task<AttendanceResult<bool>> DeleteRowAsync(int id, AttendanceActor actor)
        {
            var row = await _rows.GetByIdAsync(id);
            if (row == null) return Missing<bool>();

            var sheet = await _sheets.GetByIdAsync(row.SheetId);
            if (sheet == null || !CanSee(sheet.DepartmentId, actor)) return Missing<bool>();

            _rows.Remove(row);
            await _rows.SaveChangesAsync();
            return Ok(true);
        }

        // ---------- validation + mapping ----------

        // Checks the whole row first and only then lets the caller copy it,
        // returning the first rule it breaks.
        private static string? Check(
            SaveAttendanceRowDTO dto,
            AttendanceRow? row,
            int days,
            Dictionary<int, AttendanceCode> codes,
            Dictionary<string, AttendanceRow> numbers,
            out string number,
            out string name,
            out Dictionary<int, int> cells)
        {
            number = Clean(dto.EmployeeNumber) ?? "";
            name = Clean(dto.EmployeeName) ?? "";
            cells = new Dictionary<int, int>();

            if (number.Length == 0) return "Employee number is required.";
            if (number.Length > 20) return "Employee number is longer than 20 characters.";
            if (name.Length == 0) return "Employee name is required.";
            if (name.Length > 200) return "Employee name is longer than 200 characters.";

            if (numbers.TryGetValue(number, out var holder) && holder != row)
                return $"Employee number {number} is already on this sheet.";

            var sent = dto.Days ?? new List<int?>();
            if (sent.Count > days) return $"This month has {days} days, but {sent.Count} were sent.";

            for (var i = 0; i < sent.Count; i++)
            {
                if (sent[i] is not { } codeId) continue;
                var day = i + 1;

                if (!codes.TryGetValue(codeId, out var code))
                    return $"Day {day}: that code doesn't exist.";

                // A retired code may stay where it already was, but can't be
                // picked anew.
                var already = row?.Entries.Any(e => e.Day == day && e.CodeId == codeId) == true;
                if (!code.IsActive && !already)
                    return $"Day {day}: code {code.Code} is no longer in use.";

                cells[day] = codeId;
            }

            return null;
        }

        // Full replace of the row's cells, touching only the days that change.
        private static void ApplyCells(AttendanceRow row, Dictionary<int, int> cells)
        {
            foreach (var entry in row.Entries.ToList())
            {
                if (!cells.TryGetValue(entry.Day, out var codeId)) row.Entries.Remove(entry);
                else if (entry.CodeId != codeId) entry.CodeId = codeId;
            }

            foreach (var (day, codeId) in cells)
                if (!row.Entries.Any(e => e.Day == day))
                    row.Entries.Add(new AttendanceEntry { Day = day, CodeId = codeId });
        }

        private static AttendanceRowResponse ToRow(
            int id,
            string number,
            string name,
            int displayOrder,
            IEnumerable<(int Day, int CodeId)> cells,
            int days,
            decimal required,
            Dictionary<int, AttendanceCode> codes)
        {
            var grid = new List<int?>(new int?[days]);
            decimal total = 0;

            foreach (var (day, codeId) in cells)
            {
                if (day < 1 || day > days) continue;
                grid[day - 1] = codeId;
                if (codes.TryGetValue(codeId, out var code)) total += code.Hours;
            }

            return new AttendanceRowResponse
            {
                Id = id,
                EmployeeNumber = number,
                EmployeeName = name,
                DisplayOrder = displayOrder,
                Days = grid,
                TotalHours = total,
                Shortage = total - required
            };
        }

        private static AttendanceSheetResponse ToResponse(AttendanceSheetRow s) => new()
        {
            Id = s.Id,
            Year = s.Year,
            Month = s.Month,
            Title = Title(s.Year, s.Month),
            DepartmentId = s.DepartmentId,
            DepartmentName = s.DepartmentName,
            DaysInMonth = DateTime.DaysInMonth(s.Year, s.Month),
            RequiredHours = s.RequiredHours,
            EmployeeCount = s.EmployeeCount,
            CreatedByName = s.CreatedByName,
            CreatedAt = s.CreatedAt
        };

        internal static string Title(int year, int month) => $"{month}-{year}";

        private static List<int>? VisibleDepartments(AttendanceActor actor) =>
            actor.CanViewAll ? null : actor.DepartmentIds.ToList();

        private static bool CanSee(int departmentId, AttendanceActor actor) =>
            actor.CanViewAll || actor.DepartmentIds.Contains(departmentId);

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static AttendanceResult<T> Ok<T>(T item) => new() { Success = true, Item = item };
        private static AttendanceResult<T> Failed<T>(string error) => new() { Error = error };
        private static AttendanceResult<T> Conflicted<T>(string error) => new() { Conflict = true, Error = error };
        private static AttendanceResult<T> Missing<T>() => new() { NotFound = true };
    }
}
