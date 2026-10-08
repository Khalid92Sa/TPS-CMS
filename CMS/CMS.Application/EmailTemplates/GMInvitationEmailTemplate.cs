namespace CMS.Application.EmailTemplates;

public static class GMInterviewInvitationEmailTemplate
{
    public static string GetGMInvitationEmail(string gmName, string candidateName, string positionName, string interviewLink, ApprovalSummary approval = null)
    {
        var email = new EmailContent
        {
            Title = $"Second Interview Invitation - {candidateName}",
            Preheader = $"Second interview with {candidateName} for {positionName}",
            Tone = EmailTone.Primary,
            Eyebrow = "Interview invitation",
            Badge = "Second interview",
            Heading = $"Second interview with {candidateName}",
            RecipientName = gmName,
            IntroHtml =
                $"<p style=\"margin:0 0 14px;\">You are assigned to conduct a {EmailLayout.Strong("Second Interview")} for " +
                $"{EmailLayout.Strong(candidateName)} for the {EmailLayout.Strong(positionName)} position.</p>" +
                "<p style=\"margin:0;\">Please click the button below to view the invitation details.</p>",
            DetailsTitle = "Interview details",
            ButtonText = "View invitation",
            ButtonUrl = interviewLink,
            NoteHtml = "If you have any questions, please reach out to the HR team."
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Position", positionName));
        email.Details.Add(new EmailDetail("Interview", "Second interview"));
        ApprovalSummary.ApplyYourTurn(email, approval, candidateName, positionName);
        ApprovalSummary.AddTo(email, approval);

        return EmailLayout.Render(email);
    }
}
