using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface
{
    // Remembers the last filter/paging a user applied on a given list endpoint,
    // so a bare re-request (no query string) can restore it. Save is always
    // best-effort — a cache hiccup must never fail the underlying request.
    public interface IFilterCacheService
    {
        Task SaveFilterAsync<T>(string userId, string endpointKey, T filter) where T : class;
        Task<T?> GetFilterAsync<T>(string userId, string endpointKey) where T : class;
    }
}
