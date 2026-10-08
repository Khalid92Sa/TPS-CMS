namespace CMS.Application.EmailTemplates;

public static class ArchitectureInterviewEmailTemplate
{
    public static string GetArchiAndGmInvitationEmail(string interviewerName, string candidateName, string positionName, string interviewLink, ApprovalSummary approval = null)
    {
        var email = new EmailContent
        {
            Title = $"Architecture Interview Invitation - {candidateName}",
            Preheader = $"Architecture interview with {candidateName} for {positionName}",
            Tone = EmailTone.Primary,
            Eyebrow = "Interview invitation",
            Badge = "Architecture",
            Heading = $"Architecture interview with {candidateName}",
            RecipientName = interviewerName,
            IntroHtml =
                $"<p style=\"margin:0 0 14px;\">An interview has been scheduled with {EmailLayout.Strong("Saeed")}, and you are assigned as the " +
                $"{EmailLayout.Strong("Architecture Interviewer")} for the candidate {EmailLayout.Strong(candidateName)} " +
                $"for the position of {EmailLayout.Strong(positionName)}.</p>" +
                "<p style=\"margin:0;\">Please click the button below to view the invitation details.</p>",
            DetailsTitle = "Interview details",
            ButtonText = "View invitation",
            ButtonUrl = interviewLink,
            NoteHtml = "If you have any questions, please reach out to the HR team."
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Position", positionName));
        email.Details.Add(new EmailDetail("Your role", "Architecture interviewer"));
        email.Details.Add(new EmailDetail("Conducted with", "Saeed"));
        ApprovalSummary.ApplyYourTurn(email, approval, candidateName, positionName,
            withHtml: $", together with {EmailLayout.Strong("Saeed")},");
        ApprovalSummary.AddTo(email, approval);

        return EmailLayout.Render(email);
    }
}
