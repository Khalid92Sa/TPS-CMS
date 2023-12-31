﻿using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace CMS.Services.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;

        public AccountService(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext dbContext,
            IUserRepository userRepository, RoleManager<IdentityRole> roleManager,
            IHttpContextAccessor httpContextAccessor

            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _userRepository = userRepository;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async void LogException(string methodName, Exception ex, string additionalInfo)
        {
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var userId = currentUser?.Id;
            _dbContext.Logs.Add(new Log
            {
                MethodName = methodName,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,CreatedByUserId = userId,
                LogTime = DateTime.Now,
                
                AdditionalInfo = additionalInfo
            });
            _dbContext.SaveChanges();
        }


        public async Task<List<Login>> GetAllUsersAsync()
        {
            try
            {
                var users = _userManager.Users.ToList();
                List<Login> listuser = new List<Login>();
                for (int i = 0; i < users.Count; i++)
                {
                    Login list = new Login();
                    list.Id = users[i].Id;
                    list.UserEmail = users[i].Email;
                    list.UserName = users[i].UserName;
                    list.Password = users[i].PasswordHash;
                    listuser.Add(list);
                }
                return listuser;
            }
           catch (Exception ex)
            {
                LogException(nameof(GetAllUsersAsync), ex,null);
                throw ex;
            }
        }

        public async Task<bool> LoginAsync(Login collection)
        
        {
            try
            { 
                var signedUser = await _userManager.FindByEmailAsync(collection.UserEmail);
            if (signedUser == null)
            {
                return false;
            }

            var result = await _signInManager.PasswordSignInAsync(signedUser.UserName, collection.Password, collection.RememberMe, false);
            
            return result.Succeeded;


            }
          
             catch (Exception ex)
            {
                LogException(nameof(LoginAsync), ex, "LoginAsync not working");
                throw ex;
            }
        }

        public async Task<bool> DeleteAccountAsync(string id)
        {
            try
            {
                return await _userRepository.Delete(id);

            }
            catch (Exception ex)
            {
                LogException(nameof(DeleteAccountAsync), ex,$"Error while deleting Account with id :{id}");
                throw ex;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _signInManager.SignOutAsync();

            }
            catch (Exception ex)
            {
                LogException(nameof(LogoutAsync), ex, $"Error while Logout");
                throw ex;
            }
        }
        public async Task<Result<IList<IdentityUser>>> GetAllInterviewers()
        {
            try
            {
                // Get all users
                var users = await _userManager.Users.ToListAsync();

                if (users == null || users.Count == 0)
                {
                    return Result<IList<IdentityUser>>.Failure(null, "No users found");
                }

                // Get the HR Manager role
                var hrManagerRole = await _roleManager.FindByNameAsync("HR Manager");
                var adminRole = await _roleManager.FindByNameAsync("Admin");


                if (hrManagerRole == null)
                {
                    return Result<IList<IdentityUser>>.Failure(null, "HR Manager Role Not Found");
                }
                if (adminRole == null)
                {
                    return Result<IList<IdentityUser>>.Failure(null, "Admin Role Not Found");
                }

                // Filter out users with the HR Manager role
                var usersExcludingHRManager = users
                    .Where(user => !(_userManager.IsInRoleAsync(user, hrManagerRole.Name).Result || _userManager.IsInRoleAsync(user, adminRole.Name).Result))
                            .ToList();
                return Result<IList<IdentityUser>>.Success(usersExcludingHRManager);
            }
            catch (Exception ex)
            {
                LogException(nameof(GetAllInterviewers), ex, "Error while getting all users excluding HR Manager");
                throw ex;
            }
        }

        public async Task<Result<IList<IdentityUser>>> GetAllInterviewersGM()
        {
            try
            {
                var GMRole = await _roleManager.FindByNameAsync("General Manager");
                if (GMRole == null)
                {
                    return Result<IList<IdentityUser>>.Failure(null, "Requested Role Not Found");
                }
                var interviewers = await _userManager.GetUsersInRoleAsync(GMRole.Name);
                if (interviewers == null)
                {
                    return Result<IList<IdentityUser>>.Failure(null, "No General Manager found");
                }
                return Result<IList<IdentityUser>>.Success(interviewers);
            }


            catch (Exception ex)
            {
                LogException(nameof(GetAllInterviewersGM), ex, "Error while Getting All Interviewers");
                throw ex;
            }
        }

        public async Task<Result<IList<IdentityUser>>> GetAllArchitectureInterviewers()
        {
            try
            {
                var ArchiRole = await _roleManager.FindByNameAsync("Solution Architecture");
                if (ArchiRole == null)
                {
                    return Result<IList<IdentityUser>>.Failure(null, "Requested Role Not Found");
                }
                var architectures = await _userManager.GetUsersInRoleAsync(ArchiRole.Name);
                if (architectures == null)
                {
                    return Result<IList<IdentityUser>>.Failure(null, "No Architectures found");
                }
                return Result<IList<IdentityUser>>.Success(architectures);
            }


            catch (Exception ex)
            {
                LogException(nameof(GetAllArchitectureInterviewers), ex, "Error while Getting All Architecture");
                throw ex;
            }
        }
        public async Task<string> GetUserRoleAsync(IdentityUser user)
        {
            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                return roles.FirstOrDefault();

            }

            catch (Exception ex)
            {
                LogException(nameof(GetUserRoleAsync), ex, "Error while Getting user role");
                throw ex;
            }
        }

        public async Task<IdentityUser> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _userManager.FindByEmailAsync(email);
            }
            catch (Exception ex)
            {
                LogException(nameof(GetUserByEmailAsync), ex, "Error while Getting user by email");
                throw ex;
            }
        }
        public List<Register> GetAllUsersWithRoles()
        {
            try
            {
                var users = _userRepository.GetAllUsersWithRoles();
                var usersWithRoles = new List<Register>();

                foreach (var user in users)
                {
                    var roles = _userRepository.GetUserRoles(user);

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
           

              catch (Exception ex)
            {
                LogException(nameof(GetAllUsersWithRoles), ex, "Error while Getting all users with roles");
                throw ex;
            }
        }
        public Register GetUsersById(string userId)
        {
            try
            {
                var user = _userRepository.GetUserById(userId);

                if (user == null)
                {
                    return null;
                }
                var roles = _userRepository.GetUserRoles(user);

                var userDetails = new Register
                {
                    RegisterrId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    SelectedRole = roles.FirstOrDefault()
                };

                return userDetails;

            }

            catch (Exception ex)
            {
                LogException(nameof(GetUsersById), ex, "GetUsersById not working");
                throw ex;
            }
        }


        //public async Task SendRegistrationEmail(IdentityUser user, string password)
        //{
        //    try
        //    {
        //        var receiver = $"{user.Email}";
        //        var subject = $"New User created in CMS system for you";
        //        var message = $"Dear {user.UserName},\n\n"
        //                   + $"Your account details:\n"
        //                   + $"Username: {user.UserName}\n"
        //                   + $"Email: {user.Email}\n"
        //                   + $"Password: {password}\n\n"
        //                   + $"Login to your account: [https://apps.sssprocess.com:6134/]";

        //         _emailSender.SendEmailAsync(receiver, subject, message);

        //    }
        //    catch (Exception ex)
        //    {
        //        LogException(nameof(SendRegistrationEmail), ex);
        //        throw ex;
        //    }
        //}

        public async Task SendRegistrationEmail(IdentityUser user, string password, EmailDTOs emailmodel)
        {
            try
            {
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "mail.sssprocess.com";
                smtp.Port = 587;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.EnableSsl = false;
                smtp.UseDefaultCredentials = true;
                string UserName = "CMS@sss-process.org";
                string Password = "P@ssw0rd2023";
                smtp.Credentials = new NetworkCredential(UserName, Password);

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress("cms@techprocess.net");

                    if (emailmodel.EmailTo != null && emailmodel.EmailTo.Any())
                    {
                        foreach (var to in emailmodel.EmailTo)
                        {
                            message.To.Add(to);
                        }
                    }

                    message.Body = emailmodel.EmailBody;
                    message.Subject = emailmodel.Subject;
                    message.IsBodyHtml = true;

                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(SendRegistrationEmail), ex, "Faild to send an email");
                throw ex;
            }
        }

        }
}
