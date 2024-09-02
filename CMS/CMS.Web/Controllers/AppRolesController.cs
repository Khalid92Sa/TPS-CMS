using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CMS.Web.Controllers
{
    [Authorize]
    public class AppRolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public AppRolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
                return View("AccessDenied");

            else
            {
                var roles = _roleManager.Roles;
                return View(roles);
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!User.Identity.IsAuthenticated)
                return View("AccessDenied");
            else
                return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(IdentityRole model)
        {
            if (!User.Identity.IsAuthenticated)
                return View("AccessDenied");

            else
            {
                if (!_roleManager.RoleExistsAsync(model.Name).GetAwaiter().GetResult())
                    _roleManager.CreateAsync(new IdentityRole(model.Name)).GetAwaiter().GetResult();

                return RedirectToAction("Index");
            }
           
        }
    }
}
