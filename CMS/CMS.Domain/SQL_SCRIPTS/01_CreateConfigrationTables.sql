-- =============================================
-- Create Workflow Tables
-- =============================================
-- This script creates the WorkflowStages and WorkflowConfigurations tables
-- for the dynamic interview workflow system

USE [CMS_Training]; -- Change database name if needed
GO

-- =============================================
-- 1. Create WorkflowStages Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WorkflowStages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WorkflowStages](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [StageOrder] [int] NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsFinalStage] [bit] NOT NULL DEFAULT(0),
        [IsParallelStage] [bit] NOT NULL DEFAULT(0),
        [CreatedBy] [nvarchar](450) NULL,
        [CreatedOn] [datetime2](7) NOT NULL,
        [ModifiedBy] [nvarchar](450) NULL,
        [ModifiedOn] [datetime2](7) NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT(1),
        [IsDelete] [bit] NOT NULL DEFAULT(0),
        CONSTRAINT [PK_WorkflowStages] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    PRINT 'WorkflowStages table created successfully.';
END
ELSE
BEGIN
    PRINT 'WorkflowStages table already exists.';
END
GO

-- =============================================
-- 2. Create WorkflowConfigurations Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WorkflowConfigurations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[WorkflowConfigurations](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [WorkflowStageId] [int] NOT NULL,
        [RoleName] [nvarchar](100) NOT NULL,
        [NextStageId] [int] NULL,
        [RequiresSecondInterviewer] [bit] NOT NULL DEFAULT(0),
        [SecondInterviewerRoleName] [nvarchar](100) NULL,
        [CanStopWorkflow] [bit] NOT NULL DEFAULT(0),
        [PositionId] [int] NULL,
        [TrackId] [int] NULL,
        [CreatedBy] [nvarchar](450) NULL,
        [CreatedOn] [datetime2](7) NOT NULL,
        [ModifiedBy] [nvarchar](450) NULL,
        [ModifiedOn] [datetime2](7) NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT(1),
        [IsDelete] [bit] NOT NULL DEFAULT(0),
        CONSTRAINT [PK_WorkflowConfigurations] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    -- Foreign Key: WorkflowStageId -> WorkflowStages.Id
    ALTER TABLE [dbo].[WorkflowConfigurations]
    ADD CONSTRAINT [FK_WorkflowConfigurations_WorkflowStages] 
    FOREIGN KEY([WorkflowStageId])
    REFERENCES [dbo].[WorkflowStages] ([Id])
    ON DELETE NO ACTION;
    
    -- Foreign Key: NextStageId -> WorkflowStages.Id
    ALTER TABLE [dbo].[WorkflowConfigurations]
    ADD CONSTRAINT [FK_WorkflowConfigurations_NextStage] 
    FOREIGN KEY([NextStageId])
    REFERENCES [dbo].[WorkflowStages] ([Id])
    ON DELETE NO ACTION;
    
    -- Foreign Key: PositionId -> Positions.Id (if Positions table exists)
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Positions]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[WorkflowConfigurations]
        ADD CONSTRAINT [FK_WorkflowConfigurations_Positions] 
        FOREIGN KEY([PositionId])
        REFERENCES [dbo].[Positions] ([Id])
        ON DELETE NO ACTION;
    END
    
    -- Foreign Key: TrackId -> Tracks.Id (if Tracks table exists)
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Tracks]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[WorkflowConfigurations]
        ADD CONSTRAINT [FK_WorkflowConfigurations_Tracks] 
        FOREIGN KEY([TrackId])
        REFERENCES [dbo].[Tracks] ([Id])
        ON DELETE NO ACTION;
    END
    
    -- Index for faster lookups
    CREATE NONCLUSTERED INDEX [IX_WorkflowConfigurations_Stage_Role] 
    ON [dbo].[WorkflowConfigurations] ([WorkflowStageId], [RoleName]);
    
    CREATE NONCLUSTERED INDEX [IX_WorkflowConfigurations_Position_Track] 
    ON [dbo].[WorkflowConfigurations] ([PositionId], [TrackId]);
    
    PRINT 'WorkflowConfigurations table created successfully.';
END
ELSE
BEGIN
    PRINT 'WorkflowConfigurations table already exists.';
END
GO

-- =============================================
-- 3. Add WorkflowStageId Column to Interviews Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Interviews]') AND name = 'WorkflowStageId')
BEGIN
    ALTER TABLE [dbo].[Interviews]
    ADD [WorkflowStageId] [int] NULL;
    
    -- Foreign Key: WorkflowStageId -> WorkflowStages.Id
    ALTER TABLE [dbo].[Interviews]
    ADD CONSTRAINT [FK_Interviews_WorkflowStages] 
    FOREIGN KEY([WorkflowStageId])
    REFERENCES [dbo].[WorkflowStages] ([Id])
    ON DELETE NO ACTION;
    
    -- Index for faster lookups
    CREATE NONCLUSTERED INDEX [IX_Interviews_WorkflowStageId] 
    ON [dbo].[Interviews] ([WorkflowStageId]);
    
    PRINT 'WorkflowStageId column added to Interviews table successfully.';
END
ELSE
BEGIN
    PRINT 'WorkflowStageId column already exists in Interviews table.';
END
GO

PRINT 'All workflow tables created successfully!';
GO

