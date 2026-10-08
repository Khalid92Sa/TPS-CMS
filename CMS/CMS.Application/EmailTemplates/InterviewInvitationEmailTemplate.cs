using System;

namespace CMS.Application.EmailTemplates;

public static class InterviewInvitationEmailTemplate
{
    public static string GetInvitationEmailTemplate(
        string recipientName,
        string coInterviewerName,
        string candidateName,
        string positionName,
        DateTime interviewDate,
        int interviewId,
        bool isSecondInterview = false,
        bool includeScheduleDate = true,
        ApprovalSummary approval = null)
    {
        return Build(
            recipientName, coInterviewerName, candidateName, positionName, interviewDate,
            interviewId.ToString(), isSecondInterview, includeScheduleDate, approval,
            title: $"Interview Invitation - {candidateName}",
            eyebrow: "Interview invitation",
            badge: null,
            tone: EmailTone.Primary,
            heading: $"You're invited to interview {candidateName}",
            buttonText: "View invitation",
            closing: "Please click the button below to view the invitation details.");
    }

    public static string UpdatedInvitationEmail(
        string recipientName,
        string coInterviewerName,
        string candidateName,
        string positionName,
        DateTime interviewDate,
        string interviewId,
        bool isSecondInterview = false,
        bool includeScheduleDate = true)
    {
        return Build(
            recipientName, coInterviewerName, candidateName, positionName, interviewDate,
            interviewId, isSecondInterview, includeScheduleDate, approval: null,
            title: $"Interview Update - {candidateName}",
            eyebrow: "Interview update",
            badge: "Updated",
            tone: EmailTone.Info,
            heading: $"Interview details changed for {candidateName}",
            buttonText: "View updated invitation",
            closing: "Please click the button below to review the updated interview details.");
    }

    private static string Build(
        string recipientName,
        string coInterviewerName,
        string candidateName,
        string positionName,
        DateTime interviewDate,
        string interviewId,
        bool isSecondInterview,
        bool includeScheduleDate,
        ApprovalSummary approval,
        string title,
        string eyebrow,
        string badge,
        EmailTone tone,
        string heading,
        string buttonText,
        string closing)
    {
        string formattedDate = interviewDate.ToString("dddd, MMMM dd, yyyy hh:mm tt");
        string interviewPhase = isSecondInterview ? "second interview" : "first interview";
        string who = string.IsNullOrEmpty(coInterviewerName)
            ? "You are"
            : $"You and {EmailLayout.Strong(coInterviewerName)} are";
        string schedule = includeScheduleDate
            ? $", scheduled on {EmailLayout.Strong(formattedDate)}."
            : ".";

        var email = new EmailContent
        {
            Title = title,
            Preheader = $"{(isSecondInterview ? "Second" : "First")} interview with {candidateName} for {positionName}",
            Tone = tone,
            Eyebrow = eyebrow,
            Badge = badge,
            Heading = heading,
            RecipientName = recipientName.Replace("_", " "),
            IntroHtml =
                $"<p style=\"margin:0 0 14px;\">{who} assigned to conduct a {EmailLayout.Strong(interviewPhase)} " +
                $"with {EmailLayout.Strong(candidateName)} for the {EmailLayout.Strong(positionName)} position{schedule}</p>" +
                $"<p style=\"margin:0;\">{closing}</p>",
            DetailsTitle = "Interview details",
            ButtonText = buttonText,
            ButtonUrl = $"https://apps.sssprocess.com:6134/interviews/{interviewId}/addingresult",
            NoteHtml = "If you have any questions, please reach out to the HR team."
        };

        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Position", positionName));
        email.Details.Add(new EmailDetail("Interview", isSecondInterview ? "Second interview" : "First interview"));
        if (includeScheduleDate)
            email.Details.Add(new EmailDetail("Scheduled", formattedDate));
        if (!string.IsNullOrEmpty(coInterviewerName))
            email.Details.Add(new EmailDetail("Co-interviewer", coInterviewerName));
        ApprovalSummary.ApplyYourTurn(email, approval, candidateName, positionName,
            withHtml: string.IsNullOrEmpty(coInterviewerName) ? null : $", together with {EmailLayout.Strong(coInterviewerName)},");
        ApprovalSummary.AddTo(email, approval);

        return EmailLayout.Render(email);
    }
}
