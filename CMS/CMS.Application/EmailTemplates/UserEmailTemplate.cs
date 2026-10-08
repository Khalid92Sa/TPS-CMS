namespace CMS.Application.EmailTemplates;

public static class UserEmailTemplate
{
    private const string LoginUrl = "https://apps.sssprocess.com:6134/";

    public static string GetRegistrationEmailTemplate(string userName, string userEmail, string password)
    {
        return Build(
            userName, userEmail, password,
            title: "Welcome to CMS System",
            eyebrow: "Account created",
            badge: "New account",
            tone: EmailTone.Success,
            heading: "Welcome to the Candidate Management System",
            message: "Your account has been successfully created. Use the details below to sign in.",
            securityNote: "If you did not request this account creation, please contact support immediately.");
    }

    public static string GetAccountUpdateEmailTemplate(string userName, string userEmail, string password)
    {
        return Build(
            userName, userEmail, password,
            title: "Account Update Notification",
            eyebrow: "Account updated",
            badge: "Updated",
            tone: EmailTone.Info,
            heading: "Your account details were updated",
            message: "Your account details have been successfully updated. Your current sign-in details are below.",
            securityNote: "If you did not request this change, please contact support immediately.");
    }

    private static string Build(
        string userName,
        string userEmail,
        string password,
        string title,
        string eyebrow,
        string badge,
        EmailTone tone,
        string heading,
        string message,
        string securityNote)
    {
        var email = new EmailContent
        {
            Title = title,
            Preheader = message,
            Tone = tone,
            Eyebrow = eyebrow,
            Badge = badge,
            Heading = heading,
            RecipientName = userName.Replace("_", " "),
            IntroHtml = $"<p style=\"margin:0 0 14px;\">{EmailLayout.Encode(message)}</p>" +
                        "<p style=\"margin:0;\">To log in to your account, click the button below.</p>",
            DetailsTitle = "Your sign-in details",
            ButtonText = "Login to your account",
            ButtonUrl = LoginUrl,
            NoteHtml = $"<strong>Keep these details private.</strong> {EmailLayout.Encode(securityNote)}"
        };

        email.Details.Add(new EmailDetail("Username", userName));
        email.Details.Add(new EmailDetail("Email", userEmail));
        email.Details.Add(new EmailDetail("Password", password, monospace: true));

        return EmailLayout.Render(email);
    }
}
