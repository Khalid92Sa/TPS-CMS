
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Interviews]') AND name = 'StartFromHR')
BEGIN
    ALTER TABLE [dbo].[Interviews]
    ADD [StartFromHR] BIT NOT NULL DEFAULT 0;
    PRINT 'Column StartFromHR added to Interviews table';
END
ELSE
BEGIN
    PRINT 'Column StartFromHR already exists in Interviews table';
END
GO

-- ========== NORMAL WORKFLOW STAGES ==========

-- Enable IDENTITY_INSERT for WorkflowStages
SET IDENTITY_INSERT [dbo].[WorkflowStages] ON;
GO

-- Stage 1: Initial Interview
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowStages] WHERE Id = 1)
BEGIN
    INSERT INTO [dbo].[WorkflowStages] 
    (Id, Name, StageOrder, Description, IsFinalStage, IsParallelStage, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES 
    (1, 'Initial Interview', 1, 'First interview - can be conducted by any role', 0, 0, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowStages]
    SET Name = 'Initial Interview',
        StageOrder = 1,
        Description = 'First interview - can be conducted by any role',
        IsFinalStage = 0,
        IsParallelStage = 0,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 1;
END
GO

-- Stage 2: Management Review
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowStages] WHERE Id = 2)
BEGIN
    INSERT INTO [dbo].[WorkflowStages] 
    (Id, Name, StageOrder, Description, IsFinalStage, IsParallelStage, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES 
    (2, 'Management Review', 2, 'Management and architecture review', 0, 1, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowStages]
    SET Name = 'Management Review',
        StageOrder = 2,
        Description = 'Management and architecture review',
        IsFinalStage = 0,
        IsParallelStage = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 2;
END
GO

-- Stage 3: Final HR Interview
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowStages] WHERE Id = 3)
BEGIN
    INSERT INTO [dbo].[WorkflowStages] 
    (Id, Name, StageOrder, Description, IsFinalStage, IsParallelStage, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES 
    (3, 'Final HR Interview', 3, 'Final HR interview and decision', 1, 0, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowStages]
    SET Name = 'Final HR Interview',
        StageOrder = 3,
        Description = 'Final HR interview and decision',
        IsFinalStage = 1,
        IsParallelStage = 0,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 3;
END
GO

-- ========== REVERSE WORKFLOW STAGES ==========

-- Stage 4: HR Initial Interview
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowStages] WHERE Id = 4)
BEGIN
    INSERT INTO [dbo].[WorkflowStages] 
    (Id, Name, StageOrder, Description, IsFinalStage, IsParallelStage, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES 
    (4, 'HR Initial Interview', 1, 'HR interview - first stage in reverse workflow', 0, 0, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowStages]
    SET Name = 'HR Initial Interview',
        StageOrder = 1,
        Description = 'HR interview - first stage in reverse workflow',
        IsFinalStage = 0,
        IsParallelStage = 0,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 4;
END
GO

-- Stage 5: Interviewers Review
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowStages] WHERE Id = 5)
BEGIN
    INSERT INTO [dbo].[WorkflowStages] 
    (Id, Name, StageOrder, Description, IsFinalStage, IsParallelStage, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES 
    (5, 'Interviewers Review', 2, 'Interviewers review - second stage in reverse workflow', 0, 0, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowStages]
    SET Name = 'Interviewers Review',
        StageOrder = 2,
        Description = 'Interviewers review - second stage in reverse workflow',
        IsFinalStage = 0,
        IsParallelStage = 0,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 5;
END
GO

-- Stage 6: GM Final Review
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowStages] WHERE Id = 6)
BEGIN
    INSERT INTO [dbo].[WorkflowStages] 
    (Id, Name, StageOrder, Description, IsFinalStage, IsParallelStage, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES 
    (6, 'GM Final Review', 3, 'GM final review - last stage in reverse workflow', 1, 0, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowStages]
    SET Name = 'GM Final Review',
        StageOrder = 3,
        Description = 'GM final review - last stage in reverse workflow',
        IsFinalStage = 1,
        IsParallelStage = 0,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 6;
END
GO

-- Disable IDENTITY_INSERT for WorkflowStages
SET IDENTITY_INSERT [dbo].[WorkflowStages] OFF;
GO

-- ============================================
-- WORKFLOW CONFIGURATIONS DATA
-- ============================================

-- Enable IDENTITY_INSERT for WorkflowConfigurations
SET IDENTITY_INSERT [dbo].[WorkflowConfigurations] ON;
GO

-- ========== NORMAL WORKFLOW CONFIGURATIONS ==========

-- Configuration 1: Interviewer (Stage 1) → Stage 2 (GM + Architecture)
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 1)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (1, 1, 'Interviewer', 2, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 1,
        RoleName = 'Interviewer',
        NextStageId = 2,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 1;
END
GO

-- Configuration 2: General Manager (Stage 1) → Stage 3 (HR) - Direct to HR
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 2)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (2, 1, 'General Manager', 3, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 1,
        RoleName = 'General Manager',
        NextStageId = 3,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 2;
END
GO

-- Configuration 3: Solution Architecture (Stage 1) → Stage 2 (GM)
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 3)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (3, 1, 'Solution Architecture', 2, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 1,
        RoleName = 'Solution Architecture',
        NextStageId = 2,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 3;
END
GO

-- Configuration 4: General Manager (Stage 2) → Stage 3 (HR)
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 4)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (4, 2, 'General Manager', 3, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 2,
        RoleName = 'General Manager',
        NextStageId = 3,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 4;
END
GO

-- Configuration 5: Solution Architecture (Stage 2) → Stage 2 (GM) - Logic handles if GM already done
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 5)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (5, 2, 'Solution Architecture', 2, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 2,
        RoleName = 'Solution Architecture',
        NextStageId = 2,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 5;
END
GO

-- Configuration 6: HR Manager (Stage 3) → NULL (END - Final stage)
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 6)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (6, 3, 'HR Manager', NULL, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 3,
        RoleName = 'HR Manager',
        NextStageId = NULL,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 6;
END
GO

-- ========== REVERSE WORKFLOW CONFIGURATIONS ==========

-- Configuration 7: HR Manager (Stage 4) → Stage 5 (Interviewers Review)
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 7)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (7, 4, 'HR Manager', 5, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 4,
        RoleName = 'HR Manager',
        NextStageId = 5,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 7;
END
GO

-- Configuration 8: Interviewer (Stage 5) → Stage 6 (GM Final Review)
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 8)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (8, 5, 'Interviewer', 6, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 5,
        RoleName = 'Interviewer',
        NextStageId = 6,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 8;
END
GO

-- Configuration 9: General Manager (Stage 5) → Stage 4 (HR) - When GM is selected as interviewer
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 9)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (9, 5, 'General Manager', 4, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 5,
        RoleName = 'General Manager',
        NextStageId = 4,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 9;
END
GO

-- Configuration 10: General Manager (Stage 6) → NULL (END - Final stage in reverse workflow)
IF NOT EXISTS (SELECT * FROM [dbo].[WorkflowConfigurations] WHERE Id = 10)
BEGIN
    INSERT INTO [dbo].[WorkflowConfigurations]
    (Id, WorkflowStageId, RoleName, NextStageId, RequiresSecondInterviewer, CanStopWorkflow, PositionId, TrackId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, IsActive, IsDelete)
    VALUES
    (10, 6, 'General Manager', NULL, 0, 1, NULL, NULL, GETDATE(), 'System', GETDATE(), 'System', 1, 0);
END
ELSE
BEGIN
    UPDATE [dbo].[WorkflowConfigurations]
    SET WorkflowStageId = 6,
        RoleName = 'General Manager',
        NextStageId = NULL,
        RequiresSecondInterviewer = 0,
        CanStopWorkflow = 1,
        ModifiedOn = GETDATE(),
        ModifiedBy = 'System'
    WHERE Id = 10;
END
GO

-- Disable IDENTITY_INSERT for WorkflowConfigurations
SET IDENTITY_INSERT [dbo].[WorkflowConfigurations] OFF;
GO

-- ============================================
-- VERIFICATION QUERIES
-- ============================================

PRINT '';
PRINT '========================================';
PRINT 'WORKFLOW STAGES DATA';
PRINT '========================================';
SELECT 
    Id,
    Name,
    StageOrder,
    Description,
    CASE WHEN IsFinalStage = 1 THEN 'Yes' ELSE 'No' END AS IsFinalStage,
    CASE WHEN IsParallelStage = 1 THEN 'Yes' ELSE 'No' END AS IsParallelStage
FROM [dbo].[WorkflowStages]
ORDER BY Id;

PRINT '';
PRINT '========================================';
PRINT 'WORKFLOW CONFIGURATIONS DATA';
PRINT '========================================';
SELECT 
    wc.Id,
    ws.Name AS StageName,
    wc.RoleName,
    ws2.Name AS NextStageName,
    CASE WHEN wc.RequiresSecondInterviewer = 1 THEN 'Yes' ELSE 'No' END AS RequiresSecondInterviewer,
    CASE WHEN wc.CanStopWorkflow = 1 THEN 'Yes' ELSE 'No' END AS CanStopWorkflow
FROM [dbo].[WorkflowConfigurations] wc
LEFT JOIN [dbo].[WorkflowStages] ws ON wc.WorkflowStageId = ws.Id
LEFT JOIN [dbo].[WorkflowStages] ws2 ON wc.NextStageId = ws2.Id
ORDER BY wc.Id;

PRINT '';
PRINT '========================================';
PRINT 'SUMMARY';
PRINT '========================================';
SELECT 
    'WorkflowStages' AS TableName,
    COUNT(*) AS TotalRecords
FROM [dbo].[WorkflowStages]
UNION ALL
SELECT 
    'WorkflowConfigurations' AS TableName,
    COUNT(*) AS TotalRecords
FROM [dbo].[WorkflowConfigurations];

PRINT '';
PRINT 'Script completed successfully!';
PRINT '';
PRINT 'Expected Results:';
PRINT '- WorkflowStages: 6 records (Stages 1-6)';
PRINT '- WorkflowConfigurations: 10 records (Configurations 1-10)';

