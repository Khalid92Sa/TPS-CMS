using System;
using Microsoft.Extensions.Configuration;
using TimeZoneConverter;

namespace CMS.Application.Helpers;

public static class HangfireCronHelper
{
    public static TimeZoneInfo JordanTimeZone => TZConvert.GetTimeZoneInfo("Asia/Amman");

    /// <summary>
    /// Cron expression for the interview reminder: the configured hour, Sunday to Thursday only
    /// (never Friday or Saturday). Register it with RecurringJobOptions.TimeZone = JordanTimeZone so
    /// the hour and the day are both Jordan local time, including across daylight-saving changes.
    /// </summary>
    public static string GetJordanWorkdayCronExpression(IConfiguration configuration)
    {
        int jordanHour = configuration.GetValue<int>("HangfireSettings:InterviewReminderHourJordan");
        return $"0 {jordanHour} * * 0-4";
    }

    /// <summary>True on Friday and Saturday in Jordan, when no reminders are sent.</summary>
    public static bool IsJordanWeekend(DateTime utcNow)
    {
        DayOfWeek day = TimeZoneInfo.ConvertTimeFromUtc(utcNow, JordanTimeZone).DayOfWeek;
        return day == DayOfWeek.Friday || day == DayOfWeek.Saturday;
    }

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
