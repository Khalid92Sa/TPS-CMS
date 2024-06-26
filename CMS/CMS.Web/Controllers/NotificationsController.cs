﻿using CMS.Application.DTOs;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Services.Interfaces;
using CMS.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CMS.Web.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly INotificationsService _notificationsService;
        private readonly ITemplatesService _templatesService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationsController
            (
            INotificationsService notificationsService,
            ITemplatesService templatesService,
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _notificationsService = notificationsService;
            _templatesService = templatesService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public void LogException(string methodName, Exception ex, string additionalInfo = null)
        {
            
            _notificationsService.LogException(methodName, ex, additionalInfo);
        }
     

        // GET: NotificationsController
        public async Task<ActionResult> Index()
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
                {
                    var notifications = await _notificationsService.GetAllNotificationsAsync();
                    return View(notifications);
                }
                else
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        return View("AccessDenied");
                    }
                    else
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(Index), ex, "Index page for Notifications not working");
                throw ex;
            }
        }

        public async Task<ActionResult> IndexGMnotification()
        {
            try
            {
                if (User.IsInRole("General Manager") )
                {
                    var notifications = await _notificationsService.GetNotificationsForGeneralManager();
                return View(notifications);
                }
                else
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        return View("AccessDenied");
                    }
                    else
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(IndexGMnotification), ex, "Faild to load GM notifiacations");
                throw ex;
            }
        }

        public async Task<ActionResult> IndexArchinotification()
        {
            try
            {
                if (User.IsInRole("Solution Architecture") )
                {
                    var notifications = await _notificationsService.GetNotificationsForArchitecture();
                return View(notifications);
                }
                else
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        return View("AccessDenied");
                    }
                    else
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(IndexArchinotification), ex, "Faild to load Archi notifiacations");
                throw ex;
            }
        }

        public async Task<ActionResult> IndexInterviewernotification()
        {
            try
            {
                if (User.IsInRole("Interviewer"))
                {
                    var userId = _userManager.GetUserId(User);
                var notifications = await _notificationsService.GetNotificationsForInterviewers(userId);
                return View(notifications);
                }
                else
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        return View("AccessDenied");
                    }
                    else
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(IndexInterviewernotification), ex, "Faild to load Interviewer notifiacations");
                throw ex;
            }
        }

        public async Task<ActionResult> IndexHRnotification()
        {
            try
            {
                if (User.IsInRole("HR Manager") )
                {
                    var notifications = await _notificationsService.GetNotificationsForHRAsync();
                return View(notifications);
                }
                else
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        return View("AccessDenied");
                    }
                    else
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(IndexHRnotification), ex, "Faild to load HR notifiacations");
                throw ex;
            }
        }

        // GET: NotificationsController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // Check if the user has the appropriate role to view this notification
                if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager") || User.IsInRole("Solution Architecture") || User.IsInRole("Interviewer"))
                {
                    var notification = await _notificationsService.GetNotificationByIdforDetails(id);

                    // Check if the user is the intended recipient of the notification
                    if (notification != null && (notification.ReceiverId == userId || User.IsInRole("Admin") || User.IsInRole("HR Manager")))
                    {
                        notification.IsRead = true;
                        await _notificationsService.Update(id, notification);
                        return View(notification);
                    }
                    else
                    {
                        // User is not the intended recipient, return Access Denied
                        if (User.Identity.IsAuthenticated)
                        {
                            return View("AccessDenied");
                        }
                        else
                        {
                            return RedirectToAction("Login", "Account");
                        }
                    }
                }
                else
                {
                    // User doesn't have the required role, return Access Denied
                    if (User.Identity.IsAuthenticated)
                    {
                        return View("AccessDenied");
                    }
                    else
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(Details), ex, $"Failed to load ID: {id} details");
                throw ex;
            }
        }

        // GET: NotificationsController/Create
        public async Task<ActionResult> Create()
        {
            try
            {

            
            return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, "Can not load create Notification page");
                throw ex;
            }
        }

        // POST: NotificationsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(NotificationsDTO collection)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _notificationsService.Create(collection);
                    return RedirectToAction(nameof(Index));
                }
                return View(collection);
            }
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, "Faild to create a notification");
                throw ex;
            }
        }

        // GET: NotificationsController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                var notifications = await _notificationsService.GetNotificationByIdAsync(id);
                return View(notifications);
            }
            catch (Exception ex)
            {
                LogException(nameof(Edit), ex, "Edit not working");
                throw ex;
            }
        }

        // POST: NotificationsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, NotificationsDTO collection)
        {
            try
            {
                if (id != collection.NotificationsId)
                {
                    return NotFound();
                }

                var temp = await _templatesService.GetTemplateByIdAsync(id);

                if (ModelState.IsValid)
                {
                    await _notificationsService.Update(id, collection);
                    return RedirectToAction(nameof(Index));
                }

                return View(collection);
            }
            catch (Exception ex)
            {
                LogException(nameof(Edit), ex, "Faild to edit a notification");
                throw ex;
            }
        }



        // GET: NotificationsController/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var notifications = await _notificationsService.GetNotificationByIdAsync(id);
                return View(notifications);
            }
            catch (Exception ex)
            {
                LogException(nameof(Delete), ex, "Delete page for Notification not working");
                throw ex;
            }
        }


        // POST: NotificationsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, NotificationsDTO collection)
        {
            try
            {
                await _notificationsService.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogException(nameof(Delete), ex, "Faild to delete a notification");
                throw ex;
            }
        }




        public async Task<IActionResult> GetNotificationsForHR()
        {
            try
            {
                var notifications = await _notificationsService.GetNotificationsForHRAsyncicon();
                return Json(notifications);
            }
            catch (Exception ex)
            {
                LogException(nameof(GetNotificationsForHR), ex, "Faild to get the notification for the HR");
                throw ex;
            }
        }

        public async Task<IActionResult> GetNotificationsForInterviewers()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var notifications = await _notificationsService.GetNotificationsForInterviewersicon(userId);
                return Json(notifications);
            }
            catch (Exception ex)
            {
                LogException(nameof(GetNotificationsForInterviewers), ex, "Faild to get the notification for the Interviewers");
                throw ex;
            }
        }


        public async Task<IActionResult> GetNotificationsForGeneralManager()
        {
            try
            {
                var notifications = await _notificationsService.GetNotificationsForGeneralManagericon();
                return Json(notifications);
            }
            catch (Exception ex)
            {
                LogException(nameof(GetNotificationsForGeneralManager), ex, "Faild to get the notification for the GM");
                throw ex;
            }
        }

        public async Task<IActionResult> GetNotificationsForArchitecture()
        {
            try
            {
                var notifications = await _notificationsService.GetNotificationsForArchitectureicon();
                return Json(notifications);
            }
            catch (Exception ex)
            {
                LogException(nameof(GetNotificationsForArchitecture), ex, "Faild to get the notification for the Architecture");
                throw ex;
            }
        }

        [HttpPost]
        public async Task<ActionResult> MarkAllAsRead()
        {
            try
            {
                if (User.IsInRole("HR Manager"))
                {
                    await _notificationsService.MarkAllAsReadForHRAsync();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Access denied" });
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(MarkAllAsRead), ex, "Failed to mark all notifications as read for HR");
                return Json(new { success = false, message = "Internal server error" });
            }
        }

    }
}

