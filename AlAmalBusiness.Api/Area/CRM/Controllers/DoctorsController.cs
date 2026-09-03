using AlAmalBusiness.Application.DTOs.CRM.LeadManageList;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AlAmalBusiness.Api.Area.CRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        public DoctorsController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        [HttpPost("CreateDoctor")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> CreateDoctor(DoctorDTO dto)
        {
            var result = await _doctorService.CreateDoctorAsync(dto);
            return result.Success ? Ok(result.Doctor) : BadRequest(result.Message);
        }

        [HttpGet("Doctors")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> GetAllDoctors() =>
            Ok(await _doctorService.GetAllDoctorsAsync());

        [HttpGet("ActiveDoctors")]
        public async Task<IActionResult> GetActiveDoctors() =>
            Ok(await _doctorService.GetActiveDoctorsAsync());

        [HttpGet("Doctor/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> GetDoctorById(int id)
        {
            var result = await _doctorService.GetDoctorByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result.Message);
        }

        [HttpPut("updateDoctor/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> UpdateDoctor(int id, DoctorDTO dto)
        {
            var result = await _doctorService.UpdateDoctorAsync(id, dto);
            return result.Success ? Ok(result.Doctor) : BadRequest(result.Message);
        }
    }
}
