using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models.Mammograms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Mammograms
{
    public interface IMammogramSheetRepo
    {
        // Every sheet, newest month first, with its counts — one grouped query.
        Task<List<MammogramSheetRow>> GetAllAsync();
        Task<MammogramSheetRow?> GetRowAsync(int id);

        // Tracked.
        Task<MammogramSheet?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(int year, int month);
        Task<bool> HasRecordsAsync(int id);

        Task CreateAsync(MammogramSheet sheet);
        Task DeleteAsync(MammogramSheet sheet);
    }

    public interface IMammogramRecordRepo
    {
        // Tracked, for update/delete.
        Task<MammogramRecord?> GetByIdAsync(int id);

        // Tracked, only the ones that belong to this sheet.
        Task<List<MammogramRecord>> GetByIdsAsync(int sheetId, List<int> ids);

        Task<(List<MammogramRecord> Items, int TotalCount)> PageAsync(MammogramRecordQuery query);

        // A whole sheet in visit order, for the Excel export.
        Task<List<MammogramRecord>> GetBySheetAsync(int sheetId);

        // Distinct "Tech" values already used, for the form's autocomplete.
        Task<List<string>> GetTechNamesAsync();

        void Add(MammogramRecord record);
        void Remove(MammogramRecord record);
        Task SaveChangesAsync();
    }

    public class MammogramSheetRow
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public int RecordCount { get; set; }
        public int FollowUpCount { get; set; }
        public int ContactedCount { get; set; }
    }

    // One filter object for both the sheet view (SheetId set) and the
    // cross-sheet follow-up list (FollowUpFrom/To set) — the second is the
    // query a WhatsApp reminder job will run.
    public class MammogramRecordQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public int? SheetId { get; set; }

        // Name, MRN or phone (leading-zero tolerant).
        public string? Search { get; set; }
        public MammogramExamType? ExamType { get; set; }
        public string? Tech { get; set; }

        // Inclusive. Either bound set means "rows with a follow-up date in
        // range", ordered by that date instead of by visit.
        public DateOnly? FollowUpFrom { get; set; }
        public DateOnly? FollowUpTo { get; set; }

        public MammogramReminderStatus? ReminderStatus { get; set; }

        // Rows that have a phone number to message.
        public bool OnlyWithPhone { get; set; }
    }
}
