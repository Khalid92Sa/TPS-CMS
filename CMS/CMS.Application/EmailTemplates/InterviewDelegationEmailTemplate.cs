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
        string expires = expirationDate.ToString("dddd, MMMM dd, yyyy hh:mm tt");

        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Temporary interview delegation - {candidateName}</title>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f7fc;
                    margin: 0;
                    padding: 20px;
                }}
                .email-container {{
                    max-width: 600px;
                    margin: 0 auto;
                    background: #ffffff;
                    padding: 25px;
                    border-radius: 10px;
                    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                }}
                .header {{
                    background-color: #2A5C83;
                    color: #ffffff;
                    text-align: center;
                    padding: 15px;
                    font-size: 20px;
                    font-weight: bold;
                    border-top-left-radius: 10px;
                    border-top-right-radius: 10px;
                }}
                .content {{
                    padding: 20px;
                    font-size: 16px;
                    color: #333;
                    line-height: 1.6;
                }}
                .highlight {{
                    font-weight: bold;
                    color: #2A5C83;
                }}
                .button {{
                    display: inline-block;
                    background-color: #2A5C83;
                    color: white;
                    padding: 15px 30px;
                    font-size: 16px;
                    font-weight: bold;
                    border-radius: 8px;
                    text-decoration: none;
                }}
                .footer {{
                    text-align: center;
                    font-size: 14px;
                    color: #777;
                    padding-top: 15px;
                    border-top: 1px solid #e0e0e0;
                }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='header'>{systemName} - Temporary Interview Delegation</div>
                <div class='content'>
                    <p>Dear <span class='highlight'>{recipientName}</span>,</p>
                    <p>You have been given temporary access to enter the score for a pending interview. The original interviewer remains assigned.</p>
                    <p>
                        Candidate: <span class='highlight'>{candidateName}</span><br/>
                        Position: <span class='highlight'>{positionName}</span><br/>
                        Original interviewer: <span class='highlight'>{originalInterviewerName}</span><br/>
                        Delegated user: <span class='highlight'>{recipientName}</span><br/>
                        Delegation starts: <span class='highlight'>{starts}</span><br/>
                        Delegation expires: <span class='highlight'>{expires}</span>
                    </p>
                    <p>This access is only for this interview and ends at the expiration time. Your account role is not changed.</p>
                    <p><a href='{interviewLink}' class='button'>Open Pending Interview</a></p>
                    <p>Thank you.</p>
                </div>
                <div class='footer'>
                    <p>Regards,</p>
                    <p>Sent by: <span class='highlight'>{systemName}</span></p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetRemovalEmail(
        string recipientName,
        string candidateName,
        string positionName,
        string systemName = "CMS")
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Interview delegation removed - {candidateName}</title>
        </head>
        <body>
            <p>Dear {recipientName},</p>
            <p>Your temporary access to score {candidateName} ({positionName}) has been removed. The previous interview link no longer opens this interview for you.</p>
            <p>Regards,<br/>{systemName}</p>
        </body>
        </html>";
    }
}
