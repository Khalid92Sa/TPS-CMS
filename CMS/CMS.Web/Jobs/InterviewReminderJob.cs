using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Services.Interfaces;
using CMS.Web.Jobs.Interfaces;
using Hangfire.Server;
using Hangfire.Console;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Web.Jobs;

public class InterviewReminderJob : IInterviewReminderJob
{
    private readonly IInterviewsService _interviewsService;
    private readonly IEmailService _emailService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InterviewReminderJob> _logger;

    public InterviewReminderJob(
        IInterviewsService interviewsService,
        IEmailService emailService,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration,
        ILogger<InterviewReminderJob> logger)
    {
        _interviewsService = interviewsService;
        _emailService = emailService;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendReminderEmails(PerformContext performContext = null)
    {
        try
        {
            _logger.LogInformation("InterviewReminderJob started execution.");
            performContext?.WriteLine("- InterviewReminderJob started execution.");

            int interviewReminderDaysDelay = _configuration.GetValue<int>("HangfireSettings:InterviewReminderDaysDelay");
            _logger.LogInformation("Configured InterviewReminderDaysDelay: {delay} days", interviewReminderDaysDelay);
            performContext?.WriteLine($"- Configured InterviewReminderDaysDelay: {interviewReminderDaysDelay} days");

            Result<List<InterviewsDTO>> pendingInterviewsResult = await _interviewsService.GetInterviewsWithoutResults();
            List<InterviewsDTO> pendingInterviews = pendingInterviewsResult.Value;

            _logger.LogInformation("Fetched {count} pending interviews without results.", pendingInterviews.Count);
            performContext?.WriteLine($"- Fetched {pendingInterviews.Count} pending interviews without results.");

            foreach (InterviewsDTO interview in pendingInterviews)
            {
                DateTime interviewEndTime = interview.Date.AddDays(interviewReminderDaysDelay);
                _logger.LogInformation("Processing interview {InterviewId} for {CandidateName} scheduled on {InterviewDate}",
                                        interview.InterviewsId, interview.FullName, interview.Date);

                performContext?.WriteLine($"- Processing interview {interview.InterviewsId} for {interview.FullName} scheduled on {interview.Date}");

                if (DateTime.UtcNow >= interviewEndTime.ToUniversalTime())
                {
                    _logger.LogInformation("Sending reminder for interview {InterviewId}", interview.InterviewsId);
                    performContext?.WriteLine($"- Sending reminder for interview {interview.InterviewsId}");

                    string interviewerEmail = await _emailService.GetInterviewerEmail(interview.InterviewerId);
                    IdentityUser interviewer = await _userManager.FindByEmailAsync(interviewerEmail);

                    EmailDTOs reminderEmail = new()
                    {
                        EmailTo = [interviewerEmail],
                        Subject = "Reminder: Interview Result Submission",
                        EmailBody = $@"
                    <html>
                    <body>
                        <p>Dear {interviewer.UserName.Replace("_", " ")},</p>
                        <p>You conducted an interview for {interview.FullName} at {interview.Date}.</p>
                        <p>Please submit the interview result <a href='https://apps.sssprocess.com:6134/interviews/{interview.InterviewsId}/addingresult'>here</a> as soon as possible.</p>
                        <p>Best Regards,<br>CMS Team</p>
                    </body>
                    </html>"
                    };

                    await _emailService.SendEmailToInterviewer(interviewerEmail, interview, reminderEmail);
                    _logger.LogInformation("Reminder email sent to {Email} for interview {InterviewId}", interviewerEmail, interview.InterviewsId);
                    performContext?.WriteLine($"- Reminder email sent to {interviewerEmail} for interview {interview.InterviewsId}");
                }
            }

            _logger.LogInformation("InterviewReminderJob completed execution.");
            performContext?.WriteLine("- InterviewReminderJob completed execution.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in InterviewReminderJob.");
            performContext?.WriteLine($"- Error occurred in InterviewReminderJob: {ex.Message}");
        }
    }
}