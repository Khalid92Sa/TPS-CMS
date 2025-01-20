// CMS.Domain.Interfaces/IInterviewsService.cs
using CMS.Application.DTOs;
using CMS.Application.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces
{
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
        Task<Result<List<InterviewsDTO>>> GetAllForGeneralManager();
        Task<Result<InterviewsDTO>> Delete(int id);
        Task<Result<InterviewsDTO>> GetById(int id);
        Task<Result<InterviewsDTO>> GetById3(int id);

        Task<Result<InterviewsDTO>> Update(InterviewsDTO data);
        
        Task<string> GetInterviewerName(string id);
        Task<string> GetArchitectureName(string id);
        Task<string> GetInterviewerRole(string id);

        Task <Result<List<InterviewsDTO>>> ShowHistory(int id);
        void LogException(string methodName, Exception ex = null, string additionalInfo = null);

        Task<bool> IsSolutionArchitect(string userId);

        //Task ConductInterviewEdit(InterviewsDTO completedDTO, string firstinterviewer, string secondinterviewer);


        Task<Result<int>> SaveStopCycleNote(int id, string note);
        Task<bool> DeletePendingInterviews(int candidateId, InterviewsDTO collection);
        Task<Result<bool>> AddArchitectureInterviewer(int interviewId, string architectureId);
        Task<Result<bool>> RemoveArchitectureInterviewer(int interviewId);
        Task<Result<bool>> AddOrUpdateArchitectureInterviewer(int interviewId, string architectureId);

    }
}
