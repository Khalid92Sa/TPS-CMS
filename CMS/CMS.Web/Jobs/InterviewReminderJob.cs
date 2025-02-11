using CMS.Application.DTOs;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using CMS.Web.Jobs.Interfaces;
using CMS.Domain.Entities;
using CMS.Application.Extensions;
using Microsoft.Extensions.Configuration;

namespace CMS.Web.Jobs;

public class InterviewReminderJob : IInterviewReminderJob
{
    private readonly IInterviewsService _interviewsService;
    private readonly IEmailService _emailService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public InterviewReminderJob(
        IInterviewsService interviewsService,
        IEmailService emailService,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration)
    {
        _interviewsService = interviewsService;
        _emailService = emailService;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task SendReminderEmails()
    {
        try
        {
            // Read the delay value from appsettings.json
            int interviewReminderDaysDelay = _configuration.GetValue<int>("HangfireSettings:InterviewReminderDaysDelay");

            // Fetch interviews that have not been scored
            Result<List<InterviewsDTO>> pendingInterviewsResult = await _interviewsService.GetInterviewsWithoutResults();
            List<InterviewsDTO> pendingInterviews = pendingInterviewsResult.Value;

            foreach (InterviewsDTO interview in pendingInterviews)
            {
                // Calculate when the reminder should be sent
                DateTime interviewEndTime = interview.Date.AddDays(interviewReminderDaysDelay);

                if (DateTime.UtcNow >= interviewEndTime.ToUniversalTime())
                {
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
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending interview reminders: {ex.Message}");
        }
    }
}
