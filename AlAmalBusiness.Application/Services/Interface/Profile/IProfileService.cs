using AlAmalBusiness.Application.DTOs.Profile;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Profile
{
    public interface IProfileService
    {
        Task<ProfileResponse?> GetAsync(ProfileActor actor);
        Task<ProfileResult> UpdateNameAsync(string userId, UpdateProfileNameDTO dto);
        Task<ProfileResult> ChangePasswordAsync(string userId, ChangeOwnPasswordDTO dto);
        Task<ProfileStatsResponse> GetStatsAsync(ProfileActor actor);
    }
}
