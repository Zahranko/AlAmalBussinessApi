using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp
{
    public class DeviceTokenRepo : IDeviceTokenRepo
    {
        private readonly AppDbContext _context;

        public DeviceTokenRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpsertAsync(string userId, string token, string? platform)
        {
            var row = await _context.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token);
            if (row == null)
            {
                _context.DeviceTokens.Add(new DeviceToken { UserId = userId, Token = token, Platform = platform });
            }
            else
            {
                row.UserId = userId;
                row.Platform = platform;
                row.LastSeenAt = AppClock.Now;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two registrations of the same new token raced; the other one
                // won and holds it already, which is the outcome either way.
                _context.ChangeTracker.Clear();
            }
        }

        public Task RemoveAsync(string userId, string token) =>
            _context.DeviceTokens.Where(d => d.Token == token && d.UserId == userId).ExecuteDeleteAsync();

        public Task RemoveTokensAsync(IReadOnlyCollection<string> tokens) =>
            tokens.Count == 0
                ? Task.CompletedTask
                : _context.DeviceTokens.Where(d => tokens.Contains(d.Token!)).ExecuteDeleteAsync();

        public Task<List<string>> GetTokensForUsersAsync(IReadOnlyCollection<string> userIds) =>
            _context.DeviceTokens
                .Where(d => userIds.Contains(d.UserId!) && d.User!.IsActive)
                .Select(d => d.Token!)
                .Distinct()
                .ToListAsync();

        // Same join and predicates as UserRepo.GetActiveEmailsInRoleAsync,
        // with the device table in place of the email column: whoever is
        // emailed about something is whoever gets the push.
        public Task<List<string>> GetTokensInRoleAsync(string role, int departmentId) =>
            (from u in _context.Users
             join ur in _context.UserRoles on u.Id equals ur.UserId
             join r in _context.Roles on ur.RoleId equals r.Id
             join d in _context.DeviceTokens on u.Id equals d.UserId
             where r.Name == role
                 && u.IsActive
                 && (u.DepartmentId == departmentId
                     || u.ExtraDepartments.Any(x => x.DepartmentId == departmentId))
             select d.Token!)
            .Distinct()
            .ToListAsync();

        public Task<List<string>> GetTokensInRolesAsync(IReadOnlyCollection<string> roles, string? excludeUserId) =>
            (from u in _context.Users
             join ur in _context.UserRoles on u.Id equals ur.UserId
             join r in _context.Roles on ur.RoleId equals r.Id
             join d in _context.DeviceTokens on u.Id equals d.UserId
             where roles.Contains(r.Name!)
                 && u.IsActive
                 && u.Id != excludeUserId
             select d.Token!)
            .Distinct()
            .ToListAsync();
    }
}
