namespace CMS.Application.EmailTemplates;

public static class ArchitectureInterviewAssignmentEmailTemplate
{
    public static string GetAssignmentEmail(string interviewerName, string candidateName, string interviewDate, string interviewLink, string systemName = "CMS")
    {
        var email = new EmailContent
        {
            Title = $"New Architecture Interview Assignment - {candidateName}",
            Preheader = $"Architecture interview for {candidateName} on {interviewDate}",
            Tone = EmailTone.Primary,
            Eyebrow = "Architecture interview",
            Badge = "Assigned",
            Heading = $"You're assigned to interview {candidateName}",
            RecipientName = interviewerName,
            IntroHtml =
                $"<p style=\"margin:0 0 14px;\">You have been assigned to conduct an {EmailLayout.Strong("Architecture Interview")} " +
                $"with {EmailLayout.Strong("Saeed Abdelati")}, for {EmailLayout.Strong(candidateName)} scheduled on {EmailLayout.Strong(interviewDate)}.</p>" +
                "<p style=\"margin:0;\">Please click the button below to view the interview details.</p>",
            DetailsTitle = "Interview details",
            ButtonText = "View interview details",
            ButtonUrl = interviewLink,
            SystemName = systemName
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Interview", "Architecture interview"));
        email.Details.Add(new EmailDetail("Conducted with", "Saeed Abdelati"));
        email.Details.Add(new EmailDetail("Scheduled", interviewDate));

        return EmailLayout.Render(email);
    }

    public static string GetRemovalEmail(string interviewerName, string candidateName, string systemName = "CMS")
    {
        var email = new EmailContent
        {
            Title = $"Architecture Interview Assignment Removed - {candidateName}",
            Preheader = $"You are no longer assigned to the architecture interview for {candidateName}",
            Tone = EmailTone.Neutral,
            Eyebrow = "Architecture interview",
            Badge = "Removed",
            Heading = "Your interview assignment was removed",
            RecipientName = interviewerName,
            IntroHtml =
                $"<p style=\"margin:0;\">Your assignment for the {EmailLayout.Strong("Architecture Interview")} with " +
                $"{EmailLayout.Strong("Saeed Abdelati")}, for candidate {EmailLayout.Strong(candidateName)} has been removed. " +
                "No further action is needed from you.</p>",
            DetailsTitle = "Removed assignment",
            NoteHtml = "If you have any questions, please contact the HR department.",
            SystemName = systemName
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Interview", "Architecture interview"));

        return EmailLayout.Render(email);
    }
}
