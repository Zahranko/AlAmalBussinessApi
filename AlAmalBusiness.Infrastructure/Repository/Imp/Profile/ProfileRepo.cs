using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Profile;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Profile
{
    // My profile: the caller's own account and the counts behind "my status".
    // Every count is a plain CountAsync/SumAsync — no rows are loaded — and
    // only the areas the caller's roles reach are ever queried (ProfileService
    // decides which).
    public class ProfileRepo : IProfileRepo
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProfileRepo(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<User?> FindActiveAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user is { IsActive: true } ? user : null;
        }

        public Task<IdentityResult> UpdateFullNameAsync(User user, string fullName)
        {
            user.FullName = fullName;
            return _userManager.UpdateAsync(user);
        }

        public Task<IList<string>> GetRolesAsync(User user) => _userManager.GetRolesAsync(user);

        public Task<List<string>> GetDepartmentNamesAsync(IReadOnlyCollection<int> departmentIds) =>
            departmentIds.Count == 0
                ? Task.FromResult(new List<string>())
                : _context.Departments.Where(d => departmentIds.Contains(d.Id))
                    .OrderBy(d => d.Id).Select(d => d.Name ?? "").ToListAsync();

        public async Task<(PasswordChangeOutcome Outcome, List<string> Errors)> ChangePasswordAsync(User user, string currentPassword, string newPassword)
        {
            // Same rules as the login form (AuthRepo.LogInAsync): LockoutEnd is
            // read directly because CRMS-imported accounts have LockoutEnabled
            // off, and every wrong guess is counted.
            if (IsLockedOut(user)) return (PasswordChangeOutcome.LockedOut, new());

            if (!await _userManager.CheckPasswordAsync(user, currentPassword))
            {
                await _userManager.AccessFailedAsync(user);
                return (IsLockedOut(user) ? PasswordChangeOutcome.LockedOut : PasswordChangeOutcome.WrongPassword, new());
            }

            if (user.AccessFailedCount > 0)
                await _userManager.ResetAccessFailedCountAsync(user);

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded
                ? (PasswordChangeOutcome.Changed, new())
                : (PasswordChangeOutcome.Rejected, result.Errors.Select(e => e.Description).ToList());
        }

        private static bool IsLockedOut(User user) =>
            user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow;

        // ---- counts -----------------------------------------------------------

        public async Task<CrmProfileCounts> GetCrmCountsAsync(string userId, DateTime from, DateTime to)
        {
            // Leads carries the soft-delete query filter; LeadHistories does not
            // (see CLAUDE.md), so the success count excludes deleted leads itself.
            var mine = _context.Leads.Where(l => l.CreatedById == userId);
            return new CrmProfileCounts(
                await mine.CountAsync(l => l.CreatedDate >= from && l.CreatedDate < to),
                await mine.CountAsync(),
                await _context.Leads.CountAsync(l => l.ClaimedById == userId
                    && (l.Status == LeadStatus.Pending || l.Status == LeadStatus.Waiting)),
                await _context.LeadHistories.CountAsync(h => h.ActorId == userId
                    && h.ResultingStatus == LeadStatus.Success
                    && h.CreatedAt >= from && h.CreatedAt < to
                    && !h.Lead!.IsDeleted));
        }

        public async Task<InboxProfileCounts> GetFeedbackCountsAsync(string userId, IReadOnlyCollection<int> departmentIds, DateTime from, DateTime to)
        {
            // An empty set means no department at all — never "every department".
            var inMine = departmentIds.Count == 0
                ? _context.Feedbacks.Where(f => false)
                : _context.Feedbacks.Where(f => departmentIds.Contains(f.DepartmentId));
            return new InboxProfileCounts(
                await inMine.CountAsync(f => f.Status == FeedbackStatus.New),
                await _context.Feedbacks.CountAsync(f => f.AssignedToId == userId
                    && (f.Status == FeedbackStatus.New || f.Status == FeedbackStatus.InReview)),
                await _context.FeedbackHistories.CountAsync(h => h.ActorId == userId
                    && h.Type == FeedbackActions.StatusChanged && h.ToStatus == FeedbackStatus.Resolved
                    && h.CreatedAt >= from && h.CreatedAt < to),
                await inMine.CountAsync(f => f.CreatedDate >= from && f.CreatedDate < to));
        }

        public async Task<InboxProfileCounts> GetAppointmentCountsAsync(string userId, IReadOnlyCollection<int> departmentIds, DateTime from, DateTime to)
        {
            var inMine = departmentIds.Count == 0
                ? _context.AppointmentRequests.Where(a => false)
                : _context.AppointmentRequests.Where(a => departmentIds.Contains(a.DepartmentId));
            return new InboxProfileCounts(
                await inMine.CountAsync(a => a.Status == AppointmentStatus.New),
                await _context.AppointmentRequests.CountAsync(a => a.AssignedToId == userId
                    && (a.Status == AppointmentStatus.New || a.Status == AppointmentStatus.Contacted)),
                await _context.AppointmentHistories.CountAsync(h => h.ActorId == userId
                    && h.Type == AppointmentActions.StatusChanged && h.ToStatus == AppointmentStatus.Scheduled
                    && h.CreatedAt >= from && h.CreatedAt < to),
                await inMine.CountAsync(a => a.CreatedDate >= from && a.CreatedDate < to));
        }

        public async Task<QuestionnaireProfileCounts> GetQuestionnaireCountsAsync(IReadOnlyCollection<int> departmentIds, DateTime from, DateTime to)
        {
            if (departmentIds.Count == 0) return new QuestionnaireProfileCounts(0, 0);
            return new QuestionnaireProfileCounts(
                await _context.Questionnaires.CountAsync(q => q.IsActive && departmentIds.Contains(q.DepartmentId)),
                await _context.QuestionnaireSubmissions.CountAsync(s => departmentIds.Contains(s.Questionnaire!.DepartmentId)
                    && s.CreatedDate >= from && s.CreatedDate < to));
        }

        public async Task<RaisedProfileCounts> GetTicketsRaisedAsync(string userId, DateTime from, DateTime to)
        {
            var mine = _context.Tickets.Where(t => t.CreatedById == userId);
            return new RaisedProfileCounts(
                await mine.CountAsync(t => t.Status == TicketStatus.Open),
                await mine.CountAsync(t => t.ClosedAt >= from && t.ClosedAt < to),
                await mine.CountAsync());
        }

        public async Task<SolvedProfileCounts> GetTicketsSolvedAsync(string userId, bool support, bool insurance, DateTime from, DateTime to)
        {
            // The queue is the side(s) this person works: the support agent's
            // is every ticket not flagged insurance, the desk's the flagged ones.
            var open = _context.Tickets.Where(t => t.Status == TicketStatus.Open
                && ((support && !t.IsInsurance) || (insurance && t.IsInsurance)));
            var closedByMe = _context.Tickets.Where(t => t.AssignedToId == userId && t.Status != TicketStatus.Open);
            return new SolvedProfileCounts(
                await open.CountAsync(),
                await closedByMe.CountAsync(t => t.ClosedAt >= from && t.ClosedAt < to),
                await closedByMe.CountAsync());
        }

        public async Task<RaisedProfileCounts> GetMaintenanceRaisedAsync(string userId, DateTime from, DateTime to)
        {
            var mine = _context.MaintenanceTickets.Where(t => t.CreatedById == userId);
            return new RaisedProfileCounts(
                await mine.CountAsync(t => t.Status == TicketStatus.Open),
                await mine.CountAsync(t => t.ClosedAt >= from && t.ClosedAt < to),
                await mine.CountAsync());
        }

        public async Task<SolvedProfileCounts> GetMaintenanceSolvedAsync(string userId, DateTime from, DateTime to)
        {
            var closedByMe = _context.MaintenanceTickets.Where(t => t.AssignedToId == userId && t.Status != TicketStatus.Open);
            return new SolvedProfileCounts(
                await _context.MaintenanceTickets.CountAsync(t => t.Status == TicketStatus.Open),
                await closedByMe.CountAsync(t => t.ClosedAt >= from && t.ClosedAt < to),
                await closedByMe.CountAsync());
        }

        public async Task<MaintenanceOverviewCounts> GetMaintenanceOverviewAsync(DateTime from, DateTime to)
        {
            var open = _context.MaintenanceTickets.Where(t => t.Status == TicketStatus.Open);
            return new MaintenanceOverviewCounts(
                await open.CountAsync(),
                await open.CountAsync(t => t.Priority == MaintenancePriority.Urgent),
                await _context.MaintenanceTickets.CountAsync(t => t.ClosedAt >= from && t.ClosedAt < to));
        }

        public async Task<MammogramProfileCounts> GetMammogramCountsAsync(string userId, DateOnly today, DateTime from, DateTime to)
        {
            var monthStart = DateOnly.FromDateTime(from);
            var monthEnd = DateOnly.FromDateTime(to);
            var dayStart = today.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);
            return new MammogramProfileCounts(
                await _context.MammogramRecords.CountAsync(r => r.VisitDate >= monthStart && r.VisitDate < monthEnd),
                await _context.MammogramRecords.CountAsync(r => r.FollowUpDate == today || r.FollowUpDate2 == today),
                await _context.MammogramAppointments.CountAsync(a => a.AppointmentAt >= dayStart && a.AppointmentAt < dayEnd
                    && a.Status != MammogramAppointmentStatus.Failed),
                await _context.MammogramAppointments.CountAsync(a => a.CreatedById == userId
                    && a.CreatedAt >= from && a.CreatedAt < to));
        }

        public async Task<AttendanceProfileCounts> GetAttendanceCountsAsync(IReadOnlyCollection<int> departmentIds, int year, int month)
        {
            if (departmentIds.Count == 0) return new AttendanceProfileCounts(0, 0);
            var sheets = _context.AttendanceSheets.Where(s => s.Year == year && s.Month == month
                && departmentIds.Contains(s.DepartmentId));
            return new AttendanceProfileCounts(await sheets.CountAsync(), await sheets.CountAsync(s => s.IsClosed));
        }

        public async Task<SecurityProfileCounts> GetSecurityCountsAsync(int year, int month, DateTime from, DateTime to) =>
            new(
                await _context.CarCheckSheets.CountAsync(s => s.Year == year && s.Month == month),
                await _context.CarTrips.CountAsync(t => (t.DepartedAt ?? t.CreatedAt) >= from && (t.DepartedAt ?? t.CreatedAt) < to));

        public async Task<DriverProfileCounts> GetDriverCountsAsync(string userId, DateTime from, DateTime to)
        {
            var thisMonth = _context.CarTrips.Where(t => t.DriverId == userId
                && (t.DepartedAt ?? t.CreatedAt) >= from && (t.DepartedAt ?? t.CreatedAt) < to);
            var km = await thisMonth
                .Where(t => t.StartOdometer != null && t.EndOdometer != null && t.EndOdometer >= t.StartOdometer)
                .SumAsync(t => (int?)(t.EndOdometer!.Value - t.StartOdometer!.Value)) ?? 0;
            return new DriverProfileCounts(
                await thisMonth.CountAsync(),
                km,
                await _context.CarTrips.CountAsync(t => t.DriverId == userId && !t.IsSubmitted));
        }
    }
}
