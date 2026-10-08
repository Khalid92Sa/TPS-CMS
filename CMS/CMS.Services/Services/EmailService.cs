using CMS.Application.DTOs;
using CMS.Application.EmailTemplates;
using CMS.Application.Extensions;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly IStatusService _statusService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUserName;
    private readonly string _smtpPassword;
    private readonly bool _useDefaultCredentials;
    private readonly bool _enableSsl;
    private readonly string _fromEmail;
    private readonly string _ccEmail;
    private readonly string _approvalCcEmail;

    public EmailService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<IdentityUser> userManager,
        IInterviewsRepository interviewsRepository,
        ICandidateService candidateService,
        IInterviewsService interviewsService,
        IStatusService statusService,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _interviewsRepository = interviewsRepository;
        _candidateService = candidateService;
        _interviewsService = interviewsService;
        _statusService = statusService;
        _configuration = configuration;

        _logger = logger;
        // Load email settings from appsettings.json
        _smtpHost = _configuration["EmailSettings:Host"];
        _smtpPort = _configuration.GetValue<int>("EmailSettings:Port");
        _smtpUserName = _configuration["EmailSettings:UserName"];
        _smtpPassword = _configuration["EmailSettings:Password"];
        _useDefaultCredentials = _configuration.GetValue<bool>("EmailSettings:UseDefaultCredentials");
        _enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl");
        _fromEmail = _configuration["EmailSettings:FromEmail"];
        _ccEmail = _configuration["EmailSettings:CcEmail"];
        _approvalCcEmail = _configuration["EmailSettings:ApprovalCcEmail"];
    }

    private static void AddEmailIfValid(MailAddressCollection recipients, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        if (recipients.Any(x => string.Equals(x.Address, email, StringComparison.OrdinalIgnoreCase)))
            return;

        recipients.Add(email);
    }

    private void AddConfiguredCcRecipients(MailMessage message, bool isApprovalEmail)
    {
        AddEmailIfValid(message.CC, _ccEmail);

        if (isApprovalEmail)
            AddEmailIfValid(message.CC, _approvalCcEmail);
    }

    private async Task<bool> IsApprovedStatusAsync(int? statusId)
    {
        if (!statusId.HasValue)
            return false;

        Result<StatusDTO> statusResult = await _statusService.GetById(statusId.Value);
        if (!statusResult.IsSuccess || statusResult.Value is null)
            return false;

        return statusResult.Value.Code == Domain.Enums.StatusCode.Approved;
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
                    EmailBody = EmailLayout.Render(new EmailContent
                    {
                        Title = "Interview Score Reminder",
                        Tone = EmailTone.Warning,
                        Eyebrow = "Reminder",
                        Badge = "Score missing",
                        Heading = "Interview score reminder",
                        IntroHtml = "<p style=\"margin:0;\">You haven't provided a score for the interview. Please provide a score.</p>",
                        ButtonText = "Submit score",
                        ButtonUrl = $"https://apps.sssprocess.com:6134/interviews/{collection.InterviewsId}/addingresult"
                    }),
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
                Host = _smtpHost,
                Port = _smtpPort,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = _enableSsl,
                UseDefaultCredentials = _useDefaultCredentials
            };

            string UserName = _smtpUserName;
            string Password = _smtpPassword;
            smtp.Credentials = new NetworkCredential(UserName, Password);

            using MailMessage message = new();
            message.From = new MailAddress(_fromEmail);

            if (emailToResend.EmailTo != null && emailToResend.EmailTo.Any())
            {
                foreach (string to in emailToResend.EmailTo)
                    message.To.Add(to);
            }

            message.Body = emailToResend.EmailBody;
            message.Subject = emailToResend.Subject;
            message.IsBodyHtml = true;

            AddConfiguredCcRecipients(message, emailToResend.IsApprovalEmail);

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
        catch (Exception ex)
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
            emailModel.IsApprovalEmail = await IsApprovedStatusAsync(interview?.StatusId);

            SmtpClient smtp = new()
            {
                Host = _smtpHost,
                Port = _smtpPort,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = _enableSsl,
                UseDefaultCredentials = _useDefaultCredentials
            };

            string UserName = _smtpUserName;
            string Password = _smtpPassword;
            smtp.Credentials = new NetworkCredential(UserName, Password);

            using MailMessage message = new();
            message.From = new MailAddress(_fromEmail);

            if (emailModel.EmailTo != null && emailModel.EmailTo.Any())
            {
                foreach (string to in emailModel.EmailTo)
                    message.To.Add(to);
            }

            message.Body = emailModel.EmailBody;
            message.Subject = emailModel.Subject;
            message.IsBodyHtml = true;

            AddConfiguredCcRecipients(message, emailModel.IsApprovalEmail);

            await smtp.SendMailAsync(message);
            _logger.LogInformation("Email has been sent successfully to:" + emailModel.EmailTo.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Error sent email to:" + emailModel.EmailTo.FirstOrDefault());
            _logger.LogError(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex);

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
            emailModel.IsApprovalEmail = await IsApprovedStatusAsync(interview?.StatusId);

            SmtpClient smtp = new()
            {
                Host = _smtpHost,
                Port = _smtpPort,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = _enableSsl,
                UseDefaultCredentials = _useDefaultCredentials
            };

            string UserName = _smtpUserName;
            string Password = _smtpPassword;
            smtp.Credentials = new NetworkCredential(UserName, Password);

            using MailMessage message = new();
            message.From = new MailAddress(_fromEmail);

            if (emailModel.EmailTo != null && emailModel.EmailTo.Any())
            {
                foreach (string to in interviewersEmails)
                    message.To.Add(to);
            }

            message.Body = emailModel.EmailBody;
            message.Subject = emailModel.Subject;
            message.IsBodyHtml = true;

            AddConfiguredCcRecipients(message, emailModel.IsApprovalEmail);

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

    private static string BuildStartingSoonEmail(string interviewerName, string candidateName, InterviewsDTO interview)
    {
        var email = new EmailContent
        {
            Title = $"Interview Reminder ( {candidateName} )",
            Preheader = $"Your interview with {candidateName} starts in 15 minutes",
            Tone = EmailTone.Info,
            Eyebrow = "Interview reminder",
            Badge = "Starts in 15 min",
            Heading = "Your interview starts soon",
            RecipientName = interviewerName,
            IntroHtml = "<p style=\"margin:0;\">Your interview is scheduled to start in 15 minutes. Please be prepared.</p>",
            DetailsTitle = "Interview"
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        if (!string.IsNullOrEmpty(interview.PositionName))
            email.Details.Add(new EmailDetail("Position", interview.PositionName));
        if (interview.Date != default)
            email.Details.Add(new EmailDetail("Starts", interview.Date.ToString("dddd, MMMM dd, yyyy hh:mm tt")));

        return EmailLayout.Render(email);
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
                    EmailBody = BuildStartingSoonEmail(userInterviewer.UserName.Replace("_", " "), candidateNameresult, collection)
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