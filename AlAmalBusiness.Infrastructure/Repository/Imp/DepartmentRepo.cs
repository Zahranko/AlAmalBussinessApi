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
            bool exists = await _context.Departments
        .AnyAsync(d => d.Name == department.Name);
            if (exists)
            {
                return null!;
            }
            else
            {
               _context.Departments.Add(department);
               await _context.SaveChangesAsync();
                return department;
            }
        }
        public async Task<bool> IsDepartmentExist(string name,int departmentId)
        {

            return await _context.Departments
        .AnyAsync(d => d.Name == name && d.Id != departmentId);
        }


        public async Task<IEnumerable<Departments>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task<Departments?> GetDepartmentByIdAsync(int departmentId)
        {
            return await _context.Departments.FindAsync(departmentId);
          

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
