using AlAmalBusiness.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IDepartmentRepo
    {
        Task<IEnumerable<Departments>> GetAllDepartmentsAsync();
        Task<Departments?> GetDepartmentByIdAsync(int departmentId);
        Task<Departments> CreateDepartmentAsync(Departments department);
        Task<Departments> UpdateDepartmentAsync(Departments department);
        Task<bool> IsDepartmentExist(string name,int departmentId);
    }
}
