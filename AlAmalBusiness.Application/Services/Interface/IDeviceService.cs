using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface
{
    // The mobile app's push registration: one row per phone, see DeviceToken.
    public interface IDeviceService
    {
        Task RegisterAsync(string userId, string token, string? platform);
        Task UnregisterAsync(string userId, string token);
    }
}
