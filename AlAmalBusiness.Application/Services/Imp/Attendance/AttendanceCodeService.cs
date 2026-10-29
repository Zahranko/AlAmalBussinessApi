using AlAmalBusiness.Application.DTOs.Attendance;
using AlAmalBusiness.Application.Services.Interface.Attendance;
using AlAmalBusiness.Domain.IRepositories.Attendance;
using AlAmalBusiness.Domain.Models.Attendance;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Attendance
{
    // The letters the attendance grid's dropdowns offer, each worth a number
    // of hours. Same rules as the other lookup lists: a unique code, IsActive
    // in the same body, no delete.
    public class AttendanceCodeService : IAttendanceCodeService
    {
        private readonly IAttendanceCodeRepo _codes;

        public AttendanceCodeService(IAttendanceCodeRepo codes)
        {
            _codes = codes;
        }

        public async Task<List<AttendanceCodeDTO>> GetAllAsync() =>
            (await _codes.GetAllAsync()).Select(ToDto).ToList();

        public async Task<List<AttendanceCodeDTO>> GetActiveAsync() =>
            (await _codes.GetAllAsync()).Where(c => c.IsActive).Select(ToDto).ToList();

        public async Task<AttendanceResult<AttendanceCodeDTO>> CreateAsync(AttendanceCodeDTO dto)
        {
            var error = await CheckAsync(dto, 0);
            if (error != null) return Failed(error);

            var entity = new AttendanceCode();
            Copy(entity, dto);
            await _codes.CreateAsync(entity);
            return Ok(ToDto(entity));
        }

        public async Task<AttendanceResult<AttendanceCodeDTO>> UpdateAsync(int id, AttendanceCodeDTO dto)
        {
            var entity = await _codes.GetByIdAsync(id);
            if (entity == null) return new() { NotFound = true, Error = $"Attendance code with ID {id} not found." };

            var error = await CheckAsync(dto, id);
            if (error != null) return Failed(error);

            Copy(entity, dto);
            await _codes.SaveChangesAsync();
            return Ok(ToDto(entity));
        }

        private async Task<string?> CheckAsync(AttendanceCodeDTO dto, int excludeId)
        {
            var code = Clean(dto.Code);
            if (code == null) return "Code is empty.";
            if (code.Length > 10) return "Code is longer than 10 characters.";
            if (Clean(dto.Name)?.Length > 100) return "Description is longer than 100 characters.";
            if (dto.Hours is < 0 or > 24) return "Hours must be between 0 and 24.";
            if (await _codes.IsCodeExist(code.ToUpperInvariant(), excludeId))
                return $"Attendance code '{code.ToUpperInvariant()}' already exists.";
            return null;
        }

        private static void Copy(AttendanceCode entity, AttendanceCodeDTO dto)
        {
            entity.Code = Clean(dto.Code)!.ToUpperInvariant();
            entity.Name = Clean(dto.Name);
            entity.Hours = dto.Hours;
            entity.IsActive = dto.IsActive;
        }

        internal static AttendanceCodeDTO ToDto(AttendanceCode c) =>
            new() { Id = c.Id, Code = c.Code, Name = c.Name, Hours = c.Hours, IsActive = c.IsActive };

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static AttendanceResult<AttendanceCodeDTO> Ok(AttendanceCodeDTO item) => new() { Success = true, Item = item };
        private static AttendanceResult<AttendanceCodeDTO> Failed(string error) => new() { Error = error };
    }
}
