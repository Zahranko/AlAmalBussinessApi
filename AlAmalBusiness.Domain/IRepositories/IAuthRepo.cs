using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IAuthRepo
    {
        Task<(string,bool)> LogInAsync(string userNames,string password);
        Task<IEnumerable<string>> GetRolesAsync(string userName);
    }
}
