using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class NotificationsService : INotificationsService
{
    private readonly INotificationsRepository _notificationsRepository;
    private readonly ITemplatesService _templatesService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ICarrerOfferRepository _carrerOfferRepository;
    private readonly ICandidateService _candidateService;
    private readonly IPositionService _positionService;
    private readonly IStatusService _statusService;

    public NotificationsService
        (
        INotificationsRepository notificationsRepository,
        ITemplatesService templatesService,
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        ICarrerOfferRepository carrerOfferRepository,
        ICandidateService candidateService,
        IPositionService positionService,
        IStatusService statusService)
    {
        _notificationsRepository = notificationsRepository;
        _templatesService = templatesService;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _roleManager = roleManager;
        _userManager = userManager;
        _carrerOfferRepository = carrerOfferRepository;
        _candidateService = candidateService;
        _positionService = positionService;
        _statusService = statusService;
    }


    public async Task<IEnumerable<NotificationsDTO>> GetAllNotificationsAsync()
    {
        try
        {
            IEnumerable<Notifications> notifications = await _notificationsRepository.GetAllNotifications();

            return notifications.Select(i => new NotificationsDTO
            {
                NotificationsId = i.NotificationsId,
                SendDate = i.SendDate,
                ReceiverId = i.ReceiverId,
                IsReceived = i.IsReceived,
                Title = i.Title,
                BodyDesc = i.BodyDesc,
                IsRead = i.IsRead,
            });
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetAllNotificationsAnotherTab()
    {
        try
        {
            List<Notifications> notificationsHR = await _notificationsRepository.GetSpacificNotificationsforHR();
            List<Notifications> notificationsGM = await _notificationsRepository.GetSpacificNotificationsforGeneral();
            List<Notifications> notificationsArchi = await _notificationsRepository.GetSpacificNotificationsforArchi();

            // Get role information
            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");
            string HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault()?.Id;

            IdentityRole GM = await _roleManager.FindByNameAsync("General Manager");
            string GMId = (await _userManager.GetUsersInRoleAsync(GM.Name)).FirstOrDefault()?.Id;

            IdentityRole Archi = await _roleManager.FindByNameAsync("Solution Architecture");
            string ArchiId = (await _userManager.GetUsersInRoleAsync(Archi.Name)).FirstOrDefault()?.Id;

            string userRole = await GetLoggedInUserRoleAsync();

            // Determine the appropriate notifications to return based on the user's role
            if (userRole == "HR Manager" && HrId != null)
            {
                List<NotificationsDTO> notificationsDTOList = notificationsHR.Where(notification => notification.ReceiverId == HrId
                                                                                   // && notification.IsRead
                                                                                   )
                                                                             .Select(notification => new NotificationsDTO
                                                                             {
                                                                                 NotificationsId = notification.NotificationsId,
                                                                                 SendDate = notification.SendDate,
                                                                                 Title = notification.Title,
                                                                                 BodyDesc = notification.BodyDesc,
                                                                                 IsReceived = true,
                                                                                 IsRead = notification.IsRead,
                                                                             })
                                                                             .ToList();

                return notificationsDTOList;
            }
            else if (userRole == "General Manager" && GMId != null)
            {
                List<NotificationsDTO> notificationsDTOList = notificationsGM.Where(notification => notification.ReceiverId == GMId
                                                                                                 && notification.IsRead
                                                                                   )
                                                                             .Select(notification => new NotificationsDTO
                                                                             {
                                                                                 NotificationsId = notification.NotificationsId,
                                                                                 SendDate = notification.SendDate,
                                                                                 Title = notification.Title,
                                                                                 BodyDesc = notification.BodyDesc,
                                                                                 IsReceived = true,
                                                                                 IsRead = notification.IsRead,
                                                                             })
                                                                             .ToList();

                return notificationsDTOList;
            }

            else if (userRole == "Solution Architecture" && ArchiId != null)
            {
                List<NotificationsDTO> notificationsDTOList = notificationsArchi.Where(notification => notification.ReceiverId == ArchiId
                                                                                                    && notification.IsRead
                                                                                      )
                                                                                .Select(notification => new NotificationsDTO
                                                                                {
                                                                                    NotificationsId = notification.NotificationsId,
                                                                                    SendDate = notification.SendDate,
                                                                                    Title = notification.Title,
                                                                                    BodyDesc = notification.BodyDesc,
                                                                                    IsReceived = true,
                                                                                    IsRead = notification.IsRead,
                                                                                })
                                                                                .ToList();

                return notificationsDTOList;
            }

            // Default return value if no conditions are met
            return new List<NotificationsDTO>();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetAllNotificationsAsyncForInterviewer(string interviewerId)
    {
        try
        {
            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforInterviewer(interviewerId);
            List<NotificationsDTO> notificationsDTOList = notifications.Where(x => x.IsRead)
                                                                       .Select(notification => new NotificationsDTO
                                                                       {
                                                                           NotificationsId = notification.NotificationsId,
                                                                           SendDate = notification.SendDate,
                                                                           Title = notification.Title,
                                                                           BodyDesc = notification.BodyDesc,
                                                                           IsReceived = true,
                                                                           IsRead = notification.IsRead,
                                                                       })
                                                                       .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<NotificationsDTO> GetNotificationByIdAsync(int notificationsId)
    {
        try
        {
            Notifications notification = await _notificationsRepository.GetNotificationsById(notificationsId);

            if (notification is null)
                return null;

            TemplatesDTO templatesDTO = new TemplatesDTO
            {
                Title = notification.Title,
                BodyDesc = notification.BodyDesc,
            };

            return new NotificationsDTO
            {
                NotificationsId = notification.NotificationsId,
                SendDate = notification.SendDate,
                ReceiverId = notification.ReceiverId,
                IsReceived = notification.IsReceived,
                IsRead = notification.IsRead,
                Title = notification.Title,
                BodyDesc = notification.BodyDesc,
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<NotificationsDTO> GetNotificationByIdforDetails(int notificationsId)
    {
        try
        {
            Notifications notification = await _notificationsRepository.GetNotificationsById(notificationsId);

            if (notification is null)
                return null;

            return new NotificationsDTO
            {
                NotificationsId = notification.NotificationsId,
                SendDate = notification.SendDate,
                ReceiverId = notification.ReceiverId,
                IsReceived = notification.IsReceived,
                IsRead = true,
                Title = notification.Title,
                BodyDesc = notification.BodyDesc,
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task Create(NotificationsDTO entity)
    {
        try
        {
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            Notifications notification = new Notifications
            {
                SendDate = DateTime.Now,
                ReceiverId = entity.ReceiverId,
                IsReceived = entity.IsReceived,
                Title = entity.Title,
                BodyDesc = entity.BodyDesc,
                CreatedOn = DateTime.Now,
                CreatedBy = currentUser.Id,
                IsRead = entity.IsRead,
                CandidateId = entity.CandidateId,
            };

            await _notificationsRepository.Create(notification);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task Update(int notificationId, NotificationsDTO entity)
    {
        try
        {
            Notifications existingNotification = await _notificationsRepository.GetNotificationsById(notificationId);

            if (existingNotification == null)
                throw new Exception("Notifications not found");

            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            existingNotification.SendDate = entity.SendDate;
            existingNotification.ReceiverId = entity.ReceiverId;
            existingNotification.IsReceived = entity.IsReceived;
            existingNotification.Title = entity.Title;
            existingNotification.BodyDesc = entity.BodyDesc;
            existingNotification.IsRead = entity.IsRead;
            existingNotification.ModifiedOn = DateTime.Now;
            existingNotification.ModifiedBy = currentUser.Id;

            await _notificationsRepository.Update(existingNotification);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task Delete(int notificationId)
    {
        try
        {
            Notifications notification = await _notificationsRepository.GetNotificationsById(notificationId);

            if (notification != null)
                await _notificationsRepository.Delete(notification);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<NotificationsDTO>> GetNotificationsForUserAsync(string userId)
    {
        try
        {
            string HrId = "";
            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");

            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforHR();

            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.ReceiverId == HrId)
                                                                       .Select(notification => new NotificationsDTO
                                                                       {
                                                                           IsReceived = true,
                                                                           SendDate = DateTime.Now,
                                                                           Title = notification.Title,

                                                                       })
                                                                       .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForHRAsync()
    {
        try
        {
            string HrId = "";
            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");

            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforHR();
            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.ReceiverId == HrId && notification.IsRead == false)
                                                    .Select(notification => new NotificationsDTO
                                                    {
                                                        NotificationsId = notification.NotificationsId,
                                                        SendDate = notification.SendDate,
                                                        Title = notification.Title,
                                                        BodyDesc = notification.BodyDesc,
                                                        IsReceived = true,
                                                        IsRead = notification.IsRead,
                                                    })
                                                    .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForHRAsyncicon()
    {
        try
        {
            string HrId = "";

            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");
            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforHR();

            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.ReceiverId == HrId
                                                                                           && notification.IsRead == false
                                                                             )
                                                                       .Select(notification => new NotificationsDTO
                                                                       {
                                                                           NotificationsId = notification.NotificationsId,
                                                                           SendDate = notification.SendDate,
                                                                           Title = notification.Title,
                                                                           BodyDesc = notification.BodyDesc,
                                                                           IsReceived = true
                                                                       })
                                                                       .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForInterviewers(string interviewerId)
    {
        try
        {
            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforInterviewer(interviewerId);
            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.IsRead == false)
                                                                       .Select(notification => new NotificationsDTO
                                                                       {
                                                                           NotificationsId = notification.NotificationsId,
                                                                           SendDate = notification.SendDate,
                                                                           Title = notification.Title,
                                                                           BodyDesc = notification.BodyDesc,
                                                                           IsReceived = true,
                                                                           IsRead = notification.IsRead,
                                                                       })
                                                                       .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForInterviewersicon(string interviewerId)
    {
        try
        {
            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforInterviewer(interviewerId);
            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.IsRead == false)
                                                                       .Select(notification => new NotificationsDTO
                                                                       {
                                                                           NotificationsId = notification.NotificationsId,
                                                                           SendDate = notification.SendDate,
                                                                           Title = notification.Title,
                                                                           BodyDesc = notification.BodyDesc,
                                                                           IsReceived = true
                                                                       })
                                                                       .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForGeneralManager()
    {
        try
        {
            string managerId = "";
            IdentityRole manager = await _roleManager.FindByNameAsync("General Manager");
            managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;

            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforGeneral();
            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.ReceiverId == managerId
                                                                                           && notification.IsRead == false
                                                                             )
                                                                       .Select(notification => new NotificationsDTO
                                                                       {
                                                                           NotificationsId = notification.NotificationsId,
                                                                           SendDate = notification.SendDate,
                                                                           Title = notification.Title,
                                                                           BodyDesc = notification.BodyDesc,
                                                                           IsReceived = true,
                                                                           IsRead = notification.IsRead,
                                                                       })
                                                                       .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForGeneralManagericon()
    {
        try
        {
            string managerId = "";
            IdentityRole manager = await _roleManager.FindByNameAsync("General Manager");

            managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;

            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforGeneral();

            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.ReceiverId == managerId && notification.IsRead == false)
                                                                       .Select(notification => new NotificationsDTO
                                                                       {
                                                                           NotificationsId = notification.NotificationsId,
                                                                           SendDate = notification.SendDate,
                                                                           Title = notification.Title,
                                                                           BodyDesc = notification.BodyDesc,
                                                                           IsReceived = true
                                                                       })
                                                                       .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForArchitecture()
    {
        try
        {
            string archiId = "";
            IdentityRole archi = await _roleManager.FindByNameAsync("Solution Architecture");

            archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforArchi();

            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.ReceiverId == archiId
                                                                                           && notification.IsRead == false
                                                                             )
                .Select(notification => new NotificationsDTO
                {
                    NotificationsId = notification.NotificationsId,
                    SendDate = notification.SendDate,
                    Title = notification.Title,
                    BodyDesc = notification.BodyDesc,
                    IsReceived = true,
                    IsRead = notification.IsRead,
                })
                .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForArchitectureicon()
    {
        try
        {
            string archiId = "";
            IdentityRole archi = await _roleManager.FindByNameAsync("Solution Architecture");

            archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

            List<Notifications> notifications = await _notificationsRepository.GetSpacificNotificationsforArchi();

            List<NotificationsDTO> notificationsDTOList = notifications.Where(notification => notification.ReceiverId == archiId
                                                                                           && notification.IsRead == false
                                                                             )
                .Select(notification => new NotificationsDTO
                {
                    NotificationsId = notification.NotificationsId,
                    SendDate = notification.SendDate,
                    Title = notification.Title,
                    BodyDesc = notification.BodyDesc,
                    IsReceived = true
                })
                .ToList();

            return notificationsDTOList;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateNotificationForGeneralManagerAsync(int status, string notes, int CandidateId, int positionId, string archiInterviewerId)
    {
        try
        {
            string managerId = "";
            string HrId = "";

            Result<StatusDTO> statusResult = await _statusService.GetById(status);
            StatusDTO statusstatus = statusResult.Value;

            IdentityRole manager = await _roleManager.FindByNameAsync("General Manager");
            managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;

            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");
            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            string userName = GetLoggedInUserName();
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            string candidateName = await GetCandidateName(CandidateId);
            string positionName = await GetPositionName(positionId);

            // Create the notification for the manager.
            Notifications notification = new Notifications
            {
                SendDate = DateTime.Now,
                CandidateId = CandidateId,
                IsReceived = true,
                IsRead = false,
                Title = "",
                BodyDesc = notes,
                CreatedBy = currentUser.Id,
                CreatedOn = DateTime.Now
            };

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
            {
                notification.Title = $"You have a Second Interview with {candidateName} for the {positionName} position. Get ready to shine! 💼🚀";
                notification.ReceiverId = managerId;
            }
            else
            {
                notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";
                notification.ReceiverId = HrId;
            }

            await _notificationsRepository.Create(notification);

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
            {
                if (archiInterviewerId == "" || archiInterviewerId is null)
                {
                    Notifications hrNotification = new Notifications
                    {
                        ReceiverId = HrId,
                        SendDate = DateTime.Now,
                        IsReceived = true,
                        IsRead = false,
                        Title = $"{candidateName} Approved by {userName} for position {positionName}",
                        BodyDesc = $"The candidate has been approved by the {userName} for the {positionName} position.",
                        CreatedBy = currentUser.Id,
                        CreatedOn = DateTime.Now,
                        CandidateId = CandidateId,
                    };

                    await _notificationsRepository.Create(hrNotification);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateNotificationForArchiAsync(int status, string notes, int CandidateId, int positionId)
    {
        try
        {
            string archiId = "";
            string HrId = "";

            Result<StatusDTO> statusResult = await _statusService.GetById(status);
            StatusDTO statusstatus = statusResult.Value;

            IdentityRole archi = await _roleManager.FindByNameAsync("Solution Architecture");
            archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");
            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            string userName = GetLoggedInUserName();
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            string candidateName = await GetCandidateName(CandidateId);
            string positionName = await GetPositionName(positionId);

            // Create the notification for the manager.
            Notifications notification = new Notifications
            {
                SendDate = DateTime.Now,
                CandidateId = CandidateId,
                IsReceived = true,
                IsRead = false,
                Title = "",
                BodyDesc = notes,
                CreatedBy = currentUser.Id,
                CreatedOn = DateTime.Now
            };

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
            {
                notification.Title = $"You and the GM have a Second Interview with {candidateName} for the {positionName} position. Get ready to shine! 💼🚀";
                notification.ReceiverId = archiId;
            }
            else
            {
                notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";
                notification.ReceiverId = HrId;
            }

            await _notificationsRepository.Create(notification);

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
            {
                Notifications hrNotification = new Notifications
                {
                    ReceiverId = HrId,
                    SendDate = DateTime.Now,
                    IsReceived = true,
                    IsRead = false,
                    Title = $"{candidateName} Approved by {userName} for position {positionName}",
                    BodyDesc = $"The candidate has been approved by the {userName} for the {positionName} position.",
                    CreatedBy = currentUser.Id,
                    CreatedOn = DateTime.Now,
                    CandidateId = CandidateId,
                };

                await _notificationsRepository.Create(hrNotification);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateInterviewNotificationForInterviewerAsync(DateTime interviewDate, int candidateId, int positionId, List<string> selectedInterviewerIds, bool isCanceled)
    {
        try
        {
            string formattedDate = interviewDate.ToString("dd/MM/yyyy hh:mm tt");
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            string candidateName = await GetCandidateName(candidateId);
            string positionName = await GetPositionName(positionId);

            foreach (string selectedInterviewerId in selectedInterviewerIds)
            {
                Notifications notification = new Notifications
                {
                    ReceiverId = selectedInterviewerId,
                    SendDate = DateTime.Now,
                    IsReceived = true,
                    IsRead = false,
                    CreatedBy = currentUser.Id,
                    CreatedOn = DateTime.Now,
                    CandidateId = candidateId,
                };

                if (isCanceled)
                {
                    notification.Title = $"Interview Cancellation for {candidateName}";
                    notification.BodyDesc = $"The interview with {candidateName} for the {positionName} position, scheduled on {formattedDate}, has been canceled by HR.";
                }
                else
                {
                    string secondInterviewerName = await GetInterviewerName(selectedInterviewerIds[1]);

                    if (selectedInterviewerIds.Count == 2 && secondInterviewerName != "Unknown Interviewer")
                    {
                        if (selectedInterviewerId == selectedInterviewerIds[0])
                        {
                            notification.Title = $"New interview invitation for {candidateName}";
                            notification.BodyDesc = $"You and {secondInterviewerName} have been selected for a First Interview with {candidateName} for the {positionName} position on {formattedDate}. Get ready to shine! 💼🚀";
                        }
                        else
                        {
                            string firstInterviewerName = await GetInterviewerName(selectedInterviewerIds[0]);
                            notification.Title = $"New interview invitation for {candidateName}";
                            notification.BodyDesc = $"You and {firstInterviewerName} have been selected for a First Interview with {candidateName} for the {positionName} position on {formattedDate}. Get ready to shine! 💼🚀";
                        }
                    }
                    else
                    {
                        notification.Title = $"New interview invitation for {candidateName}";
                        notification.BodyDesc = $"You've been selected for a First Interview with {candidateName} for the {positionName} position on {formattedDate}. Get ready to shine! 💼🚀";
                    }
                }

                if (selectedInterviewerId is null)
                {

                }
                else
                    await _notificationsRepository.Create(notification);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetInterviewerName(string interviewerId)
    {
        IdentityUser interviewer = await _userManager.FindByIdAsync(interviewerId);

        if (interviewer is not null)
            return interviewer.UserName;

        return "Unknown Interviewer";
    }

    public async Task CreateInterviewNotificationForHRInterview(int status, string notes, int CandidateId, int positionId)
    {
        try
        {
            Result<StatusDTO> statusResult = await _statusService.GetById(status);
            StatusDTO statusstatus = statusResult.Value;
            string HrId = "";

            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");

            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            string userName = GetLoggedInUserName();
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            string candidateName = await GetCandidateName(CandidateId);
            string positionName = await GetPositionName(positionId);

            Notifications notification = new Notifications
            {
                ReceiverId = HrId,
                SendDate = DateTime.Now,
                IsReceived = true,
                IsRead = false,
                Title = "",
                BodyDesc = notes,
                CreatedOn = DateTime.Now,
                CreatedBy = currentUser.Id,
                CandidateId = CandidateId,
            };

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
                notification.Title = $"You have a Third Interview with {candidateName} for the {positionName} position. Get ready to shine! 💼🚀 ";

            else
                notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
            {
                Notifications hrNotification = new Notifications
                {
                    ReceiverId = HrId,
                    SendDate = DateTime.Now,
                    IsReceived = true,
                    IsRead = false,
                    Title = $"{candidateName} Approved by {userName} for position {positionName}",
                    BodyDesc = $"{candidateName} has been approved by the {userName} for the {positionName} position.",
                    CreatedBy = currentUser.Id,
                    CreatedOn = DateTime.Now,
                    CandidateId = CandidateId,
                };

                await _notificationsRepository.Create(hrNotification);
            }
            await _notificationsRepository.Create(notification);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateInterviewNotificationForFinalHRInterview(int status, string notes, int CandidateId, int positionId)
    {
        try
        {
            Result<StatusDTO> statusResult = await _statusService.GetById(status);
            StatusDTO statusstatus = statusResult.Value;

            string HrId = "";

            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");

            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            string userName = GetLoggedInUserName();
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            string candidateName = await GetCandidateName(CandidateId);
            string positionName = await GetPositionName(positionId);

            Notifications notification = new Notifications
            {
                ReceiverId = HrId,
                SendDate = DateTime.Now,
                IsReceived = true,
                IsRead = false,
                Title = "",
                BodyDesc = notes,
                CreatedOn = DateTime.Now,
                CreatedBy = currentUser.Id,
                CandidateId = CandidateId,
            };

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
                notification.Title = $"You have a Final Interview with {candidateName} for the {positionName} position. Get ready to shine! 💼🚀 ";

            else
                notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
            {
                Notifications hrNotification = new Notifications
                {
                    ReceiverId = HrId,
                    SendDate = DateTime.Now,
                    IsReceived = true,
                    IsRead = false,
                    Title = $"{candidateName} Approved by {userName} for position {positionName}",
                    BodyDesc = $"{candidateName} has been approved by the {userName} for the {positionName} position.",
                    CreatedBy = currentUser.Id,
                    CreatedOn = DateTime.Now,
                    CandidateId = CandidateId,
                };

                await _notificationsRepository.Create(hrNotification);
            }
            await _notificationsRepository.Create(notification);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateInterviewNotificationtoHrForOnHold(int status, string notes, int CandidateId, int positionId)
    {
        try
        {
            Result<StatusDTO> statusResult = await _statusService.GetById(status);
            StatusDTO statusstatus = statusResult.Value;

            string HrId = "";

            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");

            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            string userName = GetLoggedInUserName();
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            string candidateName = await GetCandidateName(CandidateId);
            string positionName = await GetPositionName(positionId);

            Notifications notification = new Notifications
            {
                ReceiverId = HrId,
                SendDate = DateTime.Now,
                IsReceived = true,
                IsRead = false,
                Title = "",
                BodyDesc = notes,
                CreatedOn = DateTime.Now,
                CreatedBy = currentUser.Id,
                CandidateId = CandidateId,
            };

            if (statusstatus.Code == Domain.Enums.StatusCode.Rejected)
                notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";

            if (statusstatus.Code == Domain.Enums.StatusCode.OnHold)
            {
                Notifications hrNotification = new Notifications
                {
                    ReceiverId = HrId,
                    SendDate = DateTime.Now,
                    IsReceived = true,
                    IsRead = false,
                    Title = $"{candidateName} On Hold by {userName}",
                    BodyDesc = $"{candidateName} has been put on hold by {userName} for the {positionName} position.",
                    CreatedBy = currentUser.Id,
                    CreatedOn = DateTime.Now,
                    CandidateId = CandidateId,
                };
                await _notificationsRepository.Create(hrNotification);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateInterviewNotificationForHRInterviewfromGM(int status, string notes, int CandidateId, int positionId)
    {
        try
        {
            Result<StatusDTO> statusResult = await _statusService.GetById(status);
            StatusDTO statusstatus = statusResult.Value;

            string HrId = "";

            IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");

            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

            string userName = GetLoggedInUserName();
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            string candidateName = await GetCandidateName(CandidateId);
            string positionName = await GetPositionName(positionId);

            Notifications notification = new Notifications
            {
                ReceiverId = HrId,
                SendDate = DateTime.Now,
                IsReceived = true,
                IsRead = false,
                Title = "",
                BodyDesc = notes,
                CreatedOn = DateTime.Now,
                CreatedBy = currentUser.Id,
                CandidateId = CandidateId,
            };

            if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
                notification.Title = $"You have a second Interview with {candidateName} for the {positionName} position. Get ready to shine! 💼🚀 ";

            else
                notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";

            await _notificationsRepository.Create(notification);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateNotificationForInterviewer(int CandidateId, string selectedInterviewerId)
    {
        try
        {
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            string candidateName = await GetCandidateName(CandidateId);

            Notifications notification = new Notifications
            {
                ReceiverId = selectedInterviewerId,
                SendDate = DateTime.Now,
                IsReceived = true,
                IsRead = false,
                Title = $"Your interview with {candidateName} cancelled",
                BodyDesc = $"The HR has rejected this candidate for some reasons, so you don't have an interview for {candidateName}.",
                CreatedBy = currentUser.Id,
                CreatedOn = DateTime.Now,
                CandidateId = CandidateId,
            };

            await _notificationsRepository.Create(notification);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public string GetLoggedInUserName()
    {
        try
        {
            return _httpContextAccessor.HttpContext.User.Identity.Name;
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<string> GetLoggedInUserRoleAsync()
    {
        try
        {
            IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            IList<string> roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetCandidateName(int candidateId)
    {
        try
        {
            CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(candidateId);

            if (candidate != null)
                return candidate.FullName;

            return "Candidate Not Found";
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetPositionName(int positionId)
    {
        try
        {
            Result<PositionDTO> result = await _positionService.GetById(positionId);

            if (result.IsSuccess)
            {
                PositionDTO position = result.Value;
                return position.Name;
            }

            return "Position Not Found";
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int> GetUnreadNotificationCount()
    {
        try
        {
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            string userId = currentUser.Id;
            IEnumerable<Notifications> notifications = await _notificationsRepository.GetAllNotifications();

            int unreadCount = notifications.Where(notification => !notification.IsRead
                                                                && notification.ReceiverId == userId
                                                 )
                                           .Count();

            return unreadCount;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task MarkAllAsReadForRoleAsync(string roleName)
    {
        try
        {
            IdentityRole role = await _roleManager.FindByNameAsync(roleName);

            if (role != null)
            {
                IList<IdentityUser> users = await _userManager.GetUsersInRoleAsync(role.Name);

                if (users.Any())
                {
                    string userId = users.First().Id;

                    List<Notifications> notifications = await _dbContext.Notifications
                                                                        .Where(n => n.ReceiverId == userId && !n.IsRead)
                                                                        .ToListAsync();

                    foreach (Notifications notification in notifications)
                    {
                        notification.IsRead = true;
                        await _notificationsRepository.Update(notification);
                    }

                    await _dbContext.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Failed to mark all notifications as read for {roleName}", ex);
        }
    }

    public async Task NotifyAssignArchiAsync(int status, string notes, int CandidateId, int positionId)
    {
        try
        {
            var archiId = "";

            var statusResult = await _statusService.GetById(status);
            var statusstatus = statusResult.Value;

            var archi = await _roleManager.FindByNameAsync("Solution Architecture");
            archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

            string userName = GetLoggedInUserName();
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            var candidateName = await GetCandidateName(CandidateId);
            var positionName = await GetPositionName(positionId);

            // Create the notification for the manager.
            var notification = new Notifications
            {
                SendDate = DateTime.Now,
                CandidateId = CandidateId,
                IsReceived = true,
                IsRead = false,
                Title = "",
                BodyDesc = notes,
                CreatedBy = currentUser.Id,
                CreatedOn = DateTime.Now
            };

            notification.Title = $"You have been assigned, along with the GM, to interview {candidateName} for the {positionName} position. Prepare to make a great impression! 💼🚀";
            notification.ReceiverId = archiId;

            await _notificationsRepository.Create(notification);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public async Task RemoveNotifyAssignArchiAsync(int status, string notes, int CandidateId, int positionId)
    {
        try
        {
            var archiId = "";

            var statusResult = await _statusService.GetById(status);
            var statusstatus = statusResult.Value;

            var archi = await _roleManager.FindByNameAsync("Solution Architecture");
            archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

            string userName = GetLoggedInUserName();
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            var candidateName = await GetCandidateName(CandidateId);
            var positionName = await GetPositionName(positionId);

            // Create the notification for the manager.
            var notification = new Notifications
            {
                SendDate = DateTime.Now,
                CandidateId = CandidateId,
                IsReceived = true,
                IsRead = false,
                Title = "",
                BodyDesc = notes,
                CreatedBy = currentUser.Id,
                CreatedOn = DateTime.Now
            };

            notification.Title = $"Your interview with the GM for {candidateName} regarding the {positionName} position has been removed. Thank you!";
            notification.ReceiverId = archiId;

            await _notificationsRepository.Create(notification);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public async Task<IEnumerable<NotificationsDTO>> GetUnreadNotificationsForGMAsync()
    {
        try
        {
            List<Notifications> notificationsGM = await _notificationsRepository.GetSpacificNotificationsforGeneral();
            List<Notifications> notificationsArchi = await _notificationsRepository.GetSpacificNotificationsforArchi();

            IdentityRole GM = await _roleManager.FindByNameAsync("General Manager");
            string GMId = (await _userManager.GetUsersInRoleAsync(GM.Name)).FirstOrDefault()?.Id;

            IdentityRole Archi = await _roleManager.FindByNameAsync("Solution Architecture");
            string ArchiId = (await _userManager.GetUsersInRoleAsync(Archi.Name)).FirstOrDefault()?.Id;

            string userRole = await GetLoggedInUserRoleAsync();

            if (userRole == "General Manager" && GMId != null)
            {
                List<NotificationsDTO> notificationsDTOList = notificationsGM.Where(notification => notification.ReceiverId == GMId)
                                                                             .Select(notification => new NotificationsDTO
                                                                             {
                                                                                 NotificationsId = notification.NotificationsId,
                                                                                 SendDate = notification.SendDate,
                                                                                 Title = notification.Title,
                                                                                 BodyDesc = notification.BodyDesc,
                                                                                 IsReceived = true,
                                                                                 IsRead = notification.IsRead,
                                                                             })
                                                                             .ToList();

                return notificationsDTOList;
            }

            else if (userRole == "Solution Architecture" && ArchiId != null)
            {
                List<NotificationsDTO> notificationsDTOList = notificationsArchi.Where(notification => notification.ReceiverId == ArchiId)
                                                                                .Select(notification => new NotificationsDTO
                                                                                {
                                                                                    NotificationsId = notification.NotificationsId,
                                                                                    SendDate = notification.SendDate,
                                                                                    Title = notification.Title,
                                                                                    BodyDesc = notification.BodyDesc,
                                                                                    IsReceived = true,
                                                                                    IsRead = notification.IsRead,
                                                                                })
                                                                                .ToList();

                return notificationsDTOList;
            }

            // Default return value if no conditions are met
            return new List<NotificationsDTO>();
        }
        catch (Exception)
        {
            throw;
        }
    }

}
