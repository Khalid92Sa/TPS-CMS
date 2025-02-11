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
    Task<bool> DeletePendingInterviews(int candidateId, int positionId, string userId);
    Task<string?> GetStatusOfNextInterview(int candidateId, int currentInterviewId);
    InterviewsDTO GetInterviewByCandidateIdWithParentId(int candidateId);
    Task<bool> DeletePendingInterviewsforStopCycle(int candidateId, int positionId);
    Task<Interviews> GetLastInterviewBeforePendingByCandidateId(int candidateId);
    Task<Interviews> GetByParentIdAsync(int parentId);
    Task<Interviews> GetByInterviewerRoleAsync(int candidateId, string roleName);
    Task<Interviews> GetThirdInterviewAsync(int candidateId);
    Task<bool> DoesInterviewExistForCandidateAsync(int candidateId);
    Task<List<Interviews>> GetFirstInterviews();
}
