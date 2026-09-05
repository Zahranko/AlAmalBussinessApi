using AlAmalBusiness.Application.DTOs.Departments;
using AlAmalBusiness.Application.DTOs.Departments.Response;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepo _repo;

        public DepartmentService(IDepartmentRepo repo)
        {
            _repo = repo;
        }

        public async Task<DepartmentResponse> CreateDepartmentAsync(DepartmentDTO department)
        {
            var existingDepartment = await _repo.IsDepartmentExist(department.Name!,0);
            if (existingDepartment)
            {
                return new DepartmentResponse
                {
                    Message = $"Department with name '{department.Name}' already exists.",
                    Success = false
                };
            }

            var departmentEntity = new Departments
            {
                Name = department.Name,
            };

            await _repo.CreateDepartmentAsync(departmentEntity);

            return new DepartmentResponse
            {
                Success = true,
                Department = new DepartmentDTO
                {
                    Id = departmentEntity.Id,
                    Name = departmentEntity.Name,
                    IsActive = departmentEntity.IsActive
                }
            };
        }

        public async Task<IEnumerable<DepartmentDTO>> GetAllDepartmentsAsync()
        {
            var departments = await _repo.GetAllDepartmentsAsync();

            if (departments == null || !departments.Any())
            {
                return new List<DepartmentDTO>();
            }

            return departments.Select(d => new DepartmentDTO
            {
                Id = d.Id,
                Name = d.Name,
                IsActive = d.IsActive
            });
        }

        public async Task<DepartmentResponse> GetDepartmentByIdAsync(int departmentId)
        {
            var department = await _repo.GetDepartmentByIdAsync(departmentId);
            if (department == null)
            {
                return new DepartmentResponse
                {
                    Message = $"Department with ID {departmentId} not found.",
                    Success = false
                };
            }

            return new DepartmentResponse
            {
                Success = true,
                Department = new DepartmentDTO
                {
                    Id = department.Id,
                    Name = department.Name,
                    IsActive = department.IsActive
                }
            };
        }

        public async Task<DepartmentResponse> UpdateDepartmentAsync(int departmentId, DepartmentDTO department)
        {
            var departmentEntity = await _repo.GetDepartmentByIdAsync(departmentId);
            if (departmentEntity == null)
            {
                return new DepartmentResponse
                {
                    Message = $"Department with ID {departmentId} not found.",
                    Success = false
                };
            }
            if (department.Name == null) {
                 return new DepartmentResponse
                {
                    Message = $"Department name is empty",
                    Success = false
                };
            } 
            var nameExists = await _repo.IsDepartmentExist(department.Name, departmentId);
            if (nameExists)
            {
                return new DepartmentResponse
                {
                    Message = $"Department with name '{department.Name}' already exists.",
                    Success = false
                };
            }
            departmentEntity.Name = department.Name;
            departmentEntity.IsActive = department.IsActive;

            await _repo.UpdateDepartmentAsync(departmentEntity);

            return new DepartmentResponse
            {
                Success = true,
                Department = new DepartmentDTO
                {
                    Id = departmentEntity.Id,
                    Name = departmentEntity.Name,
                    IsActive = departmentEntity.IsActive
                }
            };
        }
    }
}