using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Implementation;
using CMS.Repository.Interfaces;
using CMS.Repository.Repositories;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Services.Services
{
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
            IStatusService statusService
            )
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

        public void LogException(string methodName, Exception ex = null, string additionalInfo = null)
        {
            _notificationsRepository.LogException(methodName, ex,  additionalInfo);
        }


        public async Task<IEnumerable<NotificationsDTO>> GetAllNotificationsAsync()
        {
            try
            {

           
            var notifications = await _notificationsRepository.GetAllNotifications();

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
            catch (Exception ex) {
                LogException(nameof(GetAllNotificationsAsync), ex,"Error while getting all Notifications");
                throw ex;
            }

        }
        public async Task<IEnumerable<NotificationsDTO>> GetAllNotificationsAnotherTab()
        {
            try
            {
                // Fetch notifications for different roles
                var notificationsHR = await _notificationsRepository.GetSpacificNotificationsforHR();
                var notificationsGM = await _notificationsRepository.GetSpacificNotificationsforGeneral();
                var notificationsArchi = await _notificationsRepository.GetSpacificNotificationsforArchi();

                // Get role information
                var Hr = await _roleManager.FindByNameAsync("HR Manager");
                var HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault()?.Id;

                var GM = await _roleManager.FindByNameAsync("General Manager");
                var GMId = (await _userManager.GetUsersInRoleAsync(GM.Name)).FirstOrDefault()?.Id;

                var Archi = await _roleManager.FindByNameAsync("Solution Architecture");
                var ArchiId = (await _userManager.GetUsersInRoleAsync(Archi.Name)).FirstOrDefault()?.Id;

                // Get the role of the logged-in user
                var userRole = await GetLoggedInUserRoleAsync();

                // Determine the appropriate notifications to return based on the user's role
                if (userRole == "HR Manager" && HrId != null)
                {
                    var notificationsDTOList = notificationsHR
                        .Where(notification => notification.ReceiverId == HrId && notification.IsRead)
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
                    var notificationsDTOList = notificationsGM
                        .Where(notification => notification.ReceiverId == GMId && notification.IsRead)
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
                    var notificationsDTOList = notificationsArchi
                        .Where(notification => notification.ReceiverId == ArchiId && notification.IsRead)
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
            catch (Exception ex)
            {
                LogException(nameof(GetAllNotificationsAnotherTab), ex, "Error while getting all Notifications");
                throw;
            }
        }

        public async Task<IEnumerable<NotificationsDTO>> GetAllNotificationsAsyncForInterviewer(string interviewerId)
        {
            try
            {
                var notifications = await _notificationsRepository.GetSpacificNotificationsforInterviewer(interviewerId);
                var notificationsDTOList = notifications.Where(x => x.IsRead)
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
            catch (Exception ex)
            {
                LogException(nameof(GetAllNotificationsAsync), ex, "Error while getting all Notifications");
                throw ex;
            }

        }

        public async Task<NotificationsDTO> GetNotificationByIdAsync(int notificationsId)
        {
            try
            {

                

                var notification = await _notificationsRepository.GetNotificationsById(notificationsId);
            if (notification == null)
                return null;

            var templatesDTO = new TemplatesDTO
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
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationByIdAsync),ex,null);
                throw ex;
            }
        }

        public async Task<NotificationsDTO> GetNotificationByIdforDetails(int notificationsId)
        {
            try
            {
                var notification = await _notificationsRepository.GetNotificationsById(notificationsId);
                if (notification == null)
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

            catch (Exception ex)
            {

                LogException(nameof(GetNotificationByIdforDetails), ex, "GetNotificationByIdforDetails not working");
                throw ex;
            }
        }

        public async Task Create(NotificationsDTO entity)
        {

            try
            {

                

                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var notification = new Notifications
            {
                SendDate = DateTime.Now,
                ReceiverId = entity.ReceiverId,
                IsReceived = entity.IsReceived,
                Title = entity.Title,
                BodyDesc = entity.BodyDesc,
                CreatedOn = DateTime.Now,
                CreatedBy = currentUser.Id,
                IsRead=entity.IsRead,
                CandidateId = entity.CandidateId,
            };
            await _notificationsRepository.Create(notification);
            }
            catch (Exception ex)
            {

                LogException(nameof(Create), ex, "Can not create Notification");
                throw ex;
            }
        }

        public async Task Update(int notificationId, NotificationsDTO entity)
        {
            try
            {
                var existingNotification = await _notificationsRepository.GetNotificationsById(notificationId);
            if (existingNotification == null)
                throw new Exception("Notifications not found");
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
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
            catch (Exception ex)
            {

                LogException(nameof(Update), ex, "Update Notifications not working");
                throw ex;
            }
        }

        public async Task Delete(int notificationId)
        {
            try
            {
                var notification = await _notificationsRepository.GetNotificationsById(notificationId);
            if (notification != null)
                await _notificationsRepository.Delete(notification);
            }
            catch (Exception ex)
            {

                LogException(nameof(Delete), ex, "Delete for Notification is not working");
                throw ex;
            }
        }

        public async Task<List<NotificationsDTO>> GetNotificationsForUserAsync(string userId)
        {
            try
            {
                var HrId = "";

            var Hr = await _roleManager.FindByNameAsync("HR Manager");

            HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;


            var notifications = await _notificationsRepository.GetSpacificNotificationsforHR();

            var notificationsDTOList = notifications
         .Where(notification => notification.ReceiverId == HrId) //hrId
         .Select(notification => new NotificationsDTO
         {
             IsReceived = true,
             SendDate = DateTime.Now,
             Title = notification.Title,

         })
         .ToList();

            return notificationsDTOList;
            }
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForUserAsync), ex, "GetNotificationsForUserAsync not working");
                throw ex;
            }

        }

        public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForHRAsync()
        {
            try
            {
                var HrId = "";

                var Hr = await _roleManager.FindByNameAsync("HR Manager");

                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                var notifications = await _notificationsRepository.GetSpacificNotificationsforHR();
                var notificationsDTOList = notifications.Where(notification => notification.ReceiverId == HrId && notification.IsRead == false)//hrId
                                                        .Select(notification => new NotificationsDTO
                                                                                {
                                                                                    NotificationsId = notification.NotificationsId,
                                                                                    SendDate = notification.SendDate,
                                                                                    Title = notification.Title,
                                                                                    BodyDesc = notification.BodyDesc,
                                                                                    IsReceived = true,
                                                                                    IsRead=notification.IsRead,
                                                                                    
                                                                                })
                                                        .ToList();

            return notificationsDTOList;
            }
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForHRAsync), ex, "GetNotificationsForHRAsync not working");
                throw ex;
            }
        }

        public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForHRAsyncicon()
        {
            try
            {
                


                var HrId = "";

                var Hr = await _roleManager.FindByNameAsync("HR Manager");

                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;


                var notifications = await _notificationsRepository.GetSpacificNotificationsforHR();
                var notificationsDTOList = notifications
                    .Where(notification => notification.ReceiverId == HrId && notification.IsRead == false)//hrId
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
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForHRAsyncicon), ex, "GetNotificationsForHRAsyncicon not working");
                throw ex;
            }
        }

        public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForInterviewers(string interviewerId)
        {
            try
            {
                var notifications = await _notificationsRepository.GetSpacificNotificationsforInterviewer(interviewerId);
                var notificationsDTOList = notifications.Where(notification => notification.IsRead == false)
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
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForInterviewers), ex, "GetNotificationsForInterviewers not working");
                throw ex;
            }
        }

        public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForInterviewersicon(string interviewerId)
        {
            try
            {
                


                var notifications = await _notificationsRepository.GetSpacificNotificationsforInterviewer(interviewerId);
                var notificationsDTOList = notifications.Where(notification => notification.IsRead == false)
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
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForInterviewersicon), ex, "GetNotificationsForInterviewersicon not working");
                throw ex;
            }
        }
        public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForGeneralManager()
        {
            try
            {
                var managerId = "";

            var manager = await _roleManager.FindByNameAsync("General Manager");

            managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;



            var notifications = await _notificationsRepository.GetSpacificNotificationsforGeneral();
            var notificationsDTOList = notifications
                .Where(notification => notification.ReceiverId == managerId && notification.IsRead == false)//GMId
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
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForGeneralManager), ex, "GetNotificationsForGeneralManager not working");
                throw ex;
            }
        }

        public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForGeneralManagericon()
        {
            try
            {
                var managerId = "";

                var manager = await _roleManager.FindByNameAsync("General Manager");

                managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;



                var notifications = await _notificationsRepository.GetSpacificNotificationsforGeneral();
                var notificationsDTOList = notifications
                    .Where(notification => notification.ReceiverId == managerId && notification.IsRead == false)//GMId
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
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForGeneralManagericon), ex, "GetNotificationsForGeneralManagericon not working");
                throw ex;
            }
        }

        public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForArchitecture()
        {
            try
            {
                var archiId = "";

                var archi = await _roleManager.FindByNameAsync("Solution Architecture");

                archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

                var notifications = await _notificationsRepository.GetSpacificNotificationsforArchi();
                var notificationsDTOList = notifications
                    .Where(notification => notification.ReceiverId == archiId && notification.IsRead == false)//ArchiId
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
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForArchitecture), ex, "GetNotificationsForArchitecture not working");
                throw ex;
            }
        }

        public async Task<IEnumerable<NotificationsDTO>> GetNotificationsForArchitectureicon()
        {
            try
            {
                var archiId = "";

                var archi = await _roleManager.FindByNameAsync("Solution Architecture");

                archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

                var notifications = await _notificationsRepository.GetSpacificNotificationsforArchi();
                var notificationsDTOList = notifications
                    .Where(notification => notification.ReceiverId == archiId && notification.IsRead == false)//ArchiId
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
            catch (Exception ex)
            {

                LogException(nameof(GetNotificationsForArchitectureicon), ex, "GetNotificationsForArchitectureicon not working");
                throw ex;
            }
        }


        public async Task CreateNotificationForGeneralManagerAsync(int status, string notes, int CandidateId, int positionId, string archiInterviewerId)
        {
            try
            {

                var managerId = "";
                var HrId = "";

                var statusResult = await _statusService.GetById(status);
                var statusstatus = statusResult.Value;

                var manager = await _roleManager.FindByNameAsync("General Manager");
                managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;

                var Hr = await _roleManager.FindByNameAsync("HR Manager");
                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

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
                    if (archiInterviewerId == "" || archiInterviewerId == null)
                    {
                    
                    var hrNotification = new Notifications
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
            catch (Exception ex)
            {

                LogException(nameof(CreateNotificationForGeneralManagerAsync), ex,  null);
                throw ex;
            }
        }


        public async Task CreateNotificationForArchiAsync(int status, string notes, int CandidateId, int positionId)
        {
            try
            {
                var archiId = "";
                var HrId = "";

                var statusResult = await _statusService.GetById(status);
                var statusstatus = statusResult.Value;

                var archi = await _roleManager.FindByNameAsync("Solution Architecture");
                archiId = (await _userManager.GetUsersInRoleAsync(archi.Name)).FirstOrDefault().Id;

                var Hr = await _roleManager.FindByNameAsync("HR Manager");
                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

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
                    var hrNotification = new Notifications
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
            catch (Exception ex)
            {

                LogException(nameof(CreateNotificationForGeneralManagerAsync), ex, "CreateNotificationForGeneralManagerAsync not working");
                throw ex;
            }
        }

        public async Task CreateInterviewNotificationForInterviewerAsync(DateTime interviewDate, int candidateId, int positionId, List<string> selectedInterviewerIds, bool isCanceled)
        {
            try
            {
                var formattedDate = interviewDate.ToString("dd/MM/yyyy hh:mm tt");

                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var candidateName = await GetCandidateName(candidateId);
                var positionName = await GetPositionName(positionId);

                foreach (var selectedInterviewerId in selectedInterviewerIds)
                {
                    var notification = new Notifications
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
                        var secondInterviewerName = await GetInterviewerName(selectedInterviewerIds[1]);

                        if (selectedInterviewerIds.Count == 2 && secondInterviewerName != "Unknown Interviewer")
                        {

                            if (selectedInterviewerId == selectedInterviewerIds[0] )
                            {
                                notification.Title = $"New interview invitation for {candidateName}";
                                notification.BodyDesc = $"You and {secondInterviewerName} have been selected for a First Interview with {candidateName} for the {positionName} position on {formattedDate}. Get ready to shine! 💼🚀";

                            }
                            else
                            {
                                var firstInterviewerName = await GetInterviewerName(selectedInterviewerIds[0]);
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

                    if(selectedInterviewerId == null)
                    {
                        
                    }
                    else
                    {
                        await _notificationsRepository.Create(notification);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(CreateInterviewNotificationForInterviewerAsync), ex, "CreateInterviewNotificationForInterviewerAsync not working");
                throw ex;
            }
        }
        public async Task<string> GetInterviewerName(string interviewerId)
        {
            var interviewer = await _userManager.FindByIdAsync(interviewerId);

            if (interviewer != null)
            {
                return interviewer.UserName; // Replace 'Name' with the actual property name in your IdentityUser model
            }

            // Return a default name or handle the case when the user is not found
            return "Unknown Interviewer";
        }


        public async Task CreateInterviewNotificationForHRInterview(int status, string notes, int CandidateId, int positionId)
        {
            try
            {
                var statusResult = await _statusService.GetById(status);
                var statusstatus = statusResult.Value;

                var HrId = "";

                var Hr = await _roleManager.FindByNameAsync("HR Manager");

                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                string userName = GetLoggedInUserName();
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                var candidateName = await GetCandidateName(CandidateId);
                var positionName = await GetPositionName(positionId);

                var notification = new Notifications
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
                {
                    notification.Title = $"You have a Third Interview with {candidateName} for the {positionName} position. Get ready to shine! 💼🚀 ";
                }
                else
                {
                    notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";
                }
                if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
                {
                    var hrNotification = new Notifications
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
            catch (Exception ex)
            {

                LogException(nameof(CreateInterviewNotificationForHRInterview), ex, "CreateInterviewNotificationForHRInterview not working");
                throw ex;
            }
        }
        public async Task CreateInterviewNotificationForFinalHRInterview(int status, string notes, int CandidateId, int positionId)
        {
            try
            {
                var statusResult = await _statusService.GetById(status);
                var statusstatus = statusResult.Value;

                var HrId = "";

                var Hr = await _roleManager.FindByNameAsync("HR Manager");

                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                string userName = GetLoggedInUserName();
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                var candidateName = await GetCandidateName(CandidateId);
                var positionName = await GetPositionName(positionId);

                var notification = new Notifications
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
                {
                    notification.Title = $"You have a Final Interview with {candidateName} for the {positionName} position. Get ready to shine! 💼🚀 ";
                }
                else
                {
                    notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";
                }
                if (statusstatus.Code == Domain.Enums.StatusCode.Approved)
                {
                    var hrNotification = new Notifications
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
            catch (Exception ex)
            {

                LogException(nameof(CreateInterviewNotificationForHRInterview), ex, "CreateInterviewNotificationForHRInterview not working");
                throw ex;
            }
        }
        public async Task CreateInterviewNotificationtoHrForOnHold(int status, string notes, int CandidateId, int positionId)
        {
            try
            {
                var statusResult = await _statusService.GetById(status);
                var statusstatus = statusResult.Value;

                var HrId = "";

                var Hr = await _roleManager.FindByNameAsync("HR Manager");

                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                string userName = GetLoggedInUserName();
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                var candidateName = await GetCandidateName(CandidateId);
                var positionName = await GetPositionName(positionId);

                var notification = new Notifications
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

               
                 if(statusstatus.Code == Domain.Enums.StatusCode.Rejected)
                {
                    notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";
                }
                if (statusstatus.Code == Domain.Enums.StatusCode.OnHold)
                {
                    var hrNotification = new Notifications
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
            catch (Exception ex)
            {

                LogException(nameof(CreateInterviewNotificationForHRInterview), ex, "CreateInterviewNotificationForHRInterview not working");
                throw ex;
            }
        }

        public async Task CreateInterviewNotificationForHRInterviewfromGM(int status, string notes, int CandidateId, int positionId)
        {
            try
            {
                var statusResult = await _statusService.GetById(status);
                var statusstatus = statusResult.Value;

                var HrId = "";

                var Hr = await _roleManager.FindByNameAsync("HR Manager");

                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                string userName = GetLoggedInUserName();
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                var candidateName = await GetCandidateName(CandidateId);
                var positionName = await GetPositionName(positionId);

                var notification = new Notifications
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
                {
                    notification.Title = $"You have a second Interview with {candidateName} for the {positionName} position. Get ready to shine! 💼🚀 ";
                }
                else
                {
                    notification.Title = $"{candidateName} Rejected by {userName} for position {positionName}";
                }

                await _notificationsRepository.Create(notification);
            }
            catch (Exception ex)
            {

                LogException(nameof(CreateInterviewNotificationForHRInterview), ex, "CreateInterviewNotificationForHRInterview not working");
                throw ex;
            }
        }

        public async Task CreateNotificationForInterviewer(int CandidateId, string selectedInterviewerId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var candidateName = await GetCandidateName(CandidateId);

                var notification = new Notifications
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
            catch (Exception ex)
            {

                LogException(nameof(CreateNotificationForInterviewer), ex, "CreateNotificationForInterviewer not working");
                throw ex;
            }
        }

        public string GetLoggedInUserName()
        {
            try
            {
                return _httpContextAccessor.HttpContext.User.Identity.Name;
            }
            catch (Exception ex)
            {

                LogException(nameof(GetLoggedInUserName), ex, "GetLoggedInUserName not working");
                throw ex;
            }
        }

        public async Task<string> GetLoggedInUserRoleAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var roles = await _userManager.GetRolesAsync(user);
                return roles.FirstOrDefault(); // Assuming a user has a single role
            }
            catch (Exception ex)
            {
                LogException(nameof(GetLoggedInUserRoleAsync), ex, "Failed to get user role");
                throw;
            }
        }

        public async Task<string> GetCandidateName(int candidateId)
        {
            try
            {
                var candidate = await _candidateService.GetCandidateByIdAsync(candidateId);

                if (candidate != null)
                {
                    return candidate.FullName;
                }

                return "Candidate Not Found";
            }
            catch (Exception ex)
            {

                LogException(nameof(GetCandidateName), ex, "GetCandidateName not working");
                throw ex;
            }
        }

        public async Task<string> GetPositionName(int positionId)
        {
            try
            {
                var result = await _positionService.GetById(positionId);

                if (result.IsSuccess)
                {
                    var position = result.Value;
                    return position.Name;
                }

                return "Position Not Found";
            }
            catch (Exception ex)
            {

                LogException(nameof(GetPositionName), ex, "GetPositionName not working");
                throw ex;
            }
        }

        public async Task<int> GetUnreadNotificationCount()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var userId = currentUser.Id;
                var notifications = await _notificationsRepository.GetAllNotifications(); 

                var unreadCount = notifications
                    .Where(notification => !notification.IsRead && notification.ReceiverId == userId)
                    .Count();

                return unreadCount;
            }
            catch (Exception ex)
            {
                LogException(nameof(GetUnreadNotificationCount), ex);
                throw ex;
            }
        }

        public async Task MarkAllAsReadForHRAsync()
        {
            try
            {
                // Find the HR Manager role
                var HrRole = await _roleManager.FindByNameAsync("HR Manager");

                if (HrRole != null)
                {
                    // Get all users in the HR Manager role
                    var HrUsers = await _userManager.GetUsersInRoleAsync(HrRole.Name);

                    if (HrUsers.Any())
                    {
                        // Use the ID of the first user in the HR Manager role
                        var HrId = HrUsers.First().Id;

                        // Get all unread notifications for HR
                        var notifications = await _dbContext.Notifications
                            .Where(n => n.ReceiverId == HrId && !n.IsRead)
                            .ToListAsync();

                        // Mark each notification as read
                        foreach (var notification in notifications)
                        {
                            notification.IsRead = true;
                        }

                        // Save changes to the database
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (log, throw, etc.)
                throw new ApplicationException("Failed to mark all notifications as read for HR", ex);
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

                LogException(nameof(CreateNotificationForGeneralManagerAsync), ex, "CreateNotificationForGeneralManagerAsync not working");
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

                LogException(nameof(CreateNotificationForGeneralManagerAsync), ex, "CreateNotificationForGeneralManagerAsync not working");
                throw ex;
            }
        }


    }
}