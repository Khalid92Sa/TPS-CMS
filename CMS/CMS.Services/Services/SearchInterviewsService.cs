using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class SearchInterviewsService : ISearchInterviewsService
{
    private readonly IInterviewsRepository _interviewsRepository;
    private readonly ICandidateService _candidateService;
    private readonly IPositionService _positionService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IStatusRepository _statusRepository;
    private readonly ApplicationDbContext _db;
    private readonly ICompanyService _companyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAttachmentService _attachmentService;

    public SearchInterviewsService(
        IInterviewsRepository interviewsRepository,
        ICandidateService candidateService,
        IPositionService positionService,
        IAttachmentService attachmentService,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor,
        IStatusRepository statusRepository,
        ApplicationDbContext Db,
        ICompanyService companyService)
    {
        _interviewsRepository = interviewsRepository;
        _candidateService = candidateService;
        _positionService = positionService;
        _attachmentService = attachmentService;
        _userManager = userManager;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
        _statusRepository = statusRepository;
        _db = Db;
        _companyService = companyService;
    }

    public async Task<List<UsersDTO>> GetAllInterviewers()
    {
        try
        {
            List<IdentityRole> allRoles = await _roleManager.Roles.ToListAsync();

            List<UsersDTO> allUsers = [];

            foreach (IdentityRole role in allRoles)
            {
                if (!string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    IList<IdentityUser> usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);

                    List<UsersDTO> usersDtoList = usersInRole.Select(user => new UsersDTO
                    {
                        Id = user.Id,
                        Name = user.UserName,
                        Email = user.Email,
                    }).ToList();

                    allUsers.AddRange(usersDtoList);
                }
            }
            return allUsers;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<UsersDTO>> GetInterviewers()
    {
        try
        {
            IdentityRole interviewerRole = await _roleManager.FindByNameAsync("Interviewer");

            if (interviewerRole is null)
                return new List<UsersDTO>();

            IList<IdentityUser> usersInRole = await _userManager.GetUsersInRoleAsync(interviewerRole.Name);

            List<UsersDTO> interviewers = usersInRole.Select(user => new UsersDTO
            {
                Id = user.Id,
                Name = user.UserName,
                Email = user.Email,
            }).ToList();

            return interviewers;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetInterviewerName(string id)
    {
        try
        {
            IdentityUser user = await _userManager.FindByIdAsync(id);

            if (user != null)
                return user.UserName;

            return "User not found";
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Result<InterviewsDTO>> Delete(int id)
    {
        try
        {
            Interviews interview = await _interviewsRepository.GetById(id);

            if (interview != null && interview.AttachmentId != null)
            {
                int attachmentToRemove = (int)interview.AttachmentId;
                await _interviewsRepository.Delete(id);
                await _attachmentService.DeleteAttachmentAsync(attachmentToRemove);
            }
            else
                await _interviewsRepository.Delete(id);

            return Result<InterviewsDTO>.Success(null);
        }

        catch (Exception ex)
        {
            return Result<InterviewsDTO>.Failure(null, $"An error occurred while deleting the Interview{ex.InnerException.Message}");
        }
    }

    public async Task<Result<List<InterviewsDTO>>> GetAll()
    {
        try
        {
            List<Interviews> interviews = await _interviewsRepository.GetAll();

            if (interviews is null)
                return Result<List<InterviewsDTO>>.Failure(null, "No career offers found");

            List<InterviewsDTO> interviewsDTO = new List<InterviewsDTO>();
            foreach (Interviews c in interviews)
            {
                string userName = await GetInterviewerName(c.InterviewerId);
                string SeconduserName = await GetInterviewerName(c.SecondInterviewerId);

                InterviewsDTO com = new InterviewsDTO
                {
                    InterviewsId = c.InterviewsId,
                    Score = c.Score,
                    StatusId = c.StatusId,
                    StatusName = c.Status.Name,
                    Date = c.Date,
                    PositionId = c.PositionId,
                    Name = c.Position.Name,
                    TrackId = c.TrackId,
                    TrackName = c.Track.Name,
                    Notes = c.Notes,
                    StopCycleNote = c.StopCycleNote,
                    ParentId = c.ParentId,
                    InterviewerId = c.InterviewerId,
                    InterviewerName = userName,
                    SecondInterviewerId = c.SecondInterviewerId,
                    SecondInterviewerName = SeconduserName,
                    CandidateId = c.CandidateId,
                    FullName = c.Candidate.FullName,
                    AttachmentId = c.AttachmentId,
                    StartFromHR = c.StartFromHR
                };

                interviewsDTO.Add(com);
            }
            return Result<List<InterviewsDTO>>.Success(interviewsDTO);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get Interview: {ex.InnerException.Message}");
        }
    }

    public async Task<Result<InterviewsDTO>> GetById(int id)
    {
        if (id <= 0)
            return Result<InterviewsDTO>.Failure(null, "Invalid interview id");

        try
        {
            Interviews interview = await _interviewsRepository.GetById(id);
            string userName = await GetInterviewerName(interview.InterviewerId);
            string SeconduserName = await GetInterviewerName(interview.SecondInterviewerId);
            string archiName = await GetArchitectureName(interview.ArchitectureInterviewerId);
            string interviewerRole = await GetInterviewerRole(interview.InterviewerId);
            double? firstInterviewScore = await GetFirstInterviewScore(id);

            InterviewsDTO interviewDTO = new InterviewsDTO
            {
                InterviewsId = interview.InterviewsId,
                Score = interview.Score,
                FirstInterviewScore = firstInterviewScore,
                StatusId = interview.StatusId,
                StatusName = interview.Status.Name,
                Date = interview.Date,
                PositionId = interview.PositionId,
                Name = interview.Position.Name,
                TrackId = interview.TrackId,
                TrackName = interview.Track.Name,
                EvalutaionFormId = interview.Position.EvaluationId,
                Notes = interview.Notes,
                StopCycleNote = interview.StopCycleNote,
                ParentId = interview.ParentId,
                InterviewerId = interview.InterviewerId,
                InterviewerName = userName,
                CandidateId = interview.CandidateId,
                FullName = interview.Candidate.FullName,
                CandidateCVAttachmentId = interview.Candidate.CVAttachmentId,
                AttachmentId = interview.AttachmentId,
                InterviewerRole = interviewerRole,
                ActualExperience = interview.ActualExperience,
                SecondInterviewerId = interview.SecondInterviewerId,
                SecondInterviewerName = SeconduserName,
                ArchitectureInterviewerId = interview.ArchitectureInterviewerId,
                ArchitectureInterviewerName = archiName,
                StartFromHR = interview.StartFromHR,
                WorkflowStageId = interview.WorkflowStageId,
                StageName = interview.WorkflowStage?.Name,
                CreatedOn = interview.CreatedOn,
                ModifiedOn = interview.ModifiedOn,
                modifiedBy = interview.ModifiedBy
            };
            return Result<InterviewsDTO>.Success(interviewDTO);
        }
        catch (Exception ex)
        {
            return Result<InterviewsDTO>.Failure(null, $"unable to retrieve the Interview from the repository{ex.InnerException.Message}");
        }
    }

    public async Task<Result<InterviewsDTO>> Insert(InterviewsDTO data)
    {
        try
        {
            if (data is null)
                return Result<InterviewsDTO>.Failure(data, "the interview  DTO is null");

            Status status = await _statusRepository.GetByCode(StatusCode.Pending);
            Interviews interview = new Interviews
            {
                PositionId = data.PositionId,
                TrackId = data.TrackId,
                CandidateId = data.CandidateId,
                Score = data.Score,
                StatusId = status.Id,
                Date = data.Date,
                Notes = data.Notes,
                StopCycleNote = data.StopCycleNote,
                ParentId = data.ParentId,
                InterviewerId = data.InterviewerId,
                AttachmentId = data.AttachmentId,
            };

            await _interviewsRepository.Insert(interview);
            return Result<InterviewsDTO>.Success(data);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Result<InterviewsDTO>> Update(InterviewsDTO data)
    {
        try
        {
            if (data is null)
                return Result<InterviewsDTO>.Failure(data, "can not update a null object");

            Interviews interview = new Interviews
            {
                InterviewsId = data.InterviewsId,
                PositionId = data.PositionId,
                TrackId = data.TrackId,
                CandidateId = data.CandidateId,
                Score = data.Score,
                ParentId = data.ParentId,
                InterviewerId = data.InterviewerId,
                Date = data.Date,
                Notes = data.Notes,
                StopCycleNote = data.StopCycleNote,
                StatusId = (int)data.StatusId,
                AttachmentId = data.AttachmentId,
            };

            await _interviewsRepository.Update(interview);
            return Result<InterviewsDTO>.Success(data);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateInterviewAttachmentAsync(int id, string fileName, long fileSize, Stream fileStream)
    {
        try
        {
            Interviews interview = await _interviewsRepository.GetById(id);
            int attachmentId = await _attachmentService.CreateAttachmentAsync(fileName, fileSize, fileStream);
            int attachmentToRemove = (int)interview.AttachmentId;
            interview.AttachmentId = attachmentId;
            await _interviewsRepository.Update(interview);
            await _attachmentService.DeleteAttachmentAsync(attachmentToRemove);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task ConductInterview(InterviewsDTO entity)
    {
        try
        {
            Interviews interview = await _interviewsRepository.GetById(entity.InterviewsId);
            int attachmentId = await _attachmentService.CreateAttachmentAsync(entity.FileName, (long)entity.FileSize, entity.FileData);
            entity.AttachmentId = attachmentId;

            if (interview is null)
                throw new Exception();

            string managerId = "";
            Status status = await _statusRepository.GetById((int)entity.StatusId);
            IdentityRole manager = await _roleManager.FindByNameAsync("General Manger");

            if (manager != null)
                managerId = (await _userManager.GetUsersInRoleAsync(manager.Name)).FirstOrDefault().Id;

            string HRId = "";
            IdentityRole HR = await _roleManager.FindByNameAsync("HR");

            if (HR != null)
                HRId = (await _userManager.GetUsersInRoleAsync(HR.Name)).FirstOrDefault().Id;

            bool ff = false;

            if (entity.InterviewerId == HRId)
                ff = true;

            string nextInterviewer = "";

            if (interview.ParentId == null)
                nextInterviewer = managerId;

            else
                nextInterviewer = HRId;

            bool f = false;

            if (status.Code == StatusCode.Rejected)
                f = true;

            Status statID = await _statusRepository.GetByCode(StatusCode.Pending);

            if (!f && !ff)
            {
                Interviews newInterview = new Interviews
                {
                    StatusId = statID.Id,
                    Date = interview.Date,
                    CandidateId = interview.CandidateId,
                    PositionId = interview.PositionId,
                    InterviewerId = nextInterviewer,
                    ParentId = entity.InterviewsId
                };
                await _interviewsRepository.Insert(newInterview);
            }

            Interviews updatedInterview = new Interviews
            {
                InterviewsId = entity.InterviewsId,
                Score = entity.Score,
                Date = interview.Date,
                StatusId = (int)entity.StatusId,
                CandidateId = interview.CandidateId,
                PositionId = interview.PositionId,
                InterviewerId = interview.InterviewerId,
                AttachmentId = entity.AttachmentId,
                Notes = entity.Notes,
                ParentId = interview.ParentId
            };

            await _interviewsRepository.Update(updatedInterview);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Result<List<InterviewsDTO>>> MyInterviews(int? companyFilter, int? trackFilter)
    {
        try
        {
            IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            if (user is null)
                return Result<List<InterviewsDTO>>.Failure(null, "User not found.");

            List<Interviews> interviews = await _interviewsRepository.GetCurrentInterviews(user.Id, companyFilter, trackFilter);

            if (interviews is null)
                return Result<List<InterviewsDTO>>.Failure(null, "no available interviews.");

            List<InterviewsDTO> interviewsDTOs = new List<InterviewsDTO>();

            foreach (Interviews i in interviews)
            {
                string userName = await GetInterviewerName(i.InterviewerId);
                interviewsDTOs.Add(new InterviewsDTO
                {
                    InterviewsId = i.InterviewsId,
                    InterviewerId = i.InterviewerId,
                    InterviewerName = userName,
                    Score = i.Score,
                    StatusId = i.StatusId,
                    StatusName = i.Status.Name,
                    Date = i.Date,
                    PositionId = i.PositionId,
                    Name = i.Position.Name,
                    TrackId = i.TrackId,
                    TrackName = i.Track.Name,
                    Notes = i.Notes,
                    ParentId = i.ParentId,
                    CandidateId = i.CandidateId,
                    FullName = i.Candidate.FullName,
                    AttachmentId = i.AttachmentId
                });
            }

            return Result<List<InterviewsDTO>>.Success(interviewsDTOs);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get interviews: {ex.InnerException.Message}");
        }
    }

    public async Task<Result<List<InterviewsDTO>>> ShowHistory(int id)
    {
        List<InterviewsDTO> interviewsDTOs = [];
        try
        {
            Result<InterviewsDTO> currentInterviewResult = await GetById(id);
            InterviewsDTO currentInterview = currentInterviewResult.Value;

            if (currentInterview != null)
            {
                interviewsDTOs.Add(currentInterview);

                if (currentInterview.ParentId != null)
                {
                    while (currentInterview.ParentId != null)
                    {
                        Result<InterviewsDTO> parentInterviewResult = await GetById((int)currentInterview.ParentId);
                        currentInterview = parentInterviewResult.Value;
                        interviewsDTOs.Add(currentInterview);
                    }
                }
            }

            foreach (InterviewsDTO interviewDTO in interviewsDTOs)
            {
                int candidateId = interviewDTO.CandidateId;
                CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(candidateId);
                Result<CompanyDTO> companyResult = await _companyService.GetById(candidate.CompanyId);

                if (companyResult.IsSuccess)
                    interviewDTO.CompanyName = companyResult.Value.Name;

                else
                    interviewDTO.CompanyName = null;
            }

            return Result<List<InterviewsDTO>>.Success(interviewsDTOs);

        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get interview History: {ex.Message}");
        }
    }

    public async Task<string> GetArchitectureName(string id)
    {
        try
        {
            IdentityUser user = await _userManager.FindByIdAsync(id);

            if (user != null)
                return user.UserName;

            return "User not found";
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetInterviewerRole(string id)
    {
        try
        {
            IdentityUser user = await _userManager.FindByIdAsync(id);

            if (user != null)
            {
                IList<string> roles = await _userManager.GetRolesAsync(user);

                if (roles != null && roles.Count > 0)
                    return roles[0];
            }

            return "User not found";
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<double?> GetFirstInterviewScore(int interviewId)
    {
        Interviews interview = await _interviewsRepository.GetById(interviewId);

        if (interview?.ParentId != null)
            return await GetFirstInterviewScore(interview.ParentId.Value);

        return interview?.Score;
    }
    public async Task<Result<List<InterviewsDTO>>> GetAllByCandidateId(int candidateId)
    {
        var result = await GetAll();
        if (!result.IsSuccess)
            return Result<List<InterviewsDTO>>.Failure(result.Error);

        var interviews = result.Value
            .Where(i => i.CandidateId == candidateId)
            .ToList();

        return Result<List<InterviewsDTO>>.Success(interviews);
    }

    public async Task<Result<List<InterviewsDTO>>> ShowHistoryForHRFirstFlow(int id)
    {
        List<InterviewsDTO> interviewsDTOs = [];
        try
        {
            Result<InterviewsDTO> currentInterviewResult = await GetById(id);
            InterviewsDTO currentInterview = currentInterviewResult.Value;

            if (currentInterview == null)
                return Result<List<InterviewsDTO>>.Failure(null, "Interview not found");

            Interviews interviewEntity = await _interviewsRepository.GetById(id);
            if (interviewEntity == null || !interviewEntity.StartFromHR)
                return Result<List<InterviewsDTO>>.Failure(null, "This is not an HR-First flow interview");

            InterviewsDTO rootInterview = currentInterview;
            while (rootInterview.ParentId != null)
            {
                Result<InterviewsDTO> parentInterviewResult = await GetById((int)rootInterview.ParentId);
                rootInterview = parentInterviewResult.Value;
            }

            List<Interviews> allInterviews = await _interviewsRepository.GetInterviewsByCandidateIdAsync(rootInterview.CandidateId);

            IdentityRole gmRole = await _roleManager.FindByNameAsync("General Manager");
            var gmUsers = gmRole != null ? await _userManager.GetUsersInRoleAsync(gmRole.Name) : new List<IdentityUser>();
            var gmUserIds = gmUsers.Select(u => u.Id).ToList();

            InterviewsDTO hrInterview = null;
            List<InterviewsDTO> interviewerInterviews = [];
            List<InterviewsDTO> gmInterviews = [];

            Interviews currentInterviewEntity = await _interviewsRepository.GetById(id);
            if (currentInterviewEntity == null)
                return Result<List<InterviewsDTO>>.Failure(null, "Current interview not found");

            var includedInterviewIds = new HashSet<int> { currentInterviewEntity.InterviewsId };
            
            Interviews? ancestor = currentInterviewEntity;
            while (ancestor != null && ancestor.ParentId != null)
            {
                var parent = allInterviews.FirstOrDefault(i => i.InterviewsId == ancestor.ParentId.Value);
                if (parent == null)
                    break;
                
                includedInterviewIds.Add(parent.InterviewsId);
                ancestor = parent;
            }

            var allHRFirstFlowInterviews = new List<Interviews>();
            foreach (Interviews interview in allInterviews)
            {
                if (!interview.StartFromHR)
                    continue;

                if (includedInterviewIds.Contains(interview.InterviewsId))
                {
                    allHRFirstFlowInterviews.Add(interview);
                }
            }

            foreach (Interviews interview in allHRFirstFlowInterviews)
            {
                string userName = await GetInterviewerName(interview.InterviewerId);
                string secondUserName = await GetInterviewerName(interview.SecondInterviewerId);
                string archiName = await GetArchitectureName(interview.ArchitectureInterviewerId);
                string interviewerRole = await GetInterviewerRole(interview.InterviewerId);
                CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(interview.CandidateId);
                Result<CompanyDTO> companyResult = await _companyService.GetById(candidate.CompanyId);

                InterviewsDTO interviewDTO = new InterviewsDTO
                {
                    InterviewsId = interview.InterviewsId,
                    Score = interview.Score,
                    StatusId = interview.StatusId,
                    StatusName = interview.Status?.Name,
                    Date = interview.Date,
                    PositionId = interview.PositionId,
                    Name = interview.Position?.Name,
                    TrackId = interview.TrackId,
                    TrackName = interview.Track?.Name,
                    EvalutaionFormId = interview.Position?.EvaluationId,
                    Notes = interview.Notes,
                    StopCycleNote = interview.StopCycleNote,
                    ParentId = interview.ParentId,
                    InterviewerId = interview.InterviewerId,
                    InterviewerName = userName,
                    CandidateId = interview.CandidateId,
                    FullName = interview.Candidate?.FullName,
                    CandidateCVAttachmentId = interview.Candidate?.CVAttachmentId,
                    AttachmentId = interview.AttachmentId,
                    InterviewerRole = interviewerRole,
                    ActualExperience = interview.ActualExperience,
                    SecondInterviewerId = interview.SecondInterviewerId,
                    SecondInterviewerName = secondUserName,
                    ArchitectureInterviewerId = interview.ArchitectureInterviewerId,
                    ArchitectureInterviewerName = archiName,
                    WorkflowStageId = interview.WorkflowStageId,
                    StageName = interview.WorkflowStage?.Name,
                    StartFromHR = interview.StartFromHR,
                    CreatedOn = interview.CreatedOn,
                    modifiedBy = interview.ModifiedBy,
                    ModifiedOn = interview.ModifiedOn,
                    CompanyName = companyResult.IsSuccess ? companyResult.Value.Name : null
                };

                if (interview.ParentId == null)
                {
                    hrInterview = interviewDTO;
                }
                else
                {
                    bool isGMInterview = (interview.InterviewerId != null && gmUserIds.Contains(interview.InterviewerId)) ||
                                        (interview.SecondInterviewerId != null && gmUserIds.Contains(interview.SecondInterviewerId));

                    if (isGMInterview)
                    {
                        gmInterviews.Add(interviewDTO);
                    }
                    else
                    {
                        interviewerInterviews.Add(interviewDTO);
                    }
                }
            }

            if (hrInterview != null)
                interviewsDTOs.Add(hrInterview);

            interviewerInterviews = [.. interviewerInterviews.OrderBy(i => i.Date).ThenBy(i => i.InterviewsId)];
            interviewsDTOs.AddRange(interviewerInterviews);

            gmInterviews = [.. gmInterviews.OrderBy(i => i.Date).ThenBy(i => i.InterviewsId)];
            interviewsDTOs.AddRange(gmInterviews);

            return Result<List<InterviewsDTO>>.Success(interviewsDTOs);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get HR-First flow interview history: {ex.Message}");
        }
    }

    public async Task<Result<List<InterviewsDTO>>> GetHRFirstFlowInterviewDetails(int interviewId)
    {
        try
        {
            if (interviewId <= 0)
                return Result<List<InterviewsDTO>>.Failure(null, "Invalid interview id");

            Interviews currentInterview = await _interviewsRepository.GetById(interviewId);
            if (currentInterview == null)
                return Result<List<InterviewsDTO>>.Failure(null, "Interview not found");

            if (!currentInterview.StartFromHR)
                return Result<List<InterviewsDTO>>.Failure(null, "This is not an HR-First flow interview");

            List<Interviews> allInterviews = await _interviewsRepository.GetInterviewsByCandidateIdAsync(currentInterview.CandidateId);

            List<InterviewsDTO> interviewsDTOs = new List<InterviewsDTO>();

            foreach (Interviews interview in allInterviews)
            {
                string userName = await GetInterviewerName(interview.InterviewerId);
                string secondUserName = await GetInterviewerName(interview.SecondInterviewerId);
                string archiName = await GetArchitectureName(interview.ArchitectureInterviewerId);
                string interviewerRole = await GetInterviewerRole(interview.InterviewerId);

                InterviewsDTO interviewDTO = new InterviewsDTO
                {
                    InterviewsId = interview.InterviewsId,
                    Score = interview.Score,
                    StatusId = interview.StatusId,
                    StatusName = interview.Status?.Name,
                    Date = interview.Date,
                    PositionId = interview.PositionId,
                    Name = interview.Position?.Name,
                    TrackId = interview.TrackId,
                    TrackName = interview.Track?.Name,
                    EvalutaionFormId = interview.Position?.EvaluationId,
                    Notes = interview.Notes,
                    StopCycleNote = interview.StopCycleNote,
                    ParentId = interview.ParentId,
                    InterviewerId = interview.InterviewerId,
                    InterviewerName = userName,
                    CandidateId = interview.CandidateId,
                    FullName = interview.Candidate?.FullName,
                    CandidateCVAttachmentId = interview.Candidate?.CVAttachmentId,
                    AttachmentId = interview.AttachmentId,
                    InterviewerRole = interviewerRole,
                    ActualExperience = interview.ActualExperience,
                    SecondInterviewerId = interview.SecondInterviewerId,
                    SecondInterviewerName = secondUserName,
                    ArchitectureInterviewerId = interview.ArchitectureInterviewerId,
                    ArchitectureInterviewerName = archiName,
                    WorkflowStageId = interview.WorkflowStageId,
                    StageName = interview.WorkflowStage?.Name,
                    StartFromHR = interview.StartFromHR,
                    CreatedOn = interview.CreatedOn,
                    modifiedBy = interview.ModifiedBy,
                    ModifiedOn = interview.ModifiedOn
                };

                interviewsDTOs.Add(interviewDTO);
            }

            return Result<List<InterviewsDTO>>.Success(interviewsDTOs);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get HR-First flow interview details: {ex.Message}");
        }
    }

}