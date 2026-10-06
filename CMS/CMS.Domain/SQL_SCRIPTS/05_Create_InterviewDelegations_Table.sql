-- =============================================
-- Temporary interview delegation.
-- Adds a delegated user for one interview. It does not replace the assigned interviewer.
-- Safe to re-run: creates the table, or upgrades the earlier replacement-style table.
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InterviewDelegations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[InterviewDelegations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [InterviewId] INT NOT NULL,
        [CandidateId] INT NOT NULL,
        [OriginalInterviewerId] NVARCHAR(450) NOT NULL,
        [DelegatedUserId] NVARCHAR(450) NOT NULL,
        [DelegatedByUserId] NVARCHAR(450) NOT NULL,
        [StartDate] DATETIME2 NOT NULL,
        [ExpirationDate] DATETIME2 NOT NULL,
        [WorkflowStageId] INT NULL,
        [InterviewerSlot] NVARCHAR(20) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL,
        [CreatedBy] NVARCHAR(450) NULL,
        [CreatedOn] DATETIME2 NOT NULL,
        [ModifiedBy] NVARCHAR(450) NULL,
        [ModifiedOn] DATETIME2 NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_InterviewDelegations_IsActive] DEFAULT 1,
        [IsDelete] BIT NOT NULL CONSTRAINT [DF_InterviewDelegations_IsDelete] DEFAULT 0,
        CONSTRAINT [PK_InterviewDelegations] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'InterviewDelegations table created successfully.';
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.InterviewDelegations', 'PreviousInterviewerId') IS NOT NULL
       AND COL_LENGTH('dbo.InterviewDelegations', 'OriginalInterviewerId') IS NULL
        EXEC sp_rename 'dbo.InterviewDelegations.PreviousInterviewerId', 'OriginalInterviewerId', 'COLUMN';

    IF COL_LENGTH('dbo.InterviewDelegations', 'NewInterviewerId') IS NOT NULL
       AND COL_LENGTH('dbo.InterviewDelegations', 'DelegatedUserId') IS NULL
        EXEC sp_rename 'dbo.InterviewDelegations.NewInterviewerId', 'DelegatedUserId', 'COLUMN';

    IF COL_LENGTH('dbo.InterviewDelegations', 'StartDate') IS NULL
        ALTER TABLE [dbo].[InterviewDelegations] ADD [StartDate] DATETIME2 NOT NULL CONSTRAINT [DF_InterviewDelegations_StartDate] DEFAULT (GETDATE());

    IF COL_LENGTH('dbo.InterviewDelegations', 'ExpirationDate') IS NULL
        ALTER TABLE [dbo].[InterviewDelegations] ADD [ExpirationDate] DATETIME2 NOT NULL CONSTRAINT [DF_InterviewDelegations_ExpirationDate] DEFAULT (DATEADD(DAY, 1, GETDATE()));

    IF COL_LENGTH('dbo.InterviewDelegations', 'Status') IS NULL
        ALTER TABLE [dbo].[InterviewDelegations] ADD [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_InterviewDelegations_Status] DEFAULT ('Active');

    PRINT 'InterviewDelegations table already exists and was upgraded if needed.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InterviewDelegations_InterviewId' AND object_id = OBJECT_ID('dbo.InterviewDelegations'))
BEGIN
    CREATE INDEX [IX_InterviewDelegations_InterviewId] ON [dbo].[InterviewDelegations] ([InterviewId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InterviewDelegations_CandidateId' AND object_id = OBJECT_ID('dbo.InterviewDelegations'))
BEGIN
    CREATE INDEX [IX_InterviewDelegations_CandidateId] ON [dbo].[InterviewDelegations] ([CandidateId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InterviewDelegations_InterviewId_DelegatedUserId' AND object_id = OBJECT_ID('dbo.InterviewDelegations'))
BEGIN
    CREATE INDEX [IX_InterviewDelegations_InterviewId_DelegatedUserId] ON [dbo].[InterviewDelegations] ([InterviewId], [DelegatedUserId]);
END
GO
