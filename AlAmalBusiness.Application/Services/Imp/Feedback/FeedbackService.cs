using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Feedback.Response;
using AlAmalBusiness.Application.Services.Interface.Feedback;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.Feedback;
using AlAmalBusiness.Domain.Models.Feedback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Feedback
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IPatientFeedbackRepo _feedbackRepo;
        private readonly IFeedbackHistoryRepo _historyRepo;
        private readonly IDepartmentRepo _departmentRepo;
        private readonly IReferenceNumberGenerator _references;

        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 12;
        private const string DefaultCountryCode = "+962";

        public FeedbackService(
            IPatientFeedbackRepo feedbackRepo,
            IFeedbackHistoryRepo historyRepo,
            IDepartmentRepo departmentRepo,
            IReferenceNumberGenerator references)
        {
            _feedbackRepo = feedbackRepo;
            _historyRepo = historyRepo;
            _departmentRepo = departmentRepo;
            _references = references;
        }

        // ---------- public (anonymous) ----------

        public async Task<FeedbackCreatedResponse> SubmitAsync(CreateFeedbackDTO request, SubmissionContext context)
        {
            // Rules DataAnnotations can't express. Arabic messages — a patient
            // reads these.
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (request.VisitDate > today)
                return Rejected("تاريخ الزيارة لا يمكن أن يكون في المستقبل");

            if (request.VisitDate < today.AddYears(-2))
                return Rejected("تاريخ الزيارة قديم جداً (أكثر من سنتين)");

            if (request.Type == FeedbackType.Complaint && string.IsNullOrWhiteSpace(request.Details))
                return Rejected("يرجى كتابة تفاصيل الشكوى");

            var department = await _departmentRepo.GetDepartmentByIdAsync(request.DepartmentId);
            if (department == null || !department.IsActive)
                return Rejected("القسم المختار غير موجود");

            var feedback = new PatientFeedback
            {
                ReferenceNumber = await GenerateUniqueReferenceAsync(),
                Type = request.Type,
                Status = FeedbackStatus.New,
                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                PhoneCountryCode = NormalizeCountryCode(request.PhoneCountryCode),
                PhoneNumber = NormalizeNationalNumber(request.PhoneNumber),
                VisitDate = request.VisitDate,
                DepartmentId = department.Id,
                Details = Clean(request.Details),
                SubmittedFromIp = context.IpAddress,
                UserAgent = Truncate(context.UserAgent, 400),
                CreatedDate = DateTime.Now
            };

            await _feedbackRepo.CreateAsync(feedback);

            return new FeedbackCreatedResponse
            {
                Success = true,
                ReferenceNumber = feedback.ReferenceNumber,
                CreatedDate = feedback.CreatedDate
            };
        }

        // ---------- staff inbox ----------

        public async Task<PagedResultDTO<FeedbackListItemResponse>> GetPagedAsync(FeedbackListQuery query, FeedbackActor actor)
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize);
            query.RestrictToDepartmentId = VisibleDepartment(actor);

            var (items, totalCount) = await _feedbackRepo.PageFeedbacksAsync(query);

            return new PagedResultDTO<FeedbackListItemResponse>
            {
                Items = items.Select(ToListItem).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<FeedbackDetailResponse?> GetDetailAsync(int id, FeedbackActor actor)
        {
            var feedback = await _feedbackRepo.GetDetailAsync(id);
            return await VisibleOrNullAsync(feedback, actor);
        }

        public async Task<FeedbackDetailResponse?> GetByReferenceAsync(string referenceNumber, FeedbackActor actor)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber))
                return null;

            var feedback = await _feedbackRepo.GetByReferenceAsync(referenceNumber.Trim().ToUpperInvariant());
            return await VisibleOrNullAsync(feedback, actor);
        }

        public async Task<FeedbackActionResponse> ChangeStatusAsync(int id, UpdateFeedbackStatusDTO request, FeedbackActor actor)
        {
            var feedback = await _feedbackRepo.GetByIdAsync(id);
            if (feedback == null || !CanSee(feedback.DepartmentId, actor))
                return NotFound();

            // A no-op status "change" with nothing to say is a mistake, not an
            // event worth a timeline entry. The same status WITH a note is a
            // legitimate comment on where it stands, so that one is kept.
            if (feedback.Status == request.Status && string.IsNullOrWhiteSpace(request.Note))
                return Failed("الرسالة بهذه الحالة بالفعل.");

            var previous = feedback.Status;
            feedback.Status = request.Status;
            feedback.ResolvedAt = request.Status == FeedbackStatus.Resolved ? DateTime.Now : null;

            _historyRepo.Add(new FeedbackHistory
            {
                FeedbackId = feedback.Id,
                ActorId = actor.UserId,
                Type = FeedbackActions.StatusChanged,
                FromStatus = previous,
                ToStatus = request.Status,
                Note = Clean(request.Note)
            });

            await _feedbackRepo.SaveChangesAsync();

            return await ReloadAsync(id, actor);
        }

        public async Task<FeedbackActionResponse> AssignAsync(int id, AssignFeedbackDTO request, FeedbackActor actor)
        {
            var feedback = await _feedbackRepo.GetByIdAsync(id);
            if (feedback == null || !CanSee(feedback.DepartmentId, actor))
                return NotFound();

            // Only ever the actor themselves, or nobody — see AssignFeedbackDTO.
            feedback.AssignedToId = request.AssignToMe ? actor.UserId : null;

            _historyRepo.Add(new FeedbackHistory
            {
                FeedbackId = feedback.Id,
                ActorId = actor.UserId,
                Type = request.AssignToMe ? FeedbackActions.Assigned : FeedbackActions.Unassigned
            });

            await _feedbackRepo.SaveChangesAsync();

            return await ReloadAsync(id, actor);
        }

        public async Task<FeedbackActionResponse> ForwardAsync(int id, ForwardFeedbackDTO request, FeedbackActor actor)
        {
            var feedback = await _feedbackRepo.GetByIdAsync(id);
            if (feedback == null || !CanSee(feedback.DepartmentId, actor))
                return NotFound();

            if (feedback.DepartmentId == request.DepartmentId)
                return Failed("الرسالة موجودة في هذا القسم بالفعل.");

            var target = await _departmentRepo.GetDepartmentByIdAsync(request.DepartmentId);
            if (target == null || !target.IsActive)
                return Failed("القسم المختار غير موجود أو غير مفعّل.");

            var source = await _departmentRepo.GetDepartmentByIdAsync(feedback.DepartmentId);

            feedback.DepartmentId = target.Id;

            // The assignee belonged to the department this is leaving, so the
            // receiving team would otherwise see it as already owned by
            // someone they don't work with. Status is deliberately kept — it
            // reflects real progress (already resolved, say), not which
            // department is holding it.
            feedback.AssignedToId = null;

            _historyRepo.Add(new FeedbackHistory
            {
                FeedbackId = feedback.Id,
                ActorId = actor.UserId,
                Type = FeedbackActions.Forwarded,
                FromDepartmentName = source?.Name,
                ToDepartmentName = target.Name,
                Note = Clean(request.Note)
            });

            await _feedbackRepo.SaveChangesAsync();

            // No record back — see IFeedbackService.ForwardAsync.
            return new FeedbackActionResponse { Success = true };
        }

        public async Task<FeedbackActionResponse> AddNoteAsync(int id, AddFeedbackNoteDTO request, FeedbackActor actor)
        {
            var feedback = await _feedbackRepo.GetByIdAsync(id);
            if (feedback == null || !CanSee(feedback.DepartmentId, actor))
                return NotFound();

            var note = Clean(request.Note);
            if (note == null)
                return Failed("لا يمكن إضافة ملاحظة فارغة.");

            _historyRepo.Add(new FeedbackHistory
            {
                FeedbackId = feedback.Id,
                ActorId = actor.UserId,
                Type = FeedbackActions.Note,
                Note = note
            });

            await _feedbackRepo.SaveChangesAsync();

            return await ReloadAsync(id, actor);
        }

        // ---------- dashboard ----------

        public async Task<FeedbackStatsResponse> GetStatsAsync(FeedbackStatsQuery query, FeedbackActor actor)
        {
            // Same rule as the inbox, and set here rather than trusted from
            // the caller: a manager can pass departmentId all they like, the
            // restriction below still pins them to their own.
            query.RestrictToDepartmentId = VisibleDepartment(actor);

            if (query.From.HasValue && query.To.HasValue && query.From > query.To)
                (query.From, query.To) = (query.To, query.From);

            var rows = await _feedbackRepo.GetStatsAsync(query);

            var response = new FeedbackStatsResponse
            {
                From = query.From,
                To = query.To,
                DepartmentId = query.DepartmentId,
                NewCount = CountOf(rows.Statuses, FeedbackStatus.New),
                InReviewCount = CountOf(rows.Statuses, FeedbackStatus.InReview),
                ResolvedCount = CountOf(rows.Statuses, FeedbackStatus.Resolved),
                ArchivedCount = CountOf(rows.Statuses, FeedbackStatus.Archived),
                ThanksCount = CountOf(rows.Types, FeedbackType.Thanks),
                SuggestionCount = CountOf(rows.Types, FeedbackType.Suggestion),
                ComplaintCount = CountOf(rows.Types, FeedbackType.Complaint),
                AvgResolutionHours = ToHours(rows.AvgResolutionMinutes),
                OldestOpenHours = ToHours(rows.OldestOpenMinutes)
            };

            response.Total = response.NewCount + response.InReviewCount + response.ResolvedCount + response.ArchivedCount;
            response.ResolvedPercent = Percent(response.ResolvedCount, response.Total);

            response.Resolvers = rows.Resolvers
                .OrderByDescending(r => r.ResolvedCount)
                .ThenBy(r => r.ActorName)
                .Select(r => new FeedbackResolverStatResponse
                {
                    UserId = r.ActorId,
                    UserName = r.ActorName,
                    ResolvedCount = r.ResolvedCount,
                    AvgResolutionHours = ToHours(r.AvgMinutes)
                })
                .ToList();

            response.Departments = rows.Departments
                .OrderByDescending(d => d.Total)
                .ThenBy(d => d.Name)
                .Select(d => new FeedbackDepartmentStatResponse
                {
                    DepartmentId = d.DepartmentId,
                    Name = d.Name,
                    Total = d.Total,
                    OpenCount = d.NewCount + d.InReviewCount,
                    ResolvedCount = d.ResolvedCount,
                    ArchivedCount = d.ArchivedCount,
                    ResolvedPercent = Percent(d.ResolvedCount, d.Total),
                    AvgResolutionHours = ToHours(d.AvgMinutes)
                })
                .ToList();

            return response;
        }

        private static int CountOf(List<FeedbackStatusCountRow> rows, FeedbackStatus status) =>
            rows.FirstOrDefault(r => r.Status == status)?.Count ?? 0;

        private static int CountOf(List<FeedbackTypeCountRow> rows, FeedbackType type) =>
            rows.FirstOrDefault(r => r.Type == type)?.Count ?? 0;

        // Null stays null: "nothing has been resolved yet" is not "resolved
        // in zero hours", and the dashboard shows the two differently.
        private static double? ToHours(double? minutes) =>
            minutes.HasValue ? Math.Round(minutes.Value / 60.0, 1) : null;

        private static double Percent(int part, int total) =>
            total == 0 ? 0 : Math.Round(part * 100.0 / total, 1);

        // ---------- visibility ----------

        // Who sees what: only an Admin is unrestricted. Everyone else —
        // FManager included — is narrowed to the department their own account
        // belongs to: a manager runs one department's feedback, and their
        // dashboard is that department's numbers. An account with no
        // department therefore sees an empty inbox by design; give them one,
        // or Admin if they are meant to see the whole hospital.
        //
        // Null means "no restriction".
        private static int? VisibleDepartment(FeedbackActor actor) =>
            actor.CanViewAll ? null : actor.DepartmentId;

        private static bool CanSee(int departmentId, FeedbackActor actor) =>
            actor.CanViewAll || actor.DepartmentId == departmentId;

        // 404 rather than 403 for a message outside the caller's department —
        // not revealing that a given id exists is the point of the scoping.
        private async Task<FeedbackDetailResponse?> VisibleOrNullAsync(PatientFeedback? feedback, FeedbackActor actor)
        {
            if (feedback == null || !CanSee(feedback.DepartmentId, actor))
                return null;

            return await ToDetailAsync(feedback);
        }

        // The single re-read every mutation returns through, so the caller
        // always gets the message as it now stands, timeline included.
        private async Task<FeedbackActionResponse> ReloadAsync(int id, FeedbackActor actor)
        {
            var detail = await GetDetailAsync(id, actor);
            return new FeedbackActionResponse { Success = true, Feedback = detail };
        }

        // ---------- helpers ----------

        // Retries if a random reference collides with an existing one; the
        // fallback suffix guarantees termination rather than looping forever.
        private async Task<string> GenerateUniqueReferenceAsync()
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var candidate = _references.Next();
                if (!await _feedbackRepo.IsReferenceExist(candidate))
                    return candidate;
            }

            return $"{_references.Next()}-{DateTime.Now:ffff}";
        }

        private static string DigitsOnly(string value) => Regex.Replace(value, @"\D", string.Empty);

        // Drops the national leading zero (0790... -> 790...) so the stored
        // number isn't +9620790... once the country code is prepended.
        private static string NormalizeNationalNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var digits = DigitsOnly(value);
            var trimmed = digits.TrimStart('0');
            return trimmed.Length == 0 ? digits : trimmed;
        }

        private static string NormalizeCountryCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return DefaultCountryCode;
            var digits = DigitsOnly(code);
            return digits.Length == 0 ? DefaultCountryCode : $"+{digits}";
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

        private static FeedbackCreatedResponse Rejected(string error) =>
            new() { Success = false, Error = error };

        private static FeedbackActionResponse NotFound() =>
            new() { Success = false, NotFound = true };

        private static FeedbackActionResponse Failed(string error) =>
            new() { Success = false, Error = error };

        // ---------- mapping ----------

        private static FeedbackListItemResponse ToListItem(FeedbackListRow row) => new()
        {
            Id = row.Id,
            ReferenceNumber = row.ReferenceNumber,
            Type = row.Type.ToString(),
            Status = row.Status.ToString(),
            FullName = $"{row.FirstName} {row.LastName}".Trim(),
            Phone = $"{row.PhoneCountryCode}{row.PhoneNumber}",
            VisitDate = row.VisitDate,
            DepartmentId = row.DepartmentId,
            DepartmentName = row.DepartmentName,
            AssignedToId = row.AssignedToId,
            AssignedToName = row.AssignedToName,
            ResolvedAt = row.ResolvedAt,
            CreatedDate = row.CreatedDate
        };

        private async Task<FeedbackDetailResponse> ToDetailAsync(PatientFeedback feedback)
        {
            var history = await _historyRepo.GetByFeedbackAsync(feedback.Id);

            return new FeedbackDetailResponse
            {
                Id = feedback.Id,
                ReferenceNumber = feedback.ReferenceNumber,
                Type = feedback.Type.ToString(),
                Status = feedback.Status.ToString(),
                FullName = feedback.FullName,
                Phone = $"{feedback.PhoneCountryCode}{feedback.PhoneNumber}",
                VisitDate = feedback.VisitDate,
                DepartmentId = feedback.DepartmentId,
                DepartmentName = feedback.Department?.Name,
                AssignedToId = feedback.AssignedToId,
                AssignedToName = feedback.AssignedTo?.UserName,
                ResolvedAt = feedback.ResolvedAt,
                CreatedDate = feedback.CreatedDate,
                Details = feedback.Details,
                History = history.Select(h => new FeedbackHistoryResponse
                {
                    Id = h.Id,
                    Type = h.Type.ToString(),
                    FromStatus = h.FromStatus?.ToString(),
                    ToStatus = h.ToStatus?.ToString(),
                    FromDepartmentName = h.FromDepartmentName,
                    ToDepartmentName = h.ToDepartmentName,
                    ActorName = h.Actor?.UserName,
                    Note = h.Note,
                    CreatedAt = h.CreatedAt
                }).ToList()
            };
        }
    }
}
