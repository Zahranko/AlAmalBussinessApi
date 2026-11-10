using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Profile
{
    // Who the caller is, for the My profile page.
    public class ProfileResponse
    {
        public string UserId { get; set; } = "";
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? DepartmentName { get; set; }
        // Departments read beyond the home one (UserDepartments grants).
        public List<string> ExtraDepartmentNames { get; set; } = new();
        public List<string> Roles { get; set; } = new();
    }

    // The display name only — the login username is the admin's to change.
    public class UpdateProfileNameDTO
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string? FullName { get; set; }
    }

    public class ChangeOwnPasswordDTO
    {
        [Required, StringLength(128)]
        public string? CurrentPassword { get; set; }

        [Required, StringLength(128, MinimumLength = 6)]
        public string? NewPassword { get; set; }
    }

    // "My status": one section per area the caller's roles reach (never one
    // for Admin), each a list of counts. Keys, not labels — the console
    // translates them — so a new metric is an API change and a string.
    public class ProfileStatsResponse
    {
        public List<ProfileStatSection> Sections { get; set; } = new();
    }

    public class ProfileStatSection
    {
        public string Key { get; set; } = "";
        // The caller's roles that earned this section.
        public List<string> Roles { get; set; } = new();
        public List<ProfileStatMetric> Metrics { get; set; } = new();
    }

    public class ProfileStatMetric
    {
        public string Key { get; set; } = "";
        public long Value { get; set; }
    }

    public class ProfileResult
    {
        public bool Success { get; init; }
        public bool NotFound { get; init; }
        public string? Error { get; init; }
        public List<string> Errors { get; init; } = new();
        public ProfileResponse? Profile { get; init; }

        public static ProfileResult Ok(ProfileResponse? profile = null) => new() { Success = true, Profile = profile };
        public static ProfileResult Missing() => new() { NotFound = true };
        public static ProfileResult Fail(string error, List<string>? errors = null) =>
            new() { Error = error, Errors = errors ?? new() };
    }

    // Built by the controller from the caller's token.
    public record ProfileActor(string UserId, IReadOnlyCollection<string> Roles, IReadOnlyCollection<int> DepartmentIds);
}
