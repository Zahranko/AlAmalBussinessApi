using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Profile
{
    // The signed-in user's own account (My profile): their name, their
    // password, and the counts behind "my status" on each area their roles
    // reach. Every method is about one user — nothing here reads or changes
    // anyone else's account.
    public interface IProfileRepo
    {
        Task<User?> FindActiveAsync(string userId);
        Task<IdentityResult> UpdateFullNameAsync(User user, string fullName);
        Task<IList<string>> GetRolesAsync(User user);
        // Names for the given department ids, in id order; unknown ids are skipped.
        Task<List<string>> GetDepartmentNamesAsync(IReadOnlyCollection<int> departmentIds);

        // Wrong current password counts towards the same lockout as the login
        // form (AuthRepo.LogInAsync), so a borrowed session can't be used to
        // guess the password.
        // Errors carries Identity's own reasons when the new password is refused
        // (too short, needs a digit...).
        Task<(PasswordChangeOutcome Outcome, List<string> Errors)> ChangePasswordAsync(User user, string currentPassword, string newPassword);

        // ---- "my status" counts, one call per area ------------------------
        // `from`/`to` bound "this month" (Jordan time, [from, to)).
        Task<CrmProfileCounts> GetCrmCountsAsync(string userId, DateTime from, DateTime to);
        Task<InboxProfileCounts> GetFeedbackCountsAsync(string userId, IReadOnlyCollection<int> departmentIds, DateTime from, DateTime to);
        Task<InboxProfileCounts> GetAppointmentCountsAsync(string userId, IReadOnlyCollection<int> departmentIds, DateTime from, DateTime to);
        Task<QuestionnaireProfileCounts> GetQuestionnaireCountsAsync(IReadOnlyCollection<int> departmentIds, DateTime from, DateTime to);
        Task<RaisedProfileCounts> GetTicketsRaisedAsync(string userId, DateTime from, DateTime to);
        Task<SolvedProfileCounts> GetTicketsSolvedAsync(string userId, bool support, bool insurance, DateTime from, DateTime to);
        Task<RaisedProfileCounts> GetMaintenanceRaisedAsync(string userId, DateTime from, DateTime to);
        Task<SolvedProfileCounts> GetMaintenanceSolvedAsync(string userId, DateTime from, DateTime to);
        Task<MaintenanceOverviewCounts> GetMaintenanceOverviewAsync(DateTime from, DateTime to);
        Task<MammogramProfileCounts> GetMammogramCountsAsync(string userId, DateOnly today, DateTime from, DateTime to);
        Task<AttendanceProfileCounts> GetAttendanceCountsAsync(IReadOnlyCollection<int> departmentIds, int year, int month);
        Task<SecurityProfileCounts> GetSecurityCountsAsync(int year, int month, DateTime from, DateTime to);
        Task<DriverProfileCounts> GetDriverCountsAsync(string userId, DateTime from, DateTime to);
    }

    public enum PasswordChangeOutcome { Changed, WrongPassword, LockedOut, Rejected }

    public record CrmProfileCounts(int CreatedThisMonth, int CreatedAllTime, int OpenClaimed, int SucceededThisMonth);
    // Feedback and appointment inboxes share a shape: waiting in my
    // departments, open and assigned to me, finished by me this month,
    // received in my departments this month.
    public record InboxProfileCounts(int Waiting, int AssignedToMe, int FinishedByMeThisMonth, int ReceivedThisMonth);
    public record QuestionnaireProfileCounts(int Active, int ResponsesThisMonth);
    public record RaisedProfileCounts(int Open, int ClosedThisMonth, int RaisedAllTime);
    public record SolvedProfileCounts(int OpenInQueue, int ClosedByMeThisMonth, int ClosedByMeAllTime);
    public record MaintenanceOverviewCounts(int Open, int UrgentOpen, int ClosedThisMonth);
    public record MammogramProfileCounts(int VisitsThisMonth, int FollowUpsToday, int AppointmentsToday, int BookedByMeThisMonth);
    public record AttendanceProfileCounts(int SheetsThisMonth, int ClosedThisMonth);
    public record SecurityProfileCounts(int CarSheetsThisMonth, int TripsThisMonth);
    public record DriverProfileCounts(int TripsThisMonth, int KilometresThisMonth, int Drafts);
}
