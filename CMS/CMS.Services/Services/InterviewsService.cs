using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
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
    private readonly IDelegationDurationRepository _delegationDurationRepository;
    private readonly IUserRepository _userRepository;

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
        ISelectedInterviewersRepository selectedInterviewersRepository,
        IDelegationDurationRepository delegationDurationRepository,
        IUserRepository userRepository)
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
        _delegationDurationRepository = delegationDurationRepository;
        _userRepository = userRepository;
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

            await EnrichHistoryStepsAsync(interviewsDTOs);
            return Result<List<InterviewsDTO>>.Success(interviewsDTOs);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get interview History: {ex.Message}");
        }
    }

    /// <summary>
    /// History rows come from the interview-details mapping, which copies some values (interviewer,
    /// notes, experience, evaluation) from the first interview. Fill each step from its own record and
    /// record who submitted it and when, so the history shows what actually happened at each step.
    /// </summary>
    private async Task EnrichHistoryStepsAsync(List<InterviewsDTO> steps)
    {
        if (steps == null)
            return;

        foreach (InterviewsDTO dto in steps)
        {
            if (dto == null)
                continue;

            Interviews step = await _interviewsRepository.GetById(dto.InterviewsId);
            if (step == null)
                continue;

            dto.InterviewerId = step.InterviewerId;
            dto.InterviewerName = await HistoryUserNameAsync(step.InterviewerId);
            dto.SecondInterviewerId = step.SecondInterviewerId;
            dto.SecondInterviewerName = await HistoryUserNameAsync(step.SecondInterviewerId);
            dto.InterviewerRole = await HistoryUserRoleAsync(step.InterviewerId);
            dto.Score = step.Score;
            dto.Notes = step.Notes;
            dto.ActualExperience = step.ActualExperience;
            dto.AttachmentId = step.AttachmentId;
            dto.isUpdated = step.IsUpdated;
            dto.StartFromHR = step.StartFromHR;
            dto.CreatedOn = step.CreatedOn;
            dto.ModifiedOn = step.ModifiedOn;
            dto.modifiedBy = await HistoryUserNameAsync(step.ModifiedBy);
            await ApplyDelegatedSubmitterAsync(dto, step.InterviewsId);
        }
    }

    private async Task<string> HistoryUserNameAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        IdentityUser user = await _userManager.FindByIdAsync(userId);
        return user?.UserName;
    }

    private async Task<string> HistoryUserRoleAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        IdentityUser user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        IList<string> roles = await _userManager.GetRolesAsync(user);
        return roles?.FirstOrDefault();
    }

    public async Task<Result<List<InterviewsDTO>>> GetAll()
    {
        try
        {
            List<Interviews> interviews = await _interviewsRepository.GetAll();

            if (interviews is null)
                return Result<List<InterviewsDTO>>.Failure(null, "No interviews found");

            return Result<List<InterviewsDTO>>.Success(await ToInterviewListDtosAsync(interviews));
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get Interview: {ex.InnerException.Message}");
        }
    }

    /// <summary>
    /// One page of the Interviews list: each candidate's latest interview matching the filter,
    /// newest first. Only the page rows are loaded and mapped.
    /// </summary>
    public async Task<Result<PaginatedList<InterviewsDTO>>> GetLatestInterviewPerCandidatePageAsync(
        InterviewListFilter filter,
        int pageNumber,
        int pageSize)
    {
        try
        {
            (List<Interviews> items, int totalCount) = await _interviewsRepository.GetLatestInterviewPerCandidatePageAsync(filter, pageNumber, pageSize);
            List<InterviewsDTO> page = await ToInterviewListDtosAsync(items);
            return Result<PaginatedList<InterviewsDTO>>.Success(new PaginatedList<InterviewsDTO>(page, totalCount, pageNumber, pageSize));
        }
        catch (Exception ex)
        {
            return Result<PaginatedList<InterviewsDTO>>.Failure(null, $"Unable to get Interview: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>
    /// Maps interviews for the list pages. Interviewer names and roles are read for all rows in two
    /// queries instead of several identity lookups per row; values match GetInterviewerName /
    /// GetArchitectureName / GetInterviewerRole ("User not found" when missing).
    /// </summary>
    private async Task<List<InterviewsDTO>> ToInterviewListDtosAsync(List<Interviews> interviews)
    {
        List<string> userIds = interviews
            .SelectMany(i => new[] { i.InterviewerId, i.SecondInterviewerId, i.ArchitectureInterviewerId })
            .ToList();
        Dictionary<string, string> names = await _userRepository.GetUserNamesAsync(userIds);
        Dictionary<string, string> roles = await _userRepository.GetFirstRoleNamesAsync(interviews.Select(i => i.InterviewerId));

        string NameOf(string id) => !string.IsNullOrEmpty(id) && names.TryGetValue(id, out string name) ? name : "User not found";
        string RoleOf(string id) => !string.IsNullOrEmpty(id) && names.ContainsKey(id) && roles.TryGetValue(id, out string role) ? role : "User not found";

        List<InterviewsDTO> interviewsDTO = [];

        foreach (Interviews c in interviews)
        {
            string userName = NameOf(c.InterviewerId);
            string SeconduserName = NameOf(c.SecondInterviewerId);
            string archiName = NameOf(c.ArchitectureInterviewerId);
            string interviewerRole = RoleOf(c.InterviewerId);

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

        return interviewsDTO;
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

            await ApplyDelegatedSubmitterAsync(interviewDTO, interview.InterviewsId);

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
            string delegatedWorkflowRole = await GetDelegatedWorkflowRoleAsync(completedDTO.InterviewsId, currentUserrGM?.Id);
            string hrManagerIDRole = "226cca69-f046-4d15-8b81-9b9ba34f2214";
            string createdbyRole = await _interviewsRepository.GetRoleById(hrManagerIDRole);
            bool enterGmBranch = delegatedWorkflowRole != null
                ? delegatedWorkflowRole == "General Manager"
                : await _userManager.IsInRoleAsync(currentUserrGM, "General Manager");

            if (createdbyRole == "HR Manager" && enterGmBranch)
            {
                IdentityUser currentUserr = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                Interviews intervieww = await _interviewsRepository.GetById(completedDTO.InterviewsId);
                if (completedDTO.FileData != null)
                {
                    int attachmentId = await _attachmentService.CreateAttachmentAsync(completedDTO.FileName, (long)completedDTO.FileSize, completedDTO.FileData);
                    completedDTO.AttachmentId = attachmentId;
                }

                Debug.Assert(intervieww != null, "No Interview Provided for Conduct Interview Method");
                await EnsurePendingAssignmentAfterReloadAsync(intervieww);
                Status previousStatusForGmPath = await _statusRepository.GetById(intervieww.StatusId);
                bool wasAlreadyApprovedForGmPath = previousStatusForGmPath?.Code == StatusCode.Approved;
                intervieww.StatusId = (int)completedDTO.StatusId;
                intervieww.Score = completedDTO.Score;
                intervieww.Notes = completedDTO.Notes;
                intervieww.StopCycleNote = completedDTO.StopCycleNote;
                intervieww.ActualExperience = completedDTO.ActualExperience;
                intervieww.AttachmentId = completedDTO.AttachmentId;
                intervieww.ModifiedBy = currentUserr.Id;
                intervieww.ModifiedOn = DateTime.Now;
                intervieww.IsUpdated = true;
                if (previousStatusForGmPath?.Code == StatusCode.Pending)
                {
                    string saveResult = await _interviewsRepository.TryCompletePendingIfAssignedAsync(intervieww, currentUserr.Id);
                    if (saveResult != "Saved")
                        throw new InvalidOperationException(DescribeSubmitFailure(saveResult));
                }
                else
                {
                    await _interviewsRepository.Update(intervieww);
                }

                bool isHRr = delegatedWorkflowRole != null
                    ? delegatedWorkflowRole == "HR Manager"
                    : await _userManager.IsInRoleAsync(currentUserr, "HR Manager");
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
                
                if (isApprovedd && !wasAlreadyApprovedForGmPath && (!isLastInterviewerAnHRr || (isReverseWorkflow && isLastInterviewerAnHRr)))
                {
                    string completedByRole = delegatedWorkflowRole ?? await GetInterviewerRole(currentUserr.Id);
                    if (string.IsNullOrEmpty(completedByRole) || completedByRole == "User not found")
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
                await EnsurePendingAssignmentAfterReloadAsync(interview);
                Status previousStatus = await _statusRepository.GetById(interview.StatusId);
                bool wasAlreadyApproved = previousStatus?.Code == StatusCode.Approved;
                interview.StatusId = (int)completedDTO.StatusId;
                interview.Score = completedDTO.Score;
                interview.Notes = completedDTO.Notes;
                interview.ActualExperience = completedDTO.ActualExperience;
                interview.AttachmentId = completedDTO.AttachmentId ?? interview.AttachmentId;
                interview.ModifiedBy = currentUser.Id;
                interview.ModifiedOn = DateTime.Now;
                interview.IsUpdated = true;
                if (previousStatus?.Code == StatusCode.Pending)
                {
                    string saveResult = await _interviewsRepository.TryCompletePendingIfAssignedAsync(interview, currentUser.Id);
                    if (saveResult != "Saved")
                        throw new InvalidOperationException(DescribeSubmitFailure(saveResult));
                }
                else
                {
                    await _interviewsRepository.Update(interview);
                }


                bool isHR = delegatedWorkflowRole != null
                    ? delegatedWorkflowRole == "HR Manager"
                    : await _userManager.IsInRoleAsync(currentUser, "HR Manager");
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

                if (isApproved && !wasAlreadyApproved && (!isLastInterviewerAnHR || (isReverseWorkflow && isLastInterviewerAnHR)))
                {
                    string completedByRole = delegatedWorkflowRole ?? await GetInterviewerRole(currentUser.Id);
                    if (string.IsNullOrEmpty(completedByRole) || completedByRole == "User not found")
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
            string delegatedWorkflowRole = await GetDelegatedWorkflowRoleAsync(completedDTO.InterviewsId, currentUser?.Id);
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
            await EnsurePendingAssignmentAfterReloadAsync(interview);

            Status previousStatusForGm = await _statusRepository.GetById(interview.StatusId);
            bool wasAlreadyApprovedForGm = previousStatusForGm?.Code == StatusCode.Approved;

            interview.StatusId = (int)completedDTO.StatusId;
            interview.Score = completedDTO.Score;
            interview.Notes = completedDTO.Notes;
            interview.ActualExperience = completedDTO.ActualExperience;
            interview.AttachmentId = completedDTO.AttachmentId ?? interview.AttachmentId;
            interview.ModifiedBy = currentUser.Id;
            interview.ModifiedOn = DateTime.Now;
            interview.IsUpdated = true;
            if (previousStatusForGm?.Code == StatusCode.Pending)
            {
                string saveResult = await _interviewsRepository.TryCompletePendingIfAssignedAsync(interview, currentUser.Id);
                if (saveResult != "Saved")
                    throw new InvalidOperationException(DescribeSubmitFailure(saveResult));
            }
            else
            {
                await _interviewsRepository.Update(interview);
            }


            bool isHR = delegatedWorkflowRole != null
                ? delegatedWorkflowRole == "HR Manager"
                : await _userManager.IsInRoleAsync(currentUser, "HR Manager");

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

            if (isApproved && !wasAlreadyApprovedForGm && (!isLastInterviewerAnHR || (isReverseWorkflow && isLastInterviewerAnHR)))
            {
                string completedByRole = delegatedWorkflowRole ?? await GetInterviewerRole(currentUser.Id);
                if (string.IsNullOrEmpty(completedByRole) || completedByRole == "User not found")
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
            string delegatedWorkflowRole = await GetDelegatedWorkflowRoleAsync(completedDTO.InterviewsId, currentUser?.Id);
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
            await EnsurePendingAssignmentAfterReloadAsync(interview);

            Status previousStatusForArchi = await _statusRepository.GetById(interview.StatusId);
            bool wasAlreadyApprovedForArchi = previousStatusForArchi?.Code == StatusCode.Approved;

            interview.StatusId = (int)completedDTO.StatusId;
            interview.Score = completedDTO.Score;
            interview.Notes = completedDTO.Notes;
            interview.ActualExperience = completedDTO.ActualExperience;
            interview.AttachmentId = completedDTO.AttachmentId ?? interview.AttachmentId;
            interview.ModifiedBy = currentUser.Id;
            interview.ModifiedOn = DateTime.Now;
            interview.IsUpdated = true;
            if (previousStatusForArchi?.Code == StatusCode.Pending)
            {
                string saveResult = await _interviewsRepository.TryCompletePendingIfAssignedAsync(interview, currentUser.Id);
                if (saveResult != "Saved")
                    throw new InvalidOperationException(DescribeSubmitFailure(saveResult));
            }
            else
            {
                await _interviewsRepository.Update(interview);
            }


            bool isHR = delegatedWorkflowRole != null
                ? delegatedWorkflowRole == "HR Manager"
                : await _userManager.IsInRoleAsync(currentUser, "HR Manager");

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

            if (isApproved && !wasAlreadyApprovedForArchi && (!isLastInterviewerAnHR || (isReverseWorkflow && isLastInterviewerAnHR)))
            {
                string completedByRole = delegatedWorkflowRole ?? await GetInterviewerRole(currentUser.Id);
                if (string.IsNullOrEmpty(completedByRole) || completedByRole == "User not found")
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

                await ApplyDelegatedSubmitterAsync(interviewDTO, interview.InterviewsId);
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

            await EnrichHistoryStepsAsync(interviewsDTOs);
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

    /// <summary>
    /// Re-reads the interview right before a reminder is sent, so one scored or moved off Pending
    /// while the job was running is skipped.
    /// </summary>
    public async Task<bool> IsInterviewStillPendingForReminderAsync(int interviewId)
    {
        Status pending = await _statusRepository.GetByCode(StatusCode.Pending);
        return await _interviewsRepository.IsStillPendingAsync(interviewId, pending.Id);
    }

    public Task MarkReminderSentAsync(int interviewId) => _interviewsRepository.MarkReminderSentAsync(interviewId, DateTime.Now);

    public async Task<Result<List<InterviewsDTO>>> GetInterviewsWithoutResults()
    {
        try
        {
            int interviewReminderDaysDelay = _configuration.GetValue<int>("HangfireSettings:InterviewReminderDaysDelay");
            Status pending = await _statusRepository.GetByCode(StatusCode.Pending);

            // The latest state is decided in the query: only the candidate's current step, only
            // Pending records without a score, overdue, and not already reminded today.
            DateTime now = DateTime.Now;
            List<Interviews> dueInterviews = await _interviewsRepository.GetCurrentPendingInterviewsDueReminderAsync(
                pending.Id,
                notBefore: DateTime.Parse("2025-01-01 00:00:00.000"),
                dueBy: now.AddDays(-interviewReminderDaysDelay),
                remindedBefore: now.Date);

            List<InterviewsDTO> interviewsDtoList = [];

            foreach (var interview in dueInterviews)
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

            return Result<List<InterviewsDTO>>.Success(interviewsDtoList);
        }
        catch (Exception ex)
        {
            return Result<List<InterviewsDTO>>.Failure(null, $"An error occurred while fetching interviews: {ex.Message}");
        }
    }

    private const string PrimaryInterviewerSlot = "Primary";
    private const string SecondInterviewerSlot = "Second";

    public async Task<Result<DelegateInterviewPageDTO>> GetPendingInterviewDelegationAsync(int interviewId)
    {
        try
        {
            Result<bool> authorization = await EnsureUserCanDelegateAsync();
            if (!authorization.IsSuccess)
                return Result<DelegateInterviewPageDTO>.Failure(null, authorization.Error);

            Interviews interview = await _interviewsRepository.GetByIdForEdit(interviewId);
            if (interview == null)
                return Result<DelegateInterviewPageDTO>.Failure(null, "Interview not found.");

            // A scored interview still opens, read-only, so its delegation history stays visible.
            List<InterviewDelegation> history = await _interviewsRepository.GetDelegationsByInterviewIdAsync(interviewId);
            List<DelegationDuration> durations = await _delegationDurationRepository.GetActiveAsync();

            DelegateInterviewPageDTO page = new()
            {
                InterviewId = interview.InterviewsId,
                CandidateName = interview.Candidate?.FullName,
                PositionName = interview.Position?.Name,
                InterviewDate = interview.Date,
                StatusName = interview.Status?.Name,
                PrimaryInterviewerId = interview.InterviewerId,
                PrimaryInterviewerName = await GetInterviewerName(interview.InterviewerId),
                PrimaryRole = await GetInterviewerRole(interview.InterviewerId),
                SecondInterviewerId = interview.SecondInterviewerId,
                SecondInterviewerName = string.IsNullOrEmpty(interview.SecondInterviewerId)
                    ? null
                    : await GetInterviewerName(interview.SecondInterviewerId),
                SecondRole = string.IsNullOrEmpty(interview.SecondInterviewerId)
                    ? null
                    : await GetInterviewerRole(interview.SecondInterviewerId),
                Durations = durations.Select(d => new DelegationDurationDTO
                {
                    Id = d.Id,
                    Name = d.Name,
                    Amount = d.Amount,
                    Unit = d.Unit,
                    DurationMinutes = d.DurationMinutes,
                    IsUnlimited = d.IsUnlimited,
                    IsActive = d.IsActive
                }).ToList(),
                DelegateCandidates = await GetDelegationCandidateUsersAsync(interview.InterviewerId, interview.SecondInterviewerId),
                History = []
            };

            DateTime now = DateTime.Now;
            bool interviewIsPending = IsInterviewStillPending(interview);
            page.IsPending = interviewIsPending;
            if (!interviewIsPending)
            {
                page.Durations = [];
                page.DelegateCandidates = [];
            }

            foreach (InterviewDelegation item in history)
                page.History.Add(await ToHistoryItemAsync(item, interview, now, interviewIsPending));

            page.ActivePrimary = page.History.FirstOrDefault(h => h.IsOpen && h.InterviewerSlot == PrimaryInterviewerSlot);
            page.ActiveSecond = page.History.FirstOrDefault(h => h.IsOpen && h.InterviewerSlot == SecondInterviewerSlot);

            return Result<DelegateInterviewPageDTO>.Success(page);
        }
        catch (Exception ex)
        {
            return Result<DelegateInterviewPageDTO>.Failure(null, $"Unable to load the pending interview for delegation: {ex.Message}");
        }
    }

    public async Task<Result<InterviewDelegationDTO>> DelegatePendingInterviewAsync(
        int interviewId,
        string interviewerSlot,
        string originalInterviewerId,
        string delegatedUserId,
        int durationId,
        int? replaceDelegationId = null)
    {
        try
        {
            Result<bool> authorization = await EnsureUserCanDelegateAsync();
            if (!authorization.IsSuccess)
                return Result<InterviewDelegationDTO>.Failure(null, authorization.Error);

            if (string.IsNullOrWhiteSpace(delegatedUserId))
                return Result<InterviewDelegationDTO>.Failure(null, "Please select a user to delegate to.");

            if (string.IsNullOrWhiteSpace(originalInterviewerId))
                return Result<InterviewDelegationDTO>.Failure(null, "The original interviewer could not be confirmed. Refresh the page and try again.");

            DelegationDuration duration = await _delegationDurationRepository.GetActiveByIdAsync(durationId);
            if (duration == null)
                return Result<InterviewDelegationDTO>.Failure(null, "Please select an active delegation duration.");

            DateTime startDate = DateTime.Now;
            DateTime expirationDate = duration.IsUnlimited
                ? InterviewDelegation.NoExpirationDate
                : startDate.AddMinutes(duration.DurationMinutes);

            bool secondSlot = string.Equals(interviewerSlot, SecondInterviewerSlot, StringComparison.OrdinalIgnoreCase);
            if (!secondSlot && !string.Equals(interviewerSlot, PrimaryInterviewerSlot, StringComparison.OrdinalIgnoreCase))
                return Result<InterviewDelegationDTO>.Failure(null, "Select which interviewer this delegation covers.");

            Interviews interview = await _interviewsRepository.GetByIdForEdit(interviewId);
            if (interview == null)
                return Result<InterviewDelegationDTO>.Failure(null, "Interview not found.");

            if (!IsInterviewStillPending(interview))
                return Result<InterviewDelegationDTO>.Failure(null, "This interview is no longer pending. It may already have been completed.");

            string currentInterviewerId = secondSlot ? interview.SecondInterviewerId : interview.InterviewerId;
            if (string.IsNullOrEmpty(currentInterviewerId))
                return Result<InterviewDelegationDTO>.Failure(null, "This interview does not have an interviewer in the selected slot.");

            if (!string.Equals(currentInterviewerId, originalInterviewerId, StringComparison.Ordinal))
                return Result<InterviewDelegationDTO>.Failure(null, "The original interviewer changed. Refresh the page and try again.");

            if (string.Equals(interview.InterviewerId, delegatedUserId, StringComparison.Ordinal)
                || string.Equals(interview.SecondInterviewerId, delegatedUserId, StringComparison.Ordinal))
                return Result<InterviewDelegationDTO>.Failure(null, "The selected delegate is already the assigned interviewer.");

            IdentityUser delegatedUser = await _userManager.FindByIdAsync(delegatedUserId);
            if (delegatedUser == null || !await IsEligibleDelegateUserAsync(delegatedUser))
                return Result<InterviewDelegationDTO>.Failure(null, "The selected user is not eligible to act for this interview.");

            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            InterviewDelegation delegation = new()
            {
                InterviewId = interview.InterviewsId,
                CandidateId = interview.CandidateId,
                OriginalInterviewerId = currentInterviewerId,
                DelegatedUserId = delegatedUserId,
                DelegatedByUserId = currentUser.Id,
                StartDate = startDate,
                ExpirationDate = expirationDate,
                DurationName = duration.Name,
                DurationMinutes = duration.IsUnlimited ? null : duration.DurationMinutes,
                WorkflowStageId = interview.WorkflowStageId,
                InterviewerSlot = secondSlot ? SecondInterviewerSlot : PrimaryInterviewerSlot,
                Status = InterviewDelegation.StatusActive,
                CreatedBy = currentUser.Id,
                ModifiedBy = currentUser.Id
            };

            string updateResult = await _interviewsRepository.InsertInterviewDelegationAsync(
                interview.InterviewsId,
                currentInterviewerId,
                secondSlot,
                delegation,
                replaceDelegationId);

            string failure = MapDelegationFailure(updateResult);
            if (failure != null)
                return Result<InterviewDelegationDTO>.Failure(null, failure);

            return Result<InterviewDelegationDTO>.Success(new InterviewDelegationDTO
            {
                InterviewId = interview.InterviewsId,
                CandidateId = interview.CandidateId,
                CandidateName = interview.Candidate?.FullName,
                PositionId = interview.PositionId,
                PositionName = interview.Position?.Name,
                StageName = await GetStageNameAsync(interview.WorkflowStageId),
                InterviewDate = interview.Date,
                StatusId = interview.StatusId,
                OriginalInterviewerId = currentInterviewerId,
                OriginalInterviewerName = await GetInterviewerName(currentInterviewerId),
                DelegatedUserId = delegatedUserId,
                DelegatedUserName = delegatedUser.UserName,
                DelegatedByUserId = currentUser.Id,
                StartDate = startDate,
                ExpirationDate = expirationDate,
                WorkflowStageId = interview.WorkflowStageId,
                InterviewerSlot = delegation.InterviewerSlot
            });
        }
        catch (Exception ex)
        {
            return Result<InterviewDelegationDTO>.Failure(null, $"Unable to delegate the pending interview: {ex.Message}");
        }
    }

    public async Task<Result<InterviewDelegationDTO>> RemoveInterviewDelegationAsync(int interviewId, int delegationId)
    {
        try
        {
            Result<bool> authorization = await EnsureUserCanDelegateAsync();
            if (!authorization.IsSuccess)
                return Result<InterviewDelegationDTO>.Failure(null, authorization.Error);

            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            if (currentUser == null)
                return Result<InterviewDelegationDTO>.Failure(null, "You must log in first.");

            List<InterviewDelegation> existing = await _interviewsRepository.GetDelegationsByInterviewIdAsync(interviewId);
            InterviewDelegation current = existing.FirstOrDefault(d => d.Id == delegationId);
            if (current == null)
                return Result<InterviewDelegationDTO>.Failure(null, "This delegation could not be found.");

            string cancelResult = await _interviewsRepository.CancelInterviewDelegationAsync(interviewId, delegationId, currentUser.Id);
            string failure = cancelResult switch
            {
                "Success" => null,
                "NotFound" => "This delegation could not be found.",
                "AlreadyRemoved" => "This delegation has already been removed.",
                "ScoreSubmitted" => "The score has already been submitted, so this delegation is complete and can't be removed.",
                "Expired" => "This delegation has already expired. The original interviewer can already submit the score.",
                _ => "The delegation could not be removed."
            };
            if (failure != null)
                return Result<InterviewDelegationDTO>.Failure(null, failure);

            Interviews interview = await _interviewsRepository.GetByIdForEdit(interviewId);
            return Result<InterviewDelegationDTO>.Success(new InterviewDelegationDTO
            {
                InterviewId = interviewId,
                CandidateId = current.CandidateId,
                CandidateName = interview?.Candidate?.FullName,
                PositionId = interview?.PositionId ?? 0,
                PositionName = interview?.Position?.Name,
                InterviewDate = interview?.Date ?? current.StartDate,
                StatusId = interview?.StatusId,
                OriginalInterviewerId = current.OriginalInterviewerId,
                OriginalInterviewerName = await GetInterviewerName(current.OriginalInterviewerId),
                DelegatedUserId = current.DelegatedUserId,
                DelegatedUserName = await GetInterviewerName(current.DelegatedUserId),
                DelegatedByUserId = current.DelegatedByUserId,
                StartDate = current.StartDate,
                ExpirationDate = current.ExpirationDate,
                InterviewerSlot = current.InterviewerSlot
            });
        }
        catch (Exception ex)
        {
            return Result<InterviewDelegationDTO>.Failure(null, $"Unable to remove the delegation: {ex.Message}");
        }
    }

    public async Task<Result<BulkDelegationPageDTO>> GetBulkDelegationPageAsync()
    {
        try
        {
            Result<bool> authorization = await EnsureUserCanDelegateAsync();
            if (!authorization.IsSuccess)
                return Result<BulkDelegationPageDTO>.Failure(null, authorization.Error);

            Result<List<InterviewsDTO>> interviews = await GetAll();
            if (!interviews.IsSuccess)
                return Result<BulkDelegationPageDTO>.Failure(null, interviews.Error);

            Status pending = await _statusRepository.GetByCode(StatusCode.Pending);
            List<DelegationDuration> durations = await _delegationDurationRepository.GetActiveAsync();
            List<InterviewsDTO> pendingInterviews = interviews.Value
                .Where(i => i.StatusId == pending.Id)
                .OrderByDescending(i => i.Date)
                .ToList();

            DateTime now = DateTime.Now;
            Dictionary<int, List<InterviewDelegationHistoryItemDTO>> openDelegations = [];
            List<InterviewDelegation> open = await _interviewsRepository.GetOpenDelegationsForInterviewsAsync(
                pendingInterviews.Select(i => i.InterviewsId), now);
            foreach (InterviewDelegation item in open.OrderBy(d => d.InterviewerSlot))
            {
                if (!openDelegations.TryGetValue(item.InterviewId, out List<InterviewDelegationHistoryItemDTO> list))
                    openDelegations[item.InterviewId] = list = [];

                list.Add(await ToHistoryItemAsync(item, null, now, interviewIsPending: true));
            }

            return Result<BulkDelegationPageDTO>.Success(new BulkDelegationPageDTO
            {
                Interviews = pendingInterviews,
                OpenDelegations = openDelegations,
                DelegateCandidates = await GetEligibleDelegateUsersAsync(),
                Durations = durations.Select(d => new DelegationDurationDTO
                {
                    Id = d.Id,
                    Name = d.Name,
                    Amount = d.Amount,
                    Unit = d.Unit,
                    DurationMinutes = d.DurationMinutes,
                    IsUnlimited = d.IsUnlimited,
                    IsActive = d.IsActive
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return Result<BulkDelegationPageDTO>.Failure(null, $"Unable to load interviews for delegation: {ex.Message}");
        }
    }

    public async Task<Result<BulkDelegationResultDTO>> DelegatePendingInterviewsAsync(
        IEnumerable<int> interviewIds,
        string delegatedUserId,
        int durationId)
    {
        List<int> ids = interviewIds?.Where(id => id > 0).Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return Result<BulkDelegationResultDTO>.Failure(null, "Select at least one interview.");

        if (string.IsNullOrWhiteSpace(delegatedUserId))
            return Result<BulkDelegationResultDTO>.Failure(null, "Please select a delegated user.");

        if (await _delegationDurationRepository.GetActiveByIdAsync(durationId) == null)
            return Result<BulkDelegationResultDTO>.Failure(null, "Please select an active delegation duration.");

        IdentityUser delegatedUser = await _userManager.FindByIdAsync(delegatedUserId);
        if (delegatedUser == null || !await IsEligibleDelegateUserAsync(delegatedUser))
            return Result<BulkDelegationResultDTO>.Failure(null, "The selected user is not eligible to act for this interview.");

        BulkDelegationResultDTO bulk = new();
        foreach (int interviewId in ids)
        {
            Interviews interview = await _interviewsRepository.GetByIdForEdit(interviewId);
            string candidateName = interview?.Candidate?.FullName ?? $"Interview {interviewId}";
            if (interview == null || string.IsNullOrEmpty(interview.InterviewerId))
            {
                bulk.Errors.Add($"{candidateName} could not be delegated because it has no assigned interviewer.");
                continue;
            }

            Result<InterviewDelegationDTO> delegated = await DelegatePendingInterviewAsync(
                interviewId,
                PrimaryInterviewerSlot,
                interview.InterviewerId,
                delegatedUserId,
                durationId);

            if (!delegated.IsSuccess)
                bulk.Errors.Add($"{candidateName}: {delegated.Error}");
            else
                bulk.Delegated.Add(delegated.Value);
        }

        if (bulk.Delegated.Count == 0)
            return Result<BulkDelegationResultDTO>.Failure(bulk, string.Join(" ", bulk.Errors));

        return Result<BulkDelegationResultDTO>.Success(bulk);
    }

    public async Task<bool> IsCurrentUserAssignedToInterviewAsync(int interviewId)
    {
        Interviews interview = await _interviewsRepository.GetById(interviewId);
        if (interview == null)
            return false;

        IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        return user != null && IsUserAssigned(interview, user.Id);
    }

    public async Task<Result<bool>> EnsureCurrentUserCanSubmitPendingAsync(int interviewId)
    {
        Interviews interview = await _interviewsRepository.GetById(interviewId);
        if (interview == null)
            return Result<bool>.Failure(false, "Interview not found.");

        await _interviewsRepository.ReloadAsync(interview);

        Status status = await _statusRepository.GetById(interview.StatusId);
        if (status?.Code != StatusCode.Pending)
            return Result<bool>.Success(true);

        IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        // Assigned interviewers can always submit, even while their slot is delegated
        if (user != null && IsUserAssigned(interview, user.Id))
            return Result<bool>.Success(true);

        if (user == null)
            return Result<bool>.Failure(false, "You are no longer allowed to submit this interview.");

        string delegationBlock = await DescribeWhyDelegationDoesNotAllowAsync(interviewId, user.Id);
        if (delegationBlock == null)
            return Result<bool>.Success(true);

        return Result<bool>.Failure(false, delegationBlock);
    }

    public async Task<Result<bool>> EnsureCurrentUserCanOpenSubmittedResultAsync(int interviewId)
    {
        Interviews interview = await _interviewsRepository.GetById(interviewId);
        if (interview == null)
            return Result<bool>.Failure(false, "Interview not found.");

        IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (user == null)
            return Result<bool>.Failure(false, "You must log in first.");

        if (IsUserAssigned(interview, user.Id))
            return Result<bool>.Success(true);

        if (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "HR Manager"))
            return Result<bool>.Success(true);

        List<InterviewDelegation> delegations = await _interviewsRepository.GetUserDelegationsForInterviewAsync(interviewId, user.Id);
        if (delegations.Count == 0)
            return Result<bool>.Success(true);

        // The delegate keeps access after submitting the result, until the delegation period ends
        // (never, for an unlimited delegation).
        DateTime now = DateTime.Now;
        if (delegations.Any(d => d.IsActive
                && !d.IsDelete
                && (d.Status == InterviewDelegation.StatusActive || d.Status == InterviewDelegation.StatusCompleted)
                && d.StartDate <= now
                && d.ExpirationDate > now))
            return Result<bool>.Success(true);

        string delegationBlock = await DescribeWhyDelegationDoesNotAllowAsync(interviewId, user.Id);
        if (delegationBlock == null)
            return Result<bool>.Success(true);

        return Result<bool>.Failure(false, delegationBlock);
    }

    public async Task<string> GetDelegatedWorkflowRoleAsync(int interviewId, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        Interviews interview = await _interviewsRepository.GetById(interviewId);
        if (interview == null || IsUserAssigned(interview, userId))
            return null;

        InterviewDelegation delegation = await _interviewsRepository.GetActiveDelegationAsync(interviewId, userId, DateTime.Now);
        if (delegation == null)
            return null;

        string role = await GetInterviewerRole(delegation.OriginalInterviewerId);
        return role == "User not found" ? null : role;
    }

    /// <summary>
    /// True while the current user has a delegation that keeps an interview in their My Interviews:
    /// open, or already used to submit the result, until the delegation period ends.
    /// </summary>
    public async Task<bool> HasVisibleDelegationAsync()
    {
        IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (user == null)
            return false;

        return await _interviewsRepository.HasVisibleDelegationAsync(user.Id, DateTime.Now);
    }

    /// <summary>
    /// The delegation through which the current user sees this interview, or null when they are
    /// assigned to it themselves or have no delegation in force on it.
    /// </summary>
    public async Task<InterviewDelegationDTO> GetCurrentUserDelegationAccessAsync(int interviewId)
    {
        IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (user == null)
            return null;

        Interviews interview = await _interviewsRepository.GetById(interviewId);
        if (interview == null || IsUserAssigned(interview, user.Id))
            return null;

        List<InterviewDelegation> visible = await _interviewsRepository.GetVisibleDelegationsForUserAsync(user.Id, [interviewId], DateTime.Now);
        InterviewDelegation delegation = visible.FirstOrDefault();
        if (delegation == null)
            return null;

        return new InterviewDelegationDTO
        {
            InterviewId = delegation.InterviewId,
            CandidateId = delegation.CandidateId,
            OriginalInterviewerId = delegation.OriginalInterviewerId,
            OriginalInterviewerName = await GetInterviewerName(delegation.OriginalInterviewerId),
            DelegatedUserId = delegation.DelegatedUserId,
            DelegatedUserName = user.UserName,
            DelegatedByUserId = delegation.DelegatedByUserId,
            StartDate = delegation.StartDate,
            ExpirationDate = delegation.ExpirationDate,
            WorkflowStageId = delegation.WorkflowStageId,
            InterviewerSlot = delegation.InterviewerSlot,
            Status = delegation.Status
        };
    }

    /// <summary>
    /// When the interview's result was submitted by a delegate, records the delegate as the submitter
    /// and the original interviewer they acted for. Read from the Completed delegation, so it stays
    /// correct even if the interview row is edited later. The interview's own audit fields are untouched.
    /// </summary>
    private async Task ApplyDelegatedSubmitterAsync(InterviewsDTO dto, int interviewId)
    {
        InterviewDelegation delegation = await _interviewsRepository.GetCompletedDelegationAsync(interviewId);
        if (delegation == null)
            return;

        dto.ResultSubmittedByName = await HistoryUserNameAsync(delegation.DelegatedUserId);
        dto.ResultSubmittedForName = await HistoryUserNameAsync(delegation.OriginalInterviewerId);
        dto.ResultSubmittedOn = delegation.ModifiedOn;
    }

    public async Task<bool> HasInForceDelegationAsync(int interviewId)
    {
        List<InterviewDelegation> delegations = await _interviewsRepository.GetInForceDelegationsAsync(interviewId, DateTime.Now);
        return delegations.Count > 0;
    }

    public Task EnsureDelegatedSuccessorAsync(int interviewId)
    {
        return _dynamicWorkflowService.EnsureDelegatedSuccessorAsync(interviewId);
    }

    private async Task EnsurePendingAssignmentAfterReloadAsync(Interviews interview)
    {
        if (interview == null)
            throw new InvalidOperationException("Interview not found.");

        Result<bool> check = await EnsureCurrentUserCanSubmitPendingAsync(interview.InterviewsId);
        if (!check.IsSuccess)
            throw new InvalidOperationException(check.Error);
    }

    private async Task<Result<bool>> EnsureUserCanDelegateAsync()
    {
        IdentityUser user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (user == null)
            return Result<bool>.Failure(false, "You must log in first.");

        bool isHr = await _userManager.IsInRoleAsync(user, "HR Manager");
        bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isHr && !isAdmin)
            return Result<bool>.Failure(false, "You are not authorized to delegate interviews.");

        return Result<bool>.Success(true);
    }

    private static bool IsInterviewStillPending(Interviews interview)
    {
        return interview.Status != null
            && interview.Status.Code == StatusCode.Pending
            && !interview.IsUpdated;
    }

    private static bool IsUserAssigned(Interviews interview, string userId)
    {
        if (interview == null || string.IsNullOrEmpty(userId))
            return false;

        if (interview.InterviewerId == userId || interview.SecondInterviewerId == userId)
            return true;

        if (interview.ArchitectureInterviewerId != userId)
            return false;

        if (interview.StartFromHR && interview.WorkflowStageId == (int)EnumWorkflowStage.GMFinalReview)
            return true;

        if (!interview.StartFromHR
            && interview.WorkflowStageId.HasValue
            && interview.WorkflowStageId >= (int)EnumWorkflowStage.ManagementReview)
            return true;

        return interview.ModifiedBy == userId;
    }

    private Task<List<UsersDTO>> GetDelegationCandidateUsersAsync(string primaryInterviewerId, string secondInterviewerId)
    {
        return GetEligibleDelegateUsersAsync(primaryInterviewerId, secondInterviewerId);
    }

    private async Task<List<UsersDTO>> GetEligibleDelegateUsersAsync(params string[] excludedUserIds)
    {
        HashSet<string> excluded = new(
            excludedUserIds.Where(id => !string.IsNullOrEmpty(id)),
            StringComparer.Ordinal);
        List<UsersDTO> candidates = [];
        foreach (IdentityUser user in _userManager.Users.OrderBy(u => u.UserName))
        {
            if (excluded.Contains(user.Id) || !await IsEligibleDelegateUserAsync(user))
                continue;

            candidates.Add(new UsersDTO
            {
                Id = user.Id,
                Name = user.UserName,
                Email = user.Email
            });
        }

        return candidates;
    }

    private async Task<bool> IsEligibleDelegateUserAsync(IdentityUser user)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        return !roles.Any(role =>
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Viewer", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> GetStageNameAsync(int? workflowStageId)
    {
        if (!workflowStageId.HasValue)
            return null;

        Result<WorkflowStageDTO> stage = await _workflowService.GetStageByIdAsync(workflowStageId.Value);
        return stage.IsSuccess ? stage.Value?.Name : null;
    }

    private static string MapDelegationFailure(string updateResult)
    {
        return updateResult switch
        {
            "Success" => null,
            "NotFound" => "Interview not found.",
            "NotPending" => "This interview is no longer pending. It may already have been completed.",
            "InterviewerChanged" => "The original interviewer changed. Refresh the page and try again.",
            "AlreadyAssigned" => "The selected delegate is already the assigned interviewer.",
            "Overlap" => "This user already has an open delegation on this interview.",
            "SlotDelegated" => "This interviewer is already delegated. Change or remove the current delegation instead of adding another.",
            "ReplaceNotOpen" => "The delegation you are changing has ended or was removed. Refresh the page and try again.",
            _ => "Unable to delegate the pending interview."
        };
    }

    private static string DescribeStoredDuration(InterviewDelegation delegation)
    {
        if (!string.IsNullOrWhiteSpace(delegation.DurationName))
            return delegation.DurationName;

        if (delegation.DurationMinutes.HasValue && delegation.DurationMinutes.Value > 0)
            return FormatMinutes(delegation.DurationMinutes.Value);

        if (delegation.IsUnlimited)
            return "Unlimited";

        TimeSpan span = delegation.ExpirationDate - delegation.StartDate;
        if (span.TotalMinutes < 1)
            return "—";

        return FormatMinutes((int)Math.Round(span.TotalMinutes));
    }

    private static string FormatMinutes(int minutes)
    {
        if (minutes % (7 * 24 * 60) == 0)
        {
            int weeks = minutes / (7 * 24 * 60);
            return weeks == 1 ? "1 week" : $"{weeks} weeks";
        }

        if (minutes % (24 * 60) == 0)
        {
            int days = minutes / (24 * 60);
            return days == 1 ? "1 day" : $"{days} days";
        }

        if (minutes % 60 == 0)
        {
            int hours = minutes / 60;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        return minutes == 1 ? "1 minute" : $"{minutes} minutes";
    }

    /// <summary>
    /// Display status: Active, Not started, Expired, Removed, Completed (the delegate scored)
    /// or Closed (someone else scored first). <paramref name="interview"/> is optional and lets
    /// rows saved before Completed/Closed were recorded show the right final state.
    /// </summary>
    private static string DescribeDelegationStatus(InterviewDelegation delegation, DateTime now, Interviews interview = null)
    {
        if (delegation == null || delegation.IsDelete || delegation.Status == InterviewDelegation.StatusCancelled || !delegation.IsActive)
            return "Removed";

        if (delegation.Status == InterviewDelegation.StatusCompleted)
            return "Completed";

        if (delegation.Status == InterviewDelegation.StatusClosed)
            return "Closed";

        if (interview != null && !IsInterviewStillPending(interview))
        {
            DateTime scoredOn = interview.ModifiedOn;
            if (scoredOn >= delegation.ExpirationDate)
                return "Expired";

            return interview.ModifiedBy == delegation.DelegatedUserId ? "Completed" : "Closed";
        }

        if (now < delegation.StartDate)
            return "Not started";

        if (now >= delegation.ExpirationDate)
            return "Expired";

        return "Active";
    }

    private static string DescribeSubmitFailure(string saveResult)
    {
        return saveResult switch
        {
            "NotPending" => "This interview is no longer pending. It may already have been completed.",
            "Expired" => "This delegation has expired.",
            "NotStarted" => "This delegation has not started yet.",
            _ => "You are no longer allowed to submit this interview."
        };
    }

    private async Task<InterviewDelegationHistoryItemDTO> ToHistoryItemAsync(
        InterviewDelegation item,
        Interviews interview,
        DateTime now,
        bool interviewIsPending)
    {
        string status = DescribeDelegationStatus(item, now, interview);
        DateTime? endedOn = status switch
        {
            "Removed" => item.RemovedOn,
            "Completed" or "Closed" => interview?.ModifiedOn ?? item.ModifiedOn,
            "Expired" => item.ExpirationDate,
            _ => null
        };

        return new InterviewDelegationHistoryItemDTO
        {
            Id = item.Id,
            InterviewId = item.InterviewId,
            OriginalInterviewerName = await GetInterviewerName(item.OriginalInterviewerId),
            DelegatedUserId = item.DelegatedUserId,
            DelegatedUserName = await GetInterviewerName(item.DelegatedUserId),
            DelegatedByName = await GetInterviewerName(item.DelegatedByUserId),
            StartDate = item.StartDate,
            ExpirationDate = item.ExpirationDate,
            CreatedOn = item.CreatedOn,
            Duration = DescribeStoredDuration(item),
            InterviewerSlot = item.InterviewerSlot,
            Status = status,
            CanRemove = interviewIsPending && (status == "Active" || status == "Not started"),
            RemovedByName = string.IsNullOrEmpty(item.RemovedByUserId)
                ? null
                : await GetInterviewerName(item.RemovedByUserId),
            RemovedOn = item.RemovedOn,
            EndedOn = endedOn
        };
    }

    private async Task<string> DescribeWhyDelegationDoesNotAllowAsync(int interviewId, string userId)
    {
        DateTime now = DateTime.Now;
        List<InterviewDelegation> delegations = await _interviewsRepository.GetUserDelegationsForInterviewAsync(interviewId, userId);
        List<InterviewDelegation> openDelegations = delegations
            .Where(d => d.IsActive && !d.IsDelete && d.Status == InterviewDelegation.StatusActive)
            .ToList();

        if (openDelegations.Any(d => d.StartDate <= now && d.ExpirationDate > now))
            return null;

        if (openDelegations.Any(d => now < d.StartDate))
            return "This delegation has not started yet.";

        if (openDelegations.Any(d => now >= d.ExpirationDate))
            return "This delegation has expired.";

        if (delegations.Any(d => d.Status == InterviewDelegation.StatusCancelled))
            return "This delegation has been removed.";

        return "You are no longer allowed to submit this interview.";
    }
}