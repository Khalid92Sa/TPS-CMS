namespace CMS.Application.EmailTemplates;

public static class HRInvitationEmailTemplate
{
    public static string GetFinalHRInterviewEmail(string hrName, string candidateName, string positionName, string interviewLink, ApprovalSummary approval = null)
    {
        var email = new EmailContent
        {
            Title = $"Final HR Interview Invitation - {candidateName}",
            Preheader = $"Final HR interview with {candidateName} for {positionName}",
            Tone = EmailTone.Primary,
            Eyebrow = "Interview invitation",
            Badge = "Final stage",
            Heading = $"Final HR interview with {candidateName}",
            RecipientName = hrName,
            IntroHtml =
                $"<p style=\"margin:0 0 14px;\">You are assigned to conduct a {EmailLayout.Strong("Final HR Interview")} " +
                $"for {EmailLayout.Strong(candidateName)} for the {EmailLayout.Strong(positionName)} position.</p>" +
                "<p style=\"margin:0;\">Please click the button below to view the invitation details.</p>",
            DetailsTitle = "Interview details",
            ButtonText = "View invitation",
            ButtonUrl = interviewLink,
            NoteHtml = "If you have any questions, please reach out to the HR team."
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Position", positionName));
        email.Details.Add(new EmailDetail("Interview", "Final HR interview"));
        ApprovalSummary.ApplyYourTurn(email, approval, candidateName, positionName, isFinalStep: true);
        ApprovalSummary.AddTo(email, approval);

        return EmailLayout.Render(email);
    }

    public static string GetHRApprovalEmail(string hrName, string candidateName, string approvedBy, ApprovalSummary approval = null)
    {
        if (approval != null)
            return ApprovalNotice(hrName, candidateName, approval, $"Interview Approval - {candidateName}");

        return Outcome(
            hrName, candidateName, approvedBy.Replace("_", " "),
            title: $"Interview Approval - {candidateName}",
            interview: "First interview",
            badge: "Approved",
            tone: EmailTone.Success,
            heading: $"{candidateName} passed the first interview",
            sentence: $"The first interview with {EmailLayout.Strong(candidateName)} has been {EmailLayout.Strong("Approved")}",
            systemName: "CMS");
    }

    public static string GetHRSecondInterviewApprovalEmail(string hrName, string candidateName, string approvedBy, ApprovalSummary approval = null)
    {
        if (approval != null)
            return ApprovalNotice(hrName, candidateName, approval, $"Second Interview Approval - {candidateName}");

        return Outcome(
            hrName, candidateName, approvedBy.Replace("_", " "),
            title: $"Second Interview Approval - {candidateName}",
            interview: "Second interview",
            badge: "Approved",
            tone: EmailTone.Success,
            heading: $"{candidateName} passed the second interview",
            sentence: $"The second interview with {EmailLayout.Strong(candidateName)} has been {EmailLayout.Strong("Approved")}",
            systemName: "CMS");
    }

    public static string GetHRRejectionEmail(string hrName, string candidateName, string rejectedBy, string systemName = "CMS")
    {
        return Outcome(
            hrName, candidateName, rejectedBy.Replace("_", " "),
            title: $"Interview Rejection - {candidateName}",
            interview: "First interview",
            badge: "Rejected",
            tone: EmailTone.Danger,
            heading: $"{candidateName} was not approved",
            sentence: $"The first interview with {EmailLayout.Strong(candidateName)} has been {EmailLayout.Strong("rejected")}",
            systemName: systemName);
    }

    public static string GetHROnHoldEmail(string hrName, string candidateName, string putOnHoldBy, string systemName = "CMS")
    {
        return Outcome(
            hrName, candidateName, putOnHoldBy.Replace("_", " "),
            title: $"Interview On Hold - {candidateName}",
            interview: "Interview",
            badge: "On hold",
            tone: EmailTone.Warning,
            heading: $"{candidateName} has been put on hold",
            sentence: $"The interview with {EmailLayout.Strong(candidateName)} has been {EmailLayout.Strong("put on hold")}",
            systemName: systemName);
    }

    /// <summary>
    /// Approval notice in the same style as the "your turn" invitations: a green "✓ approved" banner,
    /// the candidate, and the approved step's result table. Informational: no action is asked of HR.
    /// </summary>
    private static string ApprovalNotice(string hrName, string candidateName, ApprovalSummary approval, string title)
    {
        string stage = approval.StageLabel;
        var email = new EmailContent
        {
            Title = title,
            Preheader = $"{stage} with {candidateName} approved",
            Tone = EmailTone.Success,
            Eyebrow = "Interview result",
            Badge = "Approved",
            Heading = $"{candidateName} passed the {stage.ToLowerInvariant()}",
            RecipientName = hrName,
            IntroHtml =
                approval.BannerHtml() +
                $"<p style=\"margin:0;\">The {EmailLayout.Encode(stage.ToLowerInvariant())} with {EmailLayout.Strong(candidateName)} has been " +
                $"{EmailLayout.Strong("approved")}. The result is below.</p>",
            DetailsTitle = "Interview details",
            SystemName = "CMS"
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Interview", stage));
        ApprovalSummary.AddTo(email, approval);

        return EmailLayout.Render(email);
    }

    private static string Outcome(
        string hrName,
        string candidateName,
        string decidedBy,
        string title,
        string interview,
        string badge,
        EmailTone tone,
        string heading,
        string sentence,
        string systemName)
    {
        var email = new EmailContent
        {
            Title = title,
            Preheader = $"{interview} with {candidateName}: {badge} by {decidedBy}",
            Tone = tone,
            Eyebrow = "Interview result",
            Badge = badge,
            Heading = heading,
            RecipientName = hrName,
            IntroHtml = $"<p style=\"margin:0;\">{sentence} by {EmailLayout.Strong(decidedBy)}.</p>",
            DetailsTitle = "Result",
            SystemName = systemName
        };
        email.Details.Add(new EmailDetail("Candidate", candidateName));
        email.Details.Add(new EmailDetail("Interview", interview));
        email.Details.Add(new EmailDetail("Outcome", badge));
        email.Details.Add(new EmailDetail("Decided by", decidedBy));

        return EmailLayout.Render(email);
    }
}
