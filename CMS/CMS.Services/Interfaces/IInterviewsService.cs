// CMS.Domain.Interfaces/IInterviewsService.cs
using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface IInterviewsService
{
    Task UpdateInterviewAttachmentAsync(int id, string fileName, long fileSize, Stream fileStream);
    Task ConductInterview(InterviewsDTO interviewsDTO, string firstinterviewer, string secondinterviewer);
    Task ConductInterviewForGm(InterviewsDTO completedDTO);
    Task ConductInterviewForArchi(InterviewsDTO completedDTO);
    Task<List<UsersDTO>> GetInterviewers();
    Task<Result<List<InterviewsDTO>>> MyInterviews(int? companyFilter, int? trackFilter);
    Task<Result<InterviewsDTO>> Insert(InterviewsDTO data);
    Task<Result<List<InterviewsDTO>>> GetAll();
    Task<Result<PaginatedList<InterviewsDTO>>> GetLatestInterviewPerCandidatePageAsync(InterviewListFilter filter, int pageNumber, int pageSize);
    Task<Result<List<InterviewsDTO>>> GetAllForGeneralManager();
    Task<Result<InterviewsDTO>> Delete(int id);
    Task<Result<InterviewsDTO>> GetInterviewDetails(int id);
    Task<Result<InterviewsDTO>> GetInterviewDetailsWithAdditionalInfo(int id);
    Task<Result<InterviewsDTO>> Update(InterviewsDTO data);
    Task<string> GetInterviewerName(string id);
    Task<string> GetArchitectureName(string id);
    Task<string> GetInterviewerRole(string id);
    Task<Result<List<InterviewsDTO>>> ShowHistory(int id);
    Task<bool> IsSolutionArchitect(string userId);
    Task<Result<int>> SaveStopCycleNote(int id, string note);
    Task<bool> DeletePendingInterviews(int candidateId, InterviewsDTO collection);
    Task<bool> DoesInterviewExistForCandidate(int candidateId);
    Task<Result<bool>> AddArchitectureInterviewer(int interviewId, string architectureId);
    Task<Result<bool>> RemoveArchitectureInterviewer(int interviewId);
    Task<Result<bool>> AddOrUpdateArchitectureInterviewer(int interviewId, string architectureId);
    Task<Result<List<InterviewsDTO>>> GetInterviewsWithoutResults();
    Task<bool> IsInterviewStillPendingForReminderAsync(int interviewId);
    Task MarkReminderSentAsync(int interviewId);
    Task<Result<List<InterviewsDTO>>> GetHRFirstFlowInterviewDetails(int interviewId);
    Task<Result<List<InterviewsDTO>>> ShowHistoryForHRFirstFlow(int id);
    Task<Result<DelegateInterviewPageDTO>> GetPendingInterviewDelegationAsync(int interviewId);
    Task<Result<InterviewDelegationDTO>> DelegatePendingInterviewAsync(int interviewId, string interviewerSlot, string originalInterviewerId, string delegatedUserId, int durationId, int? replaceDelegationId = null);
    Task<Result<InterviewDelegationDTO>> RemoveInterviewDelegationAsync(int interviewId, int delegationId);
    Task<Result<BulkDelegationPageDTO>> GetBulkDelegationPageAsync();
    Task<Result<BulkDelegationResultDTO>> DelegatePendingInterviewsAsync(IEnumerable<int> interviewIds, string delegatedUserId, int durationId);
    Task<Result<bool>> EnsureCurrentUserCanSubmitPendingAsync(int interviewId);
    Task<Result<bool>> EnsureCurrentUserCanOpenSubmittedResultAsync(int interviewId);
    Task<bool> IsCurrentUserAssignedToInterviewAsync(int interviewId);
    Task<string> GetDelegatedWorkflowRoleAsync(int interviewId, string userId);
    Task<bool> HasVisibleDelegationAsync();
    Task<InterviewDelegationDTO> GetCurrentUserDelegationAccessAsync(int interviewId);
    Task<bool> HasInForceDelegationAsync(int interviewId);
    Task EnsureDelegatedSuccessorAsync(int interviewId);
}
