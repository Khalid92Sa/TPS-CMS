using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    SignInManager<IdentityUser> _signInManager;

    public HomeController(SignInManager<IdentityUser> _signInManager) => this._signInManager = _signInManager;

    public IActionResult Index()
    {
        if (_signInManager.IsSignedIn(User))
            return View();

        else
            return RedirectToAction("login", "users");
    }
    // Public landing page. Signed-in users go to /users/login, which forwards them to their workspace.
    [AllowAnonymous]
    [HttpGet("/")]
    [HttpGet("/welcome")]
    public IActionResult Landing()
    {
        if (_signInManager.IsSignedIn(User))
            return Redirect("/users/login");

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult NotFound()
    {
        Response.StatusCode = 404;
        return View();
    }
}