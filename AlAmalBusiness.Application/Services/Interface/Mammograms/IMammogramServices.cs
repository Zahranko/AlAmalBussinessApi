using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Mammograms;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Mammograms
{
    public interface IMammogramService
    {
        Task<List<MammogramSheetResponse>> GetSheetsAsync();
        Task<MammogramSheetResponse?> GetSheetAsync(int id);
        Task<MammogramResult<MammogramSheetResponse>> OpenSheetAsync(OpenMammogramSheetDTO dto, string userId);
        Task<MammogramResult<bool>> DeleteSheetAsync(int id);

        Task<PagedResultDTO<MammogramRecordResponse>> GetRecordsAsync(MammogramRecordQuery query);
        Task<MammogramRecordResponse?> GetRecordAsync(int id);
        Task<MammogramResult<MammogramRecordResponse>> AddRecordAsync(int sheetId, SaveMammogramRecordDTO dto, string userId);
        Task<MammogramResult<MammogramRecordResponse>> UpdateRecordAsync(int id, SaveMammogramRecordDTO dto);
        Task<MammogramResult<bool>> DeleteRecordAsync(int id);

        // The grid's save: every changed row of one sheet in one call. Each
        // row passes or fails on its own; the good ones are saved together.
        // NotFound when the sheet doesn't exist.
        Task<MammogramResult<List<MammogramRowResult>>> SaveRowsAsync(int sheetId, List<SaveMammogramRowDTO> rows, string userId);

        Task<List<string>> GetTechNamesAsync();

        // The sheet as an .xlsx in the old workbook's column layout; null when
        // there is no such sheet.
        Task<(byte[] Content, string FileName)?> ExportSheetAsync(int id);
    }
}
