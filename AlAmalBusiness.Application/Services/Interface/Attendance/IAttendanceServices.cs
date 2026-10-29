using AlAmalBusiness.Application.DTOs.Attendance;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Attendance
{
    public interface IAttendanceCodeService
    {
        Task<List<AttendanceCodeDTO>> GetAllAsync();
        Task<List<AttendanceCodeDTO>> GetActiveAsync();
        Task<AttendanceResult<AttendanceCodeDTO>> CreateAsync(AttendanceCodeDTO dto);
        Task<AttendanceResult<AttendanceCodeDTO>> UpdateAsync(int id, AttendanceCodeDTO dto);
    }

    public interface IAttendanceService
    {
        Task<List<AttendanceSheetResponse>> GetSheetsAsync(int? year, int? month, int? departmentId, AttendanceActor actor);
        Task<AttendanceResult<OpenAttendanceSheetsResponse>> OpenSheetsAsync(OpenAttendanceSheetsDTO dto, AttendanceActor actor);
        Task<AttendanceResult<AttendanceSheetResponse>> UpdateSheetAsync(int id, UpdateAttendanceSheetDTO dto, AttendanceActor actor);

        // Only while no day has been filled.
        Task<AttendanceResult<bool>> DeleteSheetAsync(int id, AttendanceActor actor);

        Task<AttendanceGridResponse?> GetGridAsync(int id, AttendanceActor actor);

        // Every changed row in one call; each passes or fails on its own and
        // the good ones are saved together. NotFound when the sheet isn't
        // there or isn't the caller's.
        Task<AttendanceResult<List<AttendanceRowResult>>> SaveRowsAsync(int sheetId, List<SaveAttendanceRowDTO> rows, AttendanceActor actor);

        Task<AttendanceResult<bool>> DeleteRowAsync(int id, AttendanceActor actor);
    }
}
