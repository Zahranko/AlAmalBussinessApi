using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users
{
    public class UpdateUserRolesDTO
    {
        [Required]
        public List<string>? Roles { get; set; }

    }
}
