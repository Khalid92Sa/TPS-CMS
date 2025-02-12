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
        IConfiguration configuration)
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
            IdentityUser user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                IList<string> roles = await _userManager.GetRolesAsync(user);

                if (roles.Any())
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

            if (interview != null && interview.AttachmentId != null)
            {
                int? attachmentToRemove = interview.AttachmentId;
                await _interviewsRepository.Delete(id);

                if (attachmentToRemove != null)
                    await _attachmentService.DeleteAttachmentAsync((int)attachmentToRemove);
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

            List<InterviewsDTO> interviewsDTO = new List<InterviewsDTO>();

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

                // Add filtering logic here
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
            };

            if (secondInterview != null)
            {
                interviewDTO.SecondInterviewActualExperience = secondInterview.ActualExperience;
                interviewDTO.SecondInterviewNotes = secondInterview.Notes;
            }
            else
            {
                // Handle the case where secondInterview is null
                interviewDTO.SecondInterviewActualExperience = null;
                interviewDTO.SecondInterviewNotes = null;
            }

            if (thirdInterview != null)
                interviewDTO.HRNotes = thirdInterview.Notes;

            else
                // Handle the case where secondInterview is null
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
            string userName = await GetInterviewerName(interview.InterviewerId);
            string SeconduserName = await GetInterviewerName(interview.SecondInterviewerId);
            string archiName = await GetArchitectureName(interview.ArchitectureInterviewerId);
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
                InterviewerId = interview.InterviewerId,
                InterviewerName = userName,
                CandidateId = interview.CandidateId,
                FullName = interview.Candidate.FullName,
                CandidateCVAttachmentId = interview.Candidate.CVAttachmentId,
                AttachmentId = (int?)firstEvaluation ?? null,
                InterviewerRole = interviewerRole,
                ActualExperience = interview.ActualExperience,
                SecondInterviewerId = interview.SecondInterviewerId,
                SecondInterviewerName = SeconduserName,
                ArchitectureInterviewerId = interview.ArchitectureInterviewerId,
                ArchitectureInterviewerName = archiName,
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
            };

            await _interviewsRepository.Insert(interview);

            Interviews insertedInterview = await _interviewsRepository.GetById(interview.InterviewsId);

            InterviewsDTO insertedInterviewDTO = new InterviewsDTO
            {
                InterviewsId = insertedInterview.InterviewsId,
            };

            return Result<InterviewsDTO>.Success(insertedInterviewDTO);
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

            Interviews interview = new Interviews
            {
                InterviewsId = data.InterviewsId,
                PositionId = data.PositionId,
                TrackId = data.TrackId,
                CandidateId = data.CandidateId,
                Score = data.Score,
                ParentId = data.ParentId,
                InterviewerId = data.InterviewerId,
                SecondInterviewerId = data.SecondInterviewerId,
                ArchitectureInterviewerId = data.ArchitectureInterviewerId,
                Date = data.Date,
                Notes = data.Notes,
                StopCycleNote = data.StopCycleNote,
                StatusId = (int)data.StatusId,
                AttachmentId = data.AttachmentId,
                ModifiedOn = DateTime.Now,
                ModifiedBy = currentUser.Id,
                CreatedBy = previouseInterview.CreatedBy,
                CreatedOn = previouseInterview.CreatedOn,
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
            string firstInterviewerRoles = await GetInterviewerRole(firstinterviewer);
            string secondInterviewerRoles = await GetInterviewerRole(secondinterviewer);
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
                // Step 1: Update Completed Interview
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
                // Step 2: Create Next Interview if Needed.

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
                if (isApprovedd && !isLastInterviewerAnHRr) // There is a next interview
                {
                    bool isFirstMeeting = intervieww.ParentId == null;
                    Status PendeingStatus = await _statusRepository.GetByCode(StatusCode.Pending);
                    Interviews newInterview1 = new Interviews
                    {
                        StatusId = PendeingStatus.Id,
                        Date = intervieww.Date,
                        CandidateId = intervieww.CandidateId,
                        PositionId = intervieww.PositionId,
                        TrackId = intervieww.TrackId,
                        ParentId = completedDTO.InterviewsId,
                        CreatedOn = DateTime.Now,
                        CreatedBy = currentUserr.Id,
                    };

                    if (isFirstMeeting) // Second Interview Needed which done by General Manager and Solution Architecture
                    {
                        IdentityUser hr = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                        Debug.Assert(hr != null, "There is No Valid HR Manager in The System");

                        // Create an interview for the General Manager
                        Interviews hrInterview = new Interviews
                        {
                            StatusId = PendeingStatus.Id,
                            Date = intervieww.Date,
                            CandidateId = intervieww.CandidateId,
                            PositionId = intervieww.PositionId,
                            TrackId = intervieww.TrackId,
                            ParentId = completedDTO.InterviewsId,
                            CreatedOn = DateTime.Now,
                            CreatedBy = currentUserr.Id,
                            InterviewerId = hr.Id,
                            SecondInterviewerId = completedDTO.SecondInterviewerId,
                        };

                        await _interviewsRepository.Insert(hrInterview);
                        // Create an interview for the Solution Architecture

                        InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(hrInterview.CandidateId);
                        string aechituciterId = archiIdd.ArchitectureInterviewerId;

                        if (aechituciterId != null)
                        {
                            IdentityUser archi = await _userManager.FindByIdAsync(aechituciterId);
                            Debug.Assert(archi != null, "There is No Valid Solution Architecture in The System");

                            Interviews newArchiInterview = new Interviews
                            {
                                StatusId = PendeingStatus.Id,
                                Date = hrInterview.Date,
                                CandidateId = hrInterview.CandidateId,
                                PositionId = hrInterview.PositionId,
                                TrackId = hrInterview.TrackId,
                                ParentId = completedDTO.InterviewsId,
                                CreatedOn = DateTime.Now,
                                CreatedBy = currentUserr.Id,
                                InterviewerId = aechituciterId,
                                SecondInterviewerId = completedDTO.SecondInterviewerId,
                            };

                            await _interviewsRepository.Insert(newArchiInterview);
                        }

                        IdentityUser hrs = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                        Debug.Assert(hrs != null, "There is No Valid HR Manager in The System");
                        hrInterview.InterviewerId = hrs.Id;

                        await _interviewsRepository.Insert(hrInterview);
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

                Debug.Assert(interview != null, "No Interview Provided for Conduct Interview Method");
                // Step 1: Update Completed Interview
                interview.StatusId = (int)completedDTO.StatusId;
                interview.Score = completedDTO.Score;
                interview.Notes = completedDTO.Notes;
                interview.ActualExperience = completedDTO.ActualExperience;
                interview.AttachmentId = completedDTO.AttachmentId;
                interview.ModifiedBy = currentUser.Id;
                interview.ModifiedOn = DateTime.Now;
                interview.IsUpdated = true;
                await _interviewsRepository.Update(interview);

                // Step 2: Create Next Interview if Needed.

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

                if (isApproved && !isLastInterviewerAnHR) // There is a next interview
                {
                    bool isFirstMeeting = interview.ParentId == null;
                    Status PendeingStatus = await _statusRepository.GetByCode(StatusCode.Pending);
                    Interviews newInterview2 = new Interviews
                    {
                        StatusId = PendeingStatus.Id,
                        Date = interview.Date,
                        CandidateId = interview.CandidateId,
                        PositionId = interview.PositionId,
                        TrackId = interview.TrackId,
                        ParentId = completedDTO.InterviewsId,
                        CreatedOn = DateTime.Now,
                        CreatedBy = currentUser.Id,
                    };

                    if ((firstInterviewerRoles == "General Manager" && secondInterviewerRoles == "Interviewer") || (firstInterviewerRoles == "Interviewer" && secondInterviewerRoles == "General Manager"))
                    {
                        IdentityUser hr = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                        Debug.Assert(hr != null, "There is No Valid HR Manager in The System");
                        newInterview2.InterviewerId = hr.Id;
                        await _interviewsRepository.Insert(newInterview2);
                    }
                    else
                    {
                        if (isFirstMeeting) // Second Interview Needed which done by General Manager and Solution Architecture
                        {
                            IdentityUser manager = (await _userManager.GetUsersInRoleAsync("General Manager")).FirstOrDefault();
                            InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(completedDTO.CandidateId);
                            string aechituciterId = archiIdd.ArchitectureInterviewerId;

                            Debug.Assert(manager != null, "There is No Valid General Manager in The System");

                            if (manager.Id != null && aechituciterId == null)
                            {
                                // Create an interview for the General Manager
                                Interviews managerInterview = new Interviews
                                {
                                    StatusId = PendeingStatus.Id,
                                    Date = interview.Date,
                                    CandidateId = interview.CandidateId,
                                    PositionId = interview.PositionId,
                                    TrackId = interview.TrackId,
                                    ParentId = completedDTO.InterviewsId,
                                    CreatedOn = DateTime.Now,
                                    CreatedBy = currentUser.Id,
                                    InterviewerId = manager.Id,
                                    SecondInterviewerId = completedDTO.SecondInterviewerId,
                                };

                                await _interviewsRepository.Insert(managerInterview);
                            }
                            else
                            {
                                if (aechituciterId != null)
                                {
                                    IdentityUser archi = await _userManager.FindByIdAsync(aechituciterId);

                                    Debug.Assert(archi != null, "There is No Valid Solution Architecture in The System");

                                    Interviews newArchiInterview = new Interviews
                                    {
                                        StatusId = PendeingStatus.Id,
                                        Date = interview.Date,
                                        CandidateId = interview.CandidateId,
                                        PositionId = interview.PositionId,
                                        TrackId = interview.TrackId,
                                        ParentId = completedDTO.InterviewsId,
                                        CreatedOn = DateTime.Now,
                                        CreatedBy = currentUser.Id,
                                        InterviewerId = manager.Id,
                                        SecondInterviewerId = aechituciterId,
                                    };

                                    await _interviewsRepository.Update(newArchiInterview);
                                }
                            }
                        }
                        else // Third Interview Needed which done by HR Manager
                        {
                            IdentityUser hr = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                            Debug.Assert(hr != null, "There is No Valid HR Manager in The System");
                            newInterview2.InterviewerId = hr.Id;
                            await _interviewsRepository.Insert(newInterview2);
                        }
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

            Debug.Assert(interview != null, "No Interview Provided for Conduct Interview Method");

            // Step 1: Update Completed Interview
            interview.StatusId = (int)completedDTO.StatusId;
            interview.Score = completedDTO.Score;
            interview.Notes = completedDTO.Notes;
            interview.ActualExperience = completedDTO.ActualExperience;
            interview.AttachmentId = completedDTO.AttachmentId;
            interview.ModifiedBy = currentUser.Id;
            interview.ModifiedOn = DateTime.Now;
            interview.IsUpdated = true;
            await _interviewsRepository.Update(interview);

            // Step 2: Create Next Interview if Needed.

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

            if (isApproved && !isLastInterviewerAnHR) // There is a next interview
            {
                bool isFirstMeeting = interview.ParentId == null;
                Status PendeingStatus = await _statusRepository.GetByCode(StatusCode.Pending);

                Interviews newInterview = new Interviews
                {
                    StatusId = PendeingStatus.Id,
                    Date = interview.Date,
                    CandidateId = interview.CandidateId,
                    PositionId = interview.PositionId,
                    TrackId = interview.TrackId,
                    ParentId = completedDTO.InterviewsId,
                    CreatedOn = DateTime.Now,
                    CreatedBy = currentUser.Id,
                };

                if (isFirstMeeting) // Second Interview Needed which done by General Manager and Solution Architecture
                {
                    IdentityUser hr = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                    InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(completedDTO.CandidateId);
                    string aechituciterId = archiIdd.ArchitectureInterviewerId;

                    Debug.Assert(hr != null, "There is No Valid HR Manager in The System");

                    // Create an interview for the General Manager
                    Interviews hrInterview = new Interviews
                    {
                        StatusId = PendeingStatus.Id,
                        Date = interview.Date,
                        CandidateId = interview.CandidateId,
                        PositionId = interview.PositionId,
                        TrackId = interview.TrackId,
                        ParentId = completedDTO.InterviewsId,
                        CreatedOn = DateTime.Now,
                        CreatedBy = currentUser.Id,
                        InterviewerId = hr.Id
                    };

                    await _interviewsRepository.Insert(hrInterview);
                }
                else
                {
                    IdentityUser hr = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                    Debug.Assert(hr != null, "There is No Valid HR Manager in The System");
                    newInterview.InterviewerId = hr.Id;
                    await _interviewsRepository.Insert(newInterview);
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

            Debug.Assert(interview != null, "No Interview Provided for Conduct Interview Method");

            // Step 1: Update Completed Interview
            interview.StatusId = (int)completedDTO.StatusId;
            interview.Score = completedDTO.Score;
            interview.Notes = completedDTO.Notes;
            interview.ActualExperience = completedDTO.ActualExperience;
            interview.AttachmentId = completedDTO.AttachmentId;
            interview.ModifiedBy = currentUser.Id;
            interview.ModifiedOn = DateTime.Now;
            interview.IsUpdated = true;
            await _interviewsRepository.Update(interview);

            // Step 2: Create Next Interview if Needed.

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

            if (isApproved && !isLastInterviewerAnHR) // There is a next interview
            {
                bool isFirstMeeting = interview.ParentId == null;
                Status PendeingStatus = await _statusRepository.GetByCode(StatusCode.Pending);
                Interviews newInterview = new Interviews
                {
                    StatusId = PendeingStatus.Id,
                    Date = interview.Date,
                    CandidateId = interview.CandidateId,
                    PositionId = interview.PositionId,
                    TrackId = interview.TrackId,
                    ParentId = completedDTO.InterviewsId,
                    CreatedOn = DateTime.Now,
                    CreatedBy = currentUser.Id,
                };

                if (isFirstMeeting) // Second Interview Needed which done by General Manager and Solution Architecture
                {
                    IdentityUser hr = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                    InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(completedDTO.CandidateId);
                    string aechituciterId = archiIdd.ArchitectureInterviewerId;

                    Debug.Assert(hr != null, "There is No Valid HR Manager in The System");

                    // Create an interview for the General Manager
                    Interviews hrInterview = new Interviews
                    {
                        StatusId = PendeingStatus.Id,
                        Date = interview.Date,
                        CandidateId = interview.CandidateId,
                        PositionId = interview.PositionId,
                        TrackId = interview.TrackId,
                        ParentId = completedDTO.InterviewsId,
                        CreatedOn = DateTime.Now,
                        CreatedBy = currentUser.Id,
                        InterviewerId = hr.Id
                    };

                    await _interviewsRepository.Insert(hrInterview);
                }

                else
                {
                    IdentityUser hr = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                    Debug.Assert(hr != null, "There is No Valid HR Manager in The System");
                    newInterview.InterviewerId = hr.Id;
                    await _interviewsRepository.Insert(newInterview);
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
                    ActualExperience = i.ActualExperience
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
            // If there is a parent interview, recursively fetch the first interview's score
            return await GetFirstInterviewScore(interview.ParentId.Value);

        // No parent interview, return the current interview's score
        return interview?.Score;
    }

    private double? GetFirstEvaluation(int? attachmentId)
    {
        if (!attachmentId.HasValue)
            return null;

        return attachmentId;
    }

    private async Task<double?> GetFirstEvaluationIdForDetails(int interviewId)
    {
        var interview = await _interviewsRepository.GetById(interviewId);

        if (interview?.ParentId != null)
        {
            // If there is a parent interview, recursively fetch the first evaluation
            return await GetFirstEvaluationIdForDetails(interview.ParentId.Value);
        }

        // No parent interview, return the current interview's evaluation
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
            // Log the exception
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
            // Log the exception
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

                        // Exclude interviews where the interviewer is a "General Manager"
                        if (!roles.Contains("General Manager", StringComparer.OrdinalIgnoreCase))
                        {
                            interviewsDtoList.Add(new InterviewsDTO
                            {
                                InterviewsId = interview.InterviewsId,
                                CandidateId = interview.CandidateId,
                                FullName = interview.Candidate?.FullName,
                                InterviewerId = interview.InterviewerId,
                                Date = interview.Date,
                                StatusId = interview.StatusId
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