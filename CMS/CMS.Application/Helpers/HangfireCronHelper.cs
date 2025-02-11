using System;
using Microsoft.Extensions.Configuration;
using TimeZoneConverter;

namespace CMS.Application.Helpers;

public static class HangfireCronHelper
{
    /// <summary>
    /// Generates a UTC-based cron expression for Hangfire to run a job at a specific hour in Jordan time.
    /// </summary>
    /// <param name="configuration">IConfiguration instance to fetch settings</param>
    /// <returns>UTC-based cron expression</returns>
    public static string GetJordanTimeCronExpression(IConfiguration configuration)
    {
        // Read Jordan job hour from appsettings.json
        int jordanHour = configuration.GetValue<int>("HangfireSettings:InterviewReminderHourJordan");

        // Get timezone information for Jordan
        TimeZoneInfo jordanTimeZone = TZConvert.GetTimeZoneInfo("Asia/Amman");

        // Get current UTC time
        DateTime utcNow = DateTime.UtcNow;

        // Get Jordan's UTC offset (handles Daylight Saving Time automatically)
        TimeSpan jordanUtcOffset = jordanTimeZone.GetUtcOffset(utcNow);

        // Convert Jordan time (e.g., 8 AM) to UTC
        int jobHourUTC = jordanHour - jordanUtcOffset.Hours;

        // Ensure the hour is in a valid range (0-23)
        if (jobHourUTC < 0) jobHourUTC += 24;

        // Return the generated cron expression
        return $"0 {jobHourUTC} * * *"; // Runs at specified time in Jordan Time
    }
}
