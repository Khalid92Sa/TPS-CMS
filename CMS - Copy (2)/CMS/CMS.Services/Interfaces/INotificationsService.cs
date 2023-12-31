﻿using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces
{
    public interface INotificationsService
    {

        Task<IEnumerable<NotificationsDTO>> GetAllNotificationsAsync();
        Task<NotificationsDTO> GetNotificationByIdAsync(int notificationsId);
        Task Create(NotificationsDTO entity);
        Task Update(int notificationsId, NotificationsDTO entity);
        Task Delete(int notificationsId);



        Task<List<NotificationsDTO>> GetNotificationsForUserAsync(string userId);

        Task<IEnumerable<NotificationsDTO>> GetNotificationsForHRAsync();
        Task<IEnumerable<NotificationsDTO>> GetNotificationsForInterviewers();
        Task<IEnumerable<NotificationsDTO>> GetNotificationsForGeneralManager();

        Task CreateNotificationForGeneralManagerAsync(int status, string notes);

        Task CreateInterviewNotificationForInterviewerAsync(DateTime interviewDate);


        Task CreateInterviewNotificationForHRInterview(int status, string notes);

        Task<NotificationsDTO> GetNotificationByIdforDetails(int notificationsId);


    }
}
