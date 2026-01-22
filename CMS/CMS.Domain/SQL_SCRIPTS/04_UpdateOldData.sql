USE [CMS_Training_QC];
GO

BEGIN TRANSACTION;

-- 1) Ensure StartFromHR is false for all old interviews
UPDATE Interviews
SET StartFromHR = 0
WHERE StartFromHR IS NULL OR StartFromHR = 1;
PRINT 'Updated StartFromHR to false for old interviews';

-- 2) Update root interviews (no parent) to Stage 1
UPDATE i
SET i.WorkflowStageId = 1
FROM Interviews i
WHERE i.ParentId IS NULL
  AND (i.WorkflowStageId IS NULL OR i.WorkflowStageId = 0);
PRINT 'Updated root interviews to Stage 1 (Initial Interview)';

-- 3) Update interviews whose parent is in Stage 1 to Stage 2
UPDATE i
SET i.WorkflowStageId = 2
FROM Interviews i
INNER JOIN Interviews p ON i.ParentId = p.InterviewsId
WHERE p.WorkflowStageId = 1
  AND (i.WorkflowStageId IS NULL OR i.WorkflowStageId = 0);
PRINT 'Updated interviews with Stage 1 parent to Stage 2 (Management Review)';

-- 4) Update interviews whose parent is in Stage 2 to Stage 3
UPDATE i
SET i.WorkflowStageId = 3
FROM Interviews i
INNER JOIN Interviews p ON i.ParentId = p.InterviewsId
WHERE p.WorkflowStageId = 2
  AND (i.WorkflowStageId IS NULL OR i.WorkflowStageId = 0);
PRINT 'Updated interviews with Stage 2 parent to Stage 3 (Final HR Interview)';

-- 5) Fallback for remaining interviews
UPDATE i
SET i.WorkflowStageId = CASE 
        WHEN p.ParentId IS NULL THEN 2
        ELSE 3
    END
FROM Interviews i
INNER JOIN Interviews p ON i.ParentId = p.InterviewsId
WHERE (i.WorkflowStageId IS NULL OR i.WorkflowStageId = 0)
  AND p.WorkflowStageId IS NULL;
PRINT 'Updated remaining interviews based on hierarchy inference';

-- ============================================================================
-- VERIFICATION
-- ============================================================================

PRINT '';
PRINT '=== VERIFICATION ===';
PRINT '';

SELECT 
    ws.Name AS StageName,
    COUNT(i.InterviewsId) AS InterviewCount
FROM WorkflowStages ws
LEFT JOIN Interviews i ON ws.Id = i.WorkflowStageId
WHERE ws.Id IN (1, 2, 3)
GROUP BY ws.Id, ws.Name
ORDER BY ws.Id;

SELECT COUNT(*) AS InterviewsWithoutStage
FROM Interviews
WHERE WorkflowStageId IS NULL OR WorkflowStageId = 0;

SELECT COUNT(*) AS HRFirstFlowInterviews
FROM Interviews
WHERE StartFromHR = 1;

PRINT '';
PRINT '=== SCRIPT COMPLETED ===';
PRINT 'ModifiedBy and ModifiedOn were preserved.';
PRINT 'Please review the verification results above.';

COMMIT TRANSACTION;
GO
