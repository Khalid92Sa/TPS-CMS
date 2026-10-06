-- =============================================
-- Delegation duration options used by the pending-interview delegation page.
-- Safe to re-run.
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DelegationDurations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DelegationDurations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Amount] INT NOT NULL,
        [Unit] NVARCHAR(20) NOT NULL,
        [DurationMinutes] INT NOT NULL,
        [CreatedBy] NVARCHAR(450) NULL,
        [CreatedOn] DATETIME2 NOT NULL,
        [ModifiedBy] NVARCHAR(450) NULL,
        [ModifiedOn] DATETIME2 NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_DelegationDurations_IsActive] DEFAULT 1,
        [IsDelete] BIT NOT NULL CONSTRAINT [DF_DelegationDurations_IsDelete] DEFAULT 0,
        CONSTRAINT [PK_DelegationDurations] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'DelegationDurations table created successfully.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[DelegationDurations])
BEGIN
    INSERT INTO [dbo].[DelegationDurations]
        ([Name], [Amount], [Unit], [DurationMinutes], [CreatedOn], [ModifiedOn], [IsActive], [IsDelete])
    VALUES
        (N'1 hour', 1, N'Hours', 60, GETDATE(), GETDATE(), 1, 0),
        (N'4 hours', 4, N'Hours', 240, GETDATE(), GETDATE(), 1, 0),
        (N'1 day', 1, N'Days', 1440, GETDATE(), GETDATE(), 1, 0),
        (N'3 days', 3, N'Days', 4320, GETDATE(), GETDATE(), 1, 0),
        (N'1 week', 1, N'Weeks', 10080, GETDATE(), GETDATE(), 1, 0);
END
GO

IF COL_LENGTH('dbo.InterviewDelegations', 'DurationName') IS NULL
    ALTER TABLE [dbo].[InterviewDelegations] ADD [DurationName] NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.InterviewDelegations', 'DurationMinutes') IS NULL
    ALTER TABLE [dbo].[InterviewDelegations] ADD [DurationMinutes] INT NULL;
GO
