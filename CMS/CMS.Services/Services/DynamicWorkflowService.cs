using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class DynamicWorkflowService : IDynamicWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IInterviewsRepository _interviewsRepository;
    private readonly IStatusRepository _statusRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ISelectedInterviewersRepository _selectedInterviewersRepository;

    public DynamicWorkflowService(
        IWorkflowRepository workflowRepository,
        IInterviewsRepository interviewsRepository,
        IStatusRepository statusRepository,
        UserManager<IdentityUser> userManager,
        ISelectedInterviewersRepository selectedInterviewersRepository)
    {
        _workflowRepository = workflowRepository;
        _interviewsRepository = interviewsRepository;
        _statusRepository = statusRepository;
        _userManager = userManager;
        _selectedInterviewersRepository = selectedInterviewersRepository;
    }

    /// <summary>
    /// Creates next stage interviews based on the completed interview's role and status
    /// </summary>
    public async Task<Result<bool>> CreateNextStageInterviewsAsync(
        int completedInterviewId,
        string completedByRole,
        int candidateId,
        int positionId,
        int trackId,
        DateTime interviewDate,
        string createdByUserId)
    {
        try
        {
            var completedInterview = await _interviewsRepository.GetById(completedInterviewId);
            if (completedInterview == null)
                return Result<bool>.Failure(false, "Completed interview not found");

            // Get status
            var status = await _statusRepository.GetById(completedInterview.StatusId);
            if (status.Code != StatusCode.Approved)
                return Result<bool>.Success(true); // Not approved, no next interviews

            // Determine current stage
            int currentStageId = completedInterview.WorkflowStageId ?? (int)EnumWorkflowStage.InitialInterview;

            // Check if this is a reverse workflow (StartFromHR = true)
            bool isReverseWorkflow = completedInterview.StartFromHR;

            // Get pending status once for reuse
            var pendingStatus = await _statusRepository.GetByCode(StatusCode.Pending);

            // Get OnHold status for cancelling other interviewers (or use Rejected if OnHold doesn't exist)
            Status onHoldStatus = null;
            try
            {
                onHoldStatus = await _statusRepository.GetByCode(StatusCode.OnHold);
            }
            catch
            {
                // If OnHold doesn't exist, use Rejected status to mark as cancelled
                onHoldStatus = await _statusRepository.GetByCode(StatusCode.Rejected);
            }

            // ========== REVERSE WORKFLOW HANDLING ==========
            if (isReverseWorkflow)
            {
                return await HandleReverseWorkflow(
                    completedInterview,
                    completedInterviewId,
                    completedByRole,
                    candidateId,
                    positionId,
                    trackId,
                    interviewDate,
                    createdByUserId,
                    currentStageId,
                    pendingStatus,
                    onHoldStatus);
            }

            // SCENARIO 1 & 2: Handle multiple interviewers - cancel others when one approves
            if (currentStageId == (int)EnumWorkflowStage.InitialInterview && completedInterview.ParentId == null)
            {
                // Cancel other interviewers at Stage 1 (they can't submit after first one approves)
                await CancelOtherInterviewersAtSameStage(candidateId, positionId, currentStageId, completedInterviewId, onHoldStatus.Id);

                // Get GM user IDs once for reuse
                var gmUsers = await _userManager.GetUsersInRoleAsync("General Manager");
                var gmUserIds = gmUsers.Select(u => u.Id).ToList();
                bool isGMInInterview = (!string.IsNullOrEmpty(completedInterview.InterviewerId) && gmUserIds.Contains(completedInterview.InterviewerId)) ||
                                      (!string.IsNullOrEmpty(completedInterview.SecondInterviewerId) && gmUserIds.Contains(completedInterview.SecondInterviewerId));

                // If GM is one of the interviewers and GM approves, go directly to HR (skip Stage 2)
                if (isGMInInterview && completedByRole == "General Manager")
                {
                    // GM approved and GM is already involved, so go directly to HR
                    await CreateSingleInterviewAsync(
                        (int)EnumWorkflowStage.FinalHRInterview, // Stage 3 (HR)
                        "HR Manager",
                        candidateId,
                        positionId,
                        trackId,
                        interviewDate,
                        completedInterviewId,
                        createdByUserId,
                        pendingStatus.Id);
                    return Result<bool>.Success(true);
                }

                // Check if ArchitectureInterviewerId is set (selected Architecture interviewer)
                bool hasArchitectureInterviewer = !string.IsNullOrEmpty(completedInterview.ArchitectureInterviewerId);

                // Check if Architecture interviewer is assigned as Interviewer #1 or #2 (not selected via ArchitectureInterviewerId)
                // This means the Architecture interviewer's ID is in InterviewerId or SecondInterviewerId
                bool isArchiAssignedAsInterviewer = false;
                if (completedByRole == "Solution Architecture")
                {
                    // Check if InterviewerId or SecondInterviewerId has "Solution Architecture" role
                    string interviewer1Role = await GetInterviewerRole(completedInterview.InterviewerId);
                    string interviewer2Role = await GetInterviewerRole(completedInterview.SecondInterviewerId);
                    
                    isArchiAssignedAsInterviewer = interviewer1Role == "Solution Architecture" 
                        || interviewer2Role == "Solution Architecture";
                }

                if (completedByRole == "Solution Architecture" && hasArchitectureInterviewer && isArchiAssignedAsInterviewer)
                {
                    // SCENARIO: Architecture interviewer (assigned as Interviewer #1 or #2, AND also selected) approved
                    // InterviewerId = GM, SecondInterviewerId = Architecture interviewer (from ArchitectureInterviewerId)
                    await CreateParallelInterviewsAsync(
                        (int)EnumWorkflowStage.ManagementReview, // Stage 2
                        candidateId,
                        positionId,
                        trackId,
                        interviewDate,
                        completedInterviewId,
                        createdByUserId,
                        pendingStatus.Id);
                    return Result<bool>.Success(true);
                }
                else if (completedByRole == "Solution Architecture" && !hasArchitectureInterviewer && isArchiAssignedAsInterviewer)
                {
                    // SCENARIO: Architecture interviewer (assigned as Interviewer #1 or #2, not selected) approved
                    // Go to GM only: InterviewerId = GM, SecondInterviewerId = null
                    await CreateSingleInterviewAsync(
                        (int)EnumWorkflowStage.ManagementReview, // Stage 2 (GM)
                        "General Manager",
                        candidateId,
                        positionId,
                        trackId,
                        interviewDate,
                        completedInterviewId,
                        createdByUserId,
                        pendingStatus.Id);
                    return Result<bool>.Success(true);
                }

                // If GM is one of the interviewers and Interviewer approves, go directly to HR (skip Stage 2)
                if (isGMInInterview && completedByRole == "Interviewer")
                {
                    // GM is already involved, so go directly to HR
                    await CreateSingleInterviewAsync(
                        (int)EnumWorkflowStage.FinalHRInterview, // Stage 3 (HR)
                        "HR Manager",
                        candidateId,
                        positionId,
                        trackId,
                        interviewDate,
                        completedInterviewId,
                        createdByUserId,
                        pendingStatus.Id);
                    return Result<bool>.Success(true);
                }

                else if (hasArchitectureInterviewer && completedByRole == "Interviewer")
                {
                    // SCENARIO 2: Interviewer approved + Architecture Interviewer selected
                    // Create parallel interviews for GM and Architecture
                    // InterviewerId = GM, SecondInterviewerId = Archi
                    await CreateParallelInterviewsAsync(
                        (int)EnumWorkflowStage.ManagementReview, // Stage 2
                        candidateId,
                        positionId,
                        trackId,
                        interviewDate,
                        completedInterviewId,
                        createdByUserId,
                        pendingStatus.Id);
                    return Result<bool>.Success(true);
                }
                else if (completedByRole == "Interviewer")
                {
                    // SCENARIO 1: Interviewer approved, no Architecture Interviewer, and GM is not in the interview
                    // Go to GM only
                    await CreateSingleInterviewAsync(
                        (int)EnumWorkflowStage.ManagementReview, // Stage 2 (GM)
                        "General Manager",
                        candidateId,
                        positionId,
                        trackId,
                        interviewDate,
                        completedInterviewId,
                        createdByUserId,
                        pendingStatus.Id);
                    return Result<bool>.Success(true);
                }
            }

            // Handle Stage 2: When GM or Architecture approves, cancel the other if parallel
            if (currentStageId == (int)EnumWorkflowStage.ManagementReview)
            {
                // Cancel the other interviewer at Stage 2 (GM or Architecture)
                await CancelOtherInterviewersAtSameStage(candidateId, positionId, currentStageId, completedInterviewId, onHoldStatus.Id);

                // When Architecture approves after being second interviewer with GM, go to HR
                // InterviewerId = HR, SecondInterviewerId = null
                await CreateSingleInterviewAsync(
                    (int)EnumWorkflowStage.FinalHRInterview, // Stage 3 (HR)
                    "HR Manager",
                    candidateId,
                    positionId,
                    trackId,
                    interviewDate,
                    completedInterviewId,
                    createdByUserId,
                    pendingStatus.Id);
                return Result<bool>.Success(true);
            }


            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(false, $"Error creating next stage interviews: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a single interview for Stage 2 with GM as InterviewerId and Architecture Interviewer as SecondInterviewerId
    /// Used in Scenario 2: When Interviewer approves and ArchitectureInterviewerId is set
    /// </summary>
    private async Task CreateParallelInterviewsAsync(
        int stageId,
        int candidateId,
        int positionId,
        int trackId,
        DateTime interviewDate,
        int parentInterviewId,
        string createdByUserId,
        int pendingStatusId)
    {
        // Get the parent interview to check for ArchitectureInterviewerId and StartFromHR
        var parentInterview = await _interviewsRepository.GetById(parentInterviewId);
        if (parentInterview == null)
            return;

        bool startFromHR = parentInterview.StartFromHR;

        // Get GM user
        var gmUsers = await _userManager.GetUsersInRoleAsync("General Manager");
        var gmUser = gmUsers.FirstOrDefault();
        if (gmUser == null)
            return;

        // Create a single interview row with GM as InterviewerId and Architecture Interviewer as SecondInterviewerId (if exists)
        var interview = new Interviews
        {
            WorkflowStageId = stageId,
            StatusId = pendingStatusId,
            CandidateId = candidateId,
            PositionId = positionId,
            TrackId = trackId,
            Date = interviewDate,
            ParentId = parentInterviewId,
            InterviewerId = gmUser.Id, // GM as InterviewerId
            SecondInterviewerId = !string.IsNullOrEmpty(parentInterview.ArchitectureInterviewerId)
                ? parentInterview.ArchitectureInterviewerId
                : null, // Architecture Interviewer as SecondInterviewerId if exists
            CreatedOn = DateTime.Now,
            CreatedBy = createdByUserId,
            StartFromHR = startFromHR // Preserve reverse workflow flag
        };
        await _interviewsRepository.Insert(interview);
    }

    /// <summary>
    /// Creates a single interview for the next stage
    /// </summary>
    private async Task CreateSingleInterviewAsync(
        int stageId,
        string roleNameOrUserId,
        int candidateId,
        int positionId,
        int trackId,
        DateTime interviewDate,
        int parentInterviewId,
        string createdByUserId,
        int pendingStatusId,
        bool isUserSpecific = false)
    {
        string interviewerId;

        if (isUserSpecific)
        {
            // roleNameOrUserId is actually a user ID
            interviewerId = roleNameOrUserId;
        }
        else
        {
            // Get users with the required role
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleNameOrUserId);
            var interviewer = usersInRole.FirstOrDefault();

            if (interviewer == null)
                throw new Exception($"No user found with role: {roleNameOrUserId}");

            interviewerId = interviewer.Id;
        }

        // Get parent interview to preserve StartFromHR flag
        var parentInterview = await _interviewsRepository.GetById(parentInterviewId);
        bool startFromHR = parentInterview?.StartFromHR ?? false;

        var interview = new Interviews
        {
            WorkflowStageId = stageId,
            StatusId = pendingStatusId,
            CandidateId = candidateId,
            PositionId = positionId,
            TrackId = trackId,
            Date = interviewDate,
            ParentId = parentInterviewId,
            InterviewerId = interviewerId,
            SecondInterviewerId = null, // Explicitly set to null
            ArchitectureInterviewerId = null, // Explicitly set to null
            CreatedOn = DateTime.Now,
            CreatedBy = createdByUserId,
            StartFromHR = startFromHR // Preserve reverse workflow flag
        };

        await _interviewsRepository.Insert(interview);
    }

    /// <summary>
    /// Determines which role should conduct the next interview based on workflow logic
    /// </summary>
    private async Task<string> DetermineNextRoleAsync(
        string completedByRole,
        int nextStageId,
        int candidateId,
        int? positionId,
        int? trackId)
    {
        // Get configurations for next stage
        var nextStageConfigs = await _workflowRepository.GetConfigurationsByStageIdAsync(
            nextStageId,
            positionId,
            trackId);

        // Special logic for Stage 2
        if (nextStageId == (int)EnumWorkflowStage.ManagementReview) // Management Review stage
        {
            // If Architecture completed Stage 1, next is GM
            if (completedByRole == "Solution Architecture")
                return "General Manager";

            // If Interviewer completed Stage 1, need both GM and Architecture (handled separately)
            if (completedByRole == "Interviewer")
                return null; // Will be handled by parallel creation
        }

        // For Stage 3 (HR), always HR Manager
        if (nextStageId == (int)EnumWorkflowStage.FinalHRInterview)
        {
            return "HR Manager";
        }

        // Default: Get first configuration's role
        return nextStageConfigs.FirstOrDefault()?.RoleName;
    }

    /// <summary>
    /// Checks if both GM and Architecture were selected at Stage 1 (both are interviewers)
    /// This handles two scenarios:
    /// 1. Two separate Stage 1 interviews (one GM, one Architecture)
    /// 2. One Stage 1 interview with both GM and Architecture assigned
    /// </summary>
    private async Task<bool> CheckIfBothGMAndArchiSelectedAtStage1(int candidateId, int positionId)
    {
        try
        {
            // Get all Stage 1 interviews for this candidate and position
            var allInterviews = await _interviewsRepository.GetAll();
            var stage1Interviews = allInterviews
                .Where(i => i.CandidateId == candidateId
                    && i.PositionId == positionId
                    && i.ParentId == null
                    && (i.WorkflowStageId == (int)EnumWorkflowStage.InitialInterview || i.WorkflowStageId == null))
                .ToList();

            if (stage1Interviews.Count == 0)
                return false;

            // Check if we have both GM and Architecture as interviewers
            bool hasGM = false;
            bool hasArchi = false;

            foreach (var interview in stage1Interviews)
            {
                // Check primary interviewer
                var interviewerRole = await GetInterviewerRole(interview.InterviewerId);
                if (interviewerRole == "General Manager")
                    hasGM = true;
                if (interviewerRole == "Solution Architecture")
                    hasArchi = true;

                // Check SecondInterviewerId
                if (!string.IsNullOrEmpty(interview.SecondInterviewerId))
                {
                    var secondRole = await GetInterviewerRole(interview.SecondInterviewerId);
                    if (secondRole == "General Manager")
                        hasGM = true;
                    if (secondRole == "Solution Architecture")
                        hasArchi = true;
                }

                // Check ArchitectureInterviewerId
                if (!string.IsNullOrEmpty(interview.ArchitectureInterviewerId))
                {
                    var archiRole = await GetInterviewerRole(interview.ArchitectureInterviewerId);
                    if (archiRole == "Solution Architecture")
                        hasArchi = true;
                    if (archiRole == "General Manager")
                        hasGM = true;
                }
            }

            // Both GM and Architecture must be present
            return hasGM && hasArchi;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if GM and Architecture are in the same interview (not separate interviews)
    /// </summary>
    private async Task<bool> CheckIfGMAndArchiInSameInterview(Interviews interview)
    {
        try
        {
            var primaryRole = await GetInterviewerRole(interview.InterviewerId);
            bool hasGM = primaryRole == "General Manager";
            bool hasArchi = primaryRole == "Solution Architecture";

            // Check SecondInterviewerId
            if (!string.IsNullOrEmpty(interview.SecondInterviewerId))
            {
                var secondRole = await GetInterviewerRole(interview.SecondInterviewerId);
                if (secondRole == "General Manager") hasGM = true;
                if (secondRole == "Solution Architecture") hasArchi = true;
            }

            // Check ArchitectureInterviewerId
            if (!string.IsNullOrEmpty(interview.ArchitectureInterviewerId))
            {
                var archiRole = await GetInterviewerRole(interview.ArchitectureInterviewerId);
                if (archiRole == "General Manager") hasGM = true;
                if (archiRole == "Solution Architecture") hasArchi = true;
            }

            return hasGM && hasArchi;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the other Stage 1 interview (GM or Architecture) has also been completed and approved
    /// Used when GM and Architecture are in separate interviews
    /// </summary>
    private async Task<bool> CheckIfOtherStage1InterviewCompleted(
        int candidateId,
        int currentInterviewId,
        string currentRole)
    {
        try
        {
            var allInterviews = await _interviewsRepository.GetAll();
            var stage1Interviews = allInterviews
                .Where(i => i.CandidateId == candidateId
                    && i.ParentId == null
                    && i.InterviewsId != currentInterviewId
                    && (i.WorkflowStageId == (int)EnumWorkflowStage.InitialInterview || i.WorkflowStageId == null))
                .ToList();

            foreach (var interview in stage1Interviews)
            {
                var interviewerRole = await GetInterviewerRole(interview.InterviewerId);

                // Check if this is the other role (GM or Architecture)
                bool isOtherRole = (currentRole == "General Manager" && interviewerRole == "Solution Architecture") ||
                                   (currentRole == "Solution Architecture" && interviewerRole == "General Manager");

                if (isOtherRole)
                {
                    // Check if it's approved
                    var status = await _statusRepository.GetById(interview.StatusId);
                    return status.Code == StatusCode.Approved;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the role name of an interviewer
    /// </summary>
    private async Task<string> GetInterviewerRole(string interviewerId)
    {
        try
        {
            if (string.IsNullOrEmpty(interviewerId))
                return null;

            var user = await _userManager.FindByIdAsync(interviewerId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Handles reverse workflow: HR → Interviewers → GM
    /// </summary>
    private async Task<Result<bool>> HandleReverseWorkflow(
        Interviews completedInterview,
        int completedInterviewId,
        string completedByRole,
        int candidateId,
        int positionId,
        int trackId,
        DateTime interviewDate,
        string createdByUserId,
        int currentStageId,
        Status pendingStatus,
        Status onHoldStatus)
    {
        try
        {
            // Stage 4: HR Initial Interview → Stage 5 (Interviewers)
            if (currentStageId == (int)EnumWorkflowStage.HRInitialInterview && completedByRole == "HR Manager")
            {
                // Cancel other HR interviewers if multiple
                await CancelOtherInterviewersAtSameStage(candidateId, positionId, currentStageId, completedInterviewId, onHoldStatus.Id);

                // Get the original HR interview (this is the root interview with StartFromHR = true)
                var originalInterview = completedInterview;

                if (originalInterview == null)
                    return Result<bool>.Failure(false, "Original interview not found");

                // Get selected interviewers from the new SelectedInterviewers table
                var selectedInterviewers = await _selectedInterviewersRepository.GetByInterviewIdAsync(completedInterviewId);

                if (selectedInterviewers == null)
                    return Result<bool>.Failure(false, "Selected interviewers not found for this interview");

                // Get the selected interviewers
                string firstInterviewerId = selectedInterviewers.FirstInterviewerId;
                string secondInterviewerId = selectedInterviewers.SecondInterviewerId;
                string architectureInterviewerId = selectedInterviewers.ArchitectureInterviewerId;

                // Create a single interview row with all selected interviewers
                // FirstInterviewerId → InterviewerId
                // SecondInterviewerId → SecondInterviewerId
                // ArchitectureInterviewerId → ArchitectureInterviewerId

                // Get parent interview to preserve StartFromHR flag
                var parentInterview = await _interviewsRepository.GetById(completedInterviewId);
                bool startFromHR = parentInterview?.StartFromHR ?? false;

                // Determine the main interviewer (first non-GM interviewer, or first interviewer if all are GM)
                string mainInterviewerId = null;
                if (!string.IsNullOrEmpty(firstInterviewerId))
                {
                    var role1 = await GetInterviewerRole(firstInterviewerId);
                    if (role1 != "General Manager")
                    {
                        mainInterviewerId = firstInterviewerId;
                    }
                    else if (mainInterviewerId == null)
                    {
                        mainInterviewerId = firstInterviewerId; // Use GM if no other interviewer found
                    }
                }
                if (mainInterviewerId == null && !string.IsNullOrEmpty(secondInterviewerId))
                {
                    var role2 = await GetInterviewerRole(secondInterviewerId);
                    if (role2 != "General Manager")
                    {
                        mainInterviewerId = secondInterviewerId;
                    }
                    else if (mainInterviewerId == null)
                    {
                        mainInterviewerId = secondInterviewerId;
                    }
                }
                if (mainInterviewerId == null && !string.IsNullOrEmpty(architectureInterviewerId))
                {
                    mainInterviewerId = architectureInterviewerId;
                }

                if (string.IsNullOrEmpty(mainInterviewerId))
                {
                    return Result<bool>.Failure(false, "No valid interviewer found");
                }

                // Create single interview with all interviewers in one row
                var interview = new Interviews
                {
                    WorkflowStageId = (int)EnumWorkflowStage.InterviewersReview, // Stage 5 (Interviewers Review)
                    StatusId = pendingStatus.Id,
                    CandidateId = candidateId,
                    PositionId = positionId,
                    TrackId = trackId,
                    Date = interviewDate,
                    ParentId = completedInterviewId,
                    InterviewerId = firstInterviewerId, // First interviewer in InterviewerId
                    SecondInterviewerId = secondInterviewerId, // Second interviewer in SecondInterviewerId
                    ArchitectureInterviewerId = architectureInterviewerId, // Architecture interviewer in ArchitectureInterviewerId
                    CreatedOn = DateTime.Now,
                    CreatedBy = createdByUserId,
                    StartFromHR = startFromHR // Preserve reverse workflow flag
                };

                await _interviewsRepository.Insert(interview);

                return Result<bool>.Success(true);
            }

            // Stage 5: First/Second Interviewer Review → Stage 6 (GM + Architecture if exists, or GM only)
            // BUT: If GM is one of the selected interviewers, stop workflow (no Stage 6)
            if (currentStageId == (int)EnumWorkflowStage.InterviewersReview)
            {
                // Cancel other interviewers at Stage 5 (if First or Second interviewer approved, cancel the other)
                // Note: Since all interviewers are in the same interview row, when one approves, the interview status changes to Approved
                // The other interviewers won't be able to submit because GetCurrentInterviews filters by Pending status
                await CancelOtherInterviewersAtSameStage(candidateId, positionId, currentStageId, completedInterviewId, onHoldStatus.Id);

                // Get original HR interview to check for Architecture Interviewer and GM
                var originalInterview = completedInterview;
                while (originalInterview != null && originalInterview.ParentId != null)
                {
                    var parentInterview = await _interviewsRepository.GetById(originalInterview.ParentId.Value);
                    if (parentInterview == null)
                        break;
                    originalInterview = parentInterview;
                }

                if (originalInterview == null)
                    return Result<bool>.Failure(false, "Original interview not found");

                // Get selected interviewers from the new table
                var selectedInterviewers = await _selectedInterviewersRepository.GetByInterviewIdAsync(originalInterview.InterviewsId);

                if (selectedInterviewers == null)
                    return Result<bool>.Failure(false, "Selected interviewers not found");

                // Check if GM is one of the selected interviewers (FirstInterviewerId or SecondInterviewerId)
                bool hasGMAsSelectedInterviewer = false;
                if (!string.IsNullOrEmpty(selectedInterviewers.FirstInterviewerId))
                {
                    var role1 = await GetInterviewerRole(selectedInterviewers.FirstInterviewerId);
                    if (role1 == "General Manager")
                    {
                        hasGMAsSelectedInterviewer = true;
                    }
                }
                if (!string.IsNullOrEmpty(selectedInterviewers.SecondInterviewerId))
                {
                    var role2 = await GetInterviewerRole(selectedInterviewers.SecondInterviewerId);
                    if (role2 == "General Manager")
                    {
                        hasGMAsSelectedInterviewer = true;
                    }
                }

                // If GM is one of the selected interviewers, stop workflow (no Stage 6)
                // The workflow ends here because GM already approved as part of Stage 5
                if (hasGMAsSelectedInterviewer)
                {
                    // Workflow stops here - GM was one of the interviewers and has already approved
                    return Result<bool>.Success(true);
                }

                // GM is NOT one of the selected interviewers → Continue to Stage 6
                // Check if Architecture Interviewer exists
                bool hasArchitectureInterviewer = !string.IsNullOrEmpty(selectedInterviewers.ArchitectureInterviewerId);

                if (hasArchitectureInterviewer)
                {
                    // Architecture Interviewer exists → Create Stage 6 with GM as InterviewerId and Architecture Interviewer as SecondInterviewerId
                    // Get parent interview to preserve StartFromHR flag
                    var parentInterview = await _interviewsRepository.GetById(completedInterviewId);
                    bool startFromHR = parentInterview?.StartFromHR ?? false;

                    // Get GM user
                    var gmUsers = await _userManager.GetUsersInRoleAsync("General Manager");
                    var gmUser = gmUsers.FirstOrDefault();

                    if (gmUser == null)
                        return Result<bool>.Failure(false, "No General Manager found in the system");

                    // Create Stage 6 interview with GM as InterviewerId and Architecture Interviewer as SecondInterviewerId
                    var stage6Interview = new Interviews
                    {
                        WorkflowStageId = (int)EnumWorkflowStage.GMFinalReview, // Stage 6 (GM Final Review)
                        StatusId = pendingStatus.Id,
                        CandidateId = candidateId,
                        PositionId = positionId,
                        TrackId = trackId,
                        Date = interviewDate,
                        ParentId = completedInterviewId,
                        InterviewerId = gmUser.Id, // GM in InterviewerId
                        SecondInterviewerId = selectedInterviewers.ArchitectureInterviewerId, // Architecture Interviewer in SecondInterviewerId
                        CreatedOn = DateTime.Now,
                        CreatedBy = createdByUserId,
                        StartFromHR = startFromHR // Preserve reverse workflow flag
                    };

                    await _interviewsRepository.Insert(stage6Interview);
                }
                else
                {
                    // No Architecture Interviewer → Create Stage 6 with GM only
                    await CreateSingleInterviewAsync(
                        (int)EnumWorkflowStage.GMFinalReview, // Stage 6 (GM Final Review)
                        "General Manager",
                        candidateId,
                        positionId,
                        trackId,
                        interviewDate,
                        completedInterviewId,
                        createdByUserId,
                        pendingStatus.Id);
                }

                return Result<bool>.Success(true);
            }

            // Stage 6: GM Final Review (or GM + Architecture) → END (no next stage in reverse workflow)
            if (currentStageId == (int)EnumWorkflowStage.GMFinalReview)
            {
                // Cancel other interviewers at Stage 6 (if GM or Architecture approved, cancel the other)
                await CancelOtherInterviewersAtSameStage(candidateId, positionId, currentStageId, completedInterviewId, onHoldStatus.Id);

                // Check if this is GM or Architecture completing
                bool isGM = completedByRole == "General Manager";
                bool isArchitecture = completedByRole == "Solution Architecture";

                if (isGM || isArchitecture)
                {
                    // In reverse workflow, when either GM or Architecture approves, the process ends
                    // No need to go back to HR - the workflow ends here
                    return Result<bool>.Success(true);
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(false, $"Error handling reverse workflow: {ex.Message}");
        }
    }


    /// <summary>
    /// Cancels/Completes other interviewers at the same stage when one approves
    /// This prevents other interviewers from submitting after the first one has already approved
    /// </summary>
    private async Task CancelOtherInterviewersAtSameStage(
        int candidateId,
        int positionId,
        int stageId,
        int completedInterviewId,
        int onHoldStatusId)
    {
        try
        {
            var allInterviews = await _interviewsRepository.GetAll();
            var completedInterview = await _interviewsRepository.GetById(completedInterviewId);
            if (completedInterview == null) return;

            var otherInterviews = allInterviews
                .Where(i => i.CandidateId == candidateId
                    && i.PositionId == positionId
                    && i.InterviewsId != completedInterviewId
                    && (i.WorkflowStageId == stageId || (stageId == (int)EnumWorkflowStage.InitialInterview && i.WorkflowStageId == null))
                    && i.ParentId == (completedInterview.ParentId ?? (stageId == (int)EnumWorkflowStage.InitialInterview || stageId == (int)EnumWorkflowStage.HRInitialInterview ? (int?)null : completedInterview.ParentId)))
                .ToList();

            foreach (var interview in otherInterviews)
            {
                if (interview == null) continue;

                // Only cancel if status is still Pending
                try
                {
                    var interviewStatus = await _statusRepository.GetById(interview.StatusId);
                    if (interviewStatus != null && interviewStatus.Code == StatusCode.Pending)
                    {
                        interview.StatusId = onHoldStatusId;
                        interview.ModifiedOn = DateTime.Now;
                        await _interviewsRepository.Update(interview);
                    }
                }
                catch
                {
                    // Skip this interview if there's an error getting status
                    continue;
                }
            }
        }
        catch
        {
            // Log error but don't fail the workflow
        }
    }
}

