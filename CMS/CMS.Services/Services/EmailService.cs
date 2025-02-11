using CMS.Application.DTOs;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
namespace CMS.Services.Services;

public class EmailService : IEmailService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IInterviewsRepository _interviewsRepository;
    private readonly ICandidateService _candidateService;
    private readonly IInterviewsService _interviewsService;

    public EmailService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<IdentityUser> userManager,
        IInterviewsRepository interviewsRepository,
        ICandidateService candidateService,
        IInterviewsService interviewsService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _interviewsRepository = interviewsRepository;
        _candidateService = candidateService;
        _interviewsService = interviewsService;
    }


    public async Task<string> GetArchiEmail()
    {
        try
        {
            string email = await _interviewsRepository.GetArchiEmail();

            if (email != null)
                return email;
            else
                return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetGMEmail()
    {
        try
        {
            string email = await _interviewsRepository.GetGeneralManagerEmail();

            if (email != null)
                return email;
            else
                return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetHREmail()
    {
        try
        {
            string email = await _interviewsRepository.GetHREmail();

            if (email != null)
                return email;
            else
                return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetInterviewerEmail(string interviewerId)
    {
        try
        {
            string email = await _interviewsRepository.GetInterviewerEmail(interviewerId);

            if (email != null)
                return email;
            else
                return null;
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

    public async Task ReminderJobAsync(string interviewerId, InterviewsDTO collection)
    {
        try
        {
            bool hasGivenScore = await _interviewsRepository.HasGivenStatusAsync(interviewerId, collection.InterviewsId);

            if (!hasGivenScore)
            {
                string interviewerEmail2 = await GetInterviewerEmail(collection.InterviewerId);
                EmailDTOs emailModel = new()
                {
                    EmailTo = new List<string> { interviewerEmail2 },
                    EmailBody = "You haven't provided a score for the interview. Please provide a score.",
                    Subject = "Interview Score Reminder"
                };

                await SendEmailToInterviewer(interviewerEmail2, collection, emailModel);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task ResendFailedEmail(EmailDTOs emailToResend)
    {
        try
        {
            SmtpClient smtp = new()
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

            using MailMessage message = new();
            message.From = new MailAddress("cms@techprocess.net");

            if (emailToResend.EmailTo != null && emailToResend.EmailTo.Any())
            {
                foreach (string to in emailToResend.EmailTo)
                    message.To.Add(to);
            }

            message.Body = emailToResend.EmailBody;
            message.Subject = emailToResend.Subject;
            message.IsBodyHtml = true;

            await smtp.SendMailAsync(message);
        }
        catch (Exception)
        {
        }
    }

    public async Task RetryFailedEmails(EmailDTOs emailmodel)
    {
        try
        {
            List<EmailDTOs> failedEmails = new List<EmailDTOs> { emailmodel };

            List<EmailDTOs> emailsToResend = failedEmails.Take(10).ToList();

            foreach (EmailDTOs emailToResend in emailsToResend)
            {
                BackgroundJob.Schedule(() => ResendFailedEmail(emailToResend), TimeSpan.FromMinutes(20));
                failedEmails.Remove(emailToResend);
            }

            await Task.CompletedTask;
        }
        catch (Exception)
        {
        }
    }


    public async Task ScheduleInterviewReminder(InterviewsDTO collection)
    {
        try
        {
            DateTime reminderTime = collection.Date.AddMinutes(-15);

            if (DateTime.UtcNow < reminderTime)
            {
                string interviewReminderJobId = await Task.Run(() =>
                    BackgroundJob.Schedule(() => SendInterviewReminderEmail(collection), reminderTime)
                );
            }
        }
        catch (Exception)
        {
            throw;
        }
    }


    public async Task SendEmailToInterviewer(string interviewerEmail, InterviewsDTO interview, EmailDTOs emailModel)
    {
        try
        {
            SmtpClient smtp = new()
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

            using MailMessage message = new();
            message.From = new MailAddress("cms@techprocess.net");

            if (emailModel.EmailTo != null && emailModel.EmailTo.Any())
            {
                foreach (string to in emailModel.EmailTo)
                    message.To.Add(to);
            }

            message.Body = emailModel.EmailBody;
            message.Subject = emailModel.Subject;
            message.IsBodyHtml = true;

            await smtp.SendMailAsync(message);
        }
        catch (Exception)
        {
            (string To, string Subject, string Body) emailLogInfo = (
                To: string.Join(",", emailModel.EmailTo),
                Subject: emailModel.Subject,
                Body: emailModel.EmailBody
            );

            await RetryFailedEmails(emailModel);
        }
    }

    public async Task SendEmailToMultiInterviewer(List<string> interviewersEmails, InterviewsDTO interview, EmailDTOs emailModel)
    {
        try
        {
            SmtpClient smtp = new()
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

            using MailMessage message = new();
            message.From = new MailAddress("cms@techprocess.net");

            if (emailModel.EmailTo != null && emailModel.EmailTo.Any())
            {
                foreach (string to in interviewersEmails)
                    message.To.Add(to);
            }

            message.Body = emailModel.EmailBody;
            message.Subject = emailModel.Subject;
            message.IsBodyHtml = true;

            await smtp.SendMailAsync(message);
        }
        catch (Exception)
        {
            (string To, string Subject, string Body) emailLogInfo = (
                To: string.Join(",", emailModel.EmailTo),
                Subject: emailModel.Subject,
                Body: emailModel.EmailBody
            );

            await RetryFailedEmails(emailModel);
        }
    }

    public async Task SendInterviewReminderEmail(InterviewsDTO collection)
    {
        try
        {
            string interviewerEmail = await GetInterviewerEmail(collection.InterviewerId);
            IdentityUser userInterviewer = await _userManager.FindByEmailAsync(interviewerEmail);
            CandidateDTO candidateName = await _candidateService.GetCandidateByIdAsync(collection.CandidateId);
            string candidateNameresult = candidateName.FullName;

            if (!string.IsNullOrEmpty(interviewerEmail))
            {
                EmailDTOs emailModel = new()
                {
                    EmailTo = new List<string> { interviewerEmail },
                    Subject = $"Interview Reminder ( {candidateNameresult} )",
                    EmailBody = $@"<html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                            <p style='font-size: 18px; color: #333;'>
                                Dear {userInterviewer.UserName.Replace("_", " ")},
                            </p>
                            <p style='font-size: 16px; color: #555;'>
                           Your interview is scheduled to start in 15 minutes. Please be prepared.
                            </p>
                            <p style='font-size: 14px; color: #777;'>
                                Regards,
                            </p>

                    <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                        </div>
                    </body>
                 </html>"
                };

                await SendEmailToInterviewer(interviewerEmail, collection, emailModel);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}