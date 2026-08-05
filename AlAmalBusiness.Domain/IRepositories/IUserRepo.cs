using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IUserRepo
    {
        Task<IdentityResult> CreateUserAsync(User user, string password);
        Task<IdentityResult> AssignRolesAsync(User user, List<string> roles);

    }
}
