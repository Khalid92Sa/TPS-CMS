using CMS.Domain.Entities;
using System;

namespace CMS.Application.EmailTemplates;

public static class InterviewDelegationEmailTemplate
{
    public static string GetAssignmentEmail(
        string recipientName,
        string candidateName,
        string positionName,
        string originalInterviewerName,
        DateTime startDate,
        DateTime expirationDate,
        string interviewLink,
        string systemName = "CMS")
    {
        string starts = startDate.ToString("dddd, MMMM dd, yyyy hh:mm tt");
        bool unlimited = InterviewDelegation.IsNoExpiration(expirationDate);
        string expires = unlimited
            ? "No end date"
            : expirationDate.ToString("dddd, MMMM dd, yyyy hh:mm tt");

        var email = new EmailContent
        {
            Title = $"Temporary interview delegation - {candidateName}",
            Preheader = unlimited
                ? $"You can score {candidateName}'s interview until the delegation is removed"
                : $"You can score {candidateName}'s interview until {expires}",
            Tone = EmailTone.Info,
            Eyebrow = "Interview delegation",
            Badge = "Temporary access",
            Heading = $"You can now score {candidateName}'s interview",
            RecipientName = recipientName,
            IntroHtml =
                "<p style=\"margin:0;\">You have been given temporary access to enter the score for a pending interview. " +
                "The original interviewer remains assigned.</p>",
            DetailsTitle = "Delegation details",
            ButtonText = "Open pending interview",
            ButtonUrl = interviewLink,
            NoteHtml = unlimited
                ? "This access is only for this interview and has no end date; it ends when the interview is scored or the delegation is removed. Your account role is not changed."
                : "This access is only for this interview and ends at the expiration time. Your account role is not changed.",
            SystemName = systemName
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Position", positionName));
        email.Details.Add(new EmailDetail("Original interviewer", originalInterviewerName));
        email.Details.Add(new EmailDetail("Delegated user", recipientName));
        email.Details.Add(new EmailDetail("Delegation starts", starts));
        email.Details.Add(new EmailDetail("Delegation expires", expires));

        return EmailLayout.Render(email);
    }

    public static string GetRemovalEmail(
        string recipientName,
        string candidateName,
        string positionName,
        string systemName = "CMS")
    {
        var email = new EmailContent
        {
            Title = $"Interview delegation removed - {candidateName}",
            Preheader = $"Your temporary access to score {candidateName} has been removed",
            Tone = EmailTone.Neutral,
            Eyebrow = "Interview delegation",
            Badge = "Removed",
            Heading = "Your temporary interview access was removed",
            RecipientName = recipientName,
            IntroHtml =
                $"<p style=\"margin:0;\">Your temporary access to score {EmailLayout.Strong(candidateName)} " +
                $"({EmailLayout.Encode(positionName)}) has been removed. The previous interview link no longer opens this interview for you.</p>",
            DetailsTitle = "Removed access",
            SystemName = systemName
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Position", positionName));

        return EmailLayout.Render(email);
    }
}
