using CMS.Application.DTOs;

namespace CMS.Application.EmailTemplates;

public static class ReminderInterviewEmailTemplate
{
    public static string GetReminderEmailTemplate(string interviewerName, string interviewerEmail, InterviewsDTO interview)
    {
        var email = new EmailContent
        {
            Title = "Reminder: Interview Result Submission",
            Preheader = $"Your result for {interview.FullName} ({interview.PositionName}) is still pending",
            Tone = EmailTone.Warning,
            Eyebrow = "Reminder",
            Badge = "Result pending",
            Heading = "Interview result still pending",
            RecipientName = interviewerName.Replace("_", " "),
            IntroHtml =
                "<p style=\"margin:0 0 14px;\">This is a friendly reminder that we're still waiting for your input on the interview below.</p>" +
                "<p style=\"margin:0;\">Your feedback is crucial in helping us make informed hiring decisions. Please take a moment to submit your evaluation.</p>",
            DetailsTitle = "Interview",
            ButtonText = "Submit results now",
            ButtonUrl = $"https://apps.sssprocess.com:6134/interviews/{interview.InterviewsId}/addingresult",
            NoteHtml = "If you've already submitted your results, please disregard this message. " +
                       "For any questions or if you need assistance, feel free to contact our HR team."
        };
        email.Details.Add(new EmailDetail("Interview date", interview.Date.ToString("dd MMMM yyyy")));
        email.Details.Add(new EmailDetail("Candidate", interview.FullName));
        email.Details.Add(new EmailDetail("Position", interview.PositionName));

        return EmailLayout.Render(email);
    }
}
