using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceTokenRepo _repo;

        public DeviceService(IDeviceTokenRepo repo)
        {
            _repo = repo;
        }

        public Task RegisterAsync(string userId, string token, string? platform) =>
            _repo.UpsertAsync(userId, token.Trim(), NormalizePlatform(platform));

        public Task UnregisterAsync(string userId, string token) =>
            _repo.RemoveAsync(userId, token.Trim());

        private static string? NormalizePlatform(string? platform) =>
            platform?.Trim().ToLowerInvariant() switch
            {
                "android" => "android",
                "ios" => "ios",
                _ => null,
            };
    }
}
