using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Mammograms;
using AlAmalBusiness.Application.Services.Interface.Mammograms;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using AlAmalBusiness.Domain.Models.Mammograms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Mammograms
{
    // The mammogram follow-up register: one sheet per month, one row per
    // visit, in the layout of the Excel workbook it replaces (its 9-2026 tab).
    //
    // The rules the spreadsheet couldn't hold are enforced here: a row's visit
    // date must fall inside its sheet's month, a follow-up can't be before the
    // visit, and the phone is normalised to one stored form so the future
    // WhatsApp reminder can send to it without guessing.
    public class MammogramService : IMammogramService
    {
        private const string DefaultCountryCode = "+962";

        // A pasted month is ~200 rows; this leaves room without letting one
        // request rewrite an unbounded number.
        private const int MaxRowsPerSave = 1000;

        // 0-6, BI-RADS 4 may carry its A/B/C subcategory, and the sheet's
        // "0/4" form records two results.
        private static readonly Regex BiRadsPattern = new(@"^[0-6][ABC]?(/[0-6][ABC]?)?$", RegexOptions.Compiled);

        // Jordanian mobiles (77/78/79 + 7 digits) once the leading zero is
        // gone — the only numbers WhatsApp can reach here.
        private static readonly Regex JordanMobile = new(@"^7[789]\d{7}$", RegexOptions.Compiled);

        private readonly IMammogramSheetRepo _sheets;
        private readonly IMammogramRecordRepo _records;

        public MammogramService(IMammogramSheetRepo sheets, IMammogramRecordRepo records)
        {
            _sheets = sheets;
            _records = records;
        }

        // ---------- sheets ----------

        public async Task<List<MammogramSheetResponse>> GetSheetsAsync() =>
            (await _sheets.GetAllAsync()).Select(ToResponse).ToList();

        public async Task<MammogramSheetResponse?> GetSheetAsync(int id)
        {
            var row = await _sheets.GetRowAsync(id);
            return row == null ? null : ToResponse(row);
        }

        public async Task<MammogramResult<MammogramSheetResponse>> OpenSheetAsync(OpenMammogramSheetDTO dto, string userId)
        {
            if (dto.Month is < 1 or > 12) return Failed<MammogramSheetResponse>("Month must be between 1 and 12.");

            var today = AppClock.Today;
            if (dto.Year > today.Year || (dto.Year == today.Year && dto.Month > today.Month))
                return Failed<MammogramSheetResponse>("A sheet can't be opened for a month that hasn't started yet.");

            if (await _sheets.ExistsAsync(dto.Year, dto.Month))
                return Conflicted<MammogramSheetResponse>($"The sheet for {Title(dto.Year, dto.Month)} is already open.");

            var sheet = new MammogramSheet
            {
                Year = dto.Year,
                Month = dto.Month,
                CreatedById = userId,
                CreatedAt = AppClock.Now
            };
            await _sheets.CreateAsync(sheet);

            return Ok((await GetSheetAsync(sheet.Id))!);
        }

        // Only an empty sheet: one opened for the wrong month by mistake.
        // A sheet with rows is the register and stays.
        public async Task<MammogramResult<bool>> DeleteSheetAsync(int id)
        {
            var sheet = await _sheets.GetByIdAsync(id);
            if (sheet == null) return Missing<bool>();
            if (await _sheets.HasRecordsAsync(id))
                return Conflicted<bool>("Only an empty sheet can be deleted — remove its rows first.");

            await _sheets.DeleteAsync(sheet);
            return Ok(true);
        }

        // ---------- records ----------

        public async Task<PagedResultDTO<MammogramRecordResponse>> GetRecordsAsync(MammogramRecordQuery query)
        {
            var (items, total) = await _records.PageAsync(query);
            return new PagedResultDTO<MammogramRecordResponse>
            {
                Items = items.Select(ToResponse).ToList(),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<MammogramRecordResponse?> GetRecordAsync(int id)
        {
            var record = await _records.GetByIdAsync(id);
            return record == null ? null : ToResponse(record);
        }

        public async Task<MammogramResult<MammogramRecordResponse>> AddRecordAsync(int sheetId, SaveMammogramRecordDTO dto, string userId)
        {
            var sheet = await _sheets.GetByIdAsync(sheetId);
            if (sheet == null) return Missing<MammogramRecordResponse>();

            var record = new MammogramRecord
            {
                SheetId = sheet.Id,
                CreatedById = userId,
                CreatedAt = AppClock.Now
            };

            var error = Apply(record, dto, sheet);
            if (error != null) return Failed<MammogramRecordResponse>(error);

            _records.Add(record);
            await _records.SaveChangesAsync();
            return Ok(ToResponse(record));
        }

        public async Task<MammogramResult<MammogramRecordResponse>> UpdateRecordAsync(int id, SaveMammogramRecordDTO dto)
        {
            var record = await _records.GetByIdAsync(id);
            if (record == null) return Missing<MammogramRecordResponse>();

            var sheet = (await _sheets.GetByIdAsync(record.SheetId))!;

            var error = ApplyUpdate(record, dto, sheet);
            if (error != null) return Failed<MammogramRecordResponse>(error);

            await _records.SaveChangesAsync();
            return Ok(ToResponse(record));
        }

        public async Task<MammogramResult<List<MammogramRowResult>>> SaveRowsAsync(int sheetId, List<SaveMammogramRowDTO> rows, string userId)
        {
            var sheet = await _sheets.GetByIdAsync(sheetId);
            if (sheet == null) return Missing<List<MammogramRowResult>>();
            if (rows.Count == 0) return Ok(new List<MammogramRowResult>());
            if (rows.Count > MaxRowsPerSave)
                return Failed<List<MammogramRowResult>>($"At most {MaxRowsPerSave} rows can be saved at once.");

            // One query for every existing row in the batch; an id from
            // another sheet simply isn't found.
            var ids = rows.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).Distinct().ToList();
            var existing = ids.Count == 0
                ? new Dictionary<int, MammogramRecord>()
                : (await _records.GetByIdsAsync(sheetId, ids)).ToDictionary(r => r.Id);

            var results = new List<(MammogramRowResult Result, MammogramRecord? Record)>();
            foreach (var row in rows)
            {
                var result = new MammogramRowResult { ClientKey = row.ClientKey, Id = row.Id };
                MammogramRecord? record;
                string? error;

                if (row.Id is { } id)
                {
                    if (!existing.TryGetValue(id, out record))
                    {
                        result.Error = "This row no longer exists — it may have been deleted.";
                        results.Add((result, null));
                        continue;
                    }
                    error = ApplyUpdate(record, row, sheet);
                }
                else
                {
                    record = new MammogramRecord { SheetId = sheet.Id, CreatedById = userId, CreatedAt = AppClock.Now };
                    error = Apply(record, row, sheet);
                    if (error == null) _records.Add(record);
                }

                result.Error = error;
                results.Add((result, error == null ? record : null));
            }

            // A row that failed validation was never written to (Apply checks
            // before it copies), so saving now only commits the good ones.
            await _records.SaveChangesAsync();

            return Ok(results.Select(x =>
            {
                if (x.Record != null)
                {
                    x.Result.Id = x.Record.Id;
                    x.Result.Record = ToResponse(x.Record);
                }
                return x.Result;
            }).ToList());
        }

        // Apply plus what an edit adds on top: the reminder reset and the
        // edit stamp.
        private static string? ApplyUpdate(MammogramRecord record, SaveMammogramRecordDTO dto, MammogramSheet sheet)
        {
            var oldPhone = (record.PhoneCountryCode, record.PhoneNumber);
            var oldFollowUp = record.FollowUpDate;

            var error = Apply(record, dto, sheet);
            if (error != null) return error;

            // A reminder already sent went to the old number or for the old
            // date — a corrected row is due one again.
            if (oldPhone != (record.PhoneCountryCode, record.PhoneNumber) || oldFollowUp != record.FollowUpDate)
            {
                record.ReminderStatus = MammogramReminderStatus.NotSent;
                record.ReminderSentAt = null;
                record.ReminderMessageId = null;
                record.ReminderError = null;
            }

            record.UpdatedAt = AppClock.Now;
            return null;
        }

        public async Task<MammogramResult<bool>> DeleteRecordAsync(int id)
        {
            var record = await _records.GetByIdAsync(id);
            if (record == null) return Missing<bool>();

            _records.Remove(record);
            await _records.SaveChangesAsync();
            return Ok(true);
        }

        public Task<List<string>> GetTechNamesAsync() => _records.GetTechNamesAsync();

        public async Task<(byte[] Content, string FileName)?> ExportSheetAsync(int id)
        {
            var sheet = await _sheets.GetRowAsync(id);
            if (sheet == null) return null;

            var rows = await _records.GetBySheetAsync(id);
            var title = Title(sheet.Year, sheet.Month);
            return (MammogramExcelWriter.Build(title, rows), $"mammogram-{title}.xlsx");
        }

        // ---------- validation + mapping ----------

        // Checks the whole body first and only then copies it onto the
        // record, returning the first rule it breaks.
        private static string? Apply(MammogramRecord record, SaveMammogramRecordDTO dto, MammogramSheet sheet)
        {
            var mrn = Clean(dto.PatientMrn);
            if (mrn == null) return "Patient ID (MRN) is required.";
            if (mrn.Length > 20) return "Patient ID (MRN) is longer than 20 characters.";

            var name = Clean(dto.FullName);
            if (name == null) return "Patient name is required.";
            if (name.Length > 200) return "Patient name is longer than 200 characters.";

            if (Clean(dto.Tech)?.Length > 100) return "Tech is longer than 100 characters.";
            if (Clean(dto.Recommendation)?.Length > 1000) return "Recommendation is longer than 1000 characters.";
            if (Clean(dto.FollowUpExam)?.Length > 100) return "Exam type is longer than 100 characters.";
            if (Clean(dto.ContactNote)?.Length > 1000) return "Contact note is longer than 1000 characters.";
            if (Clean(dto.PhoneCountryCode)?.Length > 6) return "Country code is too long.";

            if (dto.VisitDate is not { } visit) return "Visit date is required.";
            if (visit.Year != sheet.Year || visit.Month != sheet.Month)
                return $"Visit date must be inside {Title(sheet.Year, sheet.Month)} — this is that month's sheet.";

            if (dto.FollowUpDate is { } followUp && followUp < visit)
                return "Follow-up date can't be before the visit date.";

            if (dto.Age is < 0 or > 120) return "Age must be between 0 and 120.";

            string? biRads = null;
            if (!string.IsNullOrWhiteSpace(dto.BiRads))
            {
                biRads = dto.BiRads.Replace(" ", "").ToUpperInvariant();
                if (!BiRadsPattern.IsMatch(biRads))
                    return "BI-RADS must be 0-6 (4A/4B/4C allowed), or two results like 0/4.";
            }

            var (countryCode, phone, phoneError) = NormalizePhone(dto.PhoneCountryCode, dto.PhoneNumber);
            if (phoneError != null) return phoneError;

            record.PatientMrn = mrn;
            record.Gender = dto.Gender;
            record.FullName = name;
            record.Tech = Clean(dto.Tech)?.ToUpperInvariant();
            record.Age = dto.Age;
            record.BreastFeeding = dto.BreastFeeding;
            record.PossiblePregnancy = dto.PossiblePregnancy;
            record.HasImplants = dto.HasImplants;
            record.VisitDate = visit;
            record.Procedures = (dto.Procedures ?? new()).Aggregate(MammogramProcedures.None, (all, p) => all | p);
            record.ExamType = dto.ExamType;
            record.BiRads = biRads;
            record.Recommendation = Clean(dto.Recommendation);
            record.FollowUpDate = dto.FollowUpDate;
            record.FollowUpExam = Clean(dto.FollowUpExam);
            record.PhoneCountryCode = countryCode;
            record.PhoneNumber = phone;
            record.ContactNote = Clean(dto.ContactNote);
            record.ContactedAt = dto.ContactedAt;
            return null;
        }

        // Stored the feedback/appointment way — "+962" in its own column, the
        // national number digits-only without its leading zero — and accepts
        // the number however it was typed: 0799..., 799..., +962799...,
        // 00962799...
        internal static (string CountryCode, string? Number, string? Error) NormalizePhone(string? code, string? number)
        {
            var ccDigits = DigitsOnly(code ?? "");
            if (ccDigits.Length == 0) ccDigits = DefaultCountryCode.TrimStart('+');
            var countryCode = $"+{ccDigits}";

            if (string.IsNullOrWhiteSpace(number)) return (countryCode, null, null);

            var digits = DigitsOnly(number);
            if (digits.StartsWith("00")) digits = digits[2..];
            if (digits.StartsWith(ccDigits) && digits.Length > ccDigits.Length + 6) digits = digits[ccDigits.Length..];
            digits = digits.TrimStart('0');

            if (ccDigits == "962")
            {
                if (!JordanMobile.IsMatch(digits))
                    return (countryCode, null, "Phone must be a Jordanian mobile number (07X XXX XXXX).");
            }
            else if (digits.Length is < 6 or > 14)
            {
                return (countryCode, null, "Phone number doesn't look valid.");
            }

            return (countryCode, digits, null);
        }

        // What WhatsApp's API takes: country code + national number, digits
        // only, no "+".
        internal static string? WhatsAppNumber(string countryCode, string? number) =>
            string.IsNullOrEmpty(number) ? null : DigitsOnly(countryCode) + number;

        internal static string Title(int year, int month) => $"{month}-{year}";

        internal static List<MammogramProcedures> ProcedureList(MammogramProcedures flags) =>
            Enum.GetValues<MammogramProcedures>()
                .Where(p => p != MammogramProcedures.None && flags.HasFlag(p))
                .ToList();

        private static MammogramRecordResponse ToResponse(MammogramRecord r) => new()
        {
            Id = r.Id,
            SheetId = r.SheetId,
            PatientMrn = r.PatientMrn,
            Gender = r.Gender,
            FullName = r.FullName,
            Tech = r.Tech,
            Age = r.Age,
            BreastFeeding = r.BreastFeeding,
            PossiblePregnancy = r.PossiblePregnancy,
            HasImplants = r.HasImplants,
            VisitDate = r.VisitDate,
            Procedures = ProcedureList(r.Procedures),
            ExamType = r.ExamType,
            BiRads = r.BiRads,
            Recommendation = r.Recommendation,
            FollowUpDate = r.FollowUpDate,
            FollowUpExam = r.FollowUpExam,
            PhoneCountryCode = r.PhoneCountryCode,
            PhoneNumber = r.PhoneNumber,
            WhatsAppNumber = WhatsAppNumber(r.PhoneCountryCode, r.PhoneNumber),
            ContactNote = r.ContactNote,
            ContactedAt = r.ContactedAt,
            ReminderStatus = r.ReminderStatus,
            ReminderSentAt = r.ReminderSentAt,
            ReminderError = r.ReminderError,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static MammogramSheetResponse ToResponse(MammogramSheetRow s) => new()
        {
            Id = s.Id,
            Year = s.Year,
            Month = s.Month,
            Title = Title(s.Year, s.Month),
            CreatedAt = s.CreatedAt,
            CreatedByName = s.CreatedByName,
            RecordCount = s.RecordCount,
            FollowUpCount = s.FollowUpCount,
            ContactedCount = s.ContactedCount
        };

        private static string DigitsOnly(string value) => new(value.Where(char.IsDigit).ToArray());

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static MammogramResult<T> Ok<T>(T item) => new() { Success = true, Item = item };
        private static MammogramResult<T> Failed<T>(string error) => new() { Error = error };
        private static MammogramResult<T> Conflicted<T>(string error) => new() { Conflict = true, Error = error };
        private static MammogramResult<T> Missing<T>() => new() { NotFound = true };
    }
}
