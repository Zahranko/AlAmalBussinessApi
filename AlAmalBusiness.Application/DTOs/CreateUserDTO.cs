using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.DTOs
{
    public class CreateUserDTO
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

    }
}
