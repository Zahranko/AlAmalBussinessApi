using AlAmalBusiness.Application.DTOs.Departments;
using AlAmalBusiness.Application.DTOs.Departments.Response;
using AlAmalBusiness.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDTO>> GetAllDepartmentsAsync();
        Task<DepartmentResponse> GetDepartmentByIdAsync(int departmentId);
        Task<DepartmentResponse> CreateDepartmentAsync(DepartmentDTO department);
        Task<DepartmentResponse> UpdateDepartmentAsync(int id,DepartmentDTO department);
    }
}
