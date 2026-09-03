using AlAmalBusiness.Application.DTOs.Users;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AlAmalBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("PerUserLimit")]
[Authorize(Roles = nameof(AppRoles.Admin))]
public class UserController : ControllerBase
{
    private readonly IUserServices _userServices;
    public UserController(IUserServices userServices)
    {
        _userServices = userServices;
    }
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserDTO createUserDTO)
    {
     
           var res= await _userServices.CreateUserAsync(createUserDTO);
           if (res.IsSuccess == true)
           {
               return Ok(res);
           }
           else
           {
               return BadRequest(res.Message);
           }
        }
    [HttpGet("getAll")]
    public async Task<IActionResult> GetAllUsers() {
       
            var users = await _userServices.GetAllUserAsync();
            if (users != null)
            {
                return Ok(users);
            }
            return NotFound("No users found.");

        }
    
    [HttpPost("update/{id}")]
    public async Task<IActionResult> UpdateUser(string id,UpdateUserDto updateDTO)
    {
       
            var res = await _userServices.UpdateUserAsync(id,updateDTO);
            if (res.IsSuccess)
            {
                return Ok(res.User);
            }
            else
            {
                return BadRequest(res.Message);
            }
        }
     
    [HttpPut("updateRoles/{id}")]
    public async Task<IActionResult> UpdateRoles(string id,UpdateUserRolesDTO updateDTO)
    {
       
            var res = await _userServices.UpdateRolesAsync(id,updateDTO);
            if (res.IsSuccess == true)
            {
                return Ok(res.User);
            }
            else
            {
                return BadRequest(res.Message);
            }
        }
     
    [HttpPost("reset/{id}")]
    public async Task<IActionResult> ResetPassword(string id,ResetPasswordDTO updateDTO)
    {
        
            var res = await _userServices.ResetPasswordAsync(id,updateDTO);
            if (res.IsSuccess == true)
            {
                return Ok(res.Message);
            }
            else
            {
                return BadRequest(res.Message);
            }
        }
    
    [HttpPost("DisableUser/{id}")]
    public async Task<IActionResult> DisableUser(string id)
    {
        
            var res = await _userServices.DisableUserAsync(id);
            if (res.IsSuccess == true)
            {
                return Ok(res.Message);
            }
            else
            {
                return BadRequest(res.Message);
            }
        }
    [HttpGet("getuser/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {

        var user = await _userServices.GetUserById(id);
        if (user != null)
        {
            return Ok(user);
        }
        return NotFound("No users found.");

    }





}