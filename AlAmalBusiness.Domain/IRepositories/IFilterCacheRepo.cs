using System;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories
{
    // Storage-agnostic key/value cache contract — string in, string out, no
    // JSON concern here. Implemented against IDistributedCache so the backing
    // store (in-memory today, Redis or anything else later) is a one-line swap.
    public interface IFilterCacheRepo
    {
        Task SetAsync(string key, string value, TimeSpan ttl);
        Task<string?> GetAsync(string key);
    }
}
