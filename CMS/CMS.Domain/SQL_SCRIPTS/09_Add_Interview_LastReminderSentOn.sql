-- Records when the overdue-result reminder was last sent for an interview, so the reminder
-- job sends at most one reminder per interview per day even if it runs more than once.
-- Safe to re-run.

IF COL_LENGTH('dbo.Interviews', 'LastReminderSentOn') IS NULL
    ALTER TABLE [dbo].[Interviews] ADD [LastReminderSentOn] DATETIME2 NULL;
GO
