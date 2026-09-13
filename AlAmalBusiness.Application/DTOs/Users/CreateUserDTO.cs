using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users
{
    public class CreateUserDTO
    {
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string? UserName { get; set; }
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string? Password { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string? FullName { get; set; }
        [Required]
        public int DepartmentId { get; set; }
        // Optional. Where system notifications for this user go (e.g. an
        // FManager gets new-feedback emails for their department). Blank is
        // stored as no email; format is checked in UserServices.
        [StringLength(256)]
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

    }
}
