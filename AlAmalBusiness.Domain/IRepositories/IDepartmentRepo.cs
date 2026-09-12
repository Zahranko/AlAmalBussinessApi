using AlAmalBusiness.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IDepartmentRepo
    {
        Task<IEnumerable<Departments>> GetAllDepartmentsAsync();
        // Ordered the way the public feedback form's dropdown shows them. A
        // retired department stays on old messages but can't be picked again.
        Task<IEnumerable<Departments>> GetActiveDepartmentsAsync();
        Task<Departments?> GetDepartmentByIdAsync(int departmentId);
        Task<Departments> CreateDepartmentAsync(Departments department);
        Task<Departments> UpdateDepartmentAsync(Departments department);
        Task<bool> IsDepartmentExist(string name,int departmentId);
        // Rewrites DisplayOrder across the whole table in one save. False
        // when the caller's id list isn't exactly the set of departments
        // that exist (someone added or removed one meanwhile).
        Task<bool> ReorderAsync(IReadOnlyList<int> orderedIds);
    }
}
