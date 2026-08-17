using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Infrastructure.Repository.Imp
{
    public class DepartmentRepo : IDepartmentRepo
    {
        private readonly AppDbContext _context;
        public DepartmentRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Departments> CreateDepartmentAsync(Departments department)
        {
            var searchDepartment = _context.Departments.FirstOrDefault(d => d.Name == department.Name);
            if (searchDepartment != null)
            {
                return null!;
            }
            else
            {
                await _context.Departments.AddAsync(department);
               await _context.SaveChangesAsync();
                return department;
            }
        }
        public async Task<bool> IsDepartmentExist(string name,int departmentId)
        {

            var existingDepartment = await _context.Departments.FirstOrDefaultAsync(d => d.Name == name);

            if (existingDepartment != null&& existingDepartment.Id!=departmentId)
            {
                return true;
            }

            else
            {
                return false;
            }
        }


        public async Task<IEnumerable<Departments>> GetAllDepartmentsAsync()
        {
            var ListOfDepartments =await _context.Departments.ToListAsync();
            if (ListOfDepartments == null)
            {
                return null!;
            }
            else
            {
                return ListOfDepartments;
            }
        }

        public async Task<Departments> GetDepartmentByIdAsync(int departmentId)
        {
            var searchDepartment = await _context.Departments.FindAsync(departmentId);
            if (searchDepartment == null)
            {
                return null!;
            }
            else
            {
                return searchDepartment;
            }

        }

        public async Task<Departments> UpdateDepartmentAsync(Departments department)
        {
            var searchDepartment = await _context.Departments.FindAsync(department.Id);
            if (searchDepartment == null)
            {
                return null!;
            }
            
            else
            {
                
                searchDepartment.Name = department.Name;
                _context.Departments.Update(searchDepartment);
                await _context.SaveChangesAsync();
                return searchDepartment;
            }
        }
    }
}
