using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp
{
    public class FilterCacheService : IFilterCacheService
    {
        private readonly IFilterCacheRepo _repo;
        private readonly TimeSpan _ttl;

        public FilterCacheService(IFilterCacheRepo repo, IConfiguration config)
        {
            _repo = repo;
            var ttlDays = config.GetValue<int?>("FilterCacheSettings:TtlDays") ?? 30;
            _ttl = TimeSpan.FromDays(ttlDays);
        }

        public async Task SaveFilterAsync<T>(string userId, string endpointKey, T filter) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(filter);
                await _repo.SetAsync(BuildKey(userId, endpointKey), json, _ttl);
            }
            catch
            {
                // Best-effort — a cache write failure must never fail the request.
            }
        }

        public async Task<T?> GetFilterAsync<T>(string userId, string endpointKey) where T : class
        {
            try
            {
                var json = await _repo.GetAsync(BuildKey(userId, endpointKey));
                return json == null ? null : JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string BuildKey(string userId, string endpointKey) => $"filter:{userId}:{endpointKey}";
    }
}
