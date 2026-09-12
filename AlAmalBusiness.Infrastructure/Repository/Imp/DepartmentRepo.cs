using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
               // Land last in the dropdown; the admin moves it from there.
               var lastOrder = await _context.Departments
                   .MaxAsync(d => (int?)d.DisplayOrder) ?? 0;
               department.DisplayOrder = lastOrder + 1;
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

        public async Task<IEnumerable<Departments>> GetActiveDepartmentsAsync()
        {
            // DisplayOrder, not Name: the public form lists departments the
            // way an admin arranged them (Radiology first, Others last), and
            // falls back to alphabetical only for rows that somehow tie.
            return await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<bool> ReorderAsync(IReadOnlyList<int> orderedIds)
        {
            var departments = await _context.Departments.ToListAsync();

            var positions = new Dictionary<int, int>();
            for (var i = 0; i < orderedIds.Count; i++)
            {
                // A duplicate id would leave some department unpositioned.
                if (!positions.TryAdd(orderedIds[i], i + 1))
                {
                    return false;
                }
            }

            // Every department must appear exactly once, or the result would
            // be an order the admin never saw on screen.
            if (positions.Count != departments.Count ||
                departments.Any(d => !positions.ContainsKey(d.Id)))
            {
                return false;
            }

            foreach (var department in departments)
            {
                department.DisplayOrder = positions[department.Id];
            }

            await _context.SaveChangesAsync();
            return true;
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
