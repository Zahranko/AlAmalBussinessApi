using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp
{
    public class RefreshTokenRepo : IRefreshTokenRepo
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken token) => await _context.RefreshTokens.AddAsync(token);

        // Tracked: the caller rotates or revokes the row it gets back.
        public Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
            _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public async Task RevokeAllForUserAsync(string userId)
        {
            var now = DateTime.UtcNow;
            await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
        }

        public Task<int> DeleteExpiredBeforeAsync(DateTime cutoffUtc) =>
            _context.RefreshTokens.Where(t => t.ExpiresAt < cutoffUtc).ExecuteDeleteAsync();
    }
}
