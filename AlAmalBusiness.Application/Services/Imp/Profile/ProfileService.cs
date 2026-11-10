using AlAmalBusiness.Application.DTOs.Profile;
using AlAmalBusiness.Application.Services.Interface.Profile;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.IRepositories.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Profile
{
    // My profile. Every operation acts on the caller's own account, named by
    // the token — there is no id in any route, so nobody can reach another
    // person's profile through it.
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepo _repo;
        private readonly IRefreshTokenRepo _refreshTokens;

        public ProfileService(IProfileRepo repo, IRefreshTokenRepo refreshTokens)
        {
            _repo = repo;
            _refreshTokens = refreshTokens;
        }

        public async Task<ProfileResponse?> GetAsync(ProfileActor actor)
        {
            var user = await _repo.FindActiveAsync(actor.UserId);
            if (user == null) return null;

            var home = await _repo.GetDepartmentNamesAsync(new[] { user.DepartmentId });
            var extraIds = actor.DepartmentIds.Where(id => id != user.DepartmentId).ToList();
            return new ProfileResponse
            {
                UserId = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                DepartmentName = home.FirstOrDefault(),
                ExtraDepartmentNames = await _repo.GetDepartmentNamesAsync(extraIds),
                Roles = (await _repo.GetRolesAsync(user)).OrderBy(r => r).ToList(),
            };
        }

        public async Task<ProfileResult> UpdateNameAsync(string userId, UpdateProfileNameDTO dto)
        {
            // Collapse runs of spaces too: this is what every list and email
            // shows next to the person's work.
            var name = string.Join(' ', (dto.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (name.Length < 2 || name.Length > 100)
                return ProfileResult.Fail("Name must be 2 to 100 characters.");

            var user = await _repo.FindActiveAsync(userId);
            if (user == null) return ProfileResult.Missing();

            var result = await _repo.UpdateFullNameAsync(user, name);
            if (!result.Succeeded)
                return ProfileResult.Fail("The name could not be saved.", result.Errors.Select(e => e.Description).ToList());

            // The new name reaches the token (and so the console's header) on
            // the next refresh; the console asks for one straight away.
            return ProfileResult.Ok();
        }

        public async Task<ProfileResult> ChangePasswordAsync(string userId, ChangeOwnPasswordDTO dto)
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword) || string.IsNullOrEmpty(dto.NewPassword))
                return ProfileResult.Fail("Both the current and the new password are required.");
            if (dto.NewPassword == dto.CurrentPassword)
                return ProfileResult.Fail("The new password must be different from the current one.");

            var user = await _repo.FindActiveAsync(userId);
            if (user == null) return ProfileResult.Missing();

            var (outcome, errors) = await _repo.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            switch (outcome)
            {
                case PasswordChangeOutcome.WrongPassword:
                    return ProfileResult.Fail("The current password is not correct.");
                case PasswordChangeOutcome.LockedOut:
                    return ProfileResult.Fail("Too many wrong attempts. The account is locked for 15 minutes.");
                case PasswordChangeOutcome.Rejected:
                    return ProfileResult.Fail("The new password was not accepted.", errors);
            }

            // Every session this account had ends: whoever else might have been
            // signed in with the old password is out at their next refresh. The
            // console signs this browser back in with the new password itself.
            await _refreshTokens.RevokeAllForUserAsync(userId);
            return ProfileResult.Ok();
        }

        public async Task<ProfileStatsResponse> GetStatsAsync(ProfileActor actor)
        {
            var today = AppClock.Today;
            var from = new DateTime(today.Year, today.Month, 1);
            var to = from.AddMonths(1);
            var me = actor.UserId;
            var depts = actor.DepartmentIds;
            var sections = new List<ProfileStatSection>();

            // Admin is never a section of its own: the profile shows the work
            // the person's other roles give them.
            List<string> Held(params string[] roles) => roles.Where(r => actor.Roles.Contains(r)).ToList();
            void Add(string key, List<string> roles, params (string Key, long Value)[] metrics) =>
                sections.Add(new ProfileStatSection
                {
                    Key = key,
                    Roles = roles,
                    Metrics = metrics.Select(m => new ProfileStatMetric { Key = m.Key, Value = m.Value }).ToList(),
                });

            // The repository calls run one after another on purpose: they share
            // one DbContext, which does not allow two queries at once.
            var crm = Held(AppRoles.CManager, AppRoles.CEmployee, AppRoles.CUser);
            if (crm.Count > 0)
            {
                var c = await _repo.GetCrmCountsAsync(me, from, to);
                Add("crm", crm, ("leadsCreatedMonth", c.CreatedThisMonth), ("openClaimed", c.OpenClaimed),
                    ("successMonth", c.SucceededThisMonth), ("leadsCreatedAll", c.CreatedAllTime));
            }

            var fb = Held(AppRoles.FManager);
            if (fb.Count > 0)
            {
                var c = await _repo.GetFeedbackCountsAsync(me, depts, from, to);
                Add("feedback", fb, ("waiting", c.Waiting), ("assignedToMe", c.AssignedToMe),
                    ("resolvedByMeMonth", c.FinishedByMeThisMonth), ("receivedMonth", c.ReceivedThisMonth));
            }

            var ap = Held(AppRoles.AManager);
            if (ap.Count > 0)
            {
                var c = await _repo.GetAppointmentCountsAsync(me, depts, from, to);
                Add("appointments", ap, ("waiting", c.Waiting), ("assignedToMe", c.AssignedToMe),
                    ("scheduledByMeMonth", c.FinishedByMeThisMonth), ("receivedMonth", c.ReceivedThisMonth));
            }

            var q = Held(AppRoles.QManager);
            if (q.Count > 0)
            {
                var c = await _repo.GetQuestionnaireCountsAsync(depts, from, to);
                Add("questionnaires", q, ("active", c.Active), ("responsesMonth", c.ResponsesThisMonth));
            }

            var raise = Held(AppRoles.TManager, AppRoles.TEmployee);
            if (raise.Count > 0)
            {
                var c = await _repo.GetTicketsRaisedAsync(me, from, to);
                Add("ticketsRaised", raise, ("open", c.Open), ("closedMonth", c.ClosedThisMonth), ("raisedAll", c.RaisedAllTime));
            }

            var solve = Held(AppRoles.TSupport, AppRoles.TInsurance);
            if (solve.Count > 0)
            {
                var c = await _repo.GetTicketsSolvedAsync(me, solve.Contains(AppRoles.TSupport), solve.Contains(AppRoles.TInsurance), from, to);
                Add("ticketsSolved", solve, ("queueOpen", c.OpenInQueue), ("closedByMeMonth", c.ClosedByMeThisMonth),
                    ("closedByMeAll", c.ClosedByMeAllTime));
            }

            var mRaise = Held(AppRoles.MRaiser);
            if (mRaise.Count > 0)
            {
                var c = await _repo.GetMaintenanceRaisedAsync(me, from, to);
                Add("maintenanceRaised", mRaise, ("open", c.Open), ("closedMonth", c.ClosedThisMonth), ("raisedAll", c.RaisedAllTime));
            }

            var mSolve = Held(AppRoles.MSupport);
            if (mSolve.Count > 0)
            {
                var c = await _repo.GetMaintenanceSolvedAsync(me, from, to);
                Add("maintenanceSolved", mSolve, ("queueOpen", c.OpenInQueue), ("closedByMeMonth", c.ClosedByMeThisMonth),
                    ("closedByMeAll", c.ClosedByMeAllTime));
            }

            var mManage = Held(AppRoles.MaintenanceManager);
            if (mManage.Count > 0)
            {
                var c = await _repo.GetMaintenanceOverviewAsync(from, to);
                Add("maintenanceOverview", mManage, ("open", c.Open), ("urgentOpen", c.UrgentOpen), ("closedMonth", c.ClosedThisMonth));
            }

            var mg = Held(AppRoles.MManager);
            if (mg.Count > 0)
            {
                var c = await _repo.GetMammogramCountsAsync(me, DateOnly.FromDateTime(today), from, to);
                Add("mammogram", mg, ("visitsMonth", c.VisitsThisMonth), ("followUpsToday", c.FollowUpsToday),
                    ("appointmentsToday", c.AppointmentsToday), ("bookedByMeMonth", c.BookedByMeThisMonth));
            }

            var at = Held(AppRoles.AttendanceManager);
            if (at.Count > 0)
            {
                var c = await _repo.GetAttendanceCountsAsync(depts, today.Year, today.Month);
                Add("attendance", at, ("sheetsMonth", c.SheetsThisMonth), ("closedMonth", c.ClosedThisMonth));
            }

            var sec = Held(AppRoles.SecurityManager);
            if (sec.Count > 0)
            {
                var c = await _repo.GetSecurityCountsAsync(today.Year, today.Month, from, to);
                Add("security", sec, ("carSheetsMonth", c.CarSheetsThisMonth), ("tripsMonth", c.TripsThisMonth));
            }

            var drv = Held(AppRoles.Driver);
            if (drv.Count > 0)
            {
                var c = await _repo.GetDriverCountsAsync(me, from, to);
                Add("driver", drv, ("tripsMonth", c.TripsThisMonth), ("kmMonth", c.KilometresThisMonth), ("drafts", c.Drafts));
            }

            return new ProfileStatsResponse { Sections = sections };
        }
    }
}
