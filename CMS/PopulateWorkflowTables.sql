USE [CMS_Training_QC]; -- Replace with your actual database name
GO

BEGIN TRANSACTION;

-- First, ensure StartFromHR is false for all old interviews
UPDATE Interviews
SET StartFromHR = 0
WHERE StartFromHR IS NULL OR StartFromHR = 1;
PRINT 'Updated StartFromHR to false for old interviews';

-- Update root interviews (no parent) to Stage 1
UPDATE i
SET i.WorkflowStageId = 1,
    i.ModifiedOn = GETDATE(),
    i.ModifiedBy = 'System'
FROM Interviews i
WHERE i.ParentId IS NULL
  AND (i.WorkflowStageId IS NULL OR i.WorkflowStageId = 0);
PRINT 'Updated root interviews to Stage 1 (Initial Interview)';

-- Update interviews whose parent is in Stage 1 to Stage 2
UPDATE i
SET i.WorkflowStageId = 2,
    i.ModifiedOn = GETDATE(),
    i.ModifiedBy = 'System'
FROM Interviews i
INNER JOIN Interviews p ON i.ParentId = p.InterviewsId
WHERE p.WorkflowStageId = 1
  AND (i.WorkflowStageId IS NULL OR i.WorkflowStageId = 0);
PRINT 'Updated interviews with Stage 1 parent to Stage 2 (Management Review)';

-- Update interviews whose parent is in Stage 2 to Stage 3
UPDATE i
SET i.WorkflowStageId = 3,
    i.ModifiedOn = GETDATE(),
    i.ModifiedBy = 'System'
FROM Interviews i
INNER JOIN Interviews p ON i.ParentId = p.InterviewsId
WHERE p.WorkflowStageId = 2
  AND (i.WorkflowStageId IS NULL OR i.WorkflowStageId = 0);
PRINT 'Updated interviews with Stage 2 parent to Stage 3 (Final HR Interview)';

-- Handle any remaining interviews that might have been missed
-- If they have a parent but parent's stage is not set, try to infer from hierarchy
-- This is a fallback for edge cases
UPDATE i
SET i.WorkflowStageId = CASE 
    WHEN p.ParentId IS NULL THEN 2  -- Parent is root, so this is Stage 2
    ELSE 3  -- Parent has a parent, so this is likely Stage 3
END,
    i.ModifiedOn = GETDATE(),
    i.ModifiedBy = 'System'
FROM Interviews i
INNER JOIN Interviews p ON i.ParentId = p.InterviewsId
WHERE (i.WorkflowStageId IS NULL OR i.WorkflowStageId = 0)
  AND p.WorkflowStageId IS NULL;
PRINT 'Updated remaining interviews based on hierarchy inference';

-- ============================================================================
-- STEP 4: Verification Queries
-- ============================================================================

PRINT '';
PRINT '=== VERIFICATION ===';
PRINT '';

-- Count interviews by stage
SELECT 
    ws.Name AS StageName,
    COUNT(i.InterviewsId) AS InterviewCount
FROM WorkflowStages ws
LEFT JOIN Interviews i ON ws.Id = i.WorkflowStageId
WHERE ws.Id IN (1, 2, 3)
GROUP BY ws.Id, ws.Name
ORDER BY ws.Id;

-- Count interviews without WorkflowStageId
SELECT COUNT(*) AS InterviewsWithoutStage
FROM Interviews
WHERE WorkflowStageId IS NULL OR WorkflowStageId = 0;

-- Count interviews with StartFromHR = true (should be 0 for old data)
SELECT COUNT(*) AS HRFirstFlowInterviews
FROM Interviews
WHERE StartFromHR = 1;

PRINT '';
PRINT '=== SCRIPT COMPLETED ===';
PRINT 'Please review the verification results above.';
PRINT 'If there are interviews without WorkflowStageId, you may need to manually review them.';

COMMIT TRANSACTION;
GO

