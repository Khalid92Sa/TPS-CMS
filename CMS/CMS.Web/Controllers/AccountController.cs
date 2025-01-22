using CMS.Application.DTOs;
using CMS.Application.Helpers;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("users")]
public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IAccountService _accountService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(IAccountService accountService,
                             UserManager<IdentityUser> userManager,
                             RoleManager<IdentityRole> roleManager,
                             SignInManager<IdentityUser> signInManager,
                             IHttpContextAccessor httpContextAccessor)
    {
        _accountService = accountService;
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    [Route("login")]
    public ActionResult Login()
    {
        try
        {
            if (_signInManager.IsSignedIn(User))
            {
                if (User.IsInRole("HR Manager") || User.IsInRole("Admin") || User.IsInRole("General Manager"))
                    return Redirect(Url.Action("index", "dashboard"));

                else if (User.IsInRole("Interviewer") || User.IsInRole("Solution Architecture"))
                    return Redirect(Url.Action("myInterviews", "interviews"));

                else
                    return RedirectToAction("Index", "Home");
            }
            else
                return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("login")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Login(Login collection)
    {
        try
        {
            if (ModelState.IsValid)
            {
                bool result = await _accountService.LoginAsync(collection);
                if (result)
                {
                    if (_signInManager.IsSignedIn(User))
                    {
                        if (User.IsInRole("HR Manager") || User.IsInRole("Admin") || User.IsInRole("General Manager"))
                            return Redirect(Url.Action("index", "dashboard"));

                        else if (User.IsInRole("Interviewer") || User.IsInRole("Solution Architecture"))
                            return Redirect(Url.Action("myInterviews", "interviews"));

                        else
                            return RedirectToAction("Index", "Home");
                    }
                    else
                        return RedirectToAction("Index", "Home");
                }
                else
                {
                    IdentityUser user = await _accountService.GetUserByEmailAsync(collection.UserEmail);
                    if (user is null)
                        ModelState.AddModelError(string.Empty, "Invalid email address.");

                    else
                        ModelState.AddModelError(string.Empty, $"Wrong password");

                    return View();
                }
            }
            else
                return View();
        }

        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/deleteAccount")]
    public async Task<ActionResult> DeleteAccount(string id)
    {
        try
        {
            bool result = await _accountService.DeleteAccountAsync(id);
            if (result)
                return RedirectToAction(nameof(Index));

            else
                return View("Index");
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("logout")]
    public async Task<ActionResult> Logout()
    {
        try
        {
            await _accountService.LogoutAsync();
            return Redirect(Url.Action("login", "users"));
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("index")]
    public async Task<IActionResult> Index(string userName, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
                ViewBag.userNameFilter = userName;

                List<Register> usersWithRoles = await _accountService.GetAllUsersWithRolesAsync();

                if (!string.IsNullOrEmpty(userName))
                    usersWithRoles = usersWithRoles.Where(u => u.UserName.Contains(userName, StringComparison.OrdinalIgnoreCase))
                                                   .ToList();

                List<Register> paginatedUsers = usersWithRoles
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                PaginatedList<Register> paginatedList = new(paginatedUsers, usersWithRoles.Count, pageNumber, pageSize);

                return View(paginatedList);
            }

            else if (User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            else
            {
                return Redirect(Url.Action("login", "users"));
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/details")]
    public async Task<IActionResult> Details(string id)
    {
        try
        {
            Register userDetails = await _accountService.GetUsersById(id);
            return View(userDetails);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("create")]
    public IActionResult Create()
    {
        try
        {
            List<string> roles = _roleManager.Roles.Select(r => r.Name).ToList();

            Register model = new Register
            {
                SelectedRole = roles.ToString()
            };

            ViewBag.Roles = new SelectList(roles);

            return View(model);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Register collection)
    {
        try
        {
            if (ModelState.IsValid)
            {
                IdentityUser user = new IdentityUser
                {
                    Email = collection.Email,
                    UserName = collection.UserName
                };

                IdentityResult result = await _userManager.CreateAsync(user, collection.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(collection.SelectedRole))
                        await _userManager.AddToRoleAsync(user, collection.SelectedRole);

                    EmailDTOs emailModel = new EmailDTOs
                    {
                        EmailTo = new List<string> { user.Email },
                        Subject = "Welcome to CMS System",
                        EmailBody = $"<p>Dear {user.UserName.Replace("_", " ")},</p>\n\n" +
                                "<p>Your account details:</p>\n" +
                                $"<ul>\n" +
                                $"  <li>Username: {user.UserName}</li>\n" +
                                $"  <li>Email: {user.Email}</li>\n" +
                                $"  <li>Password: {collection.Password}</li>\n" +
                                $"</ul>\n\n" +
                                $"<p>Login to your account: <a href='https://apps.sssprocess.com:6134/'>Click here</a></p>"
                    };

                    //Send an Email to the user after creted it
                    await _accountService.SendRegistrationEmail(user, collection.Password, emailModel);

                    // Your registration success logic here
                    return RedirectToAction("index");
                }
                else
                {
                    foreach (IdentityError error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            List<string> roles = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.Roles = new SelectList(roles);

            return View(collection);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/update")]
    public async Task<IActionResult> Edit(string id)
    {
        try
        {
            IdentityUser user = await _userManager.FindByIdAsync(id);

            List<string> roles = _roleManager.Roles.Select(r => r.Name).ToList();

            IList<string> userRole = await _userManager.GetRolesAsync(user);

            Register model = new()
            {
                RegisterrId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                SelectedRole = userRole.FirstOrDefault(),
            };

            ViewBag.Roles = new SelectList(roles);
            return View(model);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Register collection)
    {
        try
        {
            if (ModelState.IsValid)
            {
                IdentityUser user = await _userManager.FindByIdAsync(collection.RegisterrId);
                string currentEmail = user.Email;
                string currentUsername = user.UserName;
                IList<string> currentUserRoles = await _userManager.GetRolesAsync(user);

                user.Email = collection.Email;
                user.UserName = collection.UserName;

                if (!string.IsNullOrEmpty(collection.Password))
                {

                    string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    IdentityResult passwordChangeResult = await _userManager.ResetPasswordAsync(user, token, collection.Password);

                    if (!passwordChangeResult.Succeeded)
                    {
                        foreach (IdentityError error in passwordChangeResult.Errors)
                            ModelState.AddModelError(string.Empty, error.Description);

                        return View(collection);
                    }
                }

                IdentityResult result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(collection.SelectedRole))
                    {
                        IList<string> userRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, userRoles);
                        await _userManager.AddToRoleAsync(user, collection.SelectedRole);
                    }

                    string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    IdentityResult passwordChangeResult = await _userManager.ResetPasswordAsync(user, token, collection.Password);

                    if (currentEmail != collection.Email || currentUsername != collection.UserName || passwordChangeResult.Succeeded || !currentUserRoles.SequenceEqual(new[] { collection.SelectedRole }))
                    {
                        EmailDTOs emailModel = new EmailDTOs
                        {
                            EmailTo = new List<string> { user.Email },
                            Subject = "Account Details Updated for CMS system",
                            EmailBody = $"<p>Dear {user.UserName.Replace("_", " ")},</p>\n\n" +
                                "<p>Your account details have been updated:</p>\n" +
                                $"<ul>\n" +
                                $"  <li>Username: {user.UserName}</li>\n" +
                                $"  <li>Email: {user.Email}</li>\n" +
                                $"  <li>Password: {collection.Password}</li>\n" +
                                $"</ul>\n\n" +
                                $"<p>Login to your account: <a href='https://apps.sssprocess.com:6134/'>Click here</a></p>"
                        };

                        // Send an email only if there are changes
                        await _accountService.SendRegistrationEmail(user, collection.Password, emailModel);
                    }

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (IdentityError error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            List<string> roles = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.Roles = new SelectList(roles);

            return View(collection);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/delete")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            IdentityUser user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return NotFound();

            List<string> roles = _roleManager.Roles.Select(r => r.Name).ToList();

            Register model = new()
            {
                RegisterrId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                SelectedRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
            };

            ViewBag.Roles = new SelectList(roles);

            return View(model);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost, ActionName("Delete")]
    [Route("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        try
        {
            IdentityUser user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return NotFound();

            IdentityResult result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            else
            {
                foreach (IdentityError error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(user);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("accessDenied")]
    public IActionResult AccessDenied()
    {
        try
        {
            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("changePassword")]
    public IActionResult ChangePassword()
    {
        try
        {
            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("changePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDTO model)
    {
        try
        {
            if (ModelState.IsValid)
            {
                IdentityUser user = await _userManager.GetUserAsync(User);
                if (user is null)
                    return NotFound();

                // Check if the current password is correct
                bool isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);

                if (!isCurrentPasswordValid)
                {
                    ModelState.AddModelError(string.Empty, "The current password is incorrect.");
                    return View(model);
                }

                // Check if the new password is different from the current password
                if (model.CurrentPassword == model.NewPassword)
                {
                    ModelState.AddModelError(string.Empty, "The new password must be different from the current password.");
                    return View(model);
                }

                IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (changePasswordResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (IdentityError error in changePasswordResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("profile")]
    public IActionResult Profile()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Use the user ID to get user details
        Task<Register> user = _accountService.GetUsersById(userId);

        return View(user);
    }
}