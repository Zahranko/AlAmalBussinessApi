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
        Task<IEnumerable<DepartmentDTO>> GetActiveDepartmentsAsync();
        Task<DepartmentResponse> GetDepartmentByIdAsync(int departmentId);
        Task<DepartmentResponse> CreateDepartmentAsync(DepartmentDTO department);
        Task<DepartmentResponse> UpdateDepartmentAsync(int id,DepartmentDTO department);
        // Admin-only: rewrites the order the public feedback form lists
        // departments in. Takes every id, in the order to store.
        Task<DepartmentResponse> ReorderDepartmentsAsync(ReorderDepartmentsDTO request);
    }
}
