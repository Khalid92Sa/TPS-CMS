using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;

    public AccountService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext dbContext,
        IUserRepository userRepository,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _userRepository = userRepository;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<Login>> GetAllUsersAsync()
    {
        try
        {
            List<IdentityUser> users = await _userManager.Users.ToListAsync();
            List<Login> listuser = [];

            for (int i = 0; i < users.Count; i++)
            {
                Login list = new()
                {
                    Id = users[i].Id,
                    UserEmail = users[i].Email,
                    UserName = users[i].UserName,
                    Password = users[i].PasswordHash
                };

                listuser.Add(list);
            }

            return listuser;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> LoginAsync(Login collection)
    {
        try
        {
            IdentityUser signedUser = await _userManager.FindByEmailAsync(collection.UserEmail);

            if (signedUser is null)
                return false;

            SignInResult result = await _signInManager.PasswordSignInAsync(signedUser.UserName, collection.Password, collection.RememberMe, false);
            return result.Succeeded;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> DeleteAccountAsync(string id)
    {
        try
        {
            return await _userRepository.Delete(id);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _signInManager.SignOutAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Result<IList<IdentityUser>>> GetAllInterviewers()
    {
        try
        {
            List<IdentityUser> users = await _userManager.Users.ToListAsync();

            if (users is null || users.Count == 0)
                return Result<IList<IdentityUser>>.Failure(null, "No users found");

            // Get the HR Manager role
            IdentityRole hrManagerRole = await _roleManager.FindByNameAsync("HR Manager");
            IdentityRole adminRole = await _roleManager.FindByNameAsync("Admin");

            if (hrManagerRole is null)
                return Result<IList<IdentityUser>>.Failure(null, "HR Manager Role Not Found");

            if (adminRole is null)
                return Result<IList<IdentityUser>>.Failure(null, "Admin Role Not Found");

            // Filter out users with the HR Manager role
            List<IdentityUser> usersExcludingHRManager = users.Where(user => !(_userManager.IsInRoleAsync(user, hrManagerRole.Name).Result || _userManager.IsInRoleAsync(user, adminRole.Name).Result))
                                                              .ToList();
            return Result<IList<IdentityUser>>.Success(usersExcludingHRManager);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Result<IList<IdentityUser>>> GetAllInterviewersGM()
    {
        try
        {
            IdentityRole GMRole = await _roleManager.FindByNameAsync("General Manager");

            if (GMRole is null)
                return Result<IList<IdentityUser>>.Failure(null, "Requested Role Not Found");

            IList<IdentityUser> interviewers = await _userManager.GetUsersInRoleAsync(GMRole.Name);

            if (interviewers is null)
                return Result<IList<IdentityUser>>.Failure(null, "No General Manager found");

            return Result<IList<IdentityUser>>.Success(interviewers);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Result<IList<IdentityUser>>> GetAllArchitectureInterviewers()
    {
        try
        {
            IdentityRole ArchiRole = await _roleManager.FindByNameAsync("Solution Architecture");

            if (ArchiRole is null)
                return Result<IList<IdentityUser>>.Failure(null, "Requested Role Not Found");

            IList<IdentityUser> architectures = await _userManager.GetUsersInRoleAsync(ArchiRole.Name);

            if (architectures is null)
                return Result<IList<IdentityUser>>.Failure(null, "No Architectures found");

            return Result<IList<IdentityUser>>.Success(architectures);
        }
        catch (Exception)
        {
            throw;
        }
    }
    public async Task<string> GetUserRoleAsync(IdentityUser user)
    {
        try
        {
            IList<string> roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IdentityUser> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _userManager.FindByEmailAsync(email);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Register>> GetAllUsersWithRolesAsync()
    {
        try
        {
            List<IdentityUser> users = await _userRepository.GetAllUsersWithRoles();
            List<Register> usersWithRoles = new List<Register>();

            foreach (IdentityUser user in users)
            {
                List<string> roles = _userRepository.GetUserRoles(user);

                usersWithRoles.Add(new Register
                {
                    RegisterrId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    SelectedRole = roles.FirstOrDefault()
                });
            }

            return usersWithRoles;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Register> GetUsersById(string userId)
    {
        try
        {
            IdentityUser user = await _userRepository.GetUserById(userId);

            if (user is null)
                return null;

            List<string> roles = _userRepository.GetUserRoles(user);

            Register userDetails = new Register
            {
                RegisterrId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                SelectedRole = roles.FirstOrDefault()
            };

            return userDetails;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task SendRegistrationEmail(IdentityUser user, string password, EmailDTOs emailmodel)
    {
        try
        {
            SmtpClient smtp = new SmtpClient
            {
                Host = "mail.sssprocess.com",
                Port = 587,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = false,
                UseDefaultCredentials = true
            };
            string UserName = "CMS@sss-process.org";
            string Password = "P@ssw0rd2023";
            smtp.Credentials = new NetworkCredential(UserName, Password);

            using MailMessage message = new MailMessage();

            message.From = new MailAddress("cms@techprocess.net");

            if (emailmodel.EmailTo != null && emailmodel.EmailTo.Any())
            {
                foreach (string to in emailmodel.EmailTo)
                    message.To.Add(to);
            }

            message.Body = emailmodel.EmailBody;
            message.Subject = emailmodel.Subject;
            message.IsBodyHtml = true;

            await smtp.SendMailAsync(message);
        }
        catch (Exception)
        {
            throw;
        }
    }
}