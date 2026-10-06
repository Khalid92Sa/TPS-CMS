-- Records who removed a delegation and when.
-- Safe to re-run.

IF COL_LENGTH('dbo.InterviewDelegations', 'RemovedByUserId') IS NULL
    ALTER TABLE [dbo].[InterviewDelegations] ADD [RemovedByUserId] NVARCHAR(450) NULL;
GO

IF COL_LENGTH('dbo.InterviewDelegations', 'RemovedOn') IS NULL
    ALTER TABLE [dbo].[InterviewDelegations] ADD [RemovedOn] DATETIME2 NULL;
GO
