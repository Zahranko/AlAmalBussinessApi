using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Email;
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
    // Staff support tickets, ported from the CRMS Tickets app. The business
    // rules are that app's; the shape is AppointmentService's — actor passed
    // in from the controller, 404 for a ticket the caller may not see, a
    // timeline entry saved with every change, and one re-read every mutation
    // answers through.
    //
    // Deliberate differences from the original, besides names/departments
    // being real FKs:
    // - An Insurance ticket belongs to the insurance desk alone (2026-09-17).
    //   The original sent tickets with a payment method and a HasInvoice box
    //   to an Insurance or Cash review desk that accepted them into the queue
    //   or declined them; the invoice box, the review step and the Cash desk
    //   are gone. Cash is now an ordinary queue ticket.
    // - The in-app notification bell (and the KnownUser cache that fed it)
    //   became email, like every other notice this app sends.
    // - Reading a ticket is scoped. The original let anyone with any ticket
    //   permission open any ticket by id.
    // - Reopening clears ClosedAt and Resolution (the timeline keeps both).
    public class TicketService : ITicketService
    {
        private readonly ITicketRepo _ticketRepo;
        private readonly ITicketHistoryRepo _historyRepo;
        private readonly ITicketCategoryRepo _categoryRepo;
        private readonly ITicketProcedureRepo _procedureRepo;
        private readonly ITicketReasonRepo _reasonRepo;
        private readonly IUserRepo _userRepo;
        private readonly IEmailQueue _emailQueue;
        private readonly ILogger<TicketService> _logger;
        private readonly string? _consoleBaseUrl;

        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 12;

        // Who hears about a new ticket: the people who work its side. Admins
        // are left out, as they are from the feedback and appointment notices
        // — they would get every email the hospital sends.
        private static readonly string[] QueueRoles = { AppRoles.TManager, AppRoles.TEmployee };
        private static readonly string[] InsuranceRoles = { AppRoles.TInsurance };

        public TicketService(
            ITicketRepo ticketRepo,
            ITicketHistoryRepo historyRepo,
            ITicketCategoryRepo categoryRepo,
            ITicketProcedureRepo procedureRepo,
            ITicketReasonRepo reasonRepo,
            IUserRepo userRepo,
            IEmailQueue emailQueue,
            ILogger<TicketService> logger,
            IConfiguration config)
        {
            // Console base URL for the emails' "open ticket" button; blank
            // leaves the button out rather than linking somewhere wrong.
            _consoleBaseUrl = config["Email:ConsoleBaseUrl"]?.TrimEnd('/');
            _ticketRepo = ticketRepo;
            _historyRepo = historyRepo;
            _categoryRepo = categoryRepo;
            _procedureRepo = procedureRepo;
            _reasonRepo = reasonRepo;
            _userRepo = userRepo;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        public async Task<TicketFormOptionsResponse> GetFormOptionsAsync()
        {
            // Sequential on purpose: all three run on the one scoped DbContext,
            // which does not allow concurrent queries.
            var categories = await _categoryRepo.GetActiveAsync();
            var procedures = await _procedureRepo.GetActiveAsync();
            var reasons = await _reasonRepo.GetActiveAsync();

            return new TicketFormOptionsResponse
            {
                Categories = categories.Select(c => new TicketOptionResponse { Id = c.Id, Name = c.Name }).ToList(),
                Procedures = procedures.Select(p => new TicketOptionResponse { Id = p.Id, Name = p.Name }).ToList(),
                Reasons = reasons
                    .Select(r => new TicketReasonOptionResponse { Id = r.Id, Name = r.Name, ProcedureId = r.ProcedureId })
                    .ToList()
            };
        }

        public async Task<TicketActionResponse> CreateAsync(CreateTicketDTO request, TicketActor actor)
        {
            var title = Clean(request.Title);
            if (title == null || title.Length < 2)
                return Failed("عنوان التذكرة مطلوب (حرفان على الأقل).");

            // Inactive entries answer the same as missing ones: a retired
            // choice stays on old tickets but can't be picked again.
            if (request.CategoryId.HasValue)
            {
                var category = await _categoryRepo.GetByIdAsync(request.CategoryId.Value);
                if (category == null || !category.IsActive)
                    return Failed("التصنيف المختار غير متاح.");
            }

            if (request.ProcedureId.HasValue)
            {
                var procedure = await _procedureRepo.GetByIdAsync(request.ProcedureId.Value);
                if (procedure == null || !procedure.IsActive)
                    return Failed("الإجراء المختار غير متاح.");
            }

            if (request.ReasonId.HasValue)
            {
                var reason = await _reasonRepo.GetByIdAsync(request.ReasonId.Value);
                if (reason == null || !reason.IsActive)
                    return Failed("السبب المختار غير متاح.");
                // A reason belongs to one procedure and can only be attached
                // when that procedure is the one the ticket is about.
                if (request.ProcedureId != reason.ProcedureId)
                    return Failed("السبب المختار لا يتبع الإجراء المختار.");
            }

            var sourceUrl = Clean(request.SourceUrl);
            // The console renders this as a link, so only a real web address
            // is kept — never a javascript: or data: URL.
            if (sourceUrl != null
                && !(Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
                return Failed("رابط المصدر غير صالح.");

            var ticket = new Ticket
            {
                Title = title,
                Description = Clean(request.Description),
                Type = request.Type,
                PatientId = Clean(request.PatientId),
                SourceUrl = sourceUrl,
                CategoryId = request.CategoryId,
                ProcedureId = request.ProcedureId,
                ReasonId = request.ReasonId,
                PaymentMethod = request.PaymentMethod,
                CreatedById = actor.UserId,
                DepartmentId = actor.DepartmentId,
                Status = TicketStatus.Open,
                CreatedDate = AppClock.Now
            };

            await _ticketRepo.CreateAsync(ticket);

            _historyRepo.Add(new TicketHistory
            {
                TicketId = ticket.Id,
                ActorId = actor.UserId,
                Type = TicketActions.Created
            });
            await _ticketRepo.SaveChangesAsync();

            var detail = await LoadDetailAsync(ticket.Id);
            await NotifyNewTicketAsync(detail!, IsInsurance(ticket), actor.UserId);

            return Ok(detail);
        }

        // ---------- lists ----------

        public Task<PagedResultDTO<TicketListItemResponse>> GetQueueAsync(TicketListQuery query, TicketQueueScope scope, TicketActor actor)
        {
            query.Scope = scope;
            query.AssignedToId = scope == TicketQueueScope.Mine ? actor.UserId : null;
            query.CreatedById = null;
            // Each side only for whoever works it — someone on both (or an
            // Admin) sees one queue holding both.
            query.IncludeGeneral = actor.CanWork;
            query.IncludeInsurance = actor.CanInsurance;
            return PageAsync(query);
        }

        public Task<PagedResultDTO<TicketListItemResponse>> GetCreatedByMeAsync(TicketListQuery query, TicketActor actor)
        {
            query.CreatedById = actor.UserId;
            query.AssignedToId = null;
            return PageAsync(query);
        }

        private async Task<PagedResultDTO<TicketListItemResponse>> PageAsync(TicketListQuery query)
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize);

            var (items, totalCount) = await _ticketRepo.PageTicketsAsync(query);

            return new PagedResultDTO<TicketListItemResponse>
            {
                Items = items.Select(ToListItem).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<TicketDetailResponse?> GetDetailAsync(int id, TicketActor actor)
        {
            var row = await _ticketRepo.GetDetailAsync(id);
            if (row == null || !CanSee(row.CreatedById, row.PaymentMethod, actor))
                return null;

            return await ToDetailAsync(row);
        }

        // ---------- workflow ----------

        public async Task<TicketActionResponse> CommentAsync(int id, CommentTicketDTO request, TicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, ticket.PaymentMethod, actor))
                return NotFound();

            if (ticket.Status != TicketStatus.Open) return Failed(ClosedError);

            var note = Clean(request.Note);
            if (note == null)
                return Failed("لا يمكن إضافة تعليق فارغ.");

            // Everyone who may read it may comment: whoever works its side,
            // and the ticket's own creator.
            _historyRepo.Add(new TicketHistory
            {
                TicketId = ticket.Id,
                ActorId = actor.UserId,
                Type = TicketActions.Commented,
                Note = note
            });
            await _ticketRepo.SaveChangesAsync();

            return Ok(await LoadDetailAsync(id));
        }

        public async Task<TicketActionResponse> CloseAsync(int id, CloseTicketDTO request, TicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, ticket.PaymentMethod, actor))
                return NotFound();

            // A creator may read their own ticket but only its side closes it.
            if (!Works(ticket.PaymentMethod, actor))
                return Failed("لا تملك صلاحية إغلاق هذه التذكرة.");

            if (ticket.Status != TicketStatus.Open) return Failed(ClosedError);

            if (request.Outcome is not (TicketStatus.Success or TicketStatus.Failed))
                return Failed("اختر النتيجة: تم الحل أو تعذّر الحل.");

            string? reason = null;
            if (request.Outcome == TicketStatus.Failed)
            {
                reason = Clean(request.Reason);
                if (reason == null || reason.Length < 2)
                    return Failed("اكتب سبب تعذّر الحل (حرفان على الأقل).");
            }

            // There is no claim step: closing is the moment ownership is
            // recorded, so the closer becomes the assignee even if someone
            // else held it before.
            ticket.AssignedToId = actor.UserId;
            ticket.Status = request.Outcome;
            ticket.Resolution = reason;
            ticket.ClosedAt = AppClock.Now;

            _historyRepo.Add(new TicketHistory
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

        public async Task<TicketActionResponse> ReopenAsync(int id, TicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, ticket.PaymentMethod, actor))
                return NotFound();

            // A support-queue ticket needs a manager; an Insurance ticket is
            // the insurance desk's to reopen, since nobody else can see it.
            var mayReopen = IsInsurance(ticket.PaymentMethod) ? actor.CanInsurance : actor.CanReopen;
            if (!mayReopen)
                return Failed("لا تملك صلاحية إعادة فتح هذه التذكرة.");

            if (ticket.Status == TicketStatus.Open)
                return Failed("هذه التذكرة مفتوحة بالفعل.");

            ticket.Status = TicketStatus.Open;
            ticket.ClosedAt = null;
            ticket.Resolution = null;

            _historyRepo.Add(new TicketHistory
            {
                TicketId = ticket.Id,
                ActorId = actor.UserId,
                Type = TicketActions.Reopened
            });
            await _ticketRepo.SaveChangesAsync();

            return Ok(await LoadDetailAsync(id));
        }

        // ---------- rules ----------

        private const string ClosedError = "هذه التذكرة مغلقة. أعد فتحها قبل أي إجراء آخر.";

        private static bool IsInsurance(PaymentWays? paymentMethod) => paymentMethod == PaymentWays.Insurance;
        private static bool IsInsurance(Ticket ticket) => IsInsurance(ticket.PaymentMethod);

        // Whether the actor works this ticket's side: the insurance desk for an
        // Insurance ticket, the support queue for everything else.
        private static bool Works(PaymentWays? paymentMethod, TicketActor actor) =>
            IsInsurance(paymentMethod) ? actor.CanInsurance : actor.CanWork;

        // Who may read a ticket: whoever works its side, and its creator.
        // Everyone else — the support team on an Insurance ticket included —
        // gets a 404.
        private static bool CanSee(string? createdById, PaymentWays? paymentMethod, TicketActor actor) =>
            Works(paymentMethod, actor) || createdById == actor.UserId;

        // ---------- notifications (best-effort) ----------

        // The change is already saved, so nothing here may fail the request,
        // and the SMTP send itself happens later on the email queue's
        // background worker.

        private async Task NotifyNewTicketAsync(TicketDetailResponse ticket, bool insurance, string actorId)
        {
            try
            {
                var recipients = await _userRepo.GetActiveEmailsInRolesAsync(insurance ? InsuranceRoles : QueueRoles, actorId);
                Enqueue(ticket.Id, "new-ticket", recipients,
                    to => TicketEmailTemplate.NewTicket(to, Facts(ticket), insurance, Link(ticket.Id)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket {Id}: failed to queue the new-ticket email.", ticket.Id);
            }
        }

        private async Task NotifyCreatorClosedAsync(TicketDetailResponse ticket, bool success, string? reason)
        {
            try
            {
                if (ticket.CreatedById == null) return;
                var email = await _userRepo.GetActiveEmailAsync(ticket.CreatedById);
                var recipients = email == null ? new List<string>() : new List<string> { email };
                Enqueue(ticket.Id, "closed", recipients,
                    to => TicketEmailTemplate.Closed(to, Facts(ticket), success, ticket.AssignedToName ?? "-", reason, Link(ticket.Id)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket {Id}: failed to queue the closed email.", ticket.Id);
            }
        }

        private void Enqueue(int ticketId, string what, List<string> recipients, Func<string, EmailMessage> build)
        {
            if (recipients.Count == 0)
            {
                _logger.LogInformation("Ticket {Id}: nobody with an email to tell ({What}), no email sent.", ticketId, what);
                return;
            }

            foreach (var to in recipients)
            {
                if (!_emailQueue.Enqueue(build(to)))
                    _logger.LogWarning("Ticket {Id}: {What} email to {To} was not queued.", ticketId, what, to);
            }
        }

        private static TicketEmailTemplate.TicketFacts Facts(TicketDetailResponse ticket) => new(
            ticket.Id,
            ticket.Title ?? string.Empty,
            ticket.CreatedByName,
            ticket.DepartmentName,
            ticket.PaymentMethod switch
            {
                nameof(PaymentWays.Insurance) => "تأمين",
                nameof(PaymentWays.Cash) => "نقدي",
                _ => null
            },
            ticket.CreatedDate.ToString("yyyy-MM-dd HH:mm"));

        // The console's ticket page; its login round-trips ?next= back here.
        private string? Link(int id) =>
            string.IsNullOrWhiteSpace(_consoleBaseUrl) ? null : $"{_consoleBaseUrl}/tk/tickets/{id}";

        // ---------- helpers ----------

        // The re-read every mutation answers through, unscoped: the actor was
        // already checked against the ticket before the change.
        private async Task<TicketDetailResponse?> LoadDetailAsync(int id)
        {
            var row = await _ticketRepo.GetDetailAsync(id);
            return row == null ? null : await ToDetailAsync(row);
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static TicketActionResponse Ok(TicketDetailResponse? ticket) =>
            new() { Success = true, Ticket = ticket };

        private static TicketActionResponse NotFound() =>
            new() { Success = false, NotFound = true };

        private static TicketActionResponse Failed(string error) =>
            new() { Success = false, Error = error };

        // ---------- mapping ----------

        private static TicketListItemResponse ToListItem(TicketListRow row) => Fill(new TicketListItemResponse(), row);

        private static T Fill<T>(T target, TicketListRow row) where T : TicketListItemResponse
        {
            target.Id = row.Id;
            target.Title = row.Title;
            target.Status = row.Status.ToString();
            target.Type = row.Type.ToString();
            target.CategoryId = row.CategoryId;
            target.CategoryName = row.CategoryName;
            target.ProcedureId = row.ProcedureId;
            target.ProcedureName = row.ProcedureName;
            target.ReasonId = row.ReasonId;
            target.ReasonName = row.ReasonName;
            target.PaymentMethod = row.PaymentMethod?.ToString();
            target.SourceUrl = row.SourceUrl;
            target.CreatedById = row.CreatedById;
            target.CreatedByName = row.CreatedByName;
            target.DepartmentId = row.DepartmentId;
            target.DepartmentName = row.DepartmentName;
            target.AssignedToId = row.AssignedToId;
            target.AssignedToName = row.AssignedToName;
            target.ClosedAt = row.ClosedAt;
            target.CreatedDate = row.CreatedDate;
            return target;
        }

        private async Task<TicketDetailResponse> ToDetailAsync(TicketDetailRow row)
        {
            var history = await _historyRepo.GetByTicketAsync(row.Id);

            var detail = Fill(new TicketDetailResponse(), row);
            detail.Description = row.Description;
            detail.PatientId = row.PatientId;
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
