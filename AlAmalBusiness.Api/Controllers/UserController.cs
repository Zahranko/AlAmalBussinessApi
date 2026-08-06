using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace AlAmalBusiness.Api.Controllers;

public class UserController : Controller
{
    private readonly IUserServices _userServices;
    public UserController(IUserServices userServices)
    {
        _userServices = userServices;
    }
    
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
}