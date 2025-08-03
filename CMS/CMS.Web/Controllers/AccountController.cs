using CMS.Application.CustomRoleAuth;
using CMS.Application.DTOs;
using CMS.Application.EmailTemplates;
using CMS.Application.Helpers;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
    [AllowAnonymous]
    public ActionResult Login()
    {
        try
        {
            if (_signInManager.IsSignedIn(User))
            {
                if (User.IsInRole("HR Manager") || User.IsInRole("Admin") || User.IsInRole("General Manager") || User.IsInRole("Viewer"))
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
    [AllowAnonymous]
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
                        if (User.IsInRole("HR Manager") || User.IsInRole("Admin") || User.IsInRole("General Manager") || User.IsInRole("Viewer"))
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
    [AuthorizeRoles("Admin", "HR Manager")]
    public async Task<IActionResult> Index(string userName, int pageNumber = 1, int pageSize = 5)
    {
        try
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
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/details")]
    [AuthorizeRoles("Admin", "HR Manager")]
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
    [AuthorizeRoles("Admin", "HR Manager")]
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
            if (await _accountService.EmailExistsAsync(collection.Email))
                return Json(new { success = false, message = "Email already exists. Please use a different email." });

            if (ModelState.IsValid)
            {
                IdentityUser user = new()
                {
                    Email = collection.Email,
                    UserName = collection.UserName
                };

                IdentityResult result = await _userManager.CreateAsync(user, collection.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(collection.SelectedRole))
                        await _userManager.AddToRoleAsync(user, collection.SelectedRole);

                    string emailBody = UserEmailTemplate.GetRegistrationEmailTemplate(user.UserName, user.Email, collection.Password);

                    EmailDTOs emailModel = new()
                    {
                        EmailTo = [user.Email],
                        Subject = "Welcome to CMS System",
                        EmailBody = emailBody
                    };

                    //Send an Email to the user after creted it
                    await _accountService.SendRegistrationEmail(user, collection.Password, emailModel);

                    // Your registration success logic here
                    return Redirect(Url.Action("index", "users"));
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
    [AuthorizeRoles("Admin", "HR Manager")]
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
            IdentityUser user = await _userManager.FindByIdAsync(collection.RegisterrId);
            if (user is null)
                return Json(new { success = false, message = "User not found." });

            // Only check if the email is different from the current one
            if (!string.Equals(collection.Email, user.Email, StringComparison.OrdinalIgnoreCase) &&
                await _userManager.FindByEmailAsync(collection.Email) != null)
            {
                return Json(new { success = false, message = "Email already exists. Please use a different email." });
            }

            if (ModelState.IsValid)
            {
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
                        string emailBody = UserEmailTemplate.GetAccountUpdateEmailTemplate(user.UserName, user.Email, collection.Password);

                        EmailDTOs emailModel = new()
                        {
                            EmailTo = [user.Email],
                            Subject = "Account Details Updated for CMS system",
                            EmailBody = emailBody
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
    [AuthorizeRoles("Admin", "HR Manager")]
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
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                return BadRequest(new { field = "CurrentPassword", message = "Current Password is required." });
            }

            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest(new { field = "NewPassword", message = "New Password is required." });
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                return BadRequest(new { field = "ConfirmPassword", message = "Confirm New Password is required." });
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return BadRequest(new { field = "ConfirmPassword", message = "New Password and Confirm Password do not match." });
            }

            IdentityUser user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound();

            bool isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);

            if (!isCurrentPasswordValid)
            {
                return BadRequest(new { field = "CurrentPassword", message = "The current password is incorrect." });
            }

            if (model.CurrentPassword == model.NewPassword)
            {
                return BadRequest(new { field = "NewPassword", message = "New Password must be different from the Current Password." });
            }

            IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (changePasswordResult.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Ok(new { message = "Password successfully changed! You will be logged out." });
            }
            else
            {
                return BadRequest(new { field = "NewPassword", message = "Error changing password. Please try again." });
            }
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request." });
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

    [HttpGet]
    [Route("checkEmailExists")]
    public async Task<IActionResult> CheckEmailExists(string email)
    {
        bool emailExists = await _accountService.EmailExistsAsync(email);
        return Json(new { exists = emailExists });
    }
}