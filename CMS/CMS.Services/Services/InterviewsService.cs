using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class InterviewsService : IInterviewsService
{
    private readonly IInterviewsRepository _interviewsRepository;
    private readonly ICandidateService _candidateService;
    private readonly IPositionService _positionService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IStatusRepository _statusRepository;
    private readonly ICompanyService _companyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAttachmentService _attachmentService;
    private readonly IConfiguration _configuration;
    private readonly IDynamicWorkflowService _dynamicWorkflowService;
    private readonly IWorkflowService _workflowService;
    private readonly ISelectedInterviewersRepository _selectedInterviewersRepository;

    public InterviewsService(
        IInterviewsRepository interviewsRepository,
        ICandidateService candidateService,
        IPositionService positionService,
        IAttachmentService attachmentService,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor,
        IStatusRepository statusRepository,
        ICompanyService companyService,
        IConfiguration configuration,
        IDynamicWorkflowService dynamicWorkflowService,
        IWorkflowService workflowService,
        ISelectedInterviewersRepository selectedInterviewersRepository)
    {
        _interviewsRepository = interviewsRepository;
        _candidateService = candidateService;
        _positionService = positionService;
        _attachmentService = attachmentService;
        _userManager = userManager;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
        _statusRepository = statusRepository;
        _companyService = companyService;
        _configuration = configuration;
        _dynamicWorkflowService = dynamicWorkflowService;
        _workflowService = workflowService;
        _selectedInterviewersRepository = selectedInterviewersRepository;
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
            if (string.IsNullOrEmpty(id))
                return "User not found";

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

    public async Task<Result<InterviewsDTO>> Delete(int id)
    {
        try
        {
            Interviews interview = await _interviewsRepository.GetById(id);

            if (interview != null)
            {
                var interviewerIds = new List<string>();
                if (!string.IsNullOrEmpty(interview.InterviewerId))
                    interviewerIds.Add(interview.InterviewerId);
                if (!string.IsNullOrEmpty(interview.SecondInterviewerId))
                    interviewerIds.Add(interview.SecondInterviewerId);
                if (!string.IsNullOrEmpty(interview.ArchitectureInterviewerId))
                    interviewerIds.Add(interview.ArchitectureInterviewerId);

                if (interviewerIds.Count > 0)
                {
                    await _interviewsRepository.DeleteNotificationsByCandidateAndReceiversAsync(
                        interview.CandidateId, 
                        interviewerIds);
                }

                if (interview.ParentId == null)
                {
                    await _interviewsRepository.DeleteChildInterviewsAndNotificationsAsync(
                        id, 
                        interview.CandidateId);
                }

                if (interview.AttachmentId != null)
                {
                    int? attachmentToRemove = interview.AttachmentId;
                    await _interviewsRepository.Delete(id);

                    if (attachmentToRemove != null)
                        await _attachmentService.DeleteAttachmentAsync((int)attachmentToRemove);
                }
                else
                {
                    await _interviewsRepository.Delete(id);
                }
            }
            else
            {
                await _interviewsRepository.Delete(id);
            }

            return Result<InterviewsDTO>.Success(null);
        }
        catch (Exception ex)
        {
            return Result<InterviewsDTO>.Failure(null, $"An error occurred while deleting the Interview{ex.InnerException.Message}");
        }
    }

    public async Task<Result<List<InterviewsDTO>>> ShowHistory(int id)
    {
        List<InterviewsDTO> interviewsDTOs = [];
        try
        {
            Result<InterviewsDTO> currentInterviewResult = await GetInterviewDetails(id);
            InterviewsDTO currentInterview = currentInterviewResult.Value;

            if (currentInterview != null)
            {
                interviewsDTOs.Add(currentInterview);

                if (currentInterview.ParentId != null)
                {
                    while (currentInterview.ParentId != null)
                    {
                        Result<InterviewsDTO> parentInterviewResult = await GetInterviewDetails((int)currentInterview.ParentId);
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

    public async Task<Result<List<InterviewsDTO>>> GetAll()
    {
        try
        {
            List<Interviews> interviews = await _interviewsRepository.GetAll();

            if (interviews is null)
                return Result<List<InterviewsDTO>>.Failure(null, "No interviews found");

            List<InterviewsDTO> interviewsDTO = [];

            foreach (Interviews c in interviews)
            {
                string userName = await GetInterviewerName(c.InterviewerId);
                string SeconduserName = await GetInterviewerName(c.SecondInterviewerId);
                string archiName = await GetArchitectureName(c.ArchitectureInterviewerId);
                string interviewerRole = await GetInterviewerRole(c.InterviewerId);

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
                    EvalutaionFormId = c.Position.EvaluationId,
                    Notes = c.Notes,
                    StopCycleNote = c.StopCycleNote,
                    ParentId = c.ParentId,
                    InterviewerId = c.InterviewerId,
                    InterviewerName = userName,
                    CandidateId = c.CandidateId,
                    FullName = c.Candidate.FullName,
                    CandidateCVAttachmentId = c.Candidate.CVAttachmentId,
                    AttachmentId = c.AttachmentId,
                    InterviewerRole = interviewerRole,
                    ActualExperience = c.ActualExperience,
                    SecondInterviewerId = c.SecondInterviewerId,
                    SecondInterviewerName = SeconduserName,
                    CreatedOn = c.CreatedOn,
                    ArchitectureInterviewerId = c.ArchitectureInterviewerId,
                    ArchitectureInterviewerName = archiName,
                    WorkflowStageId = c.WorkflowStageId,
                    StageName = c.WorkflowStage?.Name,
                    StartFromHR = c.StartFromHR,
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

    public async Task<Result<List<InterviewsDTO>>> GetAllForGeneralManager()
    {
        try
        {
            List<Interviews> interviews = await _interviewsRepository.GetAll();

            if (interviews is null)
                return Result<List<InterviewsDTO>>.Failure(null, "No interviews found");

            List<InterviewsDTO> interviewsDTO = new List<InterviewsDTO>();

            foreach (Interviews c in interviews)
            {
                string userName = await GetInterviewerName(c.InterviewerId);
                string SeconduserName = await GetInterviewerName(c.SecondInterviewerId);
                string archiName = await GetArchitectureName(c.ArchitectureInterviewerId);
                string interviewerRole = await GetInterviewerRole(c.InterviewerId);

                if (interviewerRole.Equals("General Manager", StringComparison.OrdinalIgnoreCase))
                {
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
                        EvalutaionFormId = c.Position.EvaluationId,
                        Notes = c.Notes,
                        StopCycleNote = c.StopCycleNote,
                        ParentId = c.ParentId,
                        InterviewerId = c.InterviewerId,
                        InterviewerName = userName,
                        CandidateId = c.CandidateId,
                        FullName = c.Candidate.FullName,
                        CandidateCVAttachmentId = c.Candidate.CVAttachmentId,
                        AttachmentId = c.AttachmentId,
                        InterviewerRole = interviewerRole,
                        ActualExperience = c.ActualExperience,
                        WorkflowStageId = c.WorkflowStageId,
                        StageName = c.WorkflowStage?.Name,
                    };
                    interviewsDTO.Add(com);
                }
            }

            return Result<List<InterviewsDTO>>.Success(interviewsDTO);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get Interview: {ex.InnerException.Message}");
        }
    }

    public async Task<Result<InterviewsDTO>> GetInterviewDetailsWithAdditionalInfo(int id)
    {
        if (id <= 0)
            return Result<InterviewsDTO>.Failure(null, "Invalid interview id");

        try
        {
            Interviews interview = await _interviewsRepository.GetById(id);
            Interviews interviewNullParent = await _interviewsRepository.GetByParentIdAsync(interview.CandidateId);
            Interviews firstInterview = await _interviewsRepository.GetByParentIdAsync(interview.CandidateId);
            Interviews secondInterview = await _interviewsRepository.GetByInterviewerRoleAsync(interview.CandidateId, "General Manager");
            Interviews thirdInterview = await _interviewsRepository.GetThirdInterviewAsync(interview.CandidateId);
            string secondInterviewInterviewerName = secondInterview?.Interviewer?.UserName;
            string userName = await GetInterviewerName(interviewNullParent.InterviewerId);
            string SeconduserName = await GetInterviewerName(interviewNullParent.SecondInterviewerId);
            string archiName = await GetArchitectureName(firstInterview.ArchitectureInterviewerId);
            string interviewerRole = await GetInterviewerRole(interview.InterviewerId);
            double? firstInterviewScore = await GetFirstInterviewScore(id);
            double? firstEvaluation = await GetFirstEvaluationIdForDetails(id);

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
                Notes = firstInterview.Notes,
                StopCycleNote = interview.StopCycleNote,
                ParentId = interview.ParentId,
                InterviewerId = interviewNullParent.InterviewerId,
                InterviewerName = userName,
                CandidateId = interview.CandidateId,
                FullName = interview.Candidate.FullName,
                CandidateCVAttachmentId = interview.Candidate.CVAttachmentId,
                AttachmentId = (int?)firstEvaluation ?? null,
                InterviewerRole = interviewerRole,
                ActualExperience = firstInterview.ActualExperience,
                SecondInterviewerId = interviewNullParent.SecondInterviewerId,
                SecondInterviewerName = SeconduserName,
                CreatedOn = interview.CreatedOn,
                ArchitectureInterviewerId = firstInterview.ArchitectureInterviewerId,
                ArchitectureInterviewerName = archiName,
                SecondInterviewInterviewerName = secondInterviewInterviewerName,
                WorkflowStageId = interview.WorkflowStageId,
                StageName = interview.WorkflowStage?.Name,
            };

            if (secondInterview != null)
            {
                interviewDTO.SecondInterviewActualExperience = secondInterview.ActualExperience;
                interviewDTO.SecondInterviewNotes = secondInterview.Notes;
            }
            else
            {
                interviewDTO.SecondInterviewActualExperience = null;
                interviewDTO.SecondInterviewNotes = null;
            }

            if (thirdInterview != null)
                interviewDTO.HRNotes = thirdInterview.Notes;

            else
                interviewDTO.HRNotes = null;

            return Result<InterviewsDTO>.Success(interviewDTO);
        }
        catch (Exception ex)
        {
            return Result<InterviewsDTO>.Failure(null, $"unable to retrieve the Interview from the repository{ex.InnerException.Message}");
        }
    }

    public async Task<Result<InterviewsDTO>> GetInterviewDetails(int id)
    {
        if (id <= 0)
            return Result<InterviewsDTO>.Failure(null, "Invalid interview id");

        try
        {
            Interviews interview = await _interviewsRepository.GetById(id);
            
            string interviewerId = interview.InterviewerId;
            string secondInterviewerId = interview.SecondInterviewerId;
            string architectureInterviewerId = interview.ArchitectureInterviewerId;
            
            if (interview.StartFromHR == true && interview.ParentId == null)
            {
                var selectedInterviewers = await _selectedInterviewersRepository.GetByInterviewIdAsync(id);
                if (selectedInterviewers != null)
                {
                    interviewerId = selectedInterviewers.FirstInterviewerId;
                    secondInterviewerId = selectedInterviewers.SecondInterviewerId;
                    architectureInterviewerId = selectedInterviewers.ArchitectureInterviewerId;
                }
            }
            
            string userName = await GetInterviewerName(interviewerId);
            string SeconduserName = await GetInterviewerName(secondInterviewerId);
            string archiName = await GetArchitectureName(architectureInterviewerId);
            string interviewerRole = await GetInterviewerRole(interview.InterviewerId);
            double? firstInterviewScore = await GetFirstInterviewScore(id);
            double? firstEvaluation = GetFirstEvaluation(interview.AttachmentId);
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
                InterviewerId = interviewerId,
                InterviewerName = userName,
                CandidateId = interview.CandidateId,
                FullName = interview.Candidate.FullName,
                CandidateCVAttachmentId = interview.Candidate.CVAttachmentId,
                AttachmentId = (int?)firstEvaluation ?? null,
                InterviewerRole = interviewerRole,
                ActualExperience = interview.ActualExperience,
                SecondInterviewerId = secondInterviewerId,
                SecondInterviewerName = SeconduserName,
                ArchitectureInterviewerId = architectureInterviewerId,
                ArchitectureInterviewerName = archiName,
                WorkflowStageId = interview.WorkflowStageId,
                StageName = interview.WorkflowStage?.Name,
                StartFromHR = interview.StartFromHR,
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

            if (data.FileData != null)
            {
                int attachmentId = await _attachmentService.CreateAttachmentAsync(data.FileName, (long)data.FileSize, data.FileData);
                data.AttachmentId = attachmentId;
            }

            Status status = await _statusRepository.GetByCode(StatusCode.Pending);
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            int? initialStageId = null;
            if (data.StartFromHR)
            {
                initialStageId = (int)EnumWorkflowStage.HRInitialInterview;
                
                var hrUsers = await _userManager.GetUsersInRoleAsync("HR Manager");
                var hrUser = hrUsers.FirstOrDefault();
                if (hrUser == null)
                    return Result<InterviewsDTO>.Failure(data, "No HR Manager found in the system");

                Interviews hrInterview = new Interviews
                {
                    PositionId = data.PositionId,
                    TrackId = data.TrackId,
                    CandidateId = data.CandidateId,
                    Score = data.Score,
                    StatusId = status.Id,
                    Date = data.Date,
                    Notes = data.Notes,
                    StopCycleNote = data.StopCycleNote,
                    ParentId = null,
                    InterviewerId = hrUser.Id, 
                    AttachmentId = data.AttachmentId,
                    CreatedBy = currentUser.Id,
                    CreatedOn = DateTime.Now,
                    WorkflowStageId = initialStageId,
                    StartFromHR = true,
                };

                await _interviewsRepository.Insert(hrInterview);

                if (!string.IsNullOrEmpty(data.InterviewerId) || !string.IsNullOrEmpty(data.SecondInterviewerId) || !string.IsNullOrEmpty(data.ArchitectureInterviewerId))
                {
                    Domain.Entities.SelectedInterviewers selectedInterviewers = new Domain.Entities.SelectedInterviewers
                    {
                        InterviewId = hrInterview.InterviewsId,
                        FirstInterviewerId = data.InterviewerId,
                        SecondInterviewerId = data.SecondInterviewerId,
                        ArchitectureInterviewerId = data.ArchitectureInterviewerId,
                        CreatedBy = currentUser.Id,
                        CreatedOn = DateTime.Now,
                        IsActive = true,
                        IsDelete = false
                    };

                    await _selectedInterviewersRepository.InsertAsync(selectedInterviewers);
                }

                Interviews insertedInterview = await _interviewsRepository.GetById(hrInterview.InterviewsId);
                InterviewsDTO insertedInterviewDTO = new InterviewsDTO
                {
                    InterviewsId = insertedInterview.InterviewsId,
                };

                return Result<InterviewsDTO>.Success(insertedInterviewDTO);
            }
            else
            {
                var allStagesResult = await _workflowService.GetAllStagesAsync();
                if (allStagesResult.IsSuccess && allStagesResult.Value != null && allStagesResult.Value.Any())
                {
                    var firstStage = allStagesResult.Value
                        .Where(s => s.Id == (int)EnumWorkflowStage.InitialInterview)
                        .FirstOrDefault();
                    initialStageId = firstStage?.Id ?? (int)EnumWorkflowStage.InitialInterview;
                }
                else
                {
                    initialStageId = (int)EnumWorkflowStage.InitialInterview;
                }

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
                    CreatedBy = currentUser.Id,
                    CreatedOn = DateTime.Now,
                    SecondInterviewerId = data.SecondInterviewerId,
                    ArchitectureInterviewerId = data.ArchitectureInterviewerId,
                    WorkflowStageId = initialStageId,
                    StartFromHR = false,
                };

                await _interviewsRepository.Insert(interview);

                Interviews insertedInterview = await _interviewsRepository.GetById(interview.InterviewsId);
                InterviewsDTO insertedInterviewDTO = new InterviewsDTO
                {
                    InterviewsId = insertedInterview.InterviewsId,
                };

                return Result<InterviewsDTO>.Success(insertedInterviewDTO);
            }
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

            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            Interviews previouseInterview = await _interviewsRepository.GetByIdForEdit(data.InterviewsId);
            bool isRootHrInterview = data.StartFromHR && (data.ParentId ?? previouseInterview.ParentId) == null;

            Interviews interview = new Interviews
            {
                InterviewsId = data.InterviewsId,
                PositionId = data.PositionId,
                TrackId = data.TrackId,
                CandidateId = data.CandidateId,
                Score = data.Score,
                ParentId = data.ParentId ?? previouseInterview.ParentId,
                InterviewerId = data.InterviewerId,
                SecondInterviewerId = isRootHrInterview ? null : data.SecondInterviewerId,
                ArchitectureInterviewerId = isRootHrInterview ? null : data.ArchitectureInterviewerId,
                Date = data.Date,
                Notes = data.Notes,
                StopCycleNote = data.StopCycleNote,
                StatusId = (int)data.StatusId,
                AttachmentId = data.AttachmentId,
                ModifiedOn = DateTime.Now,
                ModifiedBy = currentUser.Id,
                CreatedBy = previouseInterview.CreatedBy,
                CreatedOn = previouseInterview.CreatedOn,
                StartFromHR = data.StartFromHR,
                WorkflowStageId = data.WorkflowStageId ?? previouseInterview.WorkflowStageId,
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

    public async Task ConductInterview(InterviewsDTO completedDTO, string firstinterviewer, string secondinterviewer)
    {
        try
        {
            string firstInterviewerRoles = !string.IsNullOrEmpty(firstinterviewer) 
                ? await GetInterviewerRole(firstinterviewer) 
                : null;
            string secondInterviewerRoles = !string.IsNullOrEmpty(secondinterviewer) 
                ? await GetInterviewerRole(secondinterviewer) 
                : null;
            IdentityUser currentUserrGM = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            string hrManagerIDRole = "226cca69-f046-4d15-8b81-9b9ba34f2214";
            string createdbyRole = await _interviewsRepository.GetRoleById(hrManagerIDRole);

            if (createdbyRole == "HR Manager" && await _userManager.IsInRoleAsync(currentUserrGM, "General Manager"))
            {
                IdentityUser currentUserr = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                Interviews intervieww = await _interviewsRepository.GetById(completedDTO.InterviewsId);
                if (completedDTO.FileData != null)
                {
                    int attachmentId = await _attachmentService.CreateAttachmentAsync(completedDTO.FileName, (long)completedDTO.FileSize, completedDTO.FileData);
                    completedDTO.AttachmentId = attachmentId;
                }

                Debug.Assert(intervieww != null, "No Interview Provided for Conduct Interview Method");
                intervieww.StatusId = (int)completedDTO.StatusId;
                intervieww.Score = completedDTO.Score;
                intervieww.Notes = completedDTO.Notes;
                intervieww.StopCycleNote = completedDTO.StopCycleNote;
                intervieww.ActualExperience = completedDTO.ActualExperience;
                intervieww.AttachmentId = completedDTO.AttachmentId;
                intervieww.ModifiedBy = currentUserr.Id;
                intervieww.ModifiedOn = DateTime.Now;
                intervieww.IsUpdated = true;
                await _interviewsRepository.Update(intervieww);

                bool isHRr = await _userManager.IsInRoleAsync(currentUserr, "HR Manager");
                if (!isHRr)
                {
                    Interviews generalManagerInterview = await _interviewsRepository.GetGeneralManagerInterviewForCandidate(intervieww.CandidateId);
                    if (generalManagerInterview != null)
                    {
                        generalManagerInterview.StatusId = (int)completedDTO.StatusId;
                        generalManagerInterview.Score = completedDTO.Score;
                        generalManagerInterview.Notes = completedDTO.Notes;
                        generalManagerInterview.ActualExperience = completedDTO.ActualExperience;
                        generalManagerInterview.AttachmentId = completedDTO.AttachmentId;
                        generalManagerInterview.ModifiedBy = currentUserr.Id;
                        generalManagerInterview.ModifiedOn = DateTime.Now;
                        generalManagerInterview.IsUpdated = true;
                        await _interviewsRepository.Update(generalManagerInterview);
                    }

                    Interviews archiInterview = await _interviewsRepository.GetArchiInterviewForCandidate(intervieww.CandidateId);
                    if (archiInterview != null)
                    {
                        archiInterview.StatusId = (int)completedDTO.StatusId;
                        archiInterview.Score = completedDTO.Score;
                        archiInterview.Notes = completedDTO.Notes;
                        archiInterview.ActualExperience = completedDTO.ActualExperience;
                        archiInterview.AttachmentId = completedDTO.AttachmentId;
                        archiInterview.ModifiedBy = currentUserr.Id;
                        archiInterview.ModifiedOn = DateTime.Now;
                        archiInterview.IsUpdated = true;
                        await _interviewsRepository.Update(archiInterview);
                    }
                }


                Status Completedstatuss = await _statusRepository.GetById((int)completedDTO.StatusId);
                bool isApprovedd = Completedstatuss.Code == StatusCode.Approved;
                bool isLastInterviewerAnHRr = await _userManager.IsInRoleAsync(intervieww.Interviewer, "HR Manager");
                bool isReverseWorkflow = intervieww.StartFromHR;
                
                if (isApprovedd && (!isLastInterviewerAnHRr || (isReverseWorkflow && isLastInterviewerAnHRr)))
                {
                    string completedByRole = await GetInterviewerRole(currentUserr.Id);
                    if (string.IsNullOrEmpty(completedByRole))
                    {
                        completedByRole = await GetInterviewerRole(intervieww.InterviewerId);
                    }

                    if (!string.IsNullOrEmpty(completedByRole))
                    {
                        await _dynamicWorkflowService.CreateNextStageInterviewsAsync(
                            completedDTO.InterviewsId,
                            completedByRole,
                            intervieww.CandidateId,
                            intervieww.PositionId,
                            intervieww.TrackId,
                            intervieww.Date,
                            currentUserr.Id
                        );
                    }
                }
            }
            else
            {
                IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                Interviews interview = await _interviewsRepository.GetById(completedDTO.InterviewsId);

                if (completedDTO.FileData != null)
                {
                    int attachmentId = await _attachmentService.CreateAttachmentAsync(completedDTO.FileName, (long)completedDTO.FileSize, completedDTO.FileData);
                    completedDTO.AttachmentId = attachmentId;
                }
                else
                {
                    if (!completedDTO.AttachmentId.HasValue && interview.AttachmentId.HasValue)
                    {
                        completedDTO.AttachmentId = interview.AttachmentId;
                    }
                }

                Debug.Assert(interview != null, "No Interview Provided for Conduct Interview Method");
                interview.StatusId = (int)completedDTO.StatusId;
                interview.Score = completedDTO.Score;
                interview.Notes = completedDTO.Notes;
                interview.ActualExperience = completedDTO.ActualExperience;
                interview.AttachmentId = completedDTO.AttachmentId ?? interview.AttachmentId;
                interview.ModifiedBy = currentUser.Id;
                interview.ModifiedOn = DateTime.Now;
                interview.IsUpdated = true;
                await _interviewsRepository.Update(interview);


                bool isHR = await _userManager.IsInRoleAsync(currentUser, "HR Manager");
                if (!isHR)
                {
                    Interviews generalManagerInterview = await _interviewsRepository.GetGeneralManagerInterviewForCandidate(interview.CandidateId);

                    if (generalManagerInterview != null)
                    {
                        generalManagerInterview.StatusId = (int)completedDTO.StatusId;
                        generalManagerInterview.Score = completedDTO.Score;
                        generalManagerInterview.Notes = completedDTO.Notes;
                        generalManagerInterview.ActualExperience = completedDTO.ActualExperience;
                        generalManagerInterview.AttachmentId = completedDTO.AttachmentId;
                        generalManagerInterview.ModifiedBy = currentUser.Id;
                        generalManagerInterview.ModifiedOn = DateTime.Now;
                        generalManagerInterview.IsUpdated = true;
                        await _interviewsRepository.Update(generalManagerInterview);
                    }

                    Interviews archiInterview = await _interviewsRepository.GetArchiInterviewForCandidate(interview.CandidateId);

                    if (archiInterview != null)
                    {
                        archiInterview.StatusId = (int)completedDTO.StatusId;
                        archiInterview.Score = completedDTO.Score;
                        archiInterview.Notes = completedDTO.Notes;
                        archiInterview.ActualExperience = completedDTO.ActualExperience;
                        archiInterview.AttachmentId = completedDTO.AttachmentId;
                        archiInterview.ModifiedBy = currentUser.Id;
                        archiInterview.ModifiedOn = DateTime.Now;
                        archiInterview.IsUpdated = true;
                        await _interviewsRepository.Update(archiInterview);
                    }

                    Interviews interviewerInterview = await _interviewsRepository.GetinterviewerInterviewForCandidate(interview.CandidateId);

                    if (interviewerInterview != null)
                    {
                        interviewerInterview.StatusId = (int)completedDTO.StatusId;
                        interviewerInterview.Score = completedDTO.Score;
                        interviewerInterview.Notes = completedDTO.Notes;
                        interviewerInterview.ActualExperience = completedDTO.ActualExperience;
                        interviewerInterview.AttachmentId = completedDTO.AttachmentId;
                        interviewerInterview.ModifiedBy = currentUser.Id;
                        interviewerInterview.ModifiedOn = DateTime.Now;
                        interviewerInterview.IsUpdated = true;
                        await _interviewsRepository.Update(interviewerInterview);
                    }
                }

                Status Completedstatus = await _statusRepository.GetById((int)completedDTO.StatusId);
                bool isApproved = Completedstatus.Code == StatusCode.Approved;
                bool isLastInterviewerAnHR = await _userManager.IsInRoleAsync(interview.Interviewer, "HR Manager");
                bool isReverseWorkflow = interview.StartFromHR;

                if (isApproved && (!isLastInterviewerAnHR || (isReverseWorkflow && isLastInterviewerAnHR)))
                {
                    string completedByRole = await GetInterviewerRole(currentUser.Id);
                    if (string.IsNullOrEmpty(completedByRole))
                    {
                        completedByRole = await GetInterviewerRole(interview.InterviewerId);
                    }

                    if (!string.IsNullOrEmpty(completedByRole))
                    {
                        await _dynamicWorkflowService.CreateNextStageInterviewsAsync(
                            completedDTO.InterviewsId,
                            completedByRole,
                            interview.CandidateId,
                            interview.PositionId,
                            interview.TrackId,
                            interview.Date,
                            currentUser.Id
                        );
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task ConductInterviewForGm(InterviewsDTO completedDTO)
    {
        try
        {
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            Interviews interview = await _interviewsRepository.GetById(completedDTO.InterviewsId);

            if (completedDTO.FileData != null)
            {
                int attachmentId = await _attachmentService.CreateAttachmentAsync(completedDTO.FileName, (long)completedDTO.FileSize, completedDTO.FileData);
                completedDTO.AttachmentId = attachmentId;
            }
            else
            {
                if (!completedDTO.AttachmentId.HasValue && interview.AttachmentId.HasValue)
                {
                    completedDTO.AttachmentId = interview.AttachmentId;
                }
            }

            Debug.Assert(interview != null, "No Interview Provided for Conduct Interview Method");

            interview.StatusId = (int)completedDTO.StatusId;
            interview.Score = completedDTO.Score;
            interview.Notes = completedDTO.Notes;
            interview.ActualExperience = completedDTO.ActualExperience;
            interview.AttachmentId = completedDTO.AttachmentId ?? interview.AttachmentId;
            interview.ModifiedBy = currentUser.Id;
            interview.ModifiedOn = DateTime.Now;
            interview.IsUpdated = true;
            await _interviewsRepository.Update(interview);


            bool isHR = await _userManager.IsInRoleAsync(currentUser, "HR Manager");

            if (!isHR)
            {
                Interviews generalManagerInterview = await _interviewsRepository.GetGeneralManagerInterviewForCandidate(interview.CandidateId);
                if (generalManagerInterview != null)
                {
                    generalManagerInterview.StatusId = (int)completedDTO.StatusId;
                    generalManagerInterview.Score = completedDTO.Score;
                    generalManagerInterview.Notes = completedDTO.Notes;
                    generalManagerInterview.ActualExperience = completedDTO.ActualExperience;
                    generalManagerInterview.AttachmentId = completedDTO.AttachmentId ?? generalManagerInterview.AttachmentId;
                    generalManagerInterview.ModifiedBy = currentUser.Id;
                    generalManagerInterview.ModifiedOn = DateTime.Now;
                    generalManagerInterview.IsUpdated = true;
                    await _interviewsRepository.Update(generalManagerInterview);
                }
                Interviews archiInterview = await _interviewsRepository.GetArchiInterviewForCandidate(interview.CandidateId);
                if (archiInterview != null)
                {
                    archiInterview.StatusId = (int)completedDTO.StatusId;
                    archiInterview.ActualExperience = completedDTO.ActualExperience;
                    archiInterview.AttachmentId = completedDTO.AttachmentId ?? archiInterview.AttachmentId;
                    archiInterview.ModifiedBy = currentUser.Id;
                    archiInterview.ModifiedOn = DateTime.Now;
                    archiInterview.IsUpdated = true;
                    await _interviewsRepository.Update(archiInterview);
                }
                Interviews interviewerInterview = await _interviewsRepository.GetinterviewerInterviewForCandidate(interview.CandidateId);
                if (interviewerInterview != null)
                {
                    interviewerInterview.StatusId = (int)completedDTO.StatusId;
                    interviewerInterview.Score = completedDTO.Score;
                    interviewerInterview.Notes = completedDTO.Notes;
                    interviewerInterview.ActualExperience = completedDTO.ActualExperience;
                    interviewerInterview.AttachmentId = completedDTO.AttachmentId ?? interviewerInterview.AttachmentId;
                    interviewerInterview.ModifiedBy = currentUser.Id;
                    interviewerInterview.ModifiedOn = DateTime.Now;
                    interviewerInterview.IsUpdated = true;
                    await _interviewsRepository.Update(interviewerInterview);
                }
            }

            Status Completedstatus = await _statusRepository.GetById((int)completedDTO.StatusId);
            bool isApproved = Completedstatus.Code == StatusCode.Approved;
            bool isLastInterviewerAnHR = await _userManager.IsInRoleAsync(interview.Interviewer, "HR Manager");
            bool isReverseWorkflow = interview.StartFromHR;

            if (isApproved && (!isLastInterviewerAnHR || (isReverseWorkflow && isLastInterviewerAnHR)))
            {
                string completedByRole = await GetInterviewerRole(currentUser.Id);
                if (string.IsNullOrEmpty(completedByRole))
                {
                    completedByRole = await GetInterviewerRole(interview.InterviewerId);
                }

                if (!string.IsNullOrEmpty(completedByRole))
                {
                    await _dynamicWorkflowService.CreateNextStageInterviewsAsync(
                        completedDTO.InterviewsId,
                        completedByRole,
                        interview.CandidateId,
                        interview.PositionId,
                        interview.TrackId,
                        interview.Date,
                        currentUser.Id
                    );
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task ConductInterviewForArchi(InterviewsDTO completedDTO)
    {
        try
        {
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            Interviews interview = await _interviewsRepository.GetById(completedDTO.InterviewsId);

            if (completedDTO.FileData != null)
            {
                int attachmentId = await _attachmentService.CreateAttachmentAsync(completedDTO.FileName, (long)completedDTO.FileSize, completedDTO.FileData);
                completedDTO.AttachmentId = attachmentId;
            }
            else
            {
                if (!completedDTO.AttachmentId.HasValue && interview.AttachmentId.HasValue)
                {
                    completedDTO.AttachmentId = interview.AttachmentId;
                }
            }

            Debug.Assert(interview != null, "No Interview Provided for Conduct Interview Method");

            interview.StatusId = (int)completedDTO.StatusId;
            interview.Score = completedDTO.Score;
            interview.Notes = completedDTO.Notes;
            interview.ActualExperience = completedDTO.ActualExperience;
            interview.AttachmentId = completedDTO.AttachmentId ?? interview.AttachmentId;
            interview.ModifiedBy = currentUser.Id;
            interview.ModifiedOn = DateTime.Now;
            interview.IsUpdated = true;
            await _interviewsRepository.Update(interview);


            bool isHR = await _userManager.IsInRoleAsync(currentUser, "HR Manager");

            if (!isHR)
            {
                List<Interviews> allCandidateInterviews = await _interviewsRepository.GetInterviewsByCandidateIdAsync(interview.CandidateId);
                
                var gmUsers = await _userManager.GetUsersInRoleAsync("General Manager");
                var gmUserIds = gmUsers.Select(u => u.Id).ToList();
                
                var archiUsers = await _userManager.GetUsersInRoleAsync("Solution Architecture");
                var archiUserIds = archiUsers.Select(u => u.Id).ToList();
                
                var interviewerUsers = await _userManager.GetUsersInRoleAsync("Interviewer");
                var interviewerUserIds = interviewerUsers.Select(u => u.Id).ToList();
                
                var ancestorIds = new HashSet<int>();
                Interviews? ancestor = interview;
                while (ancestor != null && ancestor.ParentId != null)
                {
                    var parent = allCandidateInterviews.FirstOrDefault(i => i.InterviewsId == ancestor.ParentId.Value);
                    if (parent == null)
                        break;
                    ancestorIds.Add(parent.InterviewsId);
                    ancestor = parent;
                }
                
                List<Interviews> parallelInterviews = allCandidateInterviews
                    .Where(i => i.InterviewsId != interview.InterviewsId &&
                                !ancestorIds.Contains(i.InterviewsId) &&
                                interview.ParentId != null && 
                                i.ParentId == interview.ParentId)
                    .ToList();

                Interviews generalManagerInterview = parallelInterviews
                    .FirstOrDefault(i => (i.InterviewerId != null && gmUserIds.Contains(i.InterviewerId)) ||
                                         (i.SecondInterviewerId != null && gmUserIds.Contains(i.SecondInterviewerId)));

                if (generalManagerInterview != null)
                {
                    generalManagerInterview.StatusId = (int)completedDTO.StatusId;
                    generalManagerInterview.Score = completedDTO.Score;
                    generalManagerInterview.Notes = completedDTO.Notes;
                    generalManagerInterview.ActualExperience = completedDTO.ActualExperience;
                    generalManagerInterview.AttachmentId = completedDTO.AttachmentId ?? generalManagerInterview.AttachmentId;
                    generalManagerInterview.ModifiedBy = currentUser.Id;
                    generalManagerInterview.ModifiedOn = DateTime.Now;
                    generalManagerInterview.IsUpdated = true;
                    await _interviewsRepository.Update(generalManagerInterview);
                }

                Interviews archiInterview = parallelInterviews
                    .FirstOrDefault(i => (i.InterviewerId != null && archiUserIds.Contains(i.InterviewerId)) ||
                                         (i.SecondInterviewerId != null && archiUserIds.Contains(i.SecondInterviewerId)) ||
                                         (!string.IsNullOrEmpty(i.ArchitectureInterviewerId) && archiUserIds.Contains(i.ArchitectureInterviewerId)));

                if (archiInterview != null)
                {
                    archiInterview.StatusId = (int)completedDTO.StatusId;
                    archiInterview.Score = completedDTO.Score;
                    archiInterview.Notes = completedDTO.Notes;
                    archiInterview.ActualExperience = completedDTO.ActualExperience;
                    archiInterview.AttachmentId = completedDTO.AttachmentId ?? archiInterview.AttachmentId;
                    archiInterview.ModifiedBy = currentUser.Id;
                    archiInterview.ModifiedOn = DateTime.Now;
                    archiInterview.IsUpdated = true;
                    await _interviewsRepository.Update(archiInterview);
                }

                Interviews interviewerInterview = parallelInterviews
                    .FirstOrDefault(i => (i.InterviewerId != null && interviewerUserIds.Contains(i.InterviewerId)) ||
                                         (i.SecondInterviewerId != null && interviewerUserIds.Contains(i.SecondInterviewerId)));

                if (interviewerInterview != null)
                {
                    interviewerInterview.StatusId = (int)completedDTO.StatusId;
                    interviewerInterview.Score = completedDTO.Score;
                    interviewerInterview.Notes = completedDTO.Notes;
                    interviewerInterview.ActualExperience = completedDTO.ActualExperience;
                    interviewerInterview.AttachmentId = completedDTO.AttachmentId ?? interviewerInterview.AttachmentId;
                    interviewerInterview.ModifiedBy = currentUser.Id;
                    interviewerInterview.ModifiedOn = DateTime.Now;
                    interviewerInterview.IsUpdated = true;
                    await _interviewsRepository.Update(interviewerInterview);
                }
            }

            Status Completedstatus = await _statusRepository.GetById((int)completedDTO.StatusId);
            bool isApproved = Completedstatus.Code == StatusCode.Approved;
            bool isLastInterviewerAnHR = await _userManager.IsInRoleAsync(interview.Interviewer, "HR Manager");
            bool isReverseWorkflow = interview.StartFromHR;

            if (isApproved && (!isLastInterviewerAnHR || (isReverseWorkflow && isLastInterviewerAnHR)))
            {
                string completedByRole = await GetInterviewerRole(currentUser.Id);
                if (string.IsNullOrEmpty(completedByRole))
                {
                    completedByRole = await GetInterviewerRole(interview.InterviewerId);
                }

                if (!string.IsNullOrEmpty(completedByRole))
                {
                    await _dynamicWorkflowService.CreateNextStageInterviewsAsync(
                        completedDTO.InterviewsId,
                        completedByRole,
                        interview.CandidateId,
                        interview.PositionId,
                        interview.TrackId,
                        interview.Date,
                        currentUser.Id
                    );
                }
            }
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
                return Result<List<InterviewsDTO>>.Failure(null, "No available interviews.");

            List<InterviewsDTO> interviewsDTOs = new List<InterviewsDTO>();

            foreach (Interviews i in interviews)
            {
                string userName = await GetInterviewerName(i.InterviewerId);
                string secondUserName = await GetInterviewerName(i.SecondInterviewerId);
                string archiName = await GetArchitectureName(i.ArchitectureInterviewerId);

                interviewsDTOs.Add(new InterviewsDTO
                {
                    InterviewsId = i.InterviewsId,
                    InterviewerId = i.InterviewerId,
                    InterviewerName = userName,
                    SecondInterviewerId = i.SecondInterviewerId,
                    SecondInterviewerName = secondUserName,
                    ArchitectureInterviewerId = i.ArchitectureInterviewerId,
                    ArchitectureInterviewerName = archiName,
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
                    CandidateCVAttachmentId = i.Candidate.CVAttachmentId,
                    AttachmentId = i.AttachmentId,
                    modifiedBy = i.ModifiedBy,
                    isUpdated = i.IsUpdated,
                    ActualExperience = i.ActualExperience,
                    WorkflowStageId = i.WorkflowStageId,
                    StageName = i.WorkflowStage?.Name,
                    StartFromHR = i.StartFromHR,
                });
            }

            return Result<List<InterviewsDTO>>.Success(interviewsDTOs);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get interviews: {ex.InnerException?.Message}");
        }
    }

    public async Task<bool> IsSolutionArchitect(string userId)
    {
        try
        {
            IdentityUser user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return false;

            return await _userManager.IsInRoleAsync(user, "Solution Architecture");
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

    private double? GetFirstEvaluation(int? attachmentId)
    {
        if (!attachmentId.HasValue)
            return null;

        return attachmentId;
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

    public async Task<Result<List<InterviewsDTO>>> ShowHistoryForHRFirstFlow(int id)
    {
        List<InterviewsDTO> interviewsDTOs = [];
        try
        {
            Result<InterviewsDTO> currentInterviewResult = await GetInterviewDetails(id);
            InterviewsDTO currentInterview = currentInterviewResult.Value;

            if (currentInterview == null)
                return Result<List<InterviewsDTO>>.Failure(null, "Interview not found");

            if (!currentInterview.StartFromHR)
                return Result<List<InterviewsDTO>>.Failure(null, "This is not an HR-First flow interview");

            InterviewsDTO rootInterview = currentInterview;
            while (rootInterview.ParentId != null)
            {
                Result<InterviewsDTO> parentInterviewResult = await GetInterviewDetails((int)rootInterview.ParentId);
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

    private async Task<double?> GetFirstEvaluationIdForDetails(int interviewId)
    {
        var interview = await _interviewsRepository.GetById(interviewId);

        if (interview?.ParentId != null)
        {
            return await GetFirstEvaluationIdForDetails(interview.ParentId.Value);
        }

        return interview?.AttachmentId;
    }


    public async Task<Result<int>> SaveStopCycleNote(int id, string note)
    {
        try
        {
            Interviews interview = await _interviewsRepository.GetLastInterviewBeforePendingByCandidateId(id);

            if (interview != null)
            {
                interview.StopCycleNote = note;
                await _interviewsRepository.Update(interview);
                return Result<int>.Success(interview.InterviewsId);
            }

            else
                return Result<int>.Failure(-1, "Interview not found.");
        }
        catch (Exception)
        {
            return Result<int>.Failure(-1, "Interview not found.");
        }
    }

    public async Task<bool> DeletePendingInterviews(int candidateId, InterviewsDTO collection)
    {
        try
        {
            bool pendingInterviews = await _interviewsRepository.DeletePendingInterviewsforStopCycle(candidateId, collection.PositionId);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<Result<bool>> AddArchitectureInterviewer(int interviewId, string architectureId)
    {
        try
        {
            var interview = await _interviewsRepository.GetById(interviewId);
            if (interview == null)
            {
                return Result<bool>.Failure(false, "Interview not found.");
            }

            interview.SecondInterviewerId = architectureId;
            await _interviewsRepository.Update(interview);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(false, "Failed to update architecture interviewer.");
        }
    }

    public async Task<Result<bool>> RemoveArchitectureInterviewer(int interviewId)
    {
        try
        {
            var interview = await _interviewsRepository.GetById(interviewId);
            if (interview == null)
            {
                return Result<bool>.Failure(false, "Interview not found.");
            }

            interview.SecondInterviewerId = null;
            await _interviewsRepository.Update(interview);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(false, "Failed to remove Architecture Interviewer.");
        }
    }

    public async Task<Result<bool>> AddOrUpdateArchitectureInterviewer(int interviewId, string architectureId)
    {
        try
        {
            var interview = await _interviewsRepository.GetById(interviewId);
            if (interview == null)
            {
                return Result<bool>.Failure(false, "Interview not found.");
            }

            interview.SecondInterviewerId = architectureId;
            await _interviewsRepository.Update(interview);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(false, "Failed to add or update Architecture Interviewer.");
        }
    }

    public async Task<bool> DoesInterviewExistForCandidate(int candidateId) => await _interviewsRepository.DoesInterviewExistForCandidateAsync(candidateId);

    public async Task<Result<List<InterviewsDTO>>> GetInterviewsWithoutResults()
    {
        try
        {
            List<Interviews> firstInterviews = await _interviewsRepository.GetFirstInterviews();
            int interviewReminderDaysDelay = _configuration.GetValue<int>("HangfireSettings:InterviewReminderDaysDelay");

            if (firstInterviews == null || firstInterviews.Count == 0)
                return Result<List<InterviewsDTO>>.Failure(null, "No interviews found.");

            List<InterviewsDTO> interviewsDtoList = [];

            foreach (var interview in firstInterviews)
            {
                if (interview.Score == null
                    && DateTime.UtcNow >= interview.Date.ToUniversalTime().AddDays(interviewReminderDaysDelay)
                    && interview.Date >= DateTime.Parse("2025-01-01 00:00:00.000"))
                {
                    IdentityUser interviewer = await _userManager.FindByIdAsync(interview.InterviewerId);

                    if (interviewer != null)
                    {
                        IList<string> roles = await _userManager.GetRolesAsync(interviewer);

                        if (!roles.Contains("General Manager", StringComparer.OrdinalIgnoreCase))
                        {
                            interviewsDtoList.Add(new InterviewsDTO
                            {
                                InterviewsId = interview.InterviewsId,
                                CandidateId = interview.CandidateId,
                                FullName = interview.Candidate?.FullName,
                                InterviewerId = interview.InterviewerId,
                                Date = interview.Date,
                                StatusId = interview.StatusId,
                                PositionName = interview.Position.Name
                            });
                        }
                    }
                }
            }

            return Result<List<InterviewsDTO>>.Success(interviewsDtoList);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"An error occurred while fetching interviews: {ex.Message}");
        }
    }
}