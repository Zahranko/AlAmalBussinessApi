using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Departments
{
    public class DepartmentDTO
    {
        public int Id { get; set; }
        public string? Name{ get; set; }
        public bool IsActive { get; set; } = true;
    }
}
