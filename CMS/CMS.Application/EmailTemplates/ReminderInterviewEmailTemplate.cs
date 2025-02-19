using CMS.Application.DTOs;

namespace CMS.Application.EmailTemplates;

public static class ReminderInterviewEmailTemplate
{
    public static string GetReminderEmailTemplate(string interviewerName, string interviewerEmail, InterviewsDTO interview)
    {
        return $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Reminder: Interview Result Submission</title>
            <style>
                body {{
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    background-color: #f7f9fc;
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
                    padding: 25px 20px;
                    font-size: 16px;
                    color: #333;
                    line-height: 1.8;
                    text-align: left;
                }}
                .highlight {{
                    font-weight: bold;
                    color: #2A5C83;
                }}
                .info-box {{
                    background: #f1f6fc;
                    padding: 15px;
                    border-radius: 8px;
                    margin: 20px 0;
                    border-left: 5px solid #2A5C83;
                }}
                .button {{
                    display: inline-block;
                    width: 100%;
                    max-width: 250px;
                    background-color: #2A5C83;
                    color: #ffffff;
                    padding: 14px;
                    text-align: center;
                    border-radius: 6px;
                    text-decoration: none;
                    font-size: 16px;
                    font-weight: bold;
                    margin: 25px auto;
                    display: block;
                }}
                .button:hover {{
                    background-color: #1f4a6d;
                }}
                .footer {{
                    text-align: center;
                    font-size: 14px;
                    color: #6c757d;
                    padding: 15px;
                    border-top: 1px solid #e0e0e0;
                    background-color: #e9ecef;
                    border-bottom-left-radius: 10px;
                    border-bottom-right-radius: 10px;
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
                <div class='header'>
                    Candidate Management System
                </div>
                
                <div class='content'>
                    <h2 style='color: #2A5C83; margin-bottom: 20px;'>📢 Reminder: Interview Results Pending</h2>
                    
                    <p>Dear <span class='highlight'>{interviewerName.Replace("_", " ")}</span>,</p>
                    
                    <p>We hope this message finds you well! 🌟 This is a friendly reminder that we're still waiting for your valuable input regarding:</p>
                    
                    <div class='info-box'>
                        <p>📅 <span class='highlight'>Interview Date:</span> {interview.Date:dd MMMM yyyy}</p>
                        <p>👤 <span class='highlight'>Candidate:</span> {interview.FullName}</p>
                        <p>💼 <span class='highlight'>Position:</span> {interview.PositionName}</p>
                    </div>

                    <p>Your feedback is crucial in helping us make informed hiring decisions. Please take a moment to submit your evaluation:</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://apps.sssprocess.com:6134/interviews/{interview.InterviewsId}/addingresult' class='button'>
                            🚀 Submit Results Now
                        </a>
                    </div>

                    <p>If you've already submitted your results, please disregard this message. For any questions or if you need assistance, feel free to contact our HR team.</p>
                </div>

                <div class='footer'>
                    <p>Regards,</p>
                    <p>Sent by: <span class='highlight'>CMS</span></p>
                </div>
            </div>
        </body>
        </html>
        ";
    }
}
