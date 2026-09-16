using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.DTOs.Appointments.Response;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.Appointments;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using AlAmalBusiness.Domain.Models.Appointments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Appointments
{
    // The public appointment page's submit, and the booking team's inbox on
    // the other side of it. Deliberately the same service shape as
    // FeedbackService — same visibility rules, same timeline, same reasons.
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRequestRepo _requestRepo;
        private readonly IAppointmentHistoryRepo _historyRepo;
        private readonly IAppointmentReferralSourceRepo _referralSourceRepo;
        private readonly IDepartmentRepo _departmentRepo;
        private readonly IUserRepo _userRepo;
        private readonly IEmailQueue _emailQueue;
        private readonly ILogger<AppointmentService> _logger;
        private readonly string? _consoleBaseUrl;

        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 12;
        private const string DefaultCountryCode = "+962";

        public AppointmentService(
            IAppointmentRequestRepo requestRepo,
            IAppointmentHistoryRepo historyRepo,
            IAppointmentReferralSourceRepo referralSourceRepo,
            IDepartmentRepo departmentRepo,
            IUserRepo userRepo,
            IEmailQueue emailQueue,
            ILogger<AppointmentService> logger,
            IConfiguration config)
        {
            // Console base URL for the email's "open request" button; blank
            // leaves the button out rather than linking somewhere wrong.
            _consoleBaseUrl = config["Email:ConsoleBaseUrl"]?.TrimEnd('/');
            _requestRepo = requestRepo;
            _historyRepo = historyRepo;
            _referralSourceRepo = referralSourceRepo;
            _departmentRepo = departmentRepo;
            _userRepo = userRepo;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        // ---------- public (anonymous) ----------

        public async Task<AppointmentFormOptionsResponse> GetFormOptionsAsync()
        {
            // Sequential on purpose: both run on the one scoped DbContext,
            // which does not allow concurrent queries.
            var departments = await _departmentRepo.GetActiveDepartmentsAsync();
            var referralSources = await _referralSourceRepo.GetActiveAsync();

            return new AppointmentFormOptionsResponse
            {
                Departments = departments
                    .Select(d => new AppointmentOptionResponse { Id = d.Id, Name = d.Name ?? string.Empty })
                    .ToList(),
                ReferralSources = referralSources
                    .Select(r => new AppointmentOptionResponse { Id = r.Id, Name = r.Name })
                    .ToList()
            };
        }

        public async Task<AppointmentCreatedResponse> SubmitAsync(CreateAppointmentRequestDTO request, SubmissionContext context)
        {
            var fullName = Clean(request.FullName);
            if (fullName == null || fullName.Length < 2)
                return Rejected("الاسم مطلوب (حرفان على الأقل)");

            // Inactive entries answer the same as missing ones: a retired
            // choice stays on old requests but can't be picked again.
            var department = await _departmentRepo.GetDepartmentByIdAsync(request.DepartmentId);
            if (department == null || !department.IsActive)
                return Rejected("القسم المختار غير موجود");

            var referralSource = await _referralSourceRepo.GetByIdAsync(request.ReferralSourceId);
            if (referralSource == null || !referralSource.IsActive)
                return Rejected("الخيار المختار في «كيف سمعت عنا» غير متاح");

            var appointment = new AppointmentRequest
            {
                FullName = fullName,
                PhoneCountryCode = NormalizeCountryCode(request.PhoneCountryCode),
                PhoneNumber = NormalizeNationalNumber(request.PhoneNumber),
                DepartmentId = department.Id,
                ReferralSourceId = referralSource.Id,
                Details = Clean(request.Details),
                Status = AppointmentStatus.New,
                SubmittedFromIp = context.IpAddress,
                UserAgent = Truncate(context.UserAgent, 400),
                CreatedDate = AppClock.Now
            };

            await _requestRepo.CreateAsync(appointment);

            await NotifyDepartmentManagersAsync(appointment, department.Name, referralSource.Name);

            return new AppointmentCreatedResponse { Success = true, CreatedDate = appointment.CreatedDate };
        }

        // Emails every active AManager of the request's department who has an
        // email address on their account — the feedback rule exactly, and the
        // reason the admin-maintained address list this used to read is gone.
        // Best-effort: the request is already saved, so nothing here may fail
        // the patient's submit, and the SMTP send itself happens later on the
        // email queue's background worker.
        private async Task NotifyDepartmentManagersAsync(AppointmentRequest appointment, string? departmentName, string referralSourceName)
        {
            try
            {
                var recipients = await _userRepo.GetActiveEmailsInRoleAsync(AppRoles.AManager, appointment.DepartmentId);
                if (recipients.Count == 0)
                {
                    _logger.LogInformation(
                        "Appointment request {Id}: no active AManager with an email in department {DepartmentId}, no email sent.",
                        appointment.Id, appointment.DepartmentId);
                    return;
                }

                var link = string.IsNullOrWhiteSpace(_consoleBaseUrl) ? null : $"{_consoleBaseUrl}/ap/inbox/{appointment.Id}";

                foreach (var to in recipients)
                {
                    if (!_emailQueue.Enqueue(AppointmentEmailTemplate.Build(to, appointment, departmentName, referralSourceName, link)))
                        _logger.LogWarning("Appointment request {Id}: email to {To} was not queued.", appointment.Id, to);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Appointment request {Id}: failed to queue the notification email.", appointment.Id);
            }
        }

        // ---------- staff inbox ----------

        public async Task<PagedResultDTO<AppointmentListItemResponse>> GetPagedAsync(AppointmentListQuery query, AppointmentActor actor)
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize);
            query.RestrictToDepartmentId = VisibleDepartment(actor);

            if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
                (query.FromDate, query.ToDate) = (query.ToDate, query.FromDate);

            var (items, totalCount) = await _requestRepo.PageRequestsAsync(query);

            return new PagedResultDTO<AppointmentListItemResponse>
            {
                Items = items.Select(ToListItem).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<AppointmentDetailResponse?> GetDetailAsync(int id, AppointmentActor actor)
        {
            var appointment = await _requestRepo.GetDetailAsync(id);
            if (appointment == null || !CanSee(appointment.DepartmentId, actor))
                return null;

            return await ToDetailAsync(appointment);
        }

        public async Task<AppointmentActionResponse> ChangeStatusAsync(int id, UpdateAppointmentStatusDTO request, AppointmentActor actor)
        {
            var appointment = await _requestRepo.GetByIdAsync(id);
            if (appointment == null || !CanSee(appointment.DepartmentId, actor))
                return NotFound();

            // A no-op status "change" with nothing to say is a mistake, not
            // an event worth a timeline entry. The same status WITH a note is
            // a legitimate comment on where it stands, so that one is kept.
            if (appointment.Status == request.Status && string.IsNullOrWhiteSpace(request.Note))
                return Failed("الطلب بهذه الحالة بالفعل.");

            var previous = appointment.Status;
            appointment.Status = request.Status;
            // Only a booked request carries a completion stamp; moving it back
            // out of Scheduled clears it, so "time to book" never averages
            // over a stamp that no longer describes the row.
            appointment.CompletedAt = request.Status == AppointmentStatus.Scheduled ? AppClock.Now : null;

            _historyRepo.Add(new AppointmentHistory
            {
                AppointmentId = appointment.Id,
                ActorId = actor.UserId,
                Type = AppointmentActions.StatusChanged,
                FromStatus = previous,
                ToStatus = request.Status,
                Note = Clean(request.Note)
            });

            await _requestRepo.SaveChangesAsync();

            return await ReloadAsync(id, actor);
        }

        public async Task<AppointmentActionResponse> AssignAsync(int id, AssignAppointmentDTO request, AppointmentActor actor)
        {
            var appointment = await _requestRepo.GetByIdAsync(id);
            if (appointment == null || !CanSee(appointment.DepartmentId, actor))
                return NotFound();

            // Only ever the actor themselves, or nobody — see AssignAppointmentDTO.
            appointment.AssignedToId = request.AssignToMe ? actor.UserId : null;

            _historyRepo.Add(new AppointmentHistory
            {
                AppointmentId = appointment.Id,
                ActorId = actor.UserId,
                Type = request.AssignToMe ? AppointmentActions.Assigned : AppointmentActions.Unassigned
            });

            await _requestRepo.SaveChangesAsync();

            return await ReloadAsync(id, actor);
        }

        public async Task<AppointmentActionResponse> ForwardAsync(int id, ForwardAppointmentDTO request, AppointmentActor actor)
        {
            var appointment = await _requestRepo.GetByIdAsync(id);
            if (appointment == null || !CanSee(appointment.DepartmentId, actor))
                return NotFound();

            if (appointment.DepartmentId == request.DepartmentId)
                return Failed("الطلب موجود في هذا القسم بالفعل.");

            var target = await _departmentRepo.GetDepartmentByIdAsync(request.DepartmentId);
            if (target == null || !target.IsActive)
                return Failed("القسم المختار غير موجود أو غير مفعّل.");

            var source = await _departmentRepo.GetDepartmentByIdAsync(appointment.DepartmentId);

            appointment.DepartmentId = target.Id;

            // The assignee belonged to the department this is leaving, so the
            // receiving team would otherwise see it as already owned by
            // someone they don't work with. Status is deliberately kept — it
            // reflects real progress (already contacted, say), not which
            // department is holding it.
            appointment.AssignedToId = null;

            _historyRepo.Add(new AppointmentHistory
            {
                AppointmentId = appointment.Id,
                ActorId = actor.UserId,
                Type = AppointmentActions.Forwarded,
                FromDepartmentName = source?.Name,
                ToDepartmentName = target.Name,
                Note = Clean(request.Note)
            });

            await _requestRepo.SaveChangesAsync();

            // No record back — see IAppointmentService.ForwardAsync.
            return new AppointmentActionResponse { Success = true };
        }

        public async Task<AppointmentActionResponse> AddNoteAsync(int id, AddAppointmentNoteDTO request, AppointmentActor actor)
        {
            var appointment = await _requestRepo.GetByIdAsync(id);
            if (appointment == null || !CanSee(appointment.DepartmentId, actor))
                return NotFound();

            var note = Clean(request.Note);
            if (note == null)
                return Failed("لا يمكن إضافة ملاحظة فارغة.");

            _historyRepo.Add(new AppointmentHistory
            {
                AppointmentId = appointment.Id,
                ActorId = actor.UserId,
                Type = AppointmentActions.Note,
                Note = note
            });

            await _requestRepo.SaveChangesAsync();

            return await ReloadAsync(id, actor);
        }

        // ---------- dashboard ----------

        public async Task<AppointmentStatsResponse> GetStatsAsync(AppointmentStatsQuery query, AppointmentActor actor)
        {
            // Same rule as the inbox, and set here rather than trusted from
            // the caller: a manager can pass departmentId all they like, the
            // restriction below still pins them to their own.
            query.RestrictToDepartmentId = VisibleDepartment(actor);

            if (query.From.HasValue && query.To.HasValue && query.From > query.To)
                (query.From, query.To) = (query.To, query.From);

            var rows = await _requestRepo.GetStatsAsync(query);

            var response = new AppointmentStatsResponse
            {
                From = query.From,
                To = query.To,
                DepartmentId = query.DepartmentId,
                NewCount = CountOf(rows.Statuses, AppointmentStatus.New),
                ContactedCount = CountOf(rows.Statuses, AppointmentStatus.Contacted),
                ScheduledCount = CountOf(rows.Statuses, AppointmentStatus.Scheduled),
                CancelledCount = CountOf(rows.Statuses, AppointmentStatus.Cancelled),
                AvgBookingHours = ToHours(rows.AvgBookingMinutes),
                OldestOpenHours = ToHours(rows.OldestOpenMinutes)
            };

            response.Total = response.NewCount + response.ContactedCount + response.ScheduledCount + response.CancelledCount;
            response.ScheduledPercent = Percent(response.ScheduledCount, response.Total);

            response.Sources = rows.Sources
                .OrderByDescending(s => s.Count)
                .ThenBy(s => s.Name)
                .Select(s => new AppointmentSourceStatResponse
                {
                    ReferralSourceId = s.ReferralSourceId,
                    Name = s.Name,
                    Count = s.Count,
                    Percent = Percent(s.Count, response.Total)
                })
                .ToList();

            response.Bookers = rows.Bookers
                .OrderByDescending(b => b.ScheduledCount)
                .ThenBy(b => b.ActorName)
                .Select(b => new AppointmentBookerStatResponse
                {
                    UserId = b.ActorId,
                    UserName = b.ActorName,
                    ScheduledCount = b.ScheduledCount,
                    AvgBookingHours = ToHours(b.AvgMinutes)
                })
                .ToList();

            response.Departments = rows.Departments
                .OrderByDescending(d => d.Total)
                .ThenBy(d => d.Name)
                .Select(d => new AppointmentDepartmentStatResponse
                {
                    DepartmentId = d.DepartmentId,
                    Name = d.Name,
                    Total = d.Total,
                    OpenCount = d.NewCount + d.ContactedCount,
                    ScheduledCount = d.ScheduledCount,
                    CancelledCount = d.CancelledCount,
                    ScheduledPercent = Percent(d.ScheduledCount, d.Total),
                    AvgBookingHours = ToHours(d.AvgMinutes)
                })
                .ToList();

            return response;
        }

        private static int CountOf(List<AppointmentStatusCountRow> rows, AppointmentStatus status) =>
            rows.FirstOrDefault(r => r.Status == status)?.Count ?? 0;

        // Null stays null: "nothing has been booked yet" is not "booked in
        // zero hours", and the dashboard shows the two differently.
        private static double? ToHours(double? minutes) =>
            minutes.HasValue ? Math.Round(minutes.Value / 60.0, 1) : null;

        private static double Percent(int part, int total) =>
            total == 0 ? 0 : Math.Round(part * 100.0 / total, 1);

        // ---------- visibility ----------

        // Who sees what: only an Admin is unrestricted. Everyone else —
        // AManager included — is narrowed to the department their own account
        // belongs to. An account with no department therefore sees an empty
        // inbox by design; give them one, or Admin if they are meant to see
        // the whole hospital. Null means "no restriction".
        private static int? VisibleDepartment(AppointmentActor actor) =>
            actor.CanViewAll ? null : actor.DepartmentId;

        private static bool CanSee(int departmentId, AppointmentActor actor) =>
            actor.CanViewAll || actor.DepartmentId == departmentId;

        // The single re-read every mutation returns through, so the caller
        // always gets the request as it now stands, timeline included.
        private async Task<AppointmentActionResponse> ReloadAsync(int id, AppointmentActor actor)
        {
            var detail = await GetDetailAsync(id, actor);
            return new AppointmentActionResponse { Success = true, Appointment = detail };
        }

        // ---------- helpers (same phone rules as FeedbackService) ----------

        private static string DigitsOnly(string value) => Regex.Replace(value, @"\D", string.Empty);

        // Drops the national leading zero (0790... -> 790...) so the number
        // isn't +9620790... once the country code is prepended.
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

        private static AppointmentCreatedResponse Rejected(string error) =>
            new() { Success = false, Error = error };

        private static AppointmentActionResponse NotFound() =>
            new() { Success = false, NotFound = true };

        private static AppointmentActionResponse Failed(string error) =>
            new() { Success = false, Error = error };

        // ---------- mapping ----------

        private static AppointmentListItemResponse ToListItem(AppointmentListRow row) => new()
        {
            Id = row.Id,
            FullName = row.FullName,
            Phone = $"{row.PhoneCountryCode}{row.PhoneNumber}",
            Status = row.Status.ToString(),
            DepartmentId = row.DepartmentId,
            DepartmentName = row.DepartmentName,
            ReferralSourceId = row.ReferralSourceId,
            ReferralSourceName = row.ReferralSourceName,
            AssignedToId = row.AssignedToId,
            AssignedToName = row.AssignedToName,
            CompletedAt = row.CompletedAt,
            CreatedDate = row.CreatedDate
        };

        private async Task<AppointmentDetailResponse> ToDetailAsync(AppointmentRequest appointment)
        {
            var history = await _historyRepo.GetByAppointmentAsync(appointment.Id);

            return new AppointmentDetailResponse
            {
                Id = appointment.Id,
                FullName = appointment.FullName,
                Phone = $"{appointment.PhoneCountryCode}{appointment.PhoneNumber}",
                Status = appointment.Status.ToString(),
                DepartmentId = appointment.DepartmentId,
                DepartmentName = appointment.Department?.Name,
                ReferralSourceId = appointment.ReferralSourceId,
                ReferralSourceName = appointment.ReferralSource?.Name,
                AssignedToId = appointment.AssignedToId,
                AssignedToName = appointment.AssignedTo?.UserName,
                CompletedAt = appointment.CompletedAt,
                CreatedDate = appointment.CreatedDate,
                Details = appointment.Details,
                History = history.Select(h => new AppointmentHistoryResponse
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
