using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.CRM.Lead;
using AlAmalBusiness.Application.DTOs.CRM.Lead.Response;
using AlAmalBusiness.Application.DTOs.CRM.Stats;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.CRM
{
    public class LeadService : ILeadService
    {
        private readonly ILeadRepo _leadRepo;
        private readonly ILeadHistoryRepo _historyRepo;
        private readonly ILeadCallRepo _callRepo;
        private readonly IReferalSourceRepo _referalSourceRepo;
        private readonly IProcedureRepo _procedureRepo;
        private readonly IDoctorRepo _doctorRepo;
        private readonly IClosedReasonRepo _closedReasonRepo;
        private readonly IUserRepo _userRepo;
        private readonly ILeadNotifier _notifier;

        private const int MaxCallsPerLead = 6;

        public LeadService(
            ILeadRepo leadRepo,
            ILeadHistoryRepo historyRepo,
            ILeadCallRepo callRepo,
            IReferalSourceRepo referalSourceRepo,
            IProcedureRepo procedureRepo,
            IDoctorRepo doctorRepo,
            IClosedReasonRepo closedReasonRepo,
            IUserRepo userRepo,
            ILeadNotifier notifier)
        {
            _leadRepo = leadRepo;
            _historyRepo = historyRepo;
            _callRepo = callRepo;
            _referalSourceRepo = referalSourceRepo;
            _procedureRepo = procedureRepo;
            _doctorRepo = doctorRepo;
            _closedReasonRepo = closedReasonRepo;
            _userRepo = userRepo;
            _notifier = notifier;
        }

        public async Task<CreateLeadResponse> CreateLeadAsync(CreateLeadDTO lead, string currentUserId)
        {
            var referalSource = await _referalSourceRepo.GetByIdAsync(lead.ReferalId);
            if (referalSource == null || !referalSource.IsActive)
                return new CreateLeadResponse { Success = false, Error = "Please select a valid referral source." };

            var procedure = await _procedureRepo.GetByIdAsync(lead.ProcedureId);
            if (procedure == null || !procedure.IsActive)
                return new CreateLeadResponse { Success = false, Error = "Please select a valid procedure." };

            int? doctorId = null;
            if (lead.DoctorId.HasValue)
            {
                var doctor = await _doctorRepo.GetByIdAsync(lead.DoctorId.Value);
                if (doctor == null || !doctor.IsActive)
                    return new CreateLeadResponse { Success = false, Error = "Please select a valid doctor." };
                doctorId = doctor.Id;
            }

            var newLead = new Lead
            {
                Name = lead.Name,
                CountryKey = lead.CountryKey,
                PhoneNum = lead.PhoneNum,
                NickName = lead.NickName,
                Description = lead.Description,
                PaymentWay = lead.PaymentWay,
                HasDoctor = lead.HasDoctor,
                DoctorId = doctorId,
                ReferalId = referalSource.Id,
                ProcedureId = procedure.Id,
                Status = LeadStatus.New,
                CreatedById = currentUserId
            };

            await _leadRepo.CreateLeadAsync(newLead);

            _historyRepo.Add(new LeadHistory
            {
                LeadId = newLead.Id,
                ActorId = currentUserId,
                Type = LeadActions.Created,
                ResultingStatus = LeadStatus.New,
                ActionDate = newLead.CreatedDate
            });
            await _leadRepo.SaveChangesAsync();

            var detail = await _leadRepo.GetLeadDetailAsync(newLead.Id);
            var item = ToListItem(detail!);

            await TryNotifyAsync(() => _notifier.LeadCreatedAsync(item));

            return new CreateLeadResponse { Success = true, Lead = item };
        }

        public async Task DeleteLeadAsync(int id)
        {
            var lead = await _leadRepo.GetLeadByIdAsync(id);
            if (lead != null)
                await _leadRepo.DeleteLeadAsync(lead);
        }

        public async Task<LeadDetailResponse?> GetLeadDetailAsync(int id)
        {
            var lead = await _leadRepo.GetLeadDetailAsync(id);
            return lead == null ? null : await ToDetailAsync(lead);
        }

        // The only list endpoint enriched with last-call info today — it's
        // what the case calendar (GET /api/Lead) reads to place cases by the
        // date of their most recently logged call.
        public async Task<List<LeadListItemResponse>> GetAllLeadsAsync(bool excludeCompleted = false)
        {
            var leads = await _leadRepo.GetAllLeadsAsync(excludeCompleted);
            var lastCalls = await _callRepo.GetLastCallsByLeadIdsAsync(leads.Select(l => l.Id));
            return leads.Select(l => ToListItem(l, lastCalls.GetValueOrDefault(l.Id))).ToList();
        }

        public async Task<List<LeadListItemResponse>> GetMineAsync(string userId, bool excludeCompleted = false) =>
            (await _leadRepo.GetMineAsync(userId, excludeCompleted)).Select(l => ToListItem(l)).ToList();

        public async Task<List<LeadListItemResponse>> GetCreatedByMeAsync(string userId, bool excludeCompleted = false) =>
            (await _leadRepo.GetCreatedByMeAsync(userId, excludeCompleted)).Select(l => ToListItem(l)).ToList();

        public async Task<PagedResultDTO<LeadListItemResponse>> GetPagedAsync(LeadListQuery query)
        {
            ClampPaging(query);
            var (items, total) = await _leadRepo.GetPagedAsync(query);
            return new PagedResultDTO<LeadListItemResponse> { Items = items.Select(l => ToListItem(l)).ToList(), TotalCount = total, Page = query.Page, PageSize = query.PageSize };
        }

        public async Task<PagedResultDTO<LeadListItemResponse>> GetCreatedByMePagedAsync(string userId, LeadListQuery query)
        {
            ClampPaging(query);
            var (items, total) = await _leadRepo.GetCreatedByMePagedAsync(userId, query);
            return new PagedResultDTO<LeadListItemResponse> { Items = items.Select(l => ToListItem(l)).ToList(), TotalCount = total, Page = query.Page, PageSize = query.PageSize };
        }

        private static void ClampPaging(LeadListQuery query)
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = Math.Clamp(query.PageSize <= 0 ? 12 : query.PageSize, 1, 100);
        }

        public async Task<LeadActionResponse> ClaimLeadAsync(int id, string userId)
        {
            var lead = await GetLeadOrThrow(id);
            EnsureNotClosed(lead);

            if (lead.ClaimedById == userId)
                throw new InvalidOperationException("This lead is already assigned to you.");

            lead.ClaimedById = userId;

            _historyRepo.Add(new LeadHistory
            {
                LeadId = lead.Id,
                ActorId = userId,
                Type = LeadActions.Claimed,
                ActionDate = DateTime.Now
            });
            await _leadRepo.SaveChangesAsync();

            return await ReloadActionResponse(lead.Id);
        }

        public async Task<LeadActionResponse> FollowUpAsync(int id, string userId, FollowUpLeadDTO request)
        {
            if (request.Status == LeadStatus.New)
                throw new InvalidOperationException("New is not a valid follow-up outcome.");

            var lead = await GetLeadOrThrow(id);
            EnsureNotClosed(lead);

            var history = new LeadHistory
            {
                LeadId = lead.Id,
                ActorId = userId,
                Type = LeadActions.FollowUp,
                ResultingStatus = request.Status,
                ActionDate = request.Date ?? DateTime.Now,
                Note = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            if (request.Status == LeadStatus.Waiting)
            {
                lead.AppointmentDate = request.Date;
                lead.HasDoctor = request.HasDoctor ?? false;

                if (request.DoctorId.HasValue)
                {
                    var doctor = await _doctorRepo.GetByIdAsync(request.DoctorId.Value);
                    if (doctor == null || !doctor.IsActive)
                        throw new InvalidOperationException("Please select a valid doctor.");
                    lead.DoctorId = doctor.Id;
                    history.DoctorId = doctor.Id;
                }
                else
                {
                    lead.DoctorId = null;
                }

                if (request.PaymentWay.HasValue)
                    lead.PaymentWay = request.PaymentWay.Value;
            }
            else if (request.Status == LeadStatus.Success)
            {
                if (request.HasDoctor == true)
                {
                    lead.HasDoctor = true;
                    if (!string.IsNullOrWhiteSpace(request.SignatureData))
                        lead.ClinicSignature = request.SignatureData;

                    if (request.DoctorId.HasValue)
                    {
                        var doctor = await _doctorRepo.GetByIdAsync(request.DoctorId.Value);
                        if (doctor == null || !doctor.IsActive)
                            throw new InvalidOperationException("Please select a valid doctor.");
                        lead.DoctorId = doctor.Id;
                        history.DoctorId = doctor.Id;
                    }
                }
            }
            else if (request.Status == LeadStatus.Closed)
            {
                var closedReason = request.ClosedReasonId.HasValue
                    ? await _closedReasonRepo.GetByIdAsync(request.ClosedReasonId.Value)
                    : null;
                if (closedReason == null || !closedReason.IsActive)
                    throw new InvalidOperationException("Please select a valid closed reason.");

                lead.ClosedReasonId = closedReason.Id;
                history.ClosedReasonId = closedReason.Id;
            }
            else if (request.Status == LeadStatus.Pending)
            {
                if (string.IsNullOrWhiteSpace(request.Notes))
                    throw new InvalidOperationException("Please add notes for this update.");

                ApplyPendingContactInfoUpdate(lead, request, userId);
            }

            lead.Status = request.Status;
            _historyRepo.Add(history);
            await _leadRepo.SaveChangesAsync();

            await TryNotifyAsync(() => _notifier.LeadStatusChangedAsync(lead.Id, lead.Status.ToString()));

            return await ReloadActionResponse(lead.Id);
        }

        // Optional Name/CountryKey/PhoneNum/NickName/PaymentWay correction offered
        // alongside a Pending follow-up. Each field is null ("don't touch") unless
        // the caller explicitly supplies it. Logged as its own Edited entry
        // (separate from the follow-up's own note) only when something changed.
        private void ApplyPendingContactInfoUpdate(Lead lead, FollowUpLeadDTO request, string userId)
        {
            var newName = request.Name is not null ? request.Name.Trim() : lead.Name;
            var newCountryKey = request.CountryKey is not null ? request.CountryKey.Trim() : lead.CountryKey;
            var newPhoneNum = request.PhoneNum is not null ? request.PhoneNum.Trim() : lead.PhoneNum;
            var newNickName = request.NickName is not null
                ? (string.IsNullOrWhiteSpace(request.NickName) ? null : request.NickName.Trim())
                : lead.NickName;
            var newPaymentWay = request.PaymentWay ?? lead.PaymentWay;

            var changes = DescribeChanges(
                ("Name", lead.Name, newName),
                ("Country key", lead.CountryKey, newCountryKey),
                ("Phone", lead.PhoneNum, newPhoneNum),
                ("Nickname", lead.NickName, newNickName),
                ("Payment way", lead.PaymentWay?.ToString(), newPaymentWay?.ToString()));

            lead.Name = newName;
            lead.CountryKey = newCountryKey;
            lead.PhoneNum = newPhoneNum;
            lead.NickName = newNickName;
            lead.PaymentWay = newPaymentWay;

            if (changes.Count > 0)
            {
                _historyRepo.Add(new LeadHistory
                {
                    LeadId = lead.Id,
                    ActorId = userId,
                    Type = LeadActions.Edited,
                    Note = string.Join("; ", changes),
                    ActionDate = DateTime.Now
                });
            }
        }

        public async Task<LeadActionResponse> ReopenAsync(int id, string adminUserId)
        {
            var lead = await GetLeadOrThrow(id);

            if (lead.Status != LeadStatus.Success && lead.Status != LeadStatus.Closed)
                throw new InvalidOperationException("Only completed leads (Success or Closed) can be reopened.");

            lead.Status = LeadStatus.New;
            _historyRepo.Add(new LeadHistory
            {
                LeadId = lead.Id,
                ActorId = adminUserId,
                Type = LeadActions.ReOpened,
                ResultingStatus = LeadStatus.New,
                ActionDate = DateTime.Now
            });
            await _leadRepo.SaveChangesAsync();

            return await ReloadActionResponse(lead.Id);
        }

        // Up to 6 calls per lead. Logging the first call on a New lead moves it
        // to Pending; any other status (including an already-Pending lead) is
        // left untouched.
        public async Task<LeadActionResponse> LogCallAsync(int id, string userId, LeadCallDTO request)
        {
            var lead = await GetLeadOrThrow(id);
            EnsureNotClosed(lead);

            var existingCount = await _callRepo.CountByLeadAsync(id);
            if (existingCount >= MaxCallsPerLead)
                throw new InvalidOperationException($"This lead already has the maximum of {MaxCallsPerLead} calls.");

            _callRepo.Add(new LeadCall
            {
                LeadId = lead.Id,
                ActorId = userId,
                Date = request.Date,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
            });

            if (lead.Status == LeadStatus.New)
                lead.Status = LeadStatus.Pending;

            await _leadRepo.SaveChangesAsync();

            await TryNotifyAsync(() => _notifier.LeadStatusChangedAsync(lead.Id, lead.Status.ToString()));

            return await ReloadActionResponse(lead.Id);
        }

        public async Task<LeadActionResponse> MarkCallDoneAsync(int id, int callId, string userId)
        {
            await GetLeadOrThrow(id);

            var call = await _callRepo.GetByIdAsync(callId);
            if (call == null || call.LeadId != id)
                throw new InvalidOperationException("This call no longer exists.");
            if (call.IsDone)
                throw new InvalidOperationException("This call is already marked done.");

            call.IsDone = true;
            await _leadRepo.SaveChangesAsync();

            return await ReloadActionResponse(id);
        }

        // Admin-only: edits every base field of an existing lead, regardless of
        // status — a data-correction tool, deliberately bypasses EnsureNotClosed.
        public async Task<LeadActionResponse> AdminUpdateLeadAsync(int id, string adminUserId, AdminUpdateLeadDTO request)
        {
            var lead = await GetLeadOrThrow(id);

            var referalSource = await _referalSourceRepo.GetByIdAsync(request.ReferalId);
            if (referalSource == null || !referalSource.IsActive)
                throw new InvalidOperationException("Please select a valid referral source.");

            var procedure = await _procedureRepo.GetByIdAsync(request.ProcedureId);
            if (procedure == null || !procedure.IsActive)
                throw new InvalidOperationException("Please select a valid procedure.");

            int? doctorId = null;
            string? doctorName = null;
            if (request.DoctorId.HasValue)
            {
                var doctor = await _doctorRepo.GetByIdAsync(request.DoctorId.Value);
                if (doctor == null || !doctor.IsActive)
                    throw new InvalidOperationException("Please select a valid doctor.");
                doctorId = doctor.Id;
                doctorName = doctor.Name;
            }

            var changes = DescribeChanges(
                ("Name", lead.Name, request.Name),
                ("Country key", lead.CountryKey, request.CountryKey),
                ("Phone", lead.PhoneNum, request.PhoneNum),
                ("Nickname", lead.NickName, request.NickName),
                ("Referral source", lead.Referal?.Name, referalSource.Name),
                ("Procedure", lead.Procedure?.Name, procedure.Name),
                ("Payment way", lead.PaymentWay.ToString(), request.PaymentWay.ToString()),
                ("Doctor", lead.Doctor?.Name, doctorName),
                ("Description", lead.Description, request.Description));

            lead.Name = request.Name;
            lead.CountryKey = request.CountryKey;
            lead.PhoneNum = request.PhoneNum;
            lead.NickName = request.NickName;
            lead.ReferalId = referalSource.Id;
            lead.ProcedureId = procedure.Id;
            lead.PaymentWay = request.PaymentWay;
            lead.HasDoctor = request.HasDoctor;
            lead.DoctorId = doctorId;
            lead.Description = request.Description;

            if (changes.Count > 0)
            {
                _historyRepo.Add(new LeadHistory
                {
                    LeadId = lead.Id,
                    ActorId = adminUserId,
                    Type = LeadActions.Edited,
                    Note = string.Join("; ", changes),
                    ActionDate = DateTime.Now
                });
            }

            await _leadRepo.SaveChangesAsync();
            return await ReloadActionResponse(lead.Id);
        }

        private static List<string> DescribeChanges(params (string Label, string? Old, string? New)[] fields)
        {
            var changes = new List<string>();
            foreach (var (label, oldValue, newValue) in fields)
            {
                var o = oldValue ?? "-";
                var n = newValue ?? "-";
                if (o != n) changes.Add($"{label}: \"{o}\" -> \"{n}\"");
            }
            return changes;
        }

        public async Task<List<AssignableUserResponse>> GetActiveUsersAsync()
        {
            var users = await _userRepo.GetAllUserAsync();
            return users
                .Where(u => u.IsActive)
                .Select(u => new AssignableUserResponse { Id = u.Id, Username = u.UserName })
                .ToList();
        }

        public async Task<QueueCountsResponse> GetQueueCountsAsync(string userId)
        {
            var (all, today, mine, unassigned, closed) = await _leadRepo.GetQueueCountsAsync(userId);
            return new QueueCountsResponse { All = all, Today = today, Mine = mine, Unassigned = unassigned, Closed = closed };
        }

        public async Task<DashboardKpiDTO> GetDashboardKpisAsync()
        {
            var now = DateTime.Now;
            var todayStart = now.Date;
            var tomorrowStart = todayStart.AddDays(1);
            var yesterdayStart = todayStart.AddDays(-1);
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);
            var priorMonthStart = monthStart.AddMonths(-1);
            var trendFrom = todayStart.AddDays(-6);

            return new DashboardKpiDTO
            {
                LeadsMonth = await BuildKpiAsync(_leadRepo.CountCreatedInRangeAsync, _leadRepo.GetCreatedDailyCountsAsync,
                    monthStart, nextMonthStart, priorMonthStart, monthStart, trendFrom, tomorrowStart),
                LeadsToday = await BuildKpiAsync(_leadRepo.CountCreatedInRangeAsync, _leadRepo.GetCreatedDailyCountsAsync,
                    todayStart, tomorrowStart, yesterdayStart, todayStart, trendFrom, tomorrowStart),
                SuccessMonth = await BuildKpiAsync(_historyRepo.CountSucceededInRangeAsync, _historyRepo.GetSucceededDailyCountsAsync,
                    monthStart, nextMonthStart, priorMonthStart, monthStart, trendFrom, tomorrowStart),
                SuccessToday = await BuildKpiAsync(_historyRepo.CountSucceededInRangeAsync, _historyRepo.GetSucceededDailyCountsAsync,
                    todayStart, tomorrowStart, yesterdayStart, todayStart, trendFrom, tomorrowStart)
            };
        }

        private static async Task<KpiMetricDTO> BuildKpiAsync(
            Func<DateTime, DateTime, Task<int>> countAsync,
            Func<DateTime, DateTime, Task<Dictionary<DateTime, int>>> dailyCountsAsync,
            DateTime currentFrom, DateTime currentTo,
            DateTime priorFrom, DateTime priorTo,
            DateTime trendFrom, DateTime trendTo)
        {
            var current = await countAsync(currentFrom, currentTo);
            var prior = await countAsync(priorFrom, priorTo);
            var daily = await dailyCountsAsync(trendFrom, trendTo);

            var trend = new List<int>();
            for (var day = trendFrom; day < trendTo; day = day.AddDays(1))
                trend.Add(daily.GetValueOrDefault(day, 0));

            var deltaPercent = prior == 0 ? (current == 0 ? 0 : 100) : Math.Round((double)(current - prior) / prior * 100, 1);
            var direction = current == prior ? "flat" : (current > prior ? "up" : "down");

            return new KpiMetricDTO { Value = current, DeltaPercent = deltaPercent, Direction = direction, Trend = trend };
        }

        public async Task<List<EmployeeCaseCountDTO>> GetEmployeeCaseCountsAsync(string period)
        {
            var now = DateTime.Now;
            DateTime from, to;
            if (period == "today")
            {
                from = now.Date;
                to = from.AddDays(1);
            }
            else
            {
                from = new DateTime(now.Year, now.Month, 1);
                to = from.AddMonths(1);
            }

            var counts = await _leadRepo.GetCreatedCountsByUserInRangeAsync(from, to);
            return counts
                .Select(c => new EmployeeCaseCountDTO { UserId = c.UserId, Username = c.Username ?? "Unknown", Count = c.Count })
                .OrderByDescending(c => c.Count)
                .ToList();
        }

        public async Task<LeadStatusBandDTO> GetStatusBandAsync()
        {
            var statusCounts = await _leadRepo.GetStatusCountsAsync();
            return new LeadStatusBandDTO
            {
                Total = statusCounts.Values.Sum(),
                Statuses = statusCounts.Select(kv => new LeadStatusCountDTO { Status = kv.Key.ToString(), Count = kv.Value }).ToList()
            };
        }

        public async Task<List<ReferralSourceStatDTO>> GetLeadSourcesAsync()
        {
            var total = await _leadRepo.CountAllAsync();
            var refCounts = await _leadRepo.GetReferralSourceCountsAsync();
            var allSources = await _referalSourceRepo.GetAllAsync();

            return allSources
                .Where(r => refCounts.ContainsKey(r.Id))
                .Select(r => new ReferralSourceStatDTO
                {
                    Name = r.Name!,
                    Count = refCounts[r.Id],
                    Percent = total > 0 ? Math.Round((double)refCounts[r.Id] / total * 100, 1) : 0
                })
                .OrderByDescending(r => r.Count)
                .ToList();
        }

        public async Task<AdminStatsDTO> GetStatsAsync()
        {
            var total = await _leadRepo.CountAllAsync();
            var statusCounts = await _leadRepo.GetStatusCountsAsync();
            var refCounts = await _leadRepo.GetReferralSourceCountsAsync();
            var allSources = await _referalSourceRepo.GetAllAsync();
            var creatorCounts = await _leadRepo.GetLeadCountsByCreatorAsync();

            var successCount = statusCounts.GetValueOrDefault(LeadStatus.Success);
            var closedCount = statusCounts.GetValueOrDefault(LeadStatus.Closed);

            var refStats = allSources
                .Where(r => refCounts.ContainsKey(r.Id))
                .Select(r => new ReferralSourceStatDTO
                {
                    Name = r.Name!,
                    Count = refCounts[r.Id],
                    Percent = total > 0 ? Math.Round((double)refCounts[r.Id] / total * 100, 1) : 0
                })
                .OrderByDescending(r => r.Count)
                .ToList();

            var empStats = creatorCounts
                .Select(c => new EmployeeStatDTO
                {
                    UserId = c.UserId,
                    Username = c.Username ?? "Unknown",
                    TotalCreated = c.Total,
                    SuccessCount = c.Success,
                    ClosedCount = c.Closed,
                    Percent = total > 0 ? Math.Round((double)c.Total / total * 100, 1) : 0
                })
                .OrderByDescending(e => e.TotalCreated)
                .ToList();

            return new AdminStatsDTO
            {
                TotalLeads = total,
                SuccessCount = successCount,
                ClosedCount = closedCount,
                SuccessPercent = total > 0 ? Math.Round((double)successCount / total * 100, 1) : 0,
                ClosedPercent = total > 0 ? Math.Round((double)closedCount / total * 100, 1) : 0,
                ReferralSources = refStats,
                Employees = empStats
            };
        }

        public async Task<HospitalManagerStatsDTO> GetHospitalManagerStatsAsync(DateTime? from, DateTime? to)
        {
            var statusCounts = await _leadRepo.GetStatusCountsAsync(from, to);
            var procedureCounts = await _leadRepo.GetLeadCountsByProcedureAsync(from, to);
            var doctorStatsRaw = await _leadRepo.GetDoctorStatsWithProceduresAsync(from, to);

            var allDoctors = await _doctorRepo.GetAllAsync();
            var allProcedures = await _procedureRepo.GetAllAsync();
            var allProceduresDict = allProcedures.ToDictionary(p => p.Id, p => p.Name ?? "Unknown");

            var total = statusCounts.Values.Sum();
            var pendingCount = statusCounts.GetValueOrDefault(LeadStatus.Pending);
            var waitingCount = statusCounts.GetValueOrDefault(LeadStatus.Waiting);
            var successCount = statusCounts.GetValueOrDefault(LeadStatus.Success);
            var closedCount = statusCounts.GetValueOrDefault(LeadStatus.Closed);

            var doctorStats = doctorStatsRaw.Select(d => new GroupStatDTO
            {
                Id = d.DoctorId,
                Name = allDoctors.FirstOrDefault(x => x.Id == d.DoctorId)?.Name ?? "Unknown",
                TotalLeads = d.Total,
                PendingCount = d.Pending,
                WaitingCount = d.Waiting,
                SuccessCount = d.Success,
                ClosedCount = d.Closed,
                SuccessRate = d.Total > 0 ? Math.Round((double)d.Success / d.Total * 100, 1) : 0,
                Procedures = d.Procedures.Select(p => new DoctorProcedureStatDTO
                {
                    ProcedureId = p.ProcedureId,
                    ProcedureName = allProceduresDict.GetValueOrDefault(p.ProcedureId, "Unknown"),
                    Count = p.Count,
                    Percent = d.Total > 0 ? Math.Round((double)p.Count / d.Total * 100, 1) : 0
                }).ToList()
            }).OrderByDescending(d => d.TotalLeads).ToList();

            var procedureStats = procedureCounts
                .Select(p => new GroupStatDTO
                {
                    Id = p.ProcedureId,
                    Name = allProceduresDict.GetValueOrDefault(p.ProcedureId, "Unknown"),
                    TotalLeads = p.Total,
                    PendingCount = p.Pending,
                    WaitingCount = p.Waiting,
                    SuccessCount = p.Success,
                    ClosedCount = p.Closed,
                    SuccessRate = p.Total > 0 ? Math.Round((double)p.Success / p.Total * 100, 1) : 0
                })
                .OrderByDescending(p => p.TotalLeads)
                .ToList();

            return new HospitalManagerStatsDTO
            {
                From = from,
                To = to,
                TotalLeads = total,
                PendingCount = pendingCount,
                WaitingCount = waitingCount,
                SuccessCount = successCount,
                ClosedCount = closedCount,
                PendingPercent = total > 0 ? Math.Round((double)pendingCount / total * 100, 1) : 0,
                WaitingPercent = total > 0 ? Math.Round((double)waitingCount / total * 100, 1) : 0,
                SuccessPercent = total > 0 ? Math.Round((double)successCount / total * 100, 1) : 0,
                ClosedPercent = total > 0 ? Math.Round((double)closedCount / total * 100, 1) : 0,
                Doctors = doctorStats,
                Procedures = procedureStats
            };
        }

        public async Task<DoctorLeadExportDTO?> GetDoctorLeadExportAsync(int doctorId, DateTime? from, DateTime? to)
        {
            var doctor = await _doctorRepo.GetByIdAsync(doctorId);
            if (doctor == null)
                return null;

            var leads = await _leadRepo.GetByDoctorAsync(doctorId, from, to);
            var followUpsByLead = (await _historyRepo.GetFollowUpsByLeadIdsAsync(leads.Select(l => l.Id)))
                .GroupBy(h => h.LeadId)
                .ToDictionary(g => g.Key, g => g.Select(FormatFollowUpLine).ToList());

            return new DoctorLeadExportDTO
            {
                DoctorName = doctor.Name ?? "Unknown",
                Leads = leads.Select(l => new DoctorLeadExportRowDTO
                {
                    PatientName = l.Name,
                    Status = l.Status.ToString(),
                    Procedure = l.Procedure?.Name,
                    ReferralSource = l.Referal?.Name,
                    CreatedByName = l.CreatedBy?.UserName,
                    ClaimedByName = l.ClaimedBy?.UserName,
                    CreatedDate = l.CreatedDate,
                    FollowUpNotes = followUpsByLead.GetValueOrDefault(l.Id, new List<string>())
                }).ToList()
            };
        }

        private static string FormatFollowUpLine(LeadHistory history)
        {
            var date = (history.ActionDate ?? history.CreatedAt).ToString("yyyy-MM-dd");
            var actor = history.Actor?.UserName ?? "?";
            var status = history.ResultingStatus?.ToString() ?? "?";
            var note = string.IsNullOrWhiteSpace(history.Note) ? "" : $": {history.Note}";
            return $"[{date}] {actor} -> {status}{note}";
        }

        private async Task<Lead> GetLeadOrThrow(int id)
        {
            var lead = await _leadRepo.GetLeadByIdAsync(id);
            if (lead == null)
                throw new InvalidOperationException("This lead no longer exists.");
            return lead;
        }

        // A completed lead (Success/Closed) is locked — only an admin Reopen can act on it.
        private static void EnsureNotClosed(Lead lead)
        {
            if (lead.Status == LeadStatus.Success || lead.Status == LeadStatus.Closed)
                throw new InvalidOperationException("This lead is closed. An admin must re-open it before any further action.");
        }

        private async Task<LeadActionResponse> ReloadActionResponse(int id)
        {
            var lead = await _leadRepo.GetLeadDetailAsync(id);
            return new LeadActionResponse { Success = true, Lead = await ToDetailAsync(lead!) };
        }

        // Real-time push is best-effort — a hub failure must not surface as an
        // error on an already-committed write.
        private static async Task TryNotifyAsync(Func<Task> notify)
        {
            try { await notify(); }
            catch { /* best-effort only */ }
        }

        private static LeadListItemResponse ToListItem(Lead lead, LeadCall? lastCall = null) => new()
        {
            Id = lead.Id,
            Name = lead.Name,
            CountryKey = lead.CountryKey,
            PhoneNum = lead.PhoneNum,
            NickName = lead.NickName,
            Status = lead.Status,
            CreatedByName = lead.CreatedBy?.UserName,
            ClaimedByName = lead.ClaimedBy?.UserName,
            CreatedDate = lead.CreatedDate,
            ReferalName = lead.Referal?.Name,
            ProcedureName = lead.Procedure?.Name,
            DoctorName = lead.Doctor?.Name,
            PaymentWay = lead.PaymentWay,
            ClosedReason = lead.ClosedReason?.Name,
            LastCallDate = lastCall?.Date,
            LastCallNote = lastCall?.Note
        };

        private async Task<LeadDetailResponse> ToDetailAsync(Lead lead)
        {
            var history = await _historyRepo.GetByLeadAsync(lead.Id);
            var calls = await _callRepo.GetByLeadAsync(lead.Id);

            return new LeadDetailResponse
            {
                Id = lead.Id,
                Name = lead.Name,
                CountryKey = lead.CountryKey,
                PhoneNum = lead.PhoneNum,
                NickName = lead.NickName,
                Description = lead.Description,
                Status = lead.Status,
                PaymentWay = lead.PaymentWay,
                HasDoctor = lead.HasDoctor,
                DoctorId = lead.DoctorId,
                DoctorName = lead.Doctor?.Name,
                AppointmentDate = lead.AppointmentDate,
                ClinicSignature = lead.ClinicSignature,
                ReferalId = lead.ReferalId,
                ReferalName = lead.Referal?.Name,
                ProcedureId = lead.ProcedureId,
                ProcedureName = lead.Procedure?.Name,
                CreatedByName = lead.CreatedBy?.UserName,
                ClaimedByName = lead.ClaimedBy?.UserName,
                CreatedDate = lead.CreatedDate,
                ClosedReason = lead.ClosedReason?.Name,
                History = history.Select(h => new LeadHistoryResponse
                {
                    Id = h.Id,
                    Type = h.Type.ToString(),
                    ResultingStatus = h.ResultingStatus?.ToString(),
                    ActorName = h.Actor?.UserName,
                    ActionDate = h.ActionDate,
                    DoctorName = h.Doctor?.Name,
                    ClosedReasonName = h.ClosedReason?.Name,
                    Note = h.Note,
                    CreatedAt = h.CreatedAt
                }).ToList(),
                Calls = calls.Select(c => new LeadCallResponse
                {
                    Id = c.Id,
                    Date = c.Date,
                    Note = c.Note,
                    IsDone = c.IsDone,
                    ActorName = c.Actor?.UserName,
                    CreatedAt = c.CreatedAt
                }).ToList()
            };
        }
    }
}
