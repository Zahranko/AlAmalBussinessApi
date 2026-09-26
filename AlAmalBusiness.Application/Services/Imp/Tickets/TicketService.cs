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
        private readonly IUserRepo _userRepo;
        private readonly IEmailQueue _emailQueue;
        private readonly ITicketNotifier _notifier;
        private readonly ILogger<TicketService> _logger;
        private readonly string? _consoleBaseUrl;

        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 12;

        // Who hears about a new ticket: whoever has to solve it, which since
        // 2026-09-19 is the support agent rather than the department roles
        // that raise them — a manager reads their department's queue, they
        // don't need an email for each one. Admins are left out, as they are
        // from the feedback and appointment notices — they would get every
        // email the hospital sends.
        private static readonly string[] QueueRoles = { AppRoles.TSupport };
        private static readonly string[] InsuranceRoles = { AppRoles.TInsurance };

        public TicketService(
            ITicketRepo ticketRepo,
            ITicketHistoryRepo historyRepo,
            ITicketCategoryRepo categoryRepo,
            ITicketProcedureRepo procedureRepo,
            IUserRepo userRepo,
            IEmailQueue emailQueue,
            ITicketNotifier notifier,
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
            _userRepo = userRepo;
            _emailQueue = emailQueue;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<TicketFormOptionsResponse> GetFormOptionsAsync()
        {
            // Sequential on purpose: both run on the one scoped DbContext,
            // which does not allow concurrent queries.
            var categories = await _categoryRepo.GetActiveAsync();
            var procedures = await _procedureRepo.GetActiveAsync();

            return new TicketFormOptionsResponse
            {
                Categories = categories.Select(c => new TicketOptionResponse { Id = c.Id, Name = c.Name }).ToList(),
                Procedures = procedures
                    .Select(p => new TicketProcedureOptionResponse { Id = p.Id, Name = p.Name, AllowsInsurance = p.AllowsInsurance })
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

            // The insurance flag is the procedure's to give: only one with
            // AllowsInsurance ("Open invoice") may carry it. Refused rather
            // than silently dropped — a ticket the raiser believed was going
            // to the insurance desk must not quietly land in the support
            // queue instead.
            var allowsInsurance = false;
            if (request.ProcedureId.HasValue)
            {
                var procedure = await _procedureRepo.GetByIdAsync(request.ProcedureId.Value);
                if (procedure == null || !procedure.IsActive)
                    return Failed("الإجراء المختار غير متاح.");
                allowsInsurance = procedure.AllowsInsurance;
            }

            if (request.IsInsurance && !allowsInsurance)
                return Failed("تأمين التذكرة متاح فقط لإجراء فتح الفاتورة.");

            var sourceUrl = Clean(request.SourceUrl);
            // The console renders this as a link, so only a real web address
            // is kept — never a javascript: or data: URL.
            if (sourceUrl != null
                && !(Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
                return Failed("رابط المصدر غير صالح.");

            var ticket = new Ticket
            {
                Title = title,
                Name = Clean(request.Name),
                Type = request.Type,
                PatientId = Clean(request.PatientId),
                SourceUrl = sourceUrl,
                CategoryId = request.CategoryId,
                ProcedureId = request.ProcedureId,
                Reason = Clean(request.Reason),
                IsInsurance = request.IsInsurance,
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
            await NotifyNewTicketAsync(detail!, ticket.IsInsurance, actor.UserId);
            await PushCreatedAsync(detail!);

            return Ok(detail);
        }

        // ---------- lists ----------

        public Task<PagedResultDTO<TicketListItemResponse>> GetQueueAsync(TicketListQuery query, TicketQueueScope scope, TicketActor actor)
        {
            query.Scope = scope;
            query.AssignedToId = scope == TicketQueueScope.Mine ? actor.UserId : null;
            query.CreatedById = null;
            // Resolved from the caller's roles and OR-ed in the repository, so
            // holding two of them widens the queue instead of narrowing it.
            query.SeesEverything = actor.IsAdmin;
            query.SeesSupportQueue = actor.CanSupport;
            query.SeesInsurance = actor.CanInsurance;
            query.SeesDepartmentId = actor.CanReadDepartment ? actor.DepartmentId : null;
            query.ViewerId = actor.UserId;
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
            if (row == null || !CanSee(row.CreatedById, row.IsInsurance, row.DepartmentId, actor))
                return null;

            return await ToDetailAsync(row);
        }

        // ---------- workflow ----------

        public async Task<TicketActionResponse> CommentAsync(int id, CommentTicketDTO request, TicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, ticket.IsInsurance, ticket.DepartmentId, actor))
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
            if (ticket == null || !CanSee(ticket.CreatedById, ticket.IsInsurance, ticket.DepartmentId, actor))
                return NotFound();

            // Reading is wider than solving: a creator follows their own
            // ticket and a manager watches their department, but only the
            // agent who owns that side closes it.
            if (!Works(ticket.IsInsurance, actor))
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
            // A close ends the parking too: if it is ever reopened it comes
            // back in its normal place, not silently still at the back.
            ticket.IsDelayed = false;

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
            {
                await NotifyCreatorClosedAsync(detail!, request.Outcome == TicketStatus.Success, reason);
                await PushResolvedAsync(detail!, request.Outcome == TicketStatus.Success, reason);
            }
            await PushChangedAsync(detail!);

            return Ok(detail);
        }

        public async Task<TicketActionResponse> ReopenAsync(int id, TicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, ticket.IsInsurance, ticket.DepartmentId, actor))
                return NotFound();

            // Whoever solves a ticket reopens it: reopening is a correction
            // to a close, so it belongs to the same desk rather than to a
            // separate seniority tier as it did before.
            if (!Works(ticket.IsInsurance, actor))
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

            var reopened = await LoadDetailAsync(id);
            await PushChangedAsync(reopened!);

            return Ok(reopened);
        }

        // Sends an open ticket to the back of the queue, or brings it back.
        // The support desk's call alone: the insurance desk's queue is its
        // own and unaffected, and the raising roles order nothing.
        public async Task<TicketActionResponse> DelayAsync(int id, DelayTicketDTO request, TicketActor actor)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null || !CanSee(ticket.CreatedById, ticket.IsInsurance, ticket.DepartmentId, actor))
                return NotFound();

            if (!CanDelay(ticket.IsInsurance, actor))
                return Failed("لا تملك صلاحية تأجيل هذه التذكرة.");

            if (ticket.Status != TicketStatus.Open) return Failed(ClosedError);

            // Asking for the state it is already in changes nothing and
            // writes no timeline entry.
            if (ticket.IsDelayed == request.Delayed) return Ok(await LoadDetailAsync(id));

            ticket.IsDelayed = request.Delayed;
            _historyRepo.Add(new TicketHistory
            {
                TicketId = ticket.Id,
                ActorId = actor.UserId,
                Type = request.Delayed ? TicketActions.Delayed : TicketActions.Undelayed
            });
            await _ticketRepo.SaveChangesAsync();

            // Still Open, so every agent's panel re-reads its queue and the
            // ticket moves to (or back from) the end.
            var detail = await LoadDetailAsync(id);
            await PushChangedAsync(detail!);

            return Ok(detail);
        }

        // ---------- dashboard ----------

        public async Task<TicketStatsResponse> GetStatsAsync(TicketStatsQuery query)
        {
            if (query.From.HasValue && query.To.HasValue && query.From > query.To)
                (query.From, query.To) = (query.To, query.From);

            var rows = await _ticketRepo.GetStatsAsync(query);

            var response = new TicketStatsResponse
            {
                From = query.From,
                To = query.To,
                OpenCount = CountOf(rows.Statuses, TicketStatus.Open),
                SuccessCount = CountOf(rows.Statuses, TicketStatus.Success),
                FailedCount = CountOf(rows.Statuses, TicketStatus.Failed),
                InsuranceCount = rows.InsuranceCount,
                AvgResolutionHours = ToHours(rows.AvgResolutionMinutes),
                OldestOpenHours = rows.OldestOpenAt.HasValue
                    ? Math.Round((AppClock.Now - rows.OldestOpenAt.Value).TotalHours, 1)
                    : null
            };

            response.Total = response.OpenCount + response.SuccessCount + response.FailedCount;
            var closed = response.SuccessCount + response.FailedCount;
            response.SuccessPercent = closed == 0 ? 0 : Math.Round(response.SuccessCount * 100.0 / closed, 1);

            response.AvgResponseHours = rows.FirstResponses.Count == 0
                ? null
                : ToHours(rows.FirstResponses.Average(r => r.Minutes));

            // One row per person, whether they answered tickets, closed them,
            // or both — keyed on the user id so the two halves meet.
            var agents = new Dictionary<string, TicketAgentStatResponse>();

            TicketAgentStatResponse For(string? id, string? name)
            {
                var key = id ?? string.Empty;
                if (!agents.TryGetValue(key, out var agent))
                {
                    agent = new TicketAgentStatResponse { UserId = id, UserName = name };
                    agents[key] = agent;
                }
                agent.UserName ??= name;
                return agent;
            }

            foreach (var closer in rows.Closers)
            {
                var agent = For(closer.ActorId, closer.ActorName);
                agent.ClosedCount = closer.ClosedCount;
                agent.SuccessCount = closer.SuccessCount;
                agent.FailedCount = closer.FailedCount;
                agent.AvgResolutionHours = ToHours(closer.AvgResolutionMinutes);
            }

            foreach (var group in rows.FirstResponses.GroupBy(r => r.ActorId ?? string.Empty))
            {
                var agent = For(group.Key, group.First().ActorName);
                agent.RespondedCount = group.Count();
                agent.AvgResponseHours = ToHours(group.Average(r => r.Minutes));
            }

            response.Agents = agents.Values
                .OrderByDescending(a => a.ClosedCount)
                .ThenByDescending(a => a.RespondedCount)
                .ThenBy(a => a.UserName)
                .ToList();

            response.Departments = Breakdown(rows.Departments);
            response.Procedures = Breakdown(rows.Procedures);
            response.Categories = Breakdown(rows.Categories);

            return response;
        }

        private static List<TicketBreakdownResponse> Breakdown(List<TicketNamedCountRow> rows) =>
            rows.OrderByDescending(r => r.Total)
                .ThenBy(r => r.Name)
                .Select(r => new TicketBreakdownResponse
                {
                    Id = r.Id,
                    Name = r.Name,
                    Total = r.Total,
                    OpenCount = r.OpenCount,
                    ClosedCount = r.Total - r.OpenCount
                })
                .ToList();

        private static int CountOf(List<TicketStatusCountRow> rows, TicketStatus status) =>
            rows.FirstOrDefault(r => r.Status == status)?.Count ?? 0;

        // Null stays null: nothing answered yet is not "answered in zero
        // hours", and the dashboard shows the two differently.
        private static double? ToHours(double? minutes) =>
            minutes.HasValue ? Math.Round(minutes.Value / 60.0, 1) : null;

        // ---------- rules ----------

        private const string ClosedError = "هذه التذكرة مغلقة. أعد فتحها قبل أي إجراء آخر.";

        // Who SOLVES a ticket: the insurance desk for a flagged one, the
        // support agent for everything else. Raising and solving are separate
        // jobs — neither a TEmployee nor a TManager closes anything, however
        // much of their department they can read.
        private static bool Works(bool isInsurance, TicketActor actor) =>
            actor.IsAdmin || (isInsurance ? actor.CanInsurance : actor.CanSupport);

        // Who may park a ticket at the back of the queue: the support agent,
        // over the tickets they solve, and Admin. Not the insurance desk and
        // not the raising roles.
        private static bool CanDelay(bool isInsurance, TicketActor actor) =>
            actor.IsAdmin || (!isInsurance && actor.CanSupport);

        // Who may READ a ticket. Wider than Works, and deliberately so: a
        // manager watches their department without being able to act on it.
        //
        // A flagged ticket is the insurance desk's alone — not the support
        // agent's, and not the raising manager's either. Its creator still
        // follows it, because nobody should lose sight of a ticket they
        // raised themselves. Everyone else gets a 404 rather than a 403: not
        // confirming that an id exists is the point.
        private static bool CanSee(string? createdById, bool isInsurance, int? departmentId, TicketActor actor)
        {
            if (actor.IsAdmin) return true;
            if (createdById == actor.UserId) return true;
            if (isInsurance) return actor.CanInsurance;
            if (actor.CanSupport) return true;

            return actor.CanReadDepartment
                && actor.DepartmentId != null
                && departmentId == actor.DepartmentId;
        }

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

        // The live push to whichever desk owns the ticket, sitting beside
        // the email rather than replacing it: the panel is for whoever is at
        // the browser now, the email for whoever isn't. Same best-effort
        // rule — the ticket is already saved and a dropped push must never
        // turn a good write into an error.
        private async Task PushCreatedAsync(TicketDetailResponse ticket)
        {
            try
            {
                var push = new TicketPushResponse
                {
                    Id = ticket.Id,
                    Title = ticket.Title,
                    Name = ticket.Name,
                    Status = ticket.Status,
                    Type = ticket.Type,
                    CategoryId = ticket.CategoryId,
                    CategoryName = ticket.CategoryName,
                    ProcedureId = ticket.ProcedureId,
                    ProcedureName = ticket.ProcedureName,
                    IsInsurance = ticket.IsInsurance,
                    SourceUrl = ticket.SourceUrl,
                    CreatedById = ticket.CreatedById,
                    CreatedByName = ticket.CreatedByName,
                    DepartmentId = ticket.DepartmentId,
                    DepartmentName = ticket.DepartmentName,
                    AssignedToId = ticket.AssignedToId,
                    AssignedToName = ticket.AssignedToName,
                    ClosedAt = ticket.ClosedAt,
                    CreatedDate = ticket.CreatedDate,
                    // The timeline is left off on purpose: a brand-new
                    // ticket's history is one Created entry, and the panel
                    // re-reads the ticket anyway the moment an agent opens it.
                    Reason = ticket.Reason,
                    PatientId = ticket.PatientId
                };
                await _notifier.TicketCreatedAsync(push);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket {Id}: failed to push the new ticket to the agents.", ticket.Id);
            }
        }

        // The raiser's own live push, sent wherever the closed email is sent
        // and on the same terms: best-effort, after the write, never to the
        // person who did it. The email is the record for whoever isn't at a
        // browser; this is for whoever is.
        private async Task PushResolvedAsync(TicketDetailResponse ticket, bool success, string? reason)
        {
            try
            {
                if (ticket.CreatedById == null) return;
                await _notifier.TicketResolvedAsync(new TicketResolvedPush
                {
                    CreatedById = ticket.CreatedById,
                    TicketId = ticket.Id,
                    Title = ticket.Title,
                    Name = ticket.Name,
                    Status = ticket.Status,
                    Success = success,
                    ByName = ticket.AssignedToName,
                    Resolution = reason,
                    ProcedureName = ticket.ProcedureName,
                    ClosedAt = ticket.ClosedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket {Id}: failed to push the close to whoever raised it.", ticket.Id);
            }
        }

        private async Task PushChangedAsync(TicketDetailResponse ticket)
        {
            try
            {
                await _notifier.TicketChangedAsync(
                    ticket.Id, ticket.IsInsurance, ticket.Status ?? string.Empty, ticket.AssignedToName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ticket {Id}: failed to push the change to the agents.", ticket.Id);
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
            ticket.IsInsurance ? "تأمين" : null,
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
            target.Name = row.Name;
            target.Status = row.Status.ToString();
            target.Type = row.Type.ToString();
            target.CategoryId = row.CategoryId;
            target.CategoryName = row.CategoryName;
            target.ProcedureId = row.ProcedureId;
            target.ProcedureName = row.ProcedureName;
            target.IsInsurance = row.IsInsurance;
            target.IsDelayed = row.IsDelayed;
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
            detail.Reason = row.Reason;
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
