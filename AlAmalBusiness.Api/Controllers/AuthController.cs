using Microsoft.AspNetCore.Mvc;

namespace AlAmalBusiness.Api.Controllers;

public class AuthController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
}