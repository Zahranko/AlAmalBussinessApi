using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users
{
    public class ResetPasswordDTO
    {
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string? Password { get; set; }
        [Required]
        public string? UserId { get; set; }



    }
}
