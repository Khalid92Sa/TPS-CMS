using System;

namespace CMS.Application.EmailTemplates;

/// <summary>
/// The approved result that led to an invitation (e.g. the first interview's approval shown in the
/// GM invitation). Rendered by <see cref="ApprovalSummary.AddTo"/> as its own section of the email.
/// </summary>
public sealed class ApprovalSummary
{
    /// <summary>Fallback name of the approved step, used when <see cref="StepNumber"/> is unknown.</summary>
    public string StageName { get; set; }

    /// <summary>Position of the approved interview in the candidate's interview chain (1 = first).</summary>
    public int StepNumber { get; set; }

    public string Result { get; set; } = "Approved";

    public string ApprovedBy { get; set; }

    /// <summary>Set when a delegate approved: the interviewer they acted for.</summary>
    public string OnBehalfOf { get; set; }

    public DateTime ApprovedOn { get; set; }

    public double? Score { get; set; }

    /// <summary>
    /// Turns an invitation into a message for the next interviewer: a green "{step} approved" banner
    /// and badge, a heading and text saying the previous interview is approved and they now need to
    /// conduct the next interview and enter the result, and an "Enter your result" button. The next
    /// interview is named by its order (Second, Third, ...) or "Final" for the last step. Does nothing
    /// when <paramref name="approval"/> is null.
    /// </summary>
    /// <param name="withHtml">Optional extra after "your turn", already HTML, e.g. ", together with &lt;strong&gt;Ali&lt;/strong&gt;,".</param>
    public static void ApplyYourTurn(EmailContent email, ApprovalSummary approval, string candidateName, string positionName, bool isFinalStep = false, string withHtml = null)
    {
        if (email == null || approval == null)
            return;

        string previous = approval.StageLabel;
        string nextStep = approval.NextStepName(isFinalStep);

        email.Title = $"{nextStep} Invitation - {candidateName}";
        email.Tone = EmailTone.Success;
        email.Eyebrow = "Action required";
        email.Badge = $"{previous} approved";
        email.Heading = $"{nextStep} with {candidateName}";
        email.Preheader = $"{previous} approved. It is now your turn to conduct the {nextStep} with {candidateName} and enter the result.";
        email.IntroHtml =
            approval.BannerHtml() +
            $"<p style=\"margin:0 0 14px;\">The {EmailLayout.Encode(previous.ToLowerInvariant())} has been {EmailLayout.Strong("approved")}. " +
            $"It is now your turn{withHtml} to conduct the {EmailLayout.Strong(nextStep)} with {EmailLayout.Strong(candidateName)} " +
            $"for the {EmailLayout.Strong(positionName)} position and {EmailLayout.Strong("enter the result")}.</p>" +
            $"<p style=\"margin:0;\">Please review the {EmailLayout.Encode(previous.ToLowerInvariant())} result below, then open the interview to enter yours.</p>";
        email.ButtonText = "Enter your result";

        // Keep the "Interview" row in the details table in step with the heading
        int row = email.Details.FindIndex(d => d.Label == "Interview");
        if (row >= 0)
            email.Details[row] = new EmailDetail("Interview", nextStep);
    }

    /// <summary>The approved step, e.g. "First interview" or "Second interview".</summary>
    public string StageLabel => StepNumber > 0
        ? $"{Ordinal(StepNumber)} interview"
        : string.IsNullOrWhiteSpace(StageName) ? "Previous interview" : StageName;

    /// <summary>The step after the approved one, e.g. "Second Interview", or "Final Interview" for the last step.</summary>
    public string NextStepName(bool isFinalStep = false)
    {
        if (isFinalStep)
            return "Final Interview";

        return StepNumber > 0 ? $"{Ordinal(StepNumber + 1)} Interview" : "Next Interview";
    }

    private static string Ordinal(int step) => step switch
    {
        1 => "First",
        2 => "Second",
        3 => "Third",
        4 => "Fourth",
        5 => "Fifth",
        6 => "Sixth",
        7 => "Seventh",
        8 => "Eighth",
        9 => "Ninth",
        10 => "Tenth",
        _ => $"{step}th"
    };

    /// <summary>Green "✓ {step} approved" banner with who approved it and when, for the top of an email.</summary>
    public string BannerHtml()
    {
        string approvedBy = string.IsNullOrWhiteSpace(OnBehalfOf) ? ApprovedBy : $"{ApprovedBy} (delegate for {OnBehalfOf})";
        string subtitle = string.IsNullOrWhiteSpace(approvedBy)
            ? $"on {ApprovedOn:dddd, MMMM dd, yyyy}"
            : $"by {approvedBy} on {ApprovedOn:dddd, MMMM dd, yyyy}";

        return EmailLayout.StatusBanner(EmailTone.Success, "✓", $"{StageLabel} approved", subtitle);
    }

    /// <summary>Adds the approval section to an email; does nothing when <paramref name="approval"/> is null.</summary>
    public static void AddTo(EmailContent email, ApprovalSummary approval)
    {
        if (email == null || approval == null)
            return;

        email.SecondaryDetailsTitle = $"{approval.StageLabel} result";
        email.SecondaryDetails.Add(new EmailDetail("Result", approval.Result));

        if (!string.IsNullOrWhiteSpace(approval.ApprovedBy))
        {
            string approvedBy = string.IsNullOrWhiteSpace(approval.OnBehalfOf)
                ? approval.ApprovedBy
                : $"{approval.ApprovedBy} (delegate for {approval.OnBehalfOf})";
            email.SecondaryDetails.Add(new EmailDetail("Approved by", approvedBy));
        }

        email.SecondaryDetails.Add(new EmailDetail("Approved on", approval.ApprovedOn.ToString("dddd, MMMM dd, yyyy hh:mm tt")));

        if (approval.Score.HasValue)
            email.SecondaryDetails.Add(new EmailDetail("Score", $"{approval.Score.Value:0.##} / 5"));
    }
}
