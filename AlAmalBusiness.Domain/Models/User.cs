using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using Microsoft.AspNetCore.Identity;


namespace AlAmalBusiness.Domain.Models;

public class User:IdentityUser
{
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public int DepartmentId { get; set; }
    public Departments Department { get; set; } = null!;


}