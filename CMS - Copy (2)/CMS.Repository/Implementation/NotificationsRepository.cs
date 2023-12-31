﻿using CMS.Domain.Entities;
using CMS.Domain;
using CMS.Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace CMS.Repository.Implementation
{
    public class NotificationsRepository : INotificationsRepository
    {

        private readonly ApplicationDbContext Db;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationsRepository(ApplicationDbContext _db, RoleManager<IdentityRole> roleManager,
             UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            Db = _db;
            _roleManager = roleManager;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async  void LogException(string methodName, Exception ex, string additionalInfo)
        {
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var userId = currentUser?.Id;
            Db.Logs.Add(new Log
            {
                MethodName = methodName,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,CreatedByUserId = userId,
                LogTime = DateTime.Now,
                
                AdditionalInfo = additionalInfo
            });
            Db.SaveChanges();
        }

        public async Task<IEnumerable<Notifications>> GetAllNotifications()
        {
            try
            {
                return await Db.Notifications.ToListAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(GetAllNotifications), ex,null);
                throw ex;
            }
        }

        public async Task<Notifications> GetNotificationsById(int interviewId)
        {
            try
            {
                return await Db.Notifications.FindAsync(interviewId);
            }
            catch (Exception ex)
            {
                LogException(nameof(GetNotificationsById), ex, $"Error retrieving notification with ID: {interviewId}");
                throw ex;
            }
        }

        public async Task Create(Notifications entity)
        {
            try
            {
                entity.IsActive = true;
                entity.ModifiedBy = entity.ModifiedBy;
                entity.ModifiedOn = DateTime.Now;

                Db.Notifications.Add(entity);
                await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, $"Notification inserted with ID: {entity.NotificationsId}");
                throw ex;
            }
        }

        public async Task Update(Notifications entity)
        {
            try
            {
                entity.IsActive = true;
                entity.ModifiedBy = entity.ModifiedBy;
                entity.ModifiedOn = DateTime.Now;

                Db.Notifications.Update(entity);
                await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(Update), ex, $"Error updating notification with ID: {entity.NotificationsId}");
                throw ex;
            }
        }

        public async Task Delete(Notifications entity )
        {
            try
            {
                entity.IsDelete = true;
                entity.ModifiedBy = entity.ModifiedBy;
                entity.ModifiedOn = DateTime.Now;

                Db.Notifications.Remove(entity);
                await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(Delete), ex, $"Notification deleted with ID: {entity.NotificationsId}");
                throw ex;
            }
        }

        public async Task<List<Notifications>> GetSpacificNotificationsforHR()
        {
            try
            {
                var HrId = "";

                var Hr = await _roleManager.FindByNameAsync("HR Manager");

                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                return await Db.Notifications.Where(x => x.ReceiverId == HrId).ToListAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(GetSpacificNotificationsforHR), ex,$"Error while getting a Spacific Notifications for HR ");
                throw ex;
            }
        }

        public async Task<List<Notifications>> GetSpacificNotificationsforGeneral()
        {
            try
            {
                var managerId = "";

                var manager = await _roleManager.FindByNameAsync("General Manager");

                managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;

                return await Db.Notifications.Where(x => x.ReceiverId == managerId).ToListAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(GetSpacificNotificationsforGeneral), ex, $"Error while getting a Spacific Notifications for General Manager ");
                throw ex;
            }
        }

        public async Task<List<Notifications>> GetSpacificNotificationsforArchi()
        {
            try
            {
                var archiId = "";

                var archi = await _roleManager.FindByNameAsync("Solution Architecture");

                archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

                return await Db.Notifications.Where(x => x.ReceiverId == archiId).ToListAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(GetSpacificNotificationsforArchi), ex, $"Error while getting a Spacific Notifications for Architecture ");
                throw ex;
            }
        }

        public async Task<List<Notifications>> GetSpacificNotificationsforInterviewer(string interviewerId)
        {
            try
            {
                return await Db.Notifications.Where(x => x.ReceiverId == interviewerId).ToListAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(GetSpacificNotificationsforInterviewer), ex, $"Error while getting a Spacific Notifications for Interviewer ");
                throw ex;
            }
        }


    

    }
}
