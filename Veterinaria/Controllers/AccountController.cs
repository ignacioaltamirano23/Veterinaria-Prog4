using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VetTest.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        // GET: /Account/AccessDenied
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
    }
}
