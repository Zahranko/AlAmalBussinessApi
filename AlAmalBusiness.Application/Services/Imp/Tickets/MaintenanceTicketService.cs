using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Email;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Tickets;
using AlAmalBusiness.Application.DTOs.Tickets.Response;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.Tickets;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.Tickets;
using AlAmalBusiness.Domain.Models.Tickets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Tickets
{
    // Maintenance requests (added 2026-10-21). TicketService's workflow with
    // its routing taken out: one desk (MSupport) receives and solves every
    // request, a raiser (MRaiser) follows only their own, and there is no
    // insurance side, department view, queue parking, live push or
    // dashboard. Rules otherwise as TicketService: 404 for a request the
    // caller may not see, a timeline entry saved with every change, and the
    // closer becomes the assignee.
    public class MaintenanceTicketService : IMaintenanceTicketService
    {
        private readonly IMaintenanceTicketRepo _ticketRepo;
        private readonly IMaintenanceTicketHistoryRepo _historyRepo;
        private readonly IUserRepo _userRepo;
        private readonly IEmailQueue _emailQueue;
        private readonly ILogger<MaintenanceTicketService> _logger;
        private readonly string? _consoleBaseUrl;

        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 12;

        // Who hears about a new request: the maintenance team. Admins are
        // left out, as they are from every other notice.
        private static readonly string[] SupportRoles = { AppRoles.MSupport };

        public MaintenanceTicketService(
            IMaintenanceTicketRepo ticketRepo,
            IMaintenanceTicketHistoryRepo historyRepo,
            IUserRepo userRepo,
            IEmailQueue emailQueue,
            ILogger<MaintenanceTicketService> logger,
            IConfiguration config)
        {
            _consoleBaseUrl = config["Email:ConsoleBaseUrl"]?.TrimEnd('/');
            _ticketRepo = ticketRepo;
            _historyRepo = historyRepo;
            _userRepo = userRepo;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        public async Task<MaintenanceTicketActionResponse> CreateAsync(CreateMaintenanceTicketDTO request, MaintenanceTicketActor actor)
        {
            var title = Clean(request.Title);
            if (title == null || title.Length < 2)
                return Failed("عنوان الطلب مطلوب (حرفان على الأقل).");

            var priority = request.Priority ?? MaintenancePriority.Normal;
            if (!Enum.IsDefined(priority))
                return Failed("اختر الأهمية من القائمة.");

            var ticket = new MaintenanceTicket
            {
                Title = title,
                Location = Clean(request.Location),
                Details = Clean(request.Details),
                Priority = priority,
                CreatedById = actor.UserId,
                DepartmentId = actor.DepartmentId,
                Status = TicketStatus.Open,
                CreatedDate = AppClock.Now
            };

            await _ticketRepo.CreateAsync(ticket);

            _historyRepo.Add(new MaintenanceTicketHistory
            {
                TicketId = ticket.Id,
                ActorId = actor.UserId,
                Type = TicketActions.Created
            });
            await _ticketRepo.SaveChangesAsync();

            var detail = await LoadDetailAsync(ticket.Id);
            await NotifyNewTicketAsync(detail!, actor.UserId);

            return Ok(detail);
        }

        // The public page (ments.<domain>, added 2026-10-25): nobody signs in,
        // so the sender types who they are, and the request is saved with no
        // account behind it and no department (the page doesn't ask). From there it
        // follows the console's path exactly — same queue, same email to MSupport.
        public async Task<MaintenancePublicSubmitResponse> SubmitPublicAsync(PublicMaintenanceTicketDTO request, SubmissionContext context)
        {
            var name = Clean(request.RequesterName);
            if (name == null || name.Length < 2 || name.Length > 100)
                return Rejected("الاسم مطلوب (بين حرفين و100 حرف)");

            var phone = Clean(request.RequesterPhone);
            if (phone == null || !IsPhone(phone))
                return Rejected("رقم الهاتف أو التحويلة مطلوب (3 أرقام على الأقل)");

            var title = Clean(request.Title);
            if (title == null || title.Length < 2 || title.Length > 200)
                return Rejected("وصف المشكلة باختصار مطلوب (بين حرفين و200 حرف)");

            // Required here, unlike the console form: the team has no account
            // to ask, so where to go has to come with the request.
            var location = Clean(request.Location);
            if (location == null || location.Length > 200)
                return Rejected("الموقع مطلوب (200 حرف كحد أقصى)");

            var details = Clean(request.Details);
            if (details != null && details.Length > 4000)
                return Rejected("التفاصيل 4000 حرف كحد أقصى");

            var priority = request.Priority ?? MaintenancePriority.Normal;
            if (!Enum.IsDefined(priority))
                return Rejected("اختر الأهمية من القائمة");

            var ticket = new MaintenanceTicket
            {
                Title = title,
                Location = location,
                Details = details,
                Priority = priority,
                RequesterName = name,
                RequesterPhone = phone,
                SubmittedFromIp = context.IpAddress,
                UserAgent = Truncate(context.UserAgent, 400),
                Status = TicketStatus.Open,
                CreatedDate = AppClock.Now
            };

            await _ticketRepo.CreateAsync(ticket);

            _historyRepo.Add(new MaintenanceTicketHistory
            {
                TicketId = ticket.Id,
                ActorId = null,
                Type = TicketActions.Created
            });
            await _ticketRepo.SaveChangesAsync();

            var detail = await LoadDetailAsync(ticket.Id);
            await NotifyNewTicketAsync(detail!, null);

            return new MaintenancePublicSubmitResponse { Success = true, TicketId = ticket.Id, CreatedDate = ticket.CreatedDate };
        }

        // A mobile, a landline or a 3-4 digit extension: digits plus the usual
        // separators, three to fifteen digits.
        private static bool IsPhone(string value)
        {
            if (value.Length > 32 || value.Any(c => !(char.IsAsciiDigit(c) || c is ' ' or '+' or '-' or '(' or ')' or '/')))
                return false;
            var digits = value.Count(char.IsAsciiDigit);
            return digits is >= 3 and <= 15;
        }

        private static MaintenancePublicSubmitResponse Rejected(string error) =>
            new() { Success = false, Error = error };

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

        // ---------- lists ----------

        public Task<PagedResultDTO<MaintenanceTicketListItemResponse>> GetQueueAsync(
            MaintenanceTicketListQuery query, MaintenanceQueueScope scope, MaintenanceTicketActor actor)
        {
            query.Scope = scope;
            query.AssignedToId = scope == MaintenanceQueueScope.Mine ? actor.UserId : null;
            query.CreatedById = null;
            query.SeesEverything = actor.IsAdmin || actor.CanSupport;
            query.ViewerId = actor.UserId;
            return PageAsync(query);
        }

        public Task<PagedResultDTO<MaintenanceTicketListItemResponse>> GetCreatedByMeAsync(
            MaintenanceTicketListQuery query, MaintenanceTicketActor actor)
        {
            query.CreatedById = actor.UserId;
            query.AssignedToId = null;
            return PageAsync(query);
        }

        private async Task<PagedResultDTO<MaintenanceTicketListItemResponse>> PageAsync(MaintenanceTicketListQuery query)
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize);

            var (items, totalCount) = await _ticketRepo.PageTicketsAsync(query);

            return new PagedResultDTO<MaintenanceTicketListItemResponse>
            {
                Items = items.Select(r => Fill(new MaintenanceTicketListItemResponse(), r)).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<MaintenanceTicketDetailResponse?> GetDetailAsync(int id, MaintenanceTicketActor actor)
        {
            var row = await _ticketRepo.GetDetailAsync(id);
            if (row == null || !CanSee(row.CreatedById, actor))
                return null;

            return await ToDetailAsync(row);
        }

        // ---------- workflow ----------

        public async Task<MaintenanceTicketActionResponse> CommentAsync(int id, CommentTicketDTO request, MaintenanceTicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, actor))
                return NotFound();

            if (ticket.Status != TicketStatus.Open) return Failed(ClosedError);

            var note = Clean(request.Note);
            if (note == null)
                return Failed("لا يمكن إضافة تعليق فارغ.");

            // Everyone who may read it may comment: the maintenance team and
            // the request's own raiser.
            _historyRepo.Add(new MaintenanceTicketHistory
            {
                TicketId = ticket.Id,
                ActorId = actor.UserId,
                Type = TicketActions.Commented,
                Note = note
            });
            await _ticketRepo.SaveChangesAsync();

            return Ok(await LoadDetailAsync(id));
        }

        public async Task<MaintenanceTicketActionResponse> CloseAsync(int id, CloseTicketDTO request, MaintenanceTicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, actor))
                return NotFound();

            // A raiser follows their own request but never closes it.
            if (!Works(actor))
                return Failed("لا تملك صلاحية إغلاق هذا الطلب.");

            if (ticket.Status != TicketStatus.Open) return Failed(ClosedError);

            if (request.Outcome is not (TicketStatus.Success or TicketStatus.Failed))
                return Failed("اختر النتيجة: تم الإصلاح أو تعذّر الإصلاح.");

            string? reason = null;
            if (request.Outcome == TicketStatus.Failed)
            {
                reason = Clean(request.Reason);
                if (reason == null || reason.Length < 2)
                    return Failed("اكتب سبب تعذّر الإصلاح (حرفان على الأقل).");
            }

            ticket.AssignedToId = actor.UserId;
            ticket.Status = request.Outcome;
            ticket.Resolution = reason;
            ticket.ClosedAt = AppClock.Now;

            _historyRepo.Add(new MaintenanceTicketHistory
            {
                TicketId = ticket.Id,
                ActorId = actor.UserId,
                Type = request.Outcome == TicketStatus.Success ? TicketActions.Success : TicketActions.Failed,
                Note = reason
            });
            await _ticketRepo.SaveChangesAsync();

            var detail = await LoadDetailAsync(id);
            if (ticket.CreatedById != actor.UserId)
                await NotifyCreatorClosedAsync(detail!, request.Outcome == TicketStatus.Success, reason);

            return Ok(detail);
        }

        public async Task<MaintenanceTicketActionResponse> ReopenAsync(int id, MaintenanceTicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, actor))
                return NotFound();

            // Reopening is a correction to a close, so it is the closer's job.
            if (!Works(actor))
                return Failed("لا تملك صلاحية إعادة فتح هذا الطلب.");

            if (ticket.Status == TicketStatus.Open)
                return Failed("هذا الطلب مفتوح بالفعل.");

            ticket.Status = TicketStatus.Open;
            ticket.ClosedAt = null;
            ticket.Resolution = null;

            _historyRepo.Add(new MaintenanceTicketHistory
            {
                TicketId = ticket.Id,
                ActorId = actor.UserId,
                Type = TicketActions.Reopened
            });
            await _ticketRepo.SaveChangesAsync();

            return Ok(await LoadDetailAsync(id));
        }

        // ---------- rules ----------

        private const string ClosedError = "هذا الطلب مغلق. أعد فتحه قبل أي إجراء آخر.";

        // Who solves: the maintenance team and Admin.
        private static bool Works(MaintenanceTicketActor actor) => actor.IsAdmin || actor.CanSupport;

        // Who reads: whoever solves, and the raiser. Anyone else gets a 404.
        private static bool CanSee(string? createdById, MaintenanceTicketActor actor) =>
            Works(actor) || createdById == actor.UserId;

        // ---------- notifications (best-effort) ----------

        // actorId is null for a public request — there is nobody to leave out.
        private async Task NotifyNewTicketAsync(MaintenanceTicketDetailResponse ticket, string? actorId)
        {
            try
            {
                var recipients = await _userRepo.GetActiveEmailsInRolesAsync(SupportRoles, actorId);
                Enqueue(ticket.Id, "new-maintenance", recipients,
                    to => TicketEmailTemplate.NewMaintenance(to, Facts(ticket), ticket.Location, ticket.RequesterPhone, PriorityLabel(ticket.Priority), Link(ticket.Id)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maintenance ticket {Id}: failed to queue the new-request email.", ticket.Id);
            }
        }

        private async Task NotifyCreatorClosedAsync(MaintenanceTicketDetailResponse ticket, bool success, string? reason)
        {
            try
            {
                if (ticket.CreatedById == null) return;
                var email = await _userRepo.GetActiveEmailAsync(ticket.CreatedById);
                var recipients = email == null ? new List<string>() : new List<string> { email };
                Enqueue(ticket.Id, "closed", recipients,
                    to => TicketEmailTemplate.MaintenanceClosed(
                        to, Facts(ticket), ticket.Location, success, ticket.AssignedToName ?? "-", reason, Link(ticket.Id)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maintenance ticket {Id}: failed to queue the closed email.", ticket.Id);
            }
        }

        private void Enqueue(int ticketId, string what, List<string> recipients, Func<string, EmailMessage> build)
        {
            if (recipients.Count == 0)
            {
                _logger.LogInformation("Maintenance ticket {Id}: nobody with an email to tell ({What}), no email sent.", ticketId, what);
                return;
            }

            foreach (var to in recipients)
            {
                if (!_emailQueue.Enqueue(build(to)))
                    _logger.LogWarning("Maintenance ticket {Id}: {What} email to {To} was not queued.", ticketId, what, to);
            }
        }

        private static TicketEmailTemplate.TicketFacts Facts(MaintenanceTicketDetailResponse ticket) => new(
            ticket.Id,
            ticket.Title ?? string.Empty,
            ticket.CreatedByName,
            ticket.DepartmentName,
            null,
            ticket.CreatedDate.ToString("yyyy-MM-dd HH:mm"));

        // Left out of the email for Normal — only worth a row when it says something.
        private static string? PriorityLabel(string? priority) => priority switch
        {
            nameof(MaintenancePriority.Important) => "مهمة",
            nameof(MaintenancePriority.Urgent) => "عاجلة",
            _ => null
        };

        // The console's request page; its login round-trips ?next= back here.
        private string? Link(int id) =>
            string.IsNullOrWhiteSpace(_consoleBaseUrl) ? null : $"{_consoleBaseUrl}/tk/maintenance/{id}";

        // ---------- helpers ----------

        private async Task<MaintenanceTicketDetailResponse?> LoadDetailAsync(int id)
        {
            var row = await _ticketRepo.GetDetailAsync(id);
            return row == null ? null : await ToDetailAsync(row);
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static MaintenanceTicketActionResponse Ok(MaintenanceTicketDetailResponse? ticket) =>
            new() { Success = true, Ticket = ticket };

        private static MaintenanceTicketActionResponse NotFound() =>
            new() { Success = false, NotFound = true };

        private static MaintenanceTicketActionResponse Failed(string error) =>
            new() { Success = false, Error = error };

        // ---------- mapping ----------

        private static T Fill<T>(T target, MaintenanceTicketListRow row) where T : MaintenanceTicketListItemResponse
        {
            target.Id = row.Id;
            target.Title = row.Title;
            target.Location = row.Location;
            target.Status = row.Status.ToString();
            target.Priority = row.Priority.ToString();
            target.CreatedById = row.CreatedById;
            target.CreatedByName = row.CreatedByName;
            target.RequesterName = row.RequesterName;
            target.RequesterPhone = row.RequesterPhone;
            target.DepartmentId = row.DepartmentId;
            target.DepartmentName = row.DepartmentName;
            target.AssignedToId = row.AssignedToId;
            target.AssignedToName = row.AssignedToName;
            target.ClosedAt = row.ClosedAt;
            target.CreatedDate = row.CreatedDate;
            return target;
        }

        private async Task<MaintenanceTicketDetailResponse> ToDetailAsync(MaintenanceTicketDetailRow row)
        {
            var history = await _historyRepo.GetByTicketAsync(row.Id);

            var detail = Fill(new MaintenanceTicketDetailResponse(), row);
            detail.Details = row.Details;
            detail.Resolution = row.Resolution;
            detail.History = history.Select(h => new TicketHistoryResponse
            {
                Id = h.Id,
                Type = h.Type.ToString(),
                ActorName = h.ActorName,
                Note = h.Note,
                CreatedAt = h.CreatedAt
            }).ToList();
            return detail;
        }
    }
}
