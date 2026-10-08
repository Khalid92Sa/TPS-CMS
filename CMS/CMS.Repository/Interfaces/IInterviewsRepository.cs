using CMS.Application.DTOs;
using CMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces;

public interface IInterviewsRepository
{
    Task<int> Insert(Interviews entity);
    Task<int> Update(Interviews entity);
    Task<int> Delete(int id);
    Task<Interviews> GetById(int id);
    Task<string> GetRoleById(string id);
    Task<Interviews> GetByIdForEdit(int id);
    Task<List<Interviews>> GetAll();
    Task<(List<Interviews> Items, int TotalCount)> GetLatestInterviewPerCandidatePageAsync(InterviewListFilter filter, int pageNumber, int pageSize);
    Task<bool> HasGivenStatusAsync(string interviewerId, int interviewId);
    Task<List<Interviews>> GetCurrentInterviews(string id, int? companyFilter, int? trackFilter);
    Task<string> GetGeneralManagerEmail();
    Task<string> GetInterviewerEmail(string interviewerId);
    Task<string> GetHREmail();
    Task<string> GetArchiEmail();
    Task<Interviews> GetGeneralManagerInterviewForCandidate(int candidateId);
    Task<Interviews> GetArchiInterviewForCandidate(int candidateId);
    Task<Interviews> GetinterviewerInterviewForCandidate(int candidateId);
    Task<int> GetInterviewCountForCandidate(int candidateId);
    Task<bool> DeletePendingInterviews(string nextInterviewStatusCode,int candidateId, int positionId, string userId);
    Task<string?> GetStatusOfNextInterview(int candidateId, int currentInterviewId);
    InterviewsDTO GetInterviewByCandidateIdWithParentId(int candidateId);
    Task<bool> DeletePendingInterviewsforStopCycle(int candidateId, int positionId);
    Task<Interviews> GetLastInterviewBeforePendingByCandidateId(int candidateId);
    Task<Interviews> GetByParentIdAsync(int parentId);
    Task<Interviews> GetByInterviewerRoleAsync(int candidateId, string roleName);
    Task<Interviews> GetThirdInterviewAsync(int candidateId);
    Task<bool> DoesInterviewExistForCandidateAsync(int candidateId);
    Task<List<Interviews>> GetFirstInterviews();
    Task<List<Interviews>> GetCurrentPendingInterviewsDueReminderAsync(int pendingStatusId, DateTime notBefore, DateTime dueBy, DateTime remindedBefore);
    Task<bool> IsStillPendingAsync(int interviewId, int pendingStatusId);
    Task MarkReminderSentAsync(int interviewId, DateTime sentOn);
    Task<List<Interviews>> GetInterviewsByCandidateIdAsync(int candidateId);
    Task<List<Interviews>> GetChildInterviewsByParentIdAsync(int parentInterviewId);
    Task<bool> DeleteChildInterviewsAndNotificationsAsync(int parentInterviewId, int candidateId);
    Task<bool> DeleteNotificationsByCandidateAndReceiversAsync(int candidateId, List<string> receiverIds);
    Task ReloadAsync(Interviews interview);
    Task<string> TryCompletePendingIfAssignedAsync(Interviews interview, string actingUserId);
    Task<string> InsertInterviewDelegationAsync(int interviewId, string originalInterviewerId, bool secondSlot, InterviewDelegation delegation, int? replaceDelegationId = null);
    Task<string> CancelInterviewDelegationAsync(int interviewId, int delegationId, string removedByUserId);
    Task<List<InterviewDelegation>> GetDelegationsByInterviewIdAsync(int interviewId);
    Task<List<InterviewDelegation>> GetInForceDelegationsAsync(int interviewId, DateTime asOf);
    Task<List<Interviews>> GetPendingChildInterviewsAsync(int parentInterviewId);
    Task<InterviewDelegation> GetActiveDelegationAsync(int interviewId, string userId, DateTime asOf);
    Task<List<InterviewDelegation>> GetUserDelegationsForInterviewAsync(int interviewId, string userId);
    Task<bool> HasVisibleDelegationAsync(string userId, DateTime asOf);
    Task<List<InterviewDelegation>> GetVisibleDelegationsForUserAsync(string userId, IEnumerable<int> interviewIds, DateTime asOf);
    Task<InterviewDelegation> GetCompletedDelegationAsync(int interviewId);
    Task<List<InterviewDelegation>> GetOpenDelegationsForInterviewsAsync(IEnumerable<int> interviewIds, DateTime asOf);
}
