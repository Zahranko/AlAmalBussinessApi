using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users.Response
{
    public class GetUserResponse
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

    }
}
