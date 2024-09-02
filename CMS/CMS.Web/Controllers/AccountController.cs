using CMS.Application.DTOs;
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

namespace CMS.Web.Controllers
{
    //Test
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

        public void LogException(string methodName, Exception ex, string additionalInfo = null) => _accountService.LogException(methodName, ex, additionalInfo);

        //GET
        public async Task<ActionResult> Login()
        {
            try
            {
                if (_signInManager.IsSignedIn(User))
                {
                    if (User.IsInRole("HR Manager") || User.IsInRole("Admin") || User.IsInRole("General Manager"))
                        return RedirectToAction("Index", "Dashboard");

                    else if (User.IsInRole("Interviewer") || User.IsInRole("Solution Architecture"))
                        return RedirectToAction("MyInterviews", "Interviews");

                    else
                        return RedirectToAction("Index", "Home");
                }
                else
                    return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(Login), ex, "Faild to went to the Login Page");
                throw;
            }
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(Login collection)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _accountService.LoginAsync(collection);
                    if (result)
                    {
                        if (_signInManager.IsSignedIn(User))
                        {
                            if (User.IsInRole("HR Manager") || User.IsInRole("Admin") || User.IsInRole("General Manager"))
                                return RedirectToAction("Index", "Dashboard");

                            else if (User.IsInRole("Interviewer") || User.IsInRole("Solution Architecture"))
                                return RedirectToAction("MyInterviews", "Interviews");

                            else
                                return RedirectToAction("Index", "Home");
                        }
                        else
                            return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        var user = await _accountService.GetUserByEmailAsync(collection.UserEmail);
                        if (user is null)
                            ModelState.AddModelError("", "Invalid email address.");

                        else
                            ModelState.AddModelError("", $"Wrong password");

                        return View();
                    }
                }
                else
                    return View();
            }

            catch (Exception ex)
            {
                LogException(nameof(Login), ex, "Faild to Login ");
                throw ex;
            }
        }

        public async Task<ActionResult> DeleteAccount(string id)
        {
            try
            {
                var result = await _accountService.DeleteAccountAsync(id);
                if (result)
                    return RedirectToAction(nameof(Index));

                else
                    // Handle user not found or deletion failure
                    return View("Index");
            }
            catch (Exception ex)
            {
                LogException(nameof(DeleteAccount), ex, $"Faild to delete User ID: {id}");
                throw;
            }
        }

        //GET
        public async Task<ActionResult> Logout()
        {
            try
            {
                await _accountService.LogoutAsync();
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                LogException(nameof(Logout), ex, "Faild to Logout");
                throw;
            }
        }

        public IActionResult Index()
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
                {
                    // User is in the Admin role
                    var usersWithRoles = _accountService.GetAllUsersWithRoles();
                    return View(usersWithRoles);
                }

                else if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");

                else
                    return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                LogException(nameof(Index), ex, "Faild to Load the Index Page");
                throw;
            }

        }

        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var userDetails = _accountService.GetUsersById(id);
                return View(userDetails);
            }
            catch (Exception ex)
            {
                LogException(nameof(Details), ex, "Faild to Load the Details of the users");
                throw;
            }
        }

        // GET: Register/Create
        public IActionResult Create()
        {
            try
            {
                var roles = _roleManager.Roles.Select(r => r.Name).ToList();

                var model = new Register
                {
                    SelectedRole = roles.ToString()
                };

                ViewBag.Roles = new SelectList(roles);

                return View(model);
            }
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, "Faild to load the Create page");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Register collection)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = new IdentityUser
                    {
                        Email = collection.Email,
                        UserName = collection.UserName
                    };

                    var result = await _userManager.CreateAsync(user, collection.Password);

                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrEmpty(collection.SelectedRole))
                            await _userManager.AddToRoleAsync(user, collection.SelectedRole);

                        var emailModel = new EmailDTOs
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
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                var roles = _roleManager.Roles.Select(r => r.Name).ToList();
                ViewBag.Roles = new SelectList(roles);

                return View(collection);
            }
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, "Faild to Create a new user");
                throw;
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                var roles = _roleManager.Roles.Select(r => r.Name).ToList();

                var userRole = await _userManager.GetRolesAsync(user);

                var model = new Register
                {
                    RegisterrId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    SelectedRole = userRole.FirstOrDefault(),
                };

                ViewBag.Roles = new SelectList(roles);
                return View(model);
            }
            catch (Exception ex)
            {
                LogException(nameof(Edit), ex, $"faild to load the edit page for the users");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Register collection)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.FindByIdAsync(collection.RegisterrId);
                    var currentEmail = user.Email;
                    var currentUsername = user.UserName;
                    var currentUserRoles = await _userManager.GetRolesAsync(user);

                    user.Email = collection.Email;
                    user.UserName = collection.UserName;

                    if (!string.IsNullOrEmpty(collection.Password))
                    {

                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var passwordChangeResult = await _userManager.ResetPasswordAsync(user, token, collection.Password);

                        if (!passwordChangeResult.Succeeded)
                        {
                            foreach (var error in passwordChangeResult.Errors)
                                ModelState.AddModelError(string.Empty, error.Description);

                            return View(collection);
                        }
                    }

                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrEmpty(collection.SelectedRole))
                        {
                            var userRoles = await _userManager.GetRolesAsync(user);
                            await _userManager.RemoveFromRolesAsync(user, userRoles);
                            await _userManager.AddToRoleAsync(user, collection.SelectedRole);
                        }

                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var passwordChangeResult = await _userManager.ResetPasswordAsync(user, token, collection.Password);

                        if (currentEmail != collection.Email || currentUsername != collection.UserName || passwordChangeResult.Succeeded || !currentUserRoles.SequenceEqual(new[] { collection.SelectedRole }))
                        {
                            var emailModel = new EmailDTOs
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
                        foreach (var error in result.Errors)
                            ModelState.AddModelError("", error.Description);
                    }
                }

                var roles = _roleManager.Roles.Select(r => r.Name).ToList();
                ViewBag.Roles = new SelectList(roles);

                return View(collection);
            }
            catch (Exception ex)
            {
                LogException(nameof(Edit), ex, $"User ID: {collection.RegisterrId}");
                throw;
            }
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return NotFound();

                var user = await _userManager.FindByIdAsync(id);

                if (user is null)
                    return NotFound();

                var roles = _roleManager.Roles.Select(r => r.Name).ToList();

                var model = new Register
                {
                    RegisterrId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    SelectedRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
                };

                ViewBag.Roles = new SelectList(roles);

                return View(model);
            }
            catch (Exception ex)
            {
                LogException(nameof(Delete), ex, "faild to load the delete page for the users");
                throw;
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user is null)
                    return NotFound();

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                    return RedirectToAction(nameof(Index));

                else
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(user);
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(DeleteConfirmed), ex, $"Faild to delete User ID: {id}");
                throw;
            }
        }

        public IActionResult AccessDenied()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(AccessDenied), ex, "Faild to load the AccessDenied page");
                throw;
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(ChangePassword), ex, "Faild to load the ChangePassword page");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user is null)
                        return NotFound();

                    // Check if the current password is correct
                    var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);

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

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                    if (changePasswordResult.Succeeded)
                    {
                        // You may want to sign in the user again with the new password
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        // Redirect to a success page or display a success message
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        foreach (var error in changePasswordResult.Errors)
                            ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                LogException(nameof(ChangePassword), ex, "Failed to change password");
                throw;
            }
        }

        public IActionResult Profile()
        {
            // Get the currently logged-in user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Use the user ID to get user details
            var user = _accountService.GetUsersById(userId);

            return View(user);
        }
    }
}