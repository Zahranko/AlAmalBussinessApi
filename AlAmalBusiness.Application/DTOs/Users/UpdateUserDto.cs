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
    }
}
