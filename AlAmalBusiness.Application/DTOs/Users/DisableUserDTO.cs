using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users
{
    public class DisableUserDTO
    {
        [Required]
        public string? UserId { get; set; }
    }
}
