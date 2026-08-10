using AlAmalBusiness.Application.DTOs.Users;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlAmalBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserServices _userServices;
    public UserController(IUserServices userServices)
    {
        _userServices = userServices;
    }
    //[Authorize(Roles = nameof(AppRoles.Admin))]
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserDTO createUserDTO)
    {
        if (ModelState.IsValid)
        {
           var res= await _userServices.CreateUserAsync(createUserDTO);
           if (res.IsSuccess == true)
           {
               return Ok(res.Message);
           }
           else
           {
               return BadRequest(res.Message);
           }
        }
        else
        { 
                return BadRequest(ModelState);  
        }
    }
    [HttpGet("getAll")]
    public async Task<IActionResult> GetAllUsers() {
        if (ModelState.IsValid)
        {
            var users = await _userServices.GetAllUserAsync();
            if (users != null)
            {
                return Ok(users);
            }
            else
            {
                return NotFound("No users found.");
            }
        }
        return BadRequest(ModelState);

    }
    [HttpPost("update")]
    public async Task<IActionResult> UpdateUser(UpdateUserDto updateDTO)
    {
        if (ModelState.IsValid)
        {
            var res = await _userServices.UpdateUserAsync(updateDTO);
            if (res.IsSuccess == true)
            {
                return Ok(res.Message);
            }
            else
            {
                return BadRequest(res.Message);
            }
        }
        else
        {
            return BadRequest(ModelState);
        }
    }
    [HttpPost("updateRoles")]
    public async Task<IActionResult> UpdateRoles(UpdateUserRolesDTO updateDTO)
    {
        if (ModelState.IsValid)
        {
            var res = await _userServices.UpdateRolesAsync(updateDTO);
            if (res.IsSuccess == true)
            {
                return Ok(res.Message);
            }
            else
            {
                return BadRequest(res.Message);
            }
        }
        else
        {
            return BadRequest(ModelState);
        }
    }
    [HttpPost("resetPassword")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDTO updateDTO)
    {
        if (ModelState.IsValid)
        {
            var res = await _userServices.ResetPasswordAsync(updateDTO);
            if (res.IsSuccess == true)
            {
                return Ok(res.Message);
            }
            else
            {
                return BadRequest(res.Message);
            }
        }
        else
        {
            return BadRequest(ModelState);
        }
    }
    [HttpPost("DisableUser")]
    public async Task<IActionResult> DisableUser(DisableUserDTO dto)
    {
        if (ModelState.IsValid)
        {
            var res = await _userServices.DisableUserAsync(dto);
            if (res.IsSuccess == true)
            {
                return Ok(res.Message);
            }
            else
            {
                return BadRequest(res.Message);
            }
        }
        else
        {
            return BadRequest(ModelState);
        }
    }


}