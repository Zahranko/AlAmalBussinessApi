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

        // Every row visited between two days (inclusive), in visit order —
        // what the report builder counts over. At most `max`.
        Task<List<MammogramRecord>> GetByVisitRangeAsync(DateOnly from, DateOnly to, int max);

        // The cross-sheet follow-up list: one line per follow-up due in the
        // query's range — a row whose two follow-ups both fall in it is
        // listed twice — ordered by that follow-up's date. ReminderStatus
        // filters on that follow-up's own reminder.
        Task<(List<(MammogramRecord Record, int Slot)> Items, int TotalCount)> PageFollowUpsAsync(MammogramRecordQuery query);

        // ---- WhatsApp reminders (MammogramReminderService) ----
        // Each write below is one conditional UPDATE rather than a tracked
        // load, so two workers (an app-pool overlap) can never both send a
        // row, and a row edited mid-send is never overwritten. `slot` is the
        // follow-up the reminder is for: 1 (FollowUpDate / Reminder*) or 2
        // (FollowUpDate2 / Reminder2*).

        // Follow-ups due a reminder that day: due on it, not reminded, a
        // phone. A row can come back once per slot.
        Task<List<MammogramReminderRow>> GetDueRemindersAsync(DateOnly day, int max);

        // NotSent -> Queued. False when someone else got there first.
        Task<bool> ClaimReminderAsync(int id, int slot, DateTime at);

        // Ends a claim: only while the reminder is still Queued with no
        // message id (an edit in between reset it to NotSent, and the edit wins).
        Task FinishReminderClaimAsync(int id, int slot, MammogramReminderStatus status, string? messageId, string? error);

        // Reminders sent since then whose outcome can still change.
        Task<List<MammogramReminderInFlight>> GetRemindersInFlightAsync(DateTime since);

        // A status read back from the platform, for that message only.
        Task UpdateReminderStatusAsync(int id, int slot, string messageId, MammogramReminderStatus status, string? error);

        void Add(MammogramRecord record);
        void Remove(MammogramRecord record);
        Task SaveChangesAsync();
    }

    public class MammogramReminderRow
    {
        public int Id { get; set; }
        public int Slot { get; set; }
        public string PatientMrn { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneCountryCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Recommendation { get; set; }
        // That slot's "نوع الفحص".
        public string? FollowUpExam { get; set; }
    }

    public class MammogramReminderInFlight
    {
        public int Id { get; set; }
        public int Slot { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public MammogramReminderStatus Status { get; set; }
    }

    public interface IMammogramReportRepo
    {
        Task<List<MammogramReportRow>> GetAllAsync();

        // Tracked.
        Task<MammogramReport?> GetByIdAsync(int id);
        Task<bool> NameExistsAsync(string name, int? excludeId);

        void Add(MammogramReport report);
        void Remove(MammogramReport report);
        Task SaveChangesAsync();
    }

    public class MammogramReportRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Definition { get; set; } = "{}";
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
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

    // One filter object for both the sheet view (SheetId set, PageAsync) and
    // the cross-sheet follow-up list (FollowUpFrom/To set, PageFollowUpsAsync).
    public class MammogramRecordQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public int? SheetId { get; set; }

        // Name, MRN or phone (leading-zero tolerant).
        public string? Search { get; set; }
        public MammogramExamType? ExamType { get; set; }
        public string? Tech { get; set; }

        // Inclusive, on either follow-up date (PageFollowUpsAsync).
        public DateOnly? FollowUpFrom { get; set; }
        public DateOnly? FollowUpTo { get; set; }

        public MammogramReminderStatus? ReminderStatus { get; set; }

        // Rows that have a phone number to message.
        public bool OnlyWithPhone { get; set; }
    }
}
