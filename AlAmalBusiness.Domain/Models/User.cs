using System.Security.Principal;
using Microsoft.AspNetCore.Identity;


namespace AlAmalBusiness.Domain.Models;

public class User:IdentityUser
{
    public string? FullName { get; set; }

}