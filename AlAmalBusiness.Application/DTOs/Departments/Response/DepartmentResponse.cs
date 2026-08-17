using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Departments.Response
{
    public class DepartmentResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DepartmentDTO? Department { get; set; }
    }
}
