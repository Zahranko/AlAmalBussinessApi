using AlAmalBusiness.Domain.IRepositories;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp
{
    public class FilterCacheRepo : IFilterCacheRepo
    {
        private readonly IDistributedCache _cache;

        public FilterCacheRepo(IDistributedCache cache)
        {
            _cache = cache;
        }

        public Task SetAsync(string key, string value, TimeSpan ttl) =>
            _cache.SetStringAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });

        public Task<string?> GetAsync(string key) => _cache.GetStringAsync(key);
    }
}
