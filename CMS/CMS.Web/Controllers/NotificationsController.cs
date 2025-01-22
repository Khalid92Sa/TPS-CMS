using CMS.Application.DTOs;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

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
        IHttpContextAccessor httpContextAccessor)
    {
        _notificationsService = notificationsService;
        _templatesService = templatesService;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<ActionResult> Index()
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
                IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetAllNotificationsAsync();
                return View(notifications);
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<ActionResult> AllNotifications()
    {
        try
        {
            IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetAllNotificationsAnotherTab();
            return Json(notifications);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("/notifications/gm")]
    public async Task<ActionResult> IndexGMnotification()
    {
        try
        {
            if (User.IsInRole("General Manager"))
            {
                IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetNotificationsForGeneralManager();
                return View(notifications);
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("/notifications/archi")]
    public async Task<ActionResult> IndexArchinotification()
    {
        try
        {
            if (User.IsInRole("Solution Architecture"))
            {
                IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetNotificationsForArchitecture();
                return View(notifications);
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("/notifications/interviewer")]
    public async Task<ActionResult> IndexInterviewernotification()
    {
        try
        {
            if (User.IsInRole("Interviewer"))
            {
                string userId = _userManager.GetUserId(User);
                IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetNotificationsForInterviewers(userId);
                return View(notifications);
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<ActionResult> IndexInterviewerForAllNotification()
    {
        try
        {
            if (User.IsInRole("Interviewer"))
            {
                string userId = _userManager.GetUserId(User);
                IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetAllNotificationsAsyncForInterviewer(userId);
                return Json(notifications);
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("/notifications/hr")]
    public async Task<ActionResult> IndexHRnotification()
    {
        try
        {
            if (User.IsInRole("HR Manager"))
            {
                IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetNotificationsForHRAsync();
                return View(notifications);
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<ActionResult> Details(int id)
    {
        try
        {
            string userId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager") || User.IsInRole("Solution Architecture") || User.IsInRole("Interviewer"))
            {
                NotificationsDTO notification = await _notificationsService.GetNotificationByIdforDetails(id);

                if (notification != null && (notification.ReceiverId == userId || User.IsInRole("Admin") || User.IsInRole("HR Manager")))
                {
                    notification.IsRead = true;
                    await _notificationsService.Update(id, notification);
                    return View(notification);
                }
                return View(notification);
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
                }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<ActionResult> Create()
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
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<ActionResult> Edit(int id)
    {
        try
        {
            NotificationsDTO notifications = await _notificationsService.GetNotificationByIdAsync(id);
            return View(notifications);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int id, NotificationsDTO collection)
    {
        try
        {
            if (id != collection.NotificationsId)
                return NotFound();

            TemplatesDTO temp = await _templatesService.GetTemplateByIdAsync(id);

            if (ModelState.IsValid)
            {
                await _notificationsService.Update(id, collection);
                return RedirectToAction(nameof(Index));
            }

            return View(collection);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            NotificationsDTO notifications = await _notificationsService.GetNotificationByIdAsync(id);
            return View(notifications);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int id, NotificationsDTO collection)
    {
        try
        {
            await _notificationsService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IActionResult> GetNotificationsForHR()
    {
        try
        {
            IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetNotificationsForHRAsyncicon();
            return Json(notifications);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IActionResult> GetNotificationsForInterviewers()
    {
        try
        {
            string userId = _userManager.GetUserId(User);
            IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetNotificationsForInterviewersicon(userId);
            return Json(notifications);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IActionResult> GetNotificationsForGeneralManager()
    {
        try
        {
            IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetNotificationsForGeneralManagericon();
            return Json(notifications);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IActionResult> GetNotificationsForArchitecture()
    {
        try
        {
            IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetNotificationsForArchitectureicon();
            return Json(notifications);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult> MarkAllAsRead()
    {
        try
        {
            if (User.IsInRole("HR Manager"))
            {
                await _notificationsService.MarkAllAsReadForRoleAsync("HR Manager");
                return Json(new { success = true });
            }
            else if (User.IsInRole("General Manager"))
            {
                await _notificationsService.MarkAllAsReadForRoleAsync("General Manager");
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Access denied" });
            }
        }
        catch (Exception)
        {
            return Json(new { success = false, message = "Internal server error" });
        }
    }


    public async Task<List<NotificationsDTO>> GetUnreadNotificationsAsync()
    {
        try
        {
            IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetAllNotificationsAnotherTab();
            List<NotificationsDTO> unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
            return unreadNotifications;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<NotificationsDTO>> GetUnreadNotificationsForGmAndArchiAsync()
    {
        try
        {
            IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetUnreadNotificationsForGMAsync();
            List<NotificationsDTO> unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
            return unreadNotifications;
        }
        catch (Exception)
        {
            throw;
        }
    }


    public async Task<List<NotificationsDTO>> GetReadNotificationsAsync()
    {
        try
        {
            IEnumerable<NotificationsDTO> notifications = await _notificationsService.GetAllNotificationsAnotherTab();
            List<NotificationsDTO> readNotifications = notifications.Where(n => n.IsRead).ToList();
            return readNotifications;
        }
        catch (Exception)
        {
            throw;
        }
    }

}