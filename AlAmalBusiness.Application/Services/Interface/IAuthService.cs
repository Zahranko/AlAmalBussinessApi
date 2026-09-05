using AlAmalBusiness.Application.DTOs.Auth;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginDTO loginDto);

        // Exchanges a refresh token for a new access token and a new refresh
        // token, invalidating the one presented (rotation).
        Task<LoginResult> RefreshAsync(string refreshToken);

        // Ends one session. Safe to call with an unknown or already-revoked
        // token: signing out must never fail.
        Task LogoutAsync(string? refreshToken);
    }
}
