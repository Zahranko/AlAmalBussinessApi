using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users
{
    public class UpdateUserDto
    {
        [Required]
        public string? UserId { get; set; }
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string? UserName { get; set; }
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string? Password { get; set; }
    }
}
