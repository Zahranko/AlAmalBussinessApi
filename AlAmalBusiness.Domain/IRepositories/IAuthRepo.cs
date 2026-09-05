using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IAuthRepo
    {
        // The authenticated User on success (the caller needs Id/UserName/
        // FullName for the token), or an error message to surface.
        Task<(User? User, string? Error)> LogInAsync(string userName, string password);
        Task<IEnumerable<string>> GetRolesAsync(string userName);
    }
}
