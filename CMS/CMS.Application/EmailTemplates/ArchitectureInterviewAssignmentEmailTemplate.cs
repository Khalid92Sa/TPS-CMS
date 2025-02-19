namespace CMS.Application.EmailTemplates;

public static class ArchitectureInterviewAssignmentEmailTemplate
{
    public static string GetAssignmentEmail(string interviewerName, string candidateName, string interviewDate, string interviewLink, string systemName = "CMS")
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>New Architecture Interview Assignment - {candidateName}</title>
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
                    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.15);
                    transition: background 0.3s, transform 0.2s;
                }}
                .button:hover {{
                    background-color: #1E4A6E;
                    transform: translateY(-2px);
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
                <div class='header'>{systemName} - Architecture Interview Assignment</div>

                <div class='content'>
                    <p>Dear <span class='highlight'>{interviewerName}</span>,</p>

                    <p>You have been assigned to conduct an <span class='highlight'>Architecture Interview</span> 
                    with <span class='highlight'>Saeed Abdelati</span>, for <span class='highlight'>{candidateName}</span> scheduled on 
                    <span class='highlight'>{interviewDate}</span>.</p>

                    <p>Please click the button below to view the interview details:</p>

                    <a href='{interviewLink}' class='button'>
                        View Interview Details
                    </a>

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

    public static string GetRemovalEmail(string interviewerName, string candidateName, string systemName = "CMS")
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Architecture Interview Assignment Removed - {candidateName}</title>
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
                    background-color: #D9534F;
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
                    color: #D9534F;
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
                <div class='header'>{systemName} - Architecture Interview Assignment Removed</div>

                <div class='content'>
                    <p>Dear <span class='highlight'>{interviewerName}</span>,</p>

                    <p>Your assignment for the <span class='highlight'>Architecture Interview</span> with <span class='highlight'>Saeed Abdelati</span>, for candidate 
                    <span class='highlight'>{candidateName}</span> has been removed.</p>

                    <p>If you have any questions, please contact the HR department.</p>

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
}
