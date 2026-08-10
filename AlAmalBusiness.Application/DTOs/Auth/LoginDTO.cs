using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Auth
{
    public class LoginDTO
    {
        [Required]
        
        public string? UserName { get; set; }
        [Required]
        
        public string? Password { get; set; }
    }
}
