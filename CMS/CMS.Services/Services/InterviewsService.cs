﻿// CMS.Application/Services/InterviewsService.cs
using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Implementation;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CMS.Services.Services
{
    public class InterviewsService : IInterviewsService
    {
        private readonly IInterviewsRepository _interviewsRepository;
        private readonly ICandidateService _candidateService;
        private readonly IPositionService _positionService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStatusRepository _statusRepository;

        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IAttachmentService _attachmentService;

        public InterviewsService(IInterviewsRepository interviewsRepository,
            ICandidateService candidateService,
            IPositionService positionService,
            IAttachmentService attachmentService,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IHttpContextAccessor httpContextAccessor,
            IStatusRepository statusRepository
            )
        {
            _interviewsRepository = interviewsRepository;
            _candidateService = candidateService;
            _positionService = positionService;
            _attachmentService = attachmentService;
            _userManager = userManager;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
            _statusRepository = statusRepository;
        }

        public void LogException(string methodName, Exception ex, string createdByUserId = null, string additionalInfo = null)
        {
            _interviewsRepository.LogException(methodName, ex, createdByUserId, additionalInfo);
        }

        public string GetUserId()
        {
            try
            {
            
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId;
            }
            catch (Exception ex)
            {
                LogException(nameof(GetUserId), ex, null, "Error while getting user id");
                throw ex;
            }
        }


        public async Task<List<UsersDTO>> GetInterviewers()
        {
            try
            {
                var interviewerRole = await _roleManager.FindByNameAsync("Interviewer");
                if (interviewerRole == null)
                {
                    return new List<UsersDTO>();
                }
                var usersInRole = await _userManager.GetUsersInRoleAsync(interviewerRole.Name);

                var interviewers = usersInRole.Select(user => new UsersDTO
                {
                    Id = user.Id,
                    Name = user.UserName,
                    Email = user.Email,
                }).ToList();
                return interviewers;
            }
           catch (Exception ex)
            {
                LogException(nameof(GetInterviewers), ex, null, "Error while getting all Interviewers");
                throw ex;
            }

        }

        public async Task<string> GetInterviewerName(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    return user.UserName;
                }

                return "User not found";
            }
       

             catch (Exception ex)
            {
                LogException(nameof(GetInterviewerName), ex, null, "Error while getting Interviewer Name");
                throw ex;
            }
        }

        public async Task<string> GetArchitectureName(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    return user.UserName;
                }

                return "User not found";
            }


            catch (Exception ex)
            {
                LogException(nameof(GetArchitectureName), ex, null, "Error while getting Architectur Name");
                throw ex;
            }
        }
        public async Task<string> GetInterviewerRole(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Any())
                    {
                        return roles[0];
                    }
                }

                return "User not found";

            }
            catch (Exception ex)
            {
                LogException(nameof(GetInterviewerRole), ex, null, "Error while getting Interviewer Role");
                throw ex;
            }
        }

        public async Task<Result<InterviewsDTO>> Delete(int id,string ByUserId)
        {
            try
            {
                var interview = await _interviewsRepository.GetById(id,GetUserId());
                if (interview != null && interview.AttachmentId != null)
                {
                    var attachmentToRemove = interview.AttachmentId;
                    await _interviewsRepository.Delete(id, GetUserId());
                    if (attachmentToRemove != null)
                    {
                        await _attachmentService.DeleteAttachmentAsync((int)attachmentToRemove);
                    }

                }
                else
                {
                    await _interviewsRepository.Delete(id, GetUserId());
                }

                return Result<InterviewsDTO>.Success(null);
            }

            catch (Exception ex)
            {
                LogException(nameof(Delete), ex, $"Deleted by : {ByUserId}", $"Error while deleteing an interview with Id : {id}");
                return Result<InterviewsDTO>.Failure(null, $"An error occurred while deleting the Interview{ex.InnerException.Message}");
            }
        }

       public async Task<Result<List<InterviewsDTO>>>ShowHistory(int id){

            

            List<InterviewsDTO> interviewsDTOs = new List<InterviewsDTO>();
            try
            {
                var Result = await GetById(id,GetUserId());
                var interview = Result.Value;
                while (interview.ParentId != null)
                {
                    var result = await GetById((int)interview.ParentId,GetUserId());
                    interview= result.Value;
                    interviewsDTOs.Add(result.Value);
                }
                return Result<List<InterviewsDTO>>.Success(interviewsDTOs);

            }
            catch (Exception ex)
            {
                LogException(nameof(ShowHistory), ex);
                return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get interview History: {ex.Message}");
            }
           


        }

        public async Task<Result<List<InterviewsDTO>>> GetAll()
        {
            try
            {
                var interviews = await _interviewsRepository.GetAll();
                if (interviews == null)
                {
                    return Result<List<InterviewsDTO>>.Failure(null, "No career offers found");
                }

                var interviewsDTO = new List<InterviewsDTO>();
                foreach (var c in interviews)
                {
                    string userName = await GetInterviewerName(c.InterviewerId);
                    string SeconduserName = await GetInterviewerName(c.SecondInterviewerId);
                    string archiName= await GetArchitectureName(c.ArchitectureInterviewerId);

                    string interviewerRole = await GetInterviewerRole(c.InterviewerId);
                    var com = new InterviewsDTO
                    {

                        InterviewsId = c.InterviewsId,
                        Score = c.Score,
                        StatusId = c.StatusId,
                        StatusName = c.Status.Name,
                        Date = c.Date,
                        PositionId = c.PositionId,
                        Name = c.Position.Name,
                        EvalutaionFormId =c.Position.EvaluationId,
                        Notes = c.Notes,
                        ParentId = c.ParentId,
                        InterviewerId = c.InterviewerId,
                        InterviewerName = userName,
                        CandidateId = c.CandidateId,
                        FullName = c.Candidate.FullName,
                        CandidateCVAttachmentId=c.Candidate.CVAttachmentId,
                        AttachmentId = c.AttachmentId,
                        InterviewerRole = interviewerRole,
                        ActualExperience= c.ActualExperience,
                        SecondInterviewerId = c.SecondInterviewerId,
                        SecondInterviewerName = SeconduserName,

                        ArchitectureInterviewerId=c.ArchitectureInterviewerId,
                        ArchitectureInterviewerName= archiName,
                    };
                    interviewsDTO.Add(com);

                }
                return Result<List<InterviewsDTO>>.Success(interviewsDTO);


            }
            catch (Exception ex)
            {
                LogException(nameof(GetAll), ex, null, null);
                return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get Interview: {ex.InnerException.Message}");
            }
        }

        public async Task<Result<InterviewsDTO>> GetById(int id,string ByUserId)
        {
            if (id <= 0)
            {
                return Result<InterviewsDTO>.Failure(null, "Invalid interview id");
            }
            try
            {

                var interview = await _interviewsRepository.GetById(id, GetUserId());
                string userName = await GetInterviewerName(interview.InterviewerId);
                string SeconduserName = await GetInterviewerName(interview.SecondInterviewerId);
                string archiName = await GetArchitectureName(interview.ArchitectureInterviewerId);

                string interviewerRole = await GetInterviewerRole(interview.InterviewerId);
                var interviewDTO = new InterviewsDTO
                {
                    InterviewsId = interview.InterviewsId,
                    Score = interview.Score,
                    StatusId = interview.StatusId,
                    StatusName = interview.Status.Name,
                    Date = interview.Date,
                    PositionId = interview.PositionId,
                    Name = interview.Position.Name,
                    EvalutaionFormId =interview.Position.EvaluationId,
                    Notes = interview.Notes,
                    ParentId = interview.ParentId,
                    InterviewerId = interview.InterviewerId,
                    InterviewerName = userName,
                    CandidateId = interview.CandidateId,
                    FullName = interview.Candidate.FullName,
                    CandidateCVAttachmentId=interview.Candidate.CVAttachmentId,
                    AttachmentId = interview.AttachmentId,
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
                LogException(nameof(GetById), ex, ByUserId, $"Error while Getting details with Id: {id}");
                return Result<InterviewsDTO>.Failure(null, $"unable to retrieve the Interview from the repository{ex.InnerException.Message}");
            }
        }

        public async Task<Result<InterviewsDTO>> Insert(InterviewsDTO data,string ByUserId)
        {
            try
            {
                if (data == null)
                {
                    return Result<InterviewsDTO>.Failure(data, "the interview  DTO is null");
                }
                var status = await _statusRepository.GetByCode(StatusCode.Pending);
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var interview = new Interviews
                {
                    PositionId = data.PositionId,
                    CandidateId = data.CandidateId,
                    Score = data.Score,
                    StatusId = status.Id,
                    Date = data.Date,
                    Notes = data.Notes,
                    ParentId = data.ParentId,
                    InterviewerId = data.InterviewerId,
                    AttachmentId = data.AttachmentId,
                    CreatedBy = currentUser.Id,
                    CreatedOn = DateTime.Now,
                    SecondInterviewerId = data.SecondInterviewerId,
                    ArchitectureInterviewerId = data.ArchitectureInterviewerId,

                };
                await _interviewsRepository.Insert(interview, GetUserId());

                _httpContextAccessor.HttpContext.Session.SetString("ArchitectureInterviewerId", data.ArchitectureInterviewerId ?? "");

                return Result<InterviewsDTO>.Success(data);

            }
            catch (Exception ex)
            {
                LogException(nameof(Insert), ex, $"Created by : {ByUserId}", $"Error while creating an interview with Id: {data.InterviewsId}");
                throw ex;
            }


        }

        public async Task<Result<InterviewsDTO>> Update(InterviewsDTO data,string ByUserId)
        {
            try
            {
                if (data == null)
                {
                    return Result<InterviewsDTO>.Failure(data, "can not update a null object");
                }
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                //if (Enum.TryParse(data.Status, out InterviewStatus status))
                // {
                var previouseInterview = await _interviewsRepository.GetByIdForEdit(data.InterviewsId, GetUserId());
                var interview = new Interviews
                {
                    InterviewsId = data.InterviewsId,
                    PositionId = data.PositionId,
                    CandidateId = data.CandidateId,
                    Score = data.Score,
                    ParentId = data.ParentId,
                    InterviewerId = data.InterviewerId,
                    SecondInterviewerId = data.SecondInterviewerId,
                    ArchitectureInterviewerId = data.ArchitectureInterviewerId,

                    Date = data.Date,
                    Notes = data.Notes,
                    StatusId = (int)data.StatusId,
                    AttachmentId = data.AttachmentId,
                    ModifiedOn = DateTime.Now,
                    ModifiedBy = currentUser.Id,
                    CreatedBy = previouseInterview.CreatedBy,
                    CreatedOn = previouseInterview.CreatedOn,

                };
                await _interviewsRepository.Update(interview, GetUserId());
                // }
                return Result<InterviewsDTO>.Success(data);

            }


            catch (Exception ex)
            {
                LogException(nameof(Update), ex, $"Modified by : {ByUserId}", $"Error while updating an interview with Id: {data.InterviewsId}");
                throw ex;
            }

        }

        public async Task UpdateInterviewAttachmentAsync(int id, string fileName, long fileSize, Stream fileStream,string ByUserId)
        {

            try
            {

            var interview = await _interviewsRepository.GetById(id, GetUserId());
            int attachmentId = await _attachmentService.CreateAttachmentAsync(fileName, fileSize, fileStream);
            int attachmentToRemove = (int)interview.AttachmentId;
            interview.AttachmentId = attachmentId;
            await _interviewsRepository.Update(interview, GetUserId());
            await _attachmentService.DeleteAttachmentAsync(attachmentToRemove);
            }


            catch (Exception ex)
            {
                LogException(nameof(UpdateInterviewAttachmentAsync), ex, ByUserId, $"Error while updating Interview Attachment with ID : {id}");
                throw ex;
            }

        }


        public async Task ConductInterview(InterviewsDTO completedDTO,string ByUserId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                var interview = await _interviewsRepository.GetById(completedDTO.InterviewsId, GetUserId());
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
                await _interviewsRepository.Update(interview, GetUserId());
                // Step 2: Create Next Interview if Needed.
                var generalManagerInterview = await _interviewsRepository.GetGeneralManagerInterviewForCandidate(interview.CandidateId);
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
                    await _interviewsRepository.Update(generalManagerInterview, GetUserId());
                }
                var archiInterview = await _interviewsRepository.GetArchiInterviewForCandidate(interview.CandidateId);
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
                    await _interviewsRepository.Update(archiInterview, GetUserId());
                }

                var Completedstatus = await _statusRepository.GetById((int)completedDTO.StatusId);
                bool isApproved = Completedstatus.Code == StatusCode.Approved;
                bool isLastInterviewerAnHR = await _userManager.IsInRoleAsync(interview.Interviewer, "HR Manager");
                if (isApproved && !isLastInterviewerAnHR) // There is a next interview
                {
                    bool isFirstMeeting = interview.ParentId == null;
                    var PendeingStatus = await _statusRepository.GetByCode(StatusCode.Pending);
                    var newInterview = new Interviews
                    {
                        StatusId = PendeingStatus.Id,
                        Date =interview.Date,
                        CandidateId = interview.CandidateId,
                        PositionId = interview.PositionId,
                        ParentId = completedDTO.InterviewsId,
                        CreatedOn = DateTime.Now,
                        CreatedBy = currentUser.Id,
                    };
                    if (isFirstMeeting) // Second Interview Needed which done by General Manager and Solution Architecture
                    {
                        var manager = (await _userManager.GetUsersInRoleAsync("General Manager")).FirstOrDefault();

                        var architectureInterviewerId = _httpContextAccessor.HttpContext.Session.GetString("ArchitectureInterviewerId");
                        var archiId = architectureInterviewerId;

                        Debug.Assert(manager != null, "There is No Valid General Manager in The System");

                        // Create an interview for the General Manager
                        var managerInterview = new Interviews
                        {
                            StatusId = PendeingStatus.Id,
                            Date = interview.Date,
                            CandidateId = interview.CandidateId,
                            PositionId = interview.PositionId,
                            ParentId = completedDTO.InterviewsId,
                            CreatedOn = DateTime.Now,
                            CreatedBy = currentUser.Id,
                            InterviewerId = manager.Id
                        };

                        await _interviewsRepository.Insert(managerInterview, GetUserId());

                        // Create an interview for the Solution Architecture
                        if (!string.IsNullOrEmpty(archiId))
                        {
                            var archi = await _userManager.FindByIdAsync(archiId);
                            Debug.Assert(archi != null, "There is No Valid Solution Architecture in The System");

                            var newArchiInterview = new Interviews
                            {
                                StatusId = PendeingStatus.Id,
                                Date = interview.Date,
                                CandidateId = interview.CandidateId,
                                PositionId = interview.PositionId,
                                ParentId = completedDTO.InterviewsId,
                                CreatedOn = DateTime.Now,
                                CreatedBy = currentUser.Id,
                                InterviewerId = archi.Id
                            };

                            await _interviewsRepository.Insert(newArchiInterview, GetUserId());
                        }
                    }
                    else // Third Interview Needed which done by HR Manager
                    {
                        var hr = (await _userManager.GetUsersInRoleAsync("HR Manager")).FirstOrDefault();
                        Debug.Assert(hr != null, "There is No Valid HR Manager in The System");
                        newInterview.InterviewerId = hr.Id;
                        await _interviewsRepository.Insert(newInterview, GetUserId());
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(ConductInterview), ex, $"Status by : {ByUserId}", "Error while interviwing");
                throw ex;
            }
        }

        public async Task<Result<List<InterviewsDTO>>> MyInterviews()
        {
            try
            {
                var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                if (user == null)
                {
                    return Result<List<InterviewsDTO>>.Failure(null, "User not found.");
                }

                var interviews = await _interviewsRepository.GetCurrentInterviews(user.Id, GetUserId());
                if (interviews == null)
                {
                    return Result<List<InterviewsDTO>>.Failure(null, "no available interviews.");
                }
                var interviewsDTOs = new List<InterviewsDTO>();
                foreach (var i in interviews)
                {
                    string userName = await GetInterviewerName(i.InterviewerId);
                    string SeconduserName = await GetInterviewerName(i.SecondInterviewerId);
                    string archiName = await GetArchitectureName(i.ArchitectureInterviewerId);

                    interviewsDTOs.Add(new InterviewsDTO
                    {
                        InterviewsId = i.InterviewsId,
                        InterviewerId = i.InterviewerId,
                        InterviewerName = userName,
                        SecondInterviewerId = i.SecondInterviewerId,
                        SecondInterviewerName = SeconduserName,

                        ArchitectureInterviewerId=i.ArchitectureInterviewerId,
                        ArchitectureInterviewerName = archiName,

                        Score = i.Score,
                        StatusId = i.StatusId,
                        StatusName = i.Status.Name,
                        Date = i.Date,
                        PositionId = i.PositionId,
                        Name = i.Position.Name,
                        Notes = i.Notes,
                        ParentId = i.ParentId,
                        CandidateId = i.CandidateId,
                        FullName = i.Candidate.FullName,
                        CandidateCVAttachmentId=i.Candidate.CVAttachmentId,
                        AttachmentId = i.AttachmentId,
                        modifiedBy = i.ModifiedBy,
                        isUpdated = i.IsUpdated,
                        ActualExperience=i.ActualExperience

                    });


                }
                return Result<List<InterviewsDTO>>.Success(interviewsDTOs);


            }
            catch (Exception ex)
            {
                LogException(nameof(MyInterviews), ex, null, "Error while getting my interviews");
                return Result<List<InterviewsDTO>>.Failure(null, $"Unable to get interviews: {ex.InnerException.Message}");
            }
        }

      


    }
}
