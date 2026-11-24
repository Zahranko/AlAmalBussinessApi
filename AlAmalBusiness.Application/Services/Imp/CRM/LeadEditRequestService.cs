using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.CRM.Lead;
using AlAmalBusiness.Application.DTOs.CRM.LeadEditRequest;
using AlAmalBusiness.Application.DTOs.Push;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.CRM
{
    // "Request edit" (2026-11-22): an employee proposes a corrected lead, an
    // Admin approves or declines it. Approval is applied by
    // LeadService.ApplyApprovedEditAsync — the same code the admin's own edit
    // runs — so the two paths can't drift apart on validation or on what the
    // timeline records.
    public class LeadEditRequestService : ILeadEditRequestService
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private readonly ILeadEditRequestRepo _repo;
        private readonly ILeadRepo _leadRepo;
        private readonly ILeadHistoryRepo _historyRepo;
        private readonly IReferalSourceRepo _referalSourceRepo;
        private readonly IProcedureRepo _procedureRepo;
        private readonly IDoctorRepo _doctorRepo;
        private readonly ILeadService _leadService;
        private readonly IPushNotifier _push;
        private readonly ILogger<LeadEditRequestService> _logger;

        public LeadEditRequestService(
            ILeadEditRequestRepo repo,
            ILeadRepo leadRepo,
            ILeadHistoryRepo historyRepo,
            IReferalSourceRepo referalSourceRepo,
            IProcedureRepo procedureRepo,
            IDoctorRepo doctorRepo,
            ILeadService leadService,
            IPushNotifier push,
            ILogger<LeadEditRequestService> logger)
        {
            _repo = repo;
            _leadRepo = leadRepo;
            _historyRepo = historyRepo;
            _referalSourceRepo = referalSourceRepo;
            _procedureRepo = procedureRepo;
            _doctorRepo = doctorRepo;
            _leadService = leadService;
            _push = push;
            _logger = logger;
        }

        public async Task<LeadEditRequestResponse> CreateAsync(int leadId, CreateLeadEditRequestDTO request, LeadEditActor actor)
        {
            var lead = await _leadRepo.GetLeadByIdAsync(leadId, includeLookups: true)
                ?? throw new InvalidOperationException("This lead no longer exists.");

            if (await _repo.GetPendingIdForLeadAsync(leadId) != null)
                throw new InvalidOperationException("An edit request for this case is already waiting for an admin.");

            var referal = await _referalSourceRepo.GetByIdAsync(request.ReferalId);
            if (referal == null || !referal.IsActive)
                throw new InvalidOperationException("Please select a valid referral source.");

            var procedure = await _procedureRepo.GetByIdAsync(request.ProcedureId);
            if (procedure == null || !procedure.IsActive)
                throw new InvalidOperationException("Please select a valid procedure.");

            string? doctorName = null;
            if (request.DoctorId.HasValue)
            {
                var doctor = await _doctorRepo.GetByIdAsync(request.DoctorId.Value);
                if (doctor == null || !doctor.IsActive)
                    throw new InvalidOperationException("Please select a valid doctor.");
                doctorName = doctor.Name;
            }

            // A successful case is defined by its doctor (FollowUpAsync's rule),
            // so an edit may change the doctor but not take it away.
            if (lead.Status == LeadStatus.Success && !request.DoctorId.HasValue)
                throw new InvalidOperationException("A successful case needs a doctor.");

            var proposed = new LeadEditValues
            {
                Name = Clean(request.Name),
                CountryKey = Clean(request.CountryKey),
                PhoneNum = Clean(request.PhoneNum),
                NickName = Clean(request.NickName),
                Description = request.Description,
                PaymentWay = request.PaymentWay,
                DoctorId = request.DoctorId,
                DoctorName = doctorName,
                ReferalId = referal.Id,
                ReferalName = referal.Name,
                ProcedureId = procedure.Id,
                ProcedureName = procedure.Name,
            };
            var current = new LeadEditValues
            {
                Name = lead.Name,
                CountryKey = lead.CountryKey,
                PhoneNum = lead.PhoneNum,
                NickName = lead.NickName,
                Description = lead.Description,
                PaymentWay = lead.PaymentWay,
                DoctorId = lead.DoctorId,
                DoctorName = lead.Doctor?.Name,
                ReferalId = lead.ReferalId,
                ReferalName = lead.Referal?.Name,
                ProcedureId = lead.ProcedureId,
                ProcedureName = lead.Procedure?.Name,
            };

            var changes = Diff(current, proposed);
            if (changes.Count == 0)
                throw new InvalidOperationException("Nothing was changed.");

            var entity = new LeadEditRequest
            {
                LeadId = lead.Id,
                RequestedById = actor.UserId,
                Note = Clean(request.Note),
                Name = proposed.Name,
                CountryKey = proposed.CountryKey,
                PhoneNum = proposed.PhoneNum,
                NickName = proposed.NickName,
                Description = proposed.Description,
                PaymentWay = proposed.PaymentWay,
                DoctorId = proposed.DoctorId,
                ReferalId = proposed.ReferalId,
                ProcedureId = proposed.ProcedureId,
            };
            _repo.Add(entity);
            _historyRepo.Add(new LeadHistory
            {
                LeadId = lead.Id,
                ActorId = actor.UserId,
                Type = LeadActions.EditRequested,
                Note = "Asked to change: " + string.Join(", ", changes.Select(c => c.Label))
                    + (entity.Note == null ? "" : $" — {entity.Note}"),
                ActionDate = entity.RequestedAt
            });
            await _repo.SaveChangesAsync();

            if (_push.IsEnabled)
            {
                _push.ToRoles(new[] { nameof(AppRoles.Admin) }, actor.UserId, new PushMessage
                {
                    Title = "Edit request",
                    Body = $"Case #{lead.Id} · {string.Join(", ", changes.Select(c => c.Label))}",
                    Kind = "lead-edit",
                    Id = entity.Id.ToString(),
                    Thread = $"lead-{lead.Id}",
                });
            }

            return (await GetAsync(entity.Id, actor))!;
        }

        public async Task<PagedResultDTO<LeadEditRequestResponse>> GetPagedAsync(
            LeadEditRequestStatus? status, bool mineOnly, int page, int pageSize, LeadEditActor actor)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

            // Only an admin reads other people's requests.
            var requestedBy = actor.IsAdmin && !mineOnly ? null : actor.UserId;
            var (items, total) = await _repo.PageAsync(status, requestedBy, page, pageSize);

            return new PagedResultDTO<LeadEditRequestResponse>
            {
                Items = items.Select(ToResponse).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
            };
        }

        // An employee reads only their own; anything else answers 404.
        public async Task<LeadEditRequestResponse?> GetAsync(int id, LeadEditActor actor)
        {
            var row = await _repo.GetRowAsync(id);
            if (row == null) return null;
            if (!actor.IsAdmin && row.RequestedById != actor.UserId) return null;
            return ToResponse(row);
        }

        public Task<int> CountPendingAsync() => _repo.CountPendingAsync();

        public async Task<LeadEditRequestResponse> ApproveAsync(int id, string? note, LeadEditActor admin)
        {
            var (request, row) = await LoadPendingAsync(id);

            request.Status = LeadEditRequestStatus.Approved;
            request.ReviewedById = admin.UserId;
            request.ReviewedAt = AppClock.Now;
            request.ReviewNote = Clean(note);

            var lead = await _leadRepo.GetLeadByIdAsync(request.LeadId)
                ?? throw new InvalidOperationException("This lead no longer exists.");

            // Saves the lead, its Edited entry and this request's new status
            // in one SaveChanges (one scoped DbContext under every repo): if
            // the edit is refused — a lookup retired since — nothing is saved.
            await _leadService.ApplyApprovedEditAsync(request.LeadId, admin.UserId, new AdminUpdateLeadDTO
            {
                Name = request.Name,
                CountryKey = request.CountryKey,
                PhoneNum = request.PhoneNum,
                NickName = request.NickName,
                Description = request.Description,
                PaymentWay = request.PaymentWay ?? lead.PaymentWay ?? PaymentWays.Cash,
                HasDoctor = request.DoctorId.HasValue || lead.HasDoctor,
                DoctorId = request.DoctorId,
                ReferalId = request.ReferalId,
                ProcedureId = request.ProcedureId,
            }, $"Approved edit request #{request.Id} from {row.RequestedByName}"
               + (request.ReviewNote == null ? "" : $" ({request.ReviewNote})"));

            NotifyRequester(row, approved: true);
            return (await GetAsync(id, admin))!;
        }

        public async Task<LeadEditRequestResponse> DeclineAsync(int id, string? note, LeadEditActor admin)
        {
            var (request, row) = await LoadPendingAsync(id);

            request.Status = LeadEditRequestStatus.Declined;
            request.ReviewedById = admin.UserId;
            request.ReviewedAt = AppClock.Now;
            request.ReviewNote = Clean(note);

            _historyRepo.Add(new LeadHistory
            {
                LeadId = request.LeadId,
                ActorId = admin.UserId,
                Type = LeadActions.EditDeclined,
                Note = $"Declined edit request #{request.Id} from {row.RequestedByName}"
                    + (request.ReviewNote == null ? "" : $": {request.ReviewNote}"),
                ActionDate = request.ReviewedAt
            });
            await _repo.SaveChangesAsync();

            NotifyRequester(row, approved: false);
            return (await GetAsync(id, admin))!;
        }

        private async Task<(LeadEditRequest Request, LeadEditRequestRow Row)> LoadPendingAsync(int id)
        {
            // The row read goes through the lead, so a request whose lead was
            // deleted is "not found" here as well.
            var row = await _repo.GetRowAsync(id)
                ?? throw new KeyNotFoundException("This edit request no longer exists.");
            if (row.Status != LeadEditRequestStatus.Pending)
                throw new InvalidOperationException(
                    $"This request was already {(row.Status == LeadEditRequestStatus.Approved ? "approved" : "declined")}.");
            var request = await _repo.GetTrackedAsync(id)
                ?? throw new KeyNotFoundException("This edit request no longer exists.");
            return (request, row);
        }

        private void NotifyRequester(LeadEditRequestRow row, bool approved)
        {
            if (!_push.IsEnabled || row.RequestedById == null) return;
            try
            {
                _push.ToUsers(new[] { row.RequestedById }, new PushMessage
                {
                    Title = approved ? "Edit approved" : "Edit declined",
                    Body = $"Case #{row.LeadId}",
                    Kind = "lead",
                    Id = row.LeadId.ToString(),
                    Thread = $"lead-{row.LeadId}",
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Edit request {Id}: push to the requester failed.", row.Id);
            }
        }

        private static LeadEditRequestResponse ToResponse(LeadEditRequestRow row) => new()
        {
            Id = row.Id,
            LeadId = row.LeadId,
            LeadName = row.LeadName,
            LeadStatus = row.LeadStatus,
            RequestedByName = row.RequestedByName,
            RequestedAt = row.RequestedAt,
            Note = row.Note,
            Status = row.Status,
            ReviewedByName = row.ReviewedByName,
            ReviewedAt = row.ReviewedAt,
            ReviewNote = row.ReviewNote,
            Changes = Diff(row.Current, row.Proposed),
        };

        // A decided request still diffs against the lead as it is now; for an
        // approved one that is usually empty, so clients show the timeline's
        // Edited entry for what was changed at the time.
        private static List<LeadEditChangeResponse> Diff(LeadEditValues from, LeadEditValues to)
        {
            var list = new List<LeadEditChangeResponse>();
            void Add(string field, string label, string? a, string? b)
            {
                if ((a ?? "") != (b ?? "")) list.Add(new() { Field = field, Label = label, From = a, To = b });
            }

            Add("name", "Name", from.Name, to.Name);
            Add("nickName", "Nickname", from.NickName, to.NickName);
            Add("countryKey", "Country code", from.CountryKey, to.CountryKey);
            Add("phone", "Phone", from.PhoneNum, to.PhoneNum);
            Add("procedure", "Procedure", from.ProcedureName, to.ProcedureName);
            Add("referral", "Referral source", from.ReferalName, to.ReferalName);
            Add("payment", "Payment", from.PaymentWay?.ToString(), to.PaymentWay?.ToString());
            Add("doctor", "Doctor", from.DoctorName, to.DoctorName);
            Add("description", "What they asked about", from.Description, to.Description);
            return list;
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
