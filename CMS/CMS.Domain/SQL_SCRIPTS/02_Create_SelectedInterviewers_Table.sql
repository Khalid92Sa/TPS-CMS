-- =============================================
-- Script to create SelectedInterviewers table
-- This table stores selected interviewers for interviews that start from HR (reverse workflow)
-- =============================================

-- Create SelectedInterviewers table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SelectedInterviewers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SelectedInterviewers] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [InterviewId] INT NOT NULL,
        [FirstInterviewerId] NVARCHAR(450) NULL,
        [SecondInterviewerId] NVARCHAR(450) NULL,
        [ArchitectureInterviewerId] NVARCHAR(450) NULL,
        [CreatedBy] NVARCHAR(450) NULL,
        [CreatedOn] DATETIME2 NOT NULL,
        [ModifiedBy] NVARCHAR(450) NULL,
        [ModifiedOn] DATETIME2 NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsDelete] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_SelectedInterviewers] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SelectedInterviewers_Interviews] FOREIGN KEY ([InterviewId]) 
            REFERENCES [dbo].[Interviews] ([InterviewsId]) ON DELETE CASCADE
    );
    
    PRINT 'SelectedInterviewers table created successfully.';
END
ELSE
BEGIN
    PRINT 'SelectedInterviewers table already exists.';
END
GO

-- Create index on InterviewId for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SelectedInterviewers_InterviewId' AND object_id = OBJECT_ID('dbo.SelectedInterviewers'))
BEGIN
    CREATE INDEX [IX_SelectedInterviewers_InterviewId] ON [dbo].[SelectedInterviewers] ([InterviewId]);
    PRINT 'Index IX_SelectedInterviewers_InterviewId created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_SelectedInterviewers_InterviewId already exists.';
END
GO

