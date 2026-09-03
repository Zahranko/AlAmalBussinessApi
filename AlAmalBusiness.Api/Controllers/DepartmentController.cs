using AlAmalBusiness.Application.DTOs.Departments;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AlAmalBusiness.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentServices;
        public DepartmentController(IDepartmentService departmentServices)
        {
            _departmentServices = departmentServices;
        }
        [HttpPost("CreateDepartment")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> CreateDepartment(DepartmentDTO dto)
        {
            var department = await _departmentServices.CreateDepartmentAsync(dto);
            if (department.Success)
            {
                return Ok(department.Department);
            }
            else
            { 
            return BadRequest(department.Message);

            }
        }
        [HttpGet("Departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _departmentServices.GetAllDepartmentsAsync();
            if (departments != null && departments.Any())
            {
                return Ok(departments);
            }
            else
            {
                return NotFound("No departments found.");
            }
        }
        [HttpGet("Department/{id}")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            var department = await _departmentServices.GetDepartmentByIdAsync(id);
            if (department.Success)
            {
                return Ok(department);
            }
            else
            {
                return NotFound(department.Message);
            }
        }
        [HttpPut("updateDepartment/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> UpdateDepartment(int id, DepartmentDTO dto)
        {
            var department = await _departmentServices.UpdateDepartmentAsync(id, dto);
            if (department.Success)
            {
                return Ok(department.Department);
            }
            else
            {
                return BadRequest(department.Message);
            }
        }
      
    }
}
