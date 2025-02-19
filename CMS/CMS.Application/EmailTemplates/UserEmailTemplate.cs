namespace CMS.Application.EmailTemplates;

public static class UserEmailTemplate
{
    public static string GetRegistrationEmailTemplate(string userName, string userEmail, string password)
    {
        return $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Welcome to CMS System</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f7fc;
                    margin: 0;
                    padding: 0;
                }}
                .email-container {{
                    max-width: 600px;
                    width: 100%;
                    margin: 20px auto;
                    background: #ffffff;
                    padding: 25px;
                    border-radius: 10px;
                    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                }}
                .header {{
                    background-color: #2A5C83;
                    color: #ffffff;
                    text-align: center;
                    padding: 20px;
                    font-size: 22px;
                    font-weight: bold;
                    border-top-left-radius: 10px;
                    border-top-right-radius: 10px;
                }}
                .content {{
                    padding: 20px;
                    font-size: 16px;
                    color: #333;
                    line-height: 1.8;
                }}
                .highlight {{
                    font-weight: bold;
                    color: #2A5C83;
                }}
                .button {{
                    display: block;
                    width: 100%;
                    max-width: 220px;
                    background-color: #2A5C83;
                    color: #ffffff !important;
                    padding: 14px;
                    text-align: center;
                    border-radius: 6px;
                    text-decoration: none;
                    font-size: 16px;
                    margin: 25px auto;
                    font-weight: bold;
                }}
                .button:hover {{
                    background-color: #1f4a6d;
                }}
                .footer {{
                    text-align: center;
                    font-size: 14px;
                    color: #777;
                    padding-top: 15px;
                    border-top: 1px solid #e0e0e0;
                }}
                /* Mobile Optimization */
                @media (max-width: 600px) {{
                    .email-container {{
                        padding: 15px;
                        width: 90%;
                    }}
                    .header {{
                        font-size: 18px;
                        padding: 15px;
                    }}
                    .content {{
                        font-size: 14px;
                    }}
                    .button {{
                        width: 100%;
                        max-width: none;
                        padding: 12px;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='header'>Welcome to CMS System</div>
                <div class='content'>
                    <p>Dear <span class='highlight'>{userName.Replace("_", " ")}</span>,</p>
                    <p>Your account has been successfully created.</p>
                    <ul>
                        <li><strong>Username:</strong> {userName}</li>
                        <li><strong>Email:</strong> {userEmail}</li>
                        <li><strong>Password:</strong> {password}</li>
                    </ul>
                    <p>To log in to your account, click the button below:</p>
                    <a href='https://apps.sssprocess.com:6134/' class='button'>Login to Your Account</a>
                    <p>If you did not request this account creation, please contact support immediately.</p>
                </div>
                <div class='footer'>
                    <p>Regards,</p>
                    <p>Sent by: <span class='highlight'>CMS</span></p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetAccountUpdateEmailTemplate(string userName, string userEmail, string password)
    {
        return $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Account Update Notification</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f7fc;
                    margin: 0;
                    padding: 0;
                }}
                .email-container {{
                    max-width: 600px;
                    width: 100%;
                    margin: 20px auto;
                    background: #ffffff;
                    padding: 25px;
                    border-radius: 10px;
                    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                }}
                .header {{
                    background-color: #2A5C83;
                    color: #ffffff;
                    text-align: center;
                    padding: 20px;
                    font-size: 22px;
                    font-weight: bold;
                    border-top-left-radius: 10px;
                    border-top-right-radius: 10px;
                }}
                .content {{
                    padding: 20px;
                    font-size: 16px;
                    color: #333;
                    line-height: 1.8;
                }}
                .highlight {{
                    font-weight: bold;
                    color: #2A5C83;
                }}
                .button {{
                    display: block;
                    width: 100%;
                    max-width: 220px;
                    background-color: #2A5C83;
                    color: #ffffff !important;
                    padding: 14px;
                    text-align: center;
                    border-radius: 6px;
                    text-decoration: none;
                    font-size: 16px;
                    margin: 25px auto;
                    font-weight: bold;
                }}
                .button:hover {{
                    background-color: #1f4a6d;
                }}
                .footer {{
                    text-align: center;
                    font-size: 14px;
                    color: #777;
                    padding-top: 15px;
                    border-top: 1px solid #e0e0e0;
                }}
                /* Mobile Optimization */
                @media (max-width: 600px) {{
                    .email-container {{
                        padding: 15px;
                        width: 90%;
                    }}
                    .header {{
                        font-size: 18px;
                        padding: 15px;
                    }}
                    .content {{
                        font-size: 14px;
                    }}
                    .button {{
                        width: 100%;
                        max-width: none;
                        padding: 12px;
                    }}
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='header'>Account Update Notification</div>
                <div class='content'>
                    <p>Dear <span class='highlight'>{userName.Replace("_", " ")}</span>,</p>
                    <p>Your account details have been successfully updated.</p>
                    <ul>
                        <li><strong>Username:</strong> {userName}</li>
                        <li><strong>Email:</strong> {userEmail}</li>
                        <li><strong>Password:</strong> {password}</li>
                    </ul>
                    <p>To log in to your account, click the button below:</p>
                    <a href='https://apps.sssprocess.com:6134/' class='button'>Login to Your Account</a>
                    <p>If you did not request this change, please contact support immediately.</p>
                </div>
                <div class='footer'>
                    <p>Regards,</p>
                    <p>Sent by: <span class='highlight'>CMS</span></p>
                </div>
            </div>
        </body>
        </html>";
    }
}
