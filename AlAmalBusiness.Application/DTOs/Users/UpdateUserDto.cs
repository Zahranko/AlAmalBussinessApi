using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users
{
    public class UpdateUserDto
    {
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string? UserName { get; set; }
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string? FullName { get; set; }
        [Required]
        public int DepartmentId { get; set; }
        // Full replace like every other field here: sending null or "" clears
        // the address. Format is checked in UserServices (so a blank form
        // field isn't a 400).
        [StringLength(256)]
        public string? Email { get; set; }
    }
}
