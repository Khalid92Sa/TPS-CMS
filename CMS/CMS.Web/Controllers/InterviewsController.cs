using CMS.Application.CustomRoleAuth;
using CMS.Application.DTOs;
using CMS.Application.EmailTemplates;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using CMS.Web.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("interviews")]
public class InterviewsController : Controller
{
    private readonly IInterviewsService _interviewsService;
    private readonly ICandidateService _candidateService;
    private readonly IPositionService _positionService;
    private readonly IStatusService _StatusService;
    private readonly IAccountService _accountService;
    private readonly INotificationsService _notificationsService;
    private readonly IInterviewsRepository _interviewsRepository;
    private readonly string _attachmentStoragePath;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IAttachmentService _attachmentService;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICompanyService _companyService;
    private readonly ITrackService _trackService;
    private readonly ISelectedInterviewersRepository _selectedInterviewersRepository;

    public InterviewsController(IInterviewsService interviewsService,
                                ICandidateService candidateService,
                                IPositionService positionService,
                                IStatusService statusService,
                                IWebHostEnvironment env,
                                IAccountService accountService,
                                INotificationsService notificationsService,
                                IInterviewsRepository interviewsRepository,
                                IHttpContextAccessor httpContextAccessor,
                                UserManager<IdentityUser> userManager,
                                IEmailService emailService,
                                IAttachmentService attachmentService,
                                SignInManager<IdentityUser> signInManager,
                                RoleManager<IdentityRole> roleManager,
                                ICompanyService companyService,
                                ITrackService trackService,
                                ISelectedInterviewersRepository selectedInterviewersRepository)
    {
        _interviewsService = interviewsService;
        _candidateService = candidateService;
        _positionService = positionService;
        _StatusService = statusService;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _emailService = emailService;
        _attachmentService = attachmentService;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _companyService = companyService;
        _trackService = trackService;
        _accountService = accountService;
        _notificationsService = notificationsService;
        _interviewsRepository = interviewsRepository;
        _selectedInterviewersRepository = selectedInterviewersRepository;
        _attachmentStoragePath = Path.Combine(env.WebRootPath, "attachments");

        if (!Directory.Exists(_attachmentStoragePath))
            Directory.CreateDirectory(_attachmentStoragePath);
    }


    [Route("myInterviews")]
    [AuthorizeRoles("HR Manager", "Interviewer", "General Manager", "Solution Architecture")]
    public async Task<ActionResult> MyInterviews(int? statusFilter, int? companyFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
                Result<List<StatusDTO>> statusesResult = await _StatusService.GetAll();
                if (!statusesResult.IsSuccess)
                {
                    ModelState.AddModelError("", statusesResult.Error);
                    return View(new PaginatedList<InterviewsDTO>(new List<InterviewsDTO>(), 0, pageNumber, pageSize));
                }

                Result<List<CompanyDTO>> companiesResult = await _companyService.GetAll();
                if (!companiesResult.IsSuccess)
                {
                    ModelState.AddModelError("", companiesResult.Error);
                    return View(new PaginatedList<InterviewsDTO>(new List<InterviewsDTO>(), 0, pageNumber, pageSize));
                }

                List<CompanyDTO> companies = companiesResult.Value;
                ViewBag.CompanyList = new SelectList(companies.OrderBy(x => x.Name), "Id", "Name");

                List<StatusDTO> statuses = statusesResult.Value;
                ViewBag.StatusList = new SelectList(statuses, "Id", "Name");

                if (!statusFilter.HasValue)
                    statusFilter = await _StatusService.GetStatusIdByName("Pending");

                ViewBag.statusFilter = statusFilter;
                ViewBag.companyFilter = companyFilter;
                ViewBag.trackFilter = trackFilter;
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;

                Result<List<TrackDTO>> tracksResult = await _trackService.GetAll();
                if (!tracksResult.IsSuccess)
                {
                    ModelState.AddModelError("", tracksResult.Error);
                    return View(new PaginatedList<InterviewsDTO>(new List<InterviewsDTO>(), 0, pageNumber, pageSize));
                }

                List<TrackDTO> tracks = tracksResult.Value;
                ViewBag.TrackList = new SelectList(tracks.OrderBy(x => x.Name), "Id", "Name");

                Result<List<InterviewsDTO>> result = await _interviewsService.MyInterviews(companyFilter, trackFilter);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError("", result.Error);
                    return View(new PaginatedList<InterviewsDTO>([], 0, pageNumber, pageSize));
                }

                List<InterviewsDTO> interviewsDTOs = result.Value;

                if (statusFilter.HasValue && statusFilter.Value > 0)
                    interviewsDTOs = interviewsDTOs.Where(i => i.StatusId == statusFilter.Value).ToList();

                interviewsDTOs = [.. interviewsDTOs.OrderByDescending(i => i.Date)];

                List<InterviewsDTO> paginatedInterviews = interviewsDTOs
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return View(new PaginatedList<InterviewsDTO>(paginatedInterviews, interviewsDTOs.Count, pageNumber, pageSize));
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("index")]
    [AuthorizeRoles("Admin", "HR Manager")]
    public async Task<ActionResult> Index(int? statusFilter, string candidateFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            ViewBag.statusFilter = statusFilter;
            ViewBag.candidateFilter = candidateFilter;
            ViewBag.trackFilter = trackFilter;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

                Result<List<StatusDTO>> statusesResult = await _StatusService.GetAll();
                if (!statusesResult.IsSuccess)
                {
                    ModelState.AddModelError("", statusesResult.Error);
                    return View(new PaginatedList<InterviewsDTO>([], 0, pageNumber, pageSize));
                }

                Result<List<TrackDTO>> tracksResult = await _trackService.GetAll();
                if (!tracksResult.IsSuccess)
                {
                    ModelState.AddModelError("", tracksResult.Error);
                    return View(new PaginatedList<InterviewsDTO>([], 0, pageNumber, pageSize));
                }

                ViewBag.TrackList = new SelectList(tracksResult.Value.OrderBy(x => x.Name), "Id", "Name");
             
                List<StatusDTO> statuses = statusesResult.Value;
                ViewBag.StatusList = new SelectList(statuses, "Id", "Name");

                IEnumerable<CandidateDTO> candidatesDTO = await _candidateService.GetAllCandidatesAsync();
                ViewBag.CandidateList = new SelectList(candidatesDTO, "Id", "FullName");

                IEnumerable<InterviewsDTO> filteredInterviews = await ApplyFiltersAndRetrieveData(statusFilter, candidateFilter, trackFilter, pageNumber, pageSize);
                var architectureRole = await _roleManager.FindByNameAsync("Solution Architecture");
                if (architectureRole == null)
                {
                    throw new Exception("Role 'Solution Architecture' not found.");
                }
                var usersInRole = await _userManager.GetUsersInRoleAsync(architectureRole.Name);

                var archiId = usersInRole.FirstOrDefault()?.Id;

                if (string.IsNullOrEmpty(archiId))
                {
                    throw new Exception("No users found in the role 'Solution Architecture'.");
                }
                ViewBag.ArchiId = archiId;

                return View(filteredInterviews);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<PaginatedList<InterviewsDTO>> ApplyFiltersAndRetrieveData(
        int? statusFilter,
        string candidateFilter,
        int? trackFilter,
        int pageNumber,
        int pageSize)
    {
        try
        {
            Result<List<InterviewsDTO>> interviewsResult = await _interviewsService.GetAll();

            if (!interviewsResult.IsSuccess)
            {
                ModelState.AddModelError("", interviewsResult.Error);
                return new PaginatedList<InterviewsDTO>([], 0, pageNumber, pageSize);
            }

            List<InterviewsDTO> interviews = interviewsResult.Value;

            if (statusFilter.HasValue && statusFilter.Value > 0)
                interviews = interviews.Where(i => i.StatusId == statusFilter.Value).ToList();

            if (trackFilter.HasValue && trackFilter.Value > 0)
                interviews = interviews.Where(i => i.TrackId == trackFilter.Value).ToList();

            if (!string.IsNullOrEmpty(candidateFilter))
                interviews = interviews.Where(i => i.FullName.Contains(candidateFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            IEnumerable<InterviewsDTO> filteredInterviews = interviews.OrderByDescending(i => i.InterviewsId)
                                               .GroupBy(i => i.CandidateId)
                                               .Select(group => group.First());

            int totalCount = filteredInterviews.Count();

            List<InterviewsDTO> paginatedInterviews = filteredInterviews
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedList<InterviewsDTO>(paginatedInterviews, totalCount, pageNumber, pageSize);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("{id}/stopCycle")]
    [AuthorizeRoles("Admin", "HR Manager")]
    public async Task<IActionResult> StopCycle(int id, int? statusFilter,
        string candidateFilter,
        int? trackFilter,
        int pageNumber = 1,
        int pageSize = 5)
    {
        try
        {
            ViewBag.statusFilter = statusFilter;
            ViewBag.candidateFilter = candidateFilter;
            ViewBag.trackFilter = trackFilter;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            if (_signInManager.IsSignedIn(User) && (User.IsInRole("HR Manager") || User.IsInRole("Admin")))
            {
                Result<InterviewsDTO> result = await _interviewsService.GetInterviewDetails(id);
                InterviewsDTO interviewsDTO = result.Value;

                return View(interviewsDTO);
            }
            else
            {
                TempData["ErrorMessage"] = "You must log in first.";
                return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/stopCycle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StopCycle(int id, InterviewsDTO collection)
    {
        try
        {
            if (_signInManager.IsSignedIn(User) && (User.IsInRole("HR Manager") || User.IsInRole("Admin")))
            {
                if (string.IsNullOrWhiteSpace(collection.StopCycleNote))
                    ModelState.AddModelError("StopCycleNote", "Please add a note.");

                if (ModelState.IsValid)
                {
                    Result<int> saveNoteResult = await _interviewsService.SaveStopCycleNote(collection.CandidateId, collection.StopCycleNote);

                    if (saveNoteResult.IsSuccess)
                    {
                        bool deletePendingResult = await _interviewsService.DeletePendingInterviews(collection.CandidateId, collection);

                        if (deletePendingResult)
                            return RedirectToAction(nameof(Index));

                        else
                            ModelState.AddModelError("", "Error deleting pending interviews.");
                    }
                    else
                        ModelState.AddModelError("", saveNoteResult.Error);
                }

                Result<InterviewsDTO> result = await _interviewsService.GetInterviewDetails(id);
                InterviewsDTO interviewsDTO = result.Value;

                return View(interviewsDTO);
            }
            else
            {
                TempData["ErrorMessage"] = "You must log in first.";
                return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/details")]
    public async Task<ActionResult> Details(
        int id,
        string previousAction,
        int? statusFilter,
        string candidateFilter,
        int? trackFilter,
        int pageNumber = 1,
        int pageSize = 5)
    {
        try
        {
            var interview = await _interviewsRepository.GetById(id);
            if (interview != null && interview.StartFromHR)
            {
                return RedirectToAction(nameof(HRFirstDetails), new { id, previousAction, statusFilter, candidateFilter, trackFilter, pageNumber, pageSize });
            }

            ViewBag.PreviousAction = previousAction ?? "Index";

            ViewBag.statusFilter = statusFilter;
            ViewBag.candidateFilter = candidateFilter;
            ViewBag.trackFilter = trackFilter;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            Result<InterviewsDTO> result = await _interviewsService.GetInterviewDetailsWithAdditionalInfo(id);

            await LoadSelectionLists();

            if (result.IsSuccess)
            {
                InterviewsDTO interviewsDTO = result.Value;
                interviewsDTO.InterviewerName = await _interviewsService.GetInterviewerName(interviewsDTO.InterviewerId);
                return View(interviewsDTO);
            }
            else
            {
                ModelState.AddModelError("", result.Error);
                return View();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/hrFirstDetails")]
    public async Task<ActionResult> HRFirstDetails(
        int id,
        string previousAction,
        int? statusFilter,
        string candidateFilter,
        int? companyFilter,
        int? trackFilter,
        int pageNumber = 1,
        int pageSize = 5)
    {
        try
        {
            ViewBag.PreviousAction = previousAction ?? "Index";

            ViewBag.statusFilter = statusFilter;
            ViewBag.candidateFilter = candidateFilter;
            ViewBag.companyFilter = companyFilter;
            ViewBag.trackFilter = trackFilter;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            Result<List<InterviewsDTO>> result = await _interviewsService.GetHRFirstFlowInterviewDetails(id);

            await LoadSelectionLists();

            if (result.IsSuccess)
            {
                List<InterviewsDTO> interviewsDTOs = result.Value;
                
                if (interviewsDTOs.Any())
                {
                    var firstInterview = interviewsDTOs.First();
                    ViewBag.CandidateName = firstInterview.FullName;
                    ViewBag.PositionName = firstInterview.Name;
                    ViewBag.TrackName = firstInterview.TrackName;
                    ViewBag.CandidateCVAttachmentId = firstInterview.CandidateCVAttachmentId;
                }

                return View(interviewsDTOs);
            }
            else
            {
                ModelState.AddModelError("", result.Error);
                return View(new List<InterviewsDTO>());
            }
        }
        catch (Exception)
        {
            throw;
        }
    }


    [Route("{id}/showHistory")]
    public async Task<ActionResult> ShowHistory(int id)
    {
        try
        {
            Result<List<InterviewsDTO>> result = await _interviewsService.ShowHistory(id);

            if (result.IsSuccess)
            {
                List<InterviewsDTO> interviewsDTOs = result.Value;
                Result<InterviewsDTO> interviews = await _interviewsService.GetInterviewDetails(id);
                InterviewsDTO interviewsResult = interviews.Value;

                if (interviewsResult != null)
                {
                    int candidateId = interviewsResult.CandidateId;
                    CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(candidateId);
                    ViewBag.CandidateName = candidate.FullName;
                }

                return View(interviewsDTOs);
            }
            else
            {
                ModelState.AddModelError("", result.Error);
                return View();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/showHistoryForHRFirstFlow")]
    public async Task<ActionResult> ShowHistoryForHRFirstFlow(
        int id,
        string previousAction = "MyInterviews",
        int? statusFilter = null,
        int? companyFilter = null,
        int? trackFilter = null)
    {
        try
        {
            ViewBag.PreviousAction = previousAction;
            ViewBag.statusFilter = statusFilter;
            ViewBag.companyFilter = companyFilter;
            ViewBag.trackFilter = trackFilter;

            Result<List<InterviewsDTO>> result = await _interviewsService.ShowHistoryForHRFirstFlow(id);

            if (result.IsSuccess)
            {
                List<InterviewsDTO> interviewsDTOs = result.Value;
                Result<InterviewsDTO> interviews = await _interviewsService.GetInterviewDetails(id);
                InterviewsDTO interviewsResult = interviews.Value;

                if (interviewsResult != null)
                {
                    int candidateId = interviewsResult.CandidateId;
                    CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(candidateId);
                    ViewBag.CandidateName = candidate.FullName;
                }

                return View(interviewsDTOs);
            }
            else
            {
                ModelState.AddModelError("", result.Error);
                return View();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("create")]
    [AuthorizeRoles("Admin", "HR Manager")]
    public async Task<ActionResult> Create()
    {
        try
        {
                await LoadSelectionLists();
                return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task LoadSelectionLists()
    {
        try
        {
            Result<IEnumerable<PositionDTO>> positions = await _positionService.GetAll();
            IOrderedEnumerable<PositionDTO> sortedPositions = positions.Value.OrderBy(x => x.Name);
            ViewBag.positionList = new SelectList(sortedPositions, "Id", "Name");

            IEnumerable<CandidateDTO> candidates = await _candidateService.GetAllCandidatesAsync();
            IOrderedEnumerable<CandidateDTO> sortedCandidates = candidates.OrderByDescending(x => x.Id);
            ViewBag.candidateList = new SelectList(sortedCandidates, "Id", "FullName");

            Result<IList<IdentityUser>> interviewers = await _accountService.GetAllInterviewers();
            ViewBag.interviewersList = new SelectList(interviewers.Value, "Id", "UserName");

            Result<IList<IdentityUser>> architectures = await _accountService.GetAllArchitectureInterviewers();
            ViewBag.architecturesList = new SelectList(architectures.Value, "Id", "UserName");

            Result<IList<IdentityUser>> gmUsers = await _accountService.GetAllInterviewersGM();
            ViewBag.GMUserIds = gmUsers.IsSuccess ? gmUsers.Value.Select(u => u.Id).ToList() : new List<string>();

            Result<List<StatusDTO>> statuses = await _StatusService.GetAll();
            ViewBag.statusList = new SelectList(statuses.Value, "Id", "Name");

            Result<List<TrackDTO>> tracks = await _trackService.GetAll();
            ViewBag.Tracks = new SelectList(tracks.Value.OrderBy(x => x.Name), "Id", "Name");
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("create")]
    public async Task<ActionResult> Create(InterviewsDTO collection)
    {
        try
        {
            string firstInterviewerRoles = await _interviewsService.GetInterviewerRole(collection.InterviewerId);
            string secondInterviewerRoles = await _interviewsService.GetInterviewerRole(collection.SecondInterviewerId);

            await LoadSelectionLists();

            if (!string.IsNullOrEmpty(collection.InterviewerId) && 
                !string.IsNullOrEmpty(collection.SecondInterviewerId) && 
                collection.InterviewerId == collection.SecondInterviewerId)
            {
                ModelState.AddModelError("InterviewerId", "This interviewer is already selected as Interviewer #2.");
                ModelState.AddModelError("SecondInterviewerId", "This interviewer is already selected as Interviewer #1.");
            }

            if (ModelState.IsValid)
            {
                Result<InterviewsDTO> result = await _interviewsService.Insert(collection);

                if (result.IsSuccess)
                {
                    if (User.IsInRole("HR Manager") || User.IsInRole("Admin"))
                    {
                        InterviewsDTO insertedInterview = result.Value;
                        collection.InterviewsId = insertedInterview.InterviewsId;

                        if (!collection.StartFromHR)
                        {
                            CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(collection.CandidateId);
                            Result<PositionDTO> positionResult = await _positionService.GetById(collection.PositionId);

                            string candidateName = candidate.FullName;
                            string positionName = positionResult.Value.Name;
                            string firstInterviewerEmail = await _emailService.GetInterviewerEmail(collection.InterviewerId);
                            string secondInterviewerEmail = await _emailService.GetInterviewerEmail(collection.SecondInterviewerId);
                            IdentityUser firstInterviewer = await _userManager.FindByEmailAsync(firstInterviewerEmail);
                            IdentityUser secondInterviewer = !string.IsNullOrEmpty(secondInterviewerEmail)
                                ? await _userManager.FindByEmailAsync(secondInterviewerEmail)
                                : null;

                            string firstInterviewerEmailBody = InterviewInvitationEmailTemplate.GetInvitationEmailTemplate(
                                firstInterviewer.UserName,
                                secondInterviewer?.UserName,
                                candidateName,
                                positionName,
                                collection.Date,
                                collection.InterviewsId
                            );

                            EmailDTOs emailModel = new()
                            {
                                EmailTo = [firstInterviewerEmail],
                                Subject = $"Interview Invitation ({candidateName})",
                                EmailBody = firstInterviewerEmailBody
                            };

                            await _emailService.SendEmailToInterviewer(firstInterviewerEmail, collection, emailModel);
                            await _notificationsService.CreateInterviewNotificationForInterviewerAsync(collection.Date, collection.CandidateId, collection.PositionId, new List<string> { collection.InterviewerId, collection.SecondInterviewerId }, isCanceled: false);

                            if (!string.IsNullOrEmpty(collection.SecondInterviewerId))
                            {
                                string secondInterviewerEmailBody = InterviewInvitationEmailTemplate.GetInvitationEmailTemplate(
                                    secondInterviewer.UserName,
                                    firstInterviewer.UserName,
                                    candidateName,
                                    positionName,
                                    collection.Date,
                                    collection.InterviewsId
                                );

                                EmailDTOs emailModel2 = new()
                                {
                                    EmailTo = [secondInterviewerEmail],
                                    Subject = $"Interview Invitation ({candidateName})",
                                    EmailBody = secondInterviewerEmailBody
                                };

                                await _emailService.SendEmailToInterviewer(secondInterviewerEmail, collection, emailModel2);
                            }
                        }

                        return RedirectToAction(nameof(Index));
                    }
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", result.Error);
            }
            else
                ModelState.AddModelError("", "Error validating the model");

            return View(collection);
        }
        catch (Exception)
        {
            throw;
        }
    }


    [Route("{id}/update")]
    [AuthorizeRoles("Admin", "HR Manager")]
    public async Task<ActionResult> Edit(int id, int? statusFilter,
        string candidateFilter,
        int? trackFilter,
        int pageNumber = 1,
        int pageSize = 5)
    {
        try
        {
            ViewBag.statusFilter = statusFilter;
            ViewBag.candidateFilter = candidateFilter;
            ViewBag.trackFilter = trackFilter;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

                if (id <= 0)
                    return NotFound();

                Result<List<StatusDTO>> StatusDTOs = await _StatusService.GetAll();
                ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

                Result<InterviewsDTO> result = await _interviewsService.GetInterviewDetails(id);
                InterviewsDTO interviewDTO = result.Value;

                if (interviewDTO is null)
                    return NotFound();

                await LoadSelectionLists();

                return View(interviewDTO);
           
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/update")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int id, InterviewsDTO collection)
    {
        try
        {
            if (collection is null)
            {
                ModelState.AddModelError("", $"The interview DTO you are trying to update is null ");
                return RedirectToAction(nameof(Index));
            }

            Result<List<StatusDTO>> StatusDTOs = await _StatusService.GetAll();
            ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

            await LoadSelectionLists();

            if (!collection.StatusId.HasValue)
            {
                ModelState.AddModelError("StatusId", "Please select a status.");
                return View(collection);
            }

            Result<StatusDTO> statusResult = await _StatusService.GetById((int)collection.StatusId);
            StatusDTO status = statusResult.Value;

            if (status.Code == Domain.Enums.StatusCode.Rejected && collection.Notes is null)
                ModelState.AddModelError("Notes", "Please add a note explaining why it was rejected.");

            if (!string.IsNullOrEmpty(collection.InterviewerId) && 
                !string.IsNullOrEmpty(collection.SecondInterviewerId) && 
                collection.InterviewerId == collection.SecondInterviewerId)
            {
                ModelState.AddModelError("InterviewerId", "This interviewer is already selected as Interviewer #2.");
                ModelState.AddModelError("SecondInterviewerId", "This interviewer is already selected as Interviewer #1.");
            }

            if (ModelState.IsValid)
            {
                var currentInterview = await _interviewsRepository.GetByIdForEdit(collection.InterviewsId);
                bool originalStartFromHR = currentInterview?.StartFromHR ?? false;
                bool newStartFromHR = collection.StartFromHR;

                if (originalStartFromHR != newStartFromHR && currentInterview != null && currentInterview.ParentId == null)
                {
                    if (newStartFromHR)
                    {
                        string firstInterviewerId = collection.InterviewerId;
                        string secondInterviewerId = collection.SecondInterviewerId;
                        string architectureInterviewerId = collection.ArchitectureInterviewerId;

                        var originalInterviewerIds = new List<string>();
                        if (!string.IsNullOrEmpty(firstInterviewerId))
                            originalInterviewerIds.Add(firstInterviewerId);
                        if (!string.IsNullOrEmpty(secondInterviewerId))
                            originalInterviewerIds.Add(secondInterviewerId);
                        if (!string.IsNullOrEmpty(architectureInterviewerId))
                            originalInterviewerIds.Add(architectureInterviewerId);

                        if (originalInterviewerIds.Count > 0)
                        {
                            await _interviewsRepository.DeleteNotificationsByCandidateAndReceiversAsync(
                                collection.CandidateId, 
                                originalInterviewerIds);
                        }

                        await _interviewsRepository.DeleteChildInterviewsAndNotificationsAsync(
                            collection.InterviewsId, 
                            collection.CandidateId);

                        collection.WorkflowStageId = (int)Domain.Enums.EnumWorkflowStage.HRInitialInterview;
                        
                        var hrUsers = await _userManager.GetUsersInRoleAsync("HR Manager");
                        var hrUser = hrUsers.FirstOrDefault();
                        if (hrUser != null)
                        {
                            collection.InterviewerId = hrUser.Id;
                        }

                        collection.SecondInterviewerId = null;
                        collection.ArchitectureInterviewerId = null;

                        if (!string.IsNullOrEmpty(firstInterviewerId) || 
                            !string.IsNullOrEmpty(secondInterviewerId) || 
                            !string.IsNullOrEmpty(architectureInterviewerId))
                        {
                            var existingSelected = await _selectedInterviewersRepository.GetByInterviewIdAsync(collection.InterviewsId);
                            if (existingSelected == null)
                            {
                                var selectedInterviewers = new Domain.Entities.SelectedInterviewers
                                {
                                    InterviewId = collection.InterviewsId,
                                    FirstInterviewerId = firstInterviewerId,
                                    SecondInterviewerId = secondInterviewerId,
                                    ArchitectureInterviewerId = architectureInterviewerId,
                                    CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier),
                                    CreatedOn = DateTime.Now,
                                    IsActive = true
                                };
                                await _selectedInterviewersRepository.InsertAsync(selectedInterviewers);
                            }
                            else
                            {
                                existingSelected.FirstInterviewerId = firstInterviewerId;
                                existingSelected.SecondInterviewerId = secondInterviewerId;
                                existingSelected.ArchitectureInterviewerId = architectureInterviewerId;
                                existingSelected.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                                existingSelected.ModifiedOn = DateTime.Now;
                                await _selectedInterviewersRepository.UpdateAsync(existingSelected);
                            }
                        }
                    }
                    else
                    {
                        await _interviewsRepository.DeleteChildInterviewsAndNotificationsAsync(
                            collection.InterviewsId, 
                            collection.CandidateId);

                        var selectedInterviewers = await _selectedInterviewersRepository.GetByInterviewIdAsync(collection.InterviewsId);
                        if (selectedInterviewers != null)
                        {
                            if (!string.IsNullOrEmpty(selectedInterviewers.FirstInterviewerId))
                            {
                                collection.InterviewerId = selectedInterviewers.FirstInterviewerId;
                            }
                            collection.SecondInterviewerId = selectedInterviewers.SecondInterviewerId;
                            collection.ArchitectureInterviewerId = selectedInterviewers.ArchitectureInterviewerId;
                            
                            await _selectedInterviewersRepository.DeleteAsync(selectedInterviewers.Id);
                        }

                        collection.WorkflowStageId = (int)Domain.Enums.EnumWorkflowStage.InitialInterview;
                    }
                }

                string previousInterviewerId = HttpContext.Session.GetString($"InterviewerId_{collection.InterviewsId}");
                string previousSecondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{collection.InterviewsId}");
                if (status.Code == Domain.Enums.StatusCode.Pending)
                {
                    await _notificationsService.CreateInterviewNotificationForInterviewerAsync(
                                                                                                collection.Date,
                                                                                                collection.CandidateId,
                                                                                                collection.PositionId,
                                                                                                new List<string> { previousInterviewerId, previousSecondInterviewerId },
                                                                                                isCanceled: true
                                                                                              );
                }

                Result<InterviewsDTO> result = await _interviewsService.Update(collection);

                if (result.IsSuccess)
                {
                    if (originalStartFromHR == newStartFromHR)
                    {
                        CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(collection.CandidateId);
                        Result<PositionDTO> positionResult = await _positionService.GetById(collection.PositionId);

                        string candidateName = candidate.FullName;
                        string positionName = positionResult.Value.Name;
                        string firstInterviewerEmail = await _emailService.GetInterviewerEmail(collection.InterviewerId);
                        string secondInterviewerEmail = await _emailService.GetInterviewerEmail(collection.SecondInterviewerId);
                        IdentityUser firstInterviewer = await _userManager.FindByEmailAsync(firstInterviewerEmail);
                        IdentityUser secondInterviewer = !string.IsNullOrEmpty(secondInterviewerEmail)
                            ? await _userManager.FindByEmailAsync(secondInterviewerEmail)
                            : null;

                        string firstInterviewerEmailBody = InterviewInvitationEmailTemplate.UpdatedInvitationEmail(
                            firstInterviewer.UserName,
                            secondInterviewer?.UserName,
                            candidateName,
                            positionName,
                            collection.Date,
                            collection.InterviewsId.ToString()
                        );

                        EmailDTOs emailModel = new()
                        {
                            EmailTo = [firstInterviewerEmail],
                            Subject = $"Updated Interview Invitation ({candidateName})",
                            EmailBody = firstInterviewerEmailBody
                        };

                        await _emailService.SendEmailToInterviewer(firstInterviewerEmail, collection, emailModel);
                        await _notificationsService.CreateInterviewNotificationForInterviewerAsync(collection.Date, collection.CandidateId, collection.PositionId, new List<string> { collection.InterviewerId, collection.SecondInterviewerId }, isCanceled: false);

                        if (!string.IsNullOrEmpty(collection.SecondInterviewerId))
                        {
                            string secondInterviewerEmailBody = InterviewInvitationEmailTemplate.UpdatedInvitationEmail(
                                secondInterviewer.UserName,
                                firstInterviewer.UserName,
                                candidateName,
                                positionName,
                                collection.Date,
                                collection.InterviewsId.ToString()
                            );

                            EmailDTOs emailModel2 = new()
                            {
                                EmailTo = [secondInterviewerEmail],
                                Subject = $"Updated Interview Invitation ({candidateName})",
                                EmailBody = secondInterviewerEmailBody
                            };

                            await _emailService.SendEmailToInterviewer(secondInterviewerEmail, collection, emailModel2);
                        }
                    }
                    else if (!newStartFromHR && originalStartFromHR)
                    {
                        CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(collection.CandidateId);
                        Result<PositionDTO> positionResult = await _positionService.GetById(collection.PositionId);

                        string candidateName = candidate.FullName;
                        string positionName = positionResult.Value.Name;
                        string firstInterviewerEmail = await _emailService.GetInterviewerEmail(collection.InterviewerId);
                        string secondInterviewerEmail = await _emailService.GetInterviewerEmail(collection.SecondInterviewerId);
                        IdentityUser firstInterviewer = await _userManager.FindByEmailAsync(firstInterviewerEmail);
                        IdentityUser secondInterviewer = !string.IsNullOrEmpty(secondInterviewerEmail)
                            ? await _userManager.FindByEmailAsync(secondInterviewerEmail)
                            : null;

                        if (!string.IsNullOrEmpty(collection.InterviewerId))
                        {
                            string firstInterviewerEmailBody = InterviewInvitationEmailTemplate.GetInvitationEmailTemplate(
                                firstInterviewer.UserName,
                                secondInterviewer?.UserName,
                                candidateName,
                                positionName,
                                collection.Date,
                                collection.InterviewsId
                            );

                            EmailDTOs emailModel = new()
                            {
                                EmailTo = [firstInterviewerEmail],
                                Subject = $"Interview Invitation ({candidateName})",
                                EmailBody = firstInterviewerEmailBody
                            };

                            await _emailService.SendEmailToInterviewer(firstInterviewerEmail, collection, emailModel);
                            await _notificationsService.CreateInterviewNotificationForInterviewerAsync(collection.Date, collection.CandidateId, collection.PositionId, new List<string> { collection.InterviewerId, collection.SecondInterviewerId }, isCanceled: false);

                            if (!string.IsNullOrEmpty(collection.SecondInterviewerId))
                            {
                                string secondInterviewerEmailBody = InterviewInvitationEmailTemplate.GetInvitationEmailTemplate(
                                    secondInterviewer.UserName,
                                    firstInterviewer.UserName,
                                    candidateName,
                                    positionName,
                                    collection.Date,
                                    collection.InterviewsId
                                );

                                EmailDTOs emailModel2 = new()
                                {
                                    EmailTo = [secondInterviewerEmail],
                                    Subject = $"Interview Invitation ({candidateName})",
                                    EmailBody = secondInterviewerEmailBody
                                };

                                await _emailService.SendEmailToInterviewer(secondInterviewerEmail, collection, emailModel2);
                            }
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", result.Error);
            }
            else
            {
                ModelState.AddModelError("", "Error validating the model");
            }

            return View(collection);
        }
        catch (Exception)
        {
            throw;
        }
    }


    [Route("{id}/delete")]
    [AuthorizeRoles("Admin", "HR Manager")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            Result<InterviewsDTO> result = await _interviewsService.GetInterviewDetails(id);

            if (result.IsSuccess)
            {
                InterviewsDTO interviewDTO = result.Value;
                interviewDTO.InterviewerName = await _interviewsService.GetInterviewerName(interviewDTO.InterviewerId);
                return View(interviewDTO);
            }

            else
            {
                ModelState.AddModelError("", result.Error);
                return View();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int id, InterviewsDTO collection)
    {
        try
        {
            if (id <= 0)
                return BadRequest("invalid career offer id");

            Result<InterviewsDTO> result = await _interviewsService.Delete(id);

            if (result.IsSuccess)
                return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", result.Error);
            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/updateAttachment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttachment(int id, IFormFile file)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Please choose a file to upload.");
                return View();
            }

            if (ModelState.IsValid)
            {
                FileStream stream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);

                try
                {
                    await _interviewsService.UpdateInterviewAttachmentAsync(id, file.FileName, file.Length, stream);
                    return RedirectToAction(nameof(Index));
                }
                finally
                {
                    stream.Close();
                    AttachmentHelper.removeFile(file.FileName, _attachmentStoragePath);
                }

            }
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/updateResult")]
    public async Task<IActionResult> UpdateAfterInterviewForEdit(
        int id,
        int? statusFilter = null,
        int? companyFilter = null,
        int? trackFilter = null)
    {
        try
        {
            ViewBag.statusFilter = statusFilter;
            ViewBag.companyFilter = companyFilter;
            ViewBag.trackFilter = trackFilter;

            Result<List<StatusDTO>> StatusDTOs = await _StatusService.GetAll();
            ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

            Result<InterviewsDTO> result = await _interviewsService.GetInterviewDetails(id);
            InterviewsDTO InterviewsDTO = result.Value;

            if (InterviewsDTO.AttachmentId != null)
                ViewBag.ExistingAttachmentId = InterviewsDTO.AttachmentId;

            else
            {
                if (InterviewsDTO.AttachmentId.HasValue)
                {
                    AttachmentDTO existingAttachment = await _attachmentService.GetAttachmentByIdAsync(InterviewsDTO.AttachmentId.Value);

                    if (existingAttachment != null)
                    {
                        AttachmentDTO attachmentDTO = existingAttachment;
                        InterviewsDTO.FileName = attachmentDTO.FileName;
                        InterviewsDTO.FileSize = attachmentDTO.FileSize;
                    }
                    ViewBag.FileName = InterviewsDTO.FileName;
                }

                return View(InterviewsDTO);
            }
            return View(InterviewsDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/addingResult")]
    public async Task<IActionResult> UpdateAfterInterview(
        int id,
        int? statusFilter = null,
        int? companyFilter = null,
        int? trackFilter = null)
    {
        try
        {
            if (_signInManager.IsSignedIn(User))
            {
                ViewBag.statusFilter = statusFilter;
                ViewBag.companyFilter = companyFilter;
                ViewBag.trackFilter = trackFilter;

                Result<List<StatusDTO>> StatusDTOs = await _StatusService.GetAll();
                ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

                Result<InterviewsDTO> result = await _interviewsService.GetInterviewDetails(id);
                InterviewsDTO InterviewsDTO = result.Value;

                string secondInterviewerIdFromSession = HttpContext.Session.GetString($"SecondInterviewerId_{id}");
                if (!string.IsNullOrEmpty(secondInterviewerIdFromSession))
                {
                    InterviewsDTO.SecondInterviewerId = secondInterviewerIdFromSession;
                }

                if (InterviewsDTO != null && InterviewsDTO.StatusId.HasValue)
                {
                    Result<StatusDTO> currentStatusResult = await _StatusService.GetById(InterviewsDTO.StatusId.Value);
                    if (currentStatusResult.IsSuccess && currentStatusResult.Value != null)
                    {
                        string code = currentStatusResult.Value.Code;
                        if (!string.Equals(code, Domain.Enums.StatusCode.Pending, StringComparison.OrdinalIgnoreCase))
                        {
                            return View("ResultAlreadySubmitted", InterviewsDTO);
                        }
                    }
                }

                return View(InterviewsDTO);
            }
            else
            {
                TempData["ErrorMessage"] = "You must log in first.";
                return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/addingResult")]
    public async Task<IActionResult> UpdateAfterInterview(InterviewsDTO interviewsDTO, IFormFile file)
    {
        try
        {
            if (!interviewsDTO.StatusId.HasValue)
                ModelState.AddModelError("StatusId", "Please select a status.");

            string firstInterviewerRoles = await _interviewsService.GetInterviewerRole(interviewsDTO.InterviewerId);
            string secondInterviewerRoles = await _interviewsService.GetInterviewerRole(interviewsDTO.SecondInterviewerId);

            CandidateDTO candidateName = await _candidateService.GetCandidateByIdAsync(interviewsDTO.CandidateId);
            string candidateNameresult = candidateName.FullName;

            Result<PositionDTO> positionName = await _positionService.GetById(interviewsDTO.PositionId);
            PositionDTO positionNameresult = positionName.Value;
            string lastPositionName = positionNameresult.Name;

            Result<List<StatusDTO>> StatusDTOs = await _StatusService.GetAll();
            ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

            List<string> validationErrors = new List<string>();

            if ((file is null || file.Length == 0) && User.IsInRole("Interviewer"))
                ModelState.AddModelError("AttachmentId", "Please choose a file to upload.");

            if (User.IsInRole("Interviewer") || User.IsInRole("General Manager") || User.IsInRole("Solution Architecture"))
            {
                if (interviewsDTO.ActualExperience is null)
                    ModelState.AddModelError("ActualExperience", "Please add the actual experience.");
            }


            if (!interviewsDTO.StatusId.HasValue)
                ModelState.AddModelError("StatusId", "Please select a status.");

            else
            {
                Result<StatusDTO> statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);

                if (!statusResult.IsSuccess)
                    ModelState.AddModelError("StatusId", "Invalid status selected.");

                else
                {
                    StatusDTO status = statusResult.Value;

                    if (status.Code == Domain.Enums.StatusCode.Rejected && string.IsNullOrWhiteSpace(interviewsDTO.Notes))
                        ModelState.AddModelError("Notes", "Please add a note for why (He/She) was rejected.");
                }
            }


            if (interviewsDTO.Score is null && User.IsInRole("Interviewer"))
                ModelState.AddModelError("Score", "Please add a score.");

            if (interviewsDTO.StatusId is null)
                ModelState.AddModelError("StatusId", "Please select a status.");

            if (validationErrors.Count() > 0)
            {
                foreach (string validation in validationErrors)
                    ModelState.AddModelError("", validation);

                return View(interviewsDTO);
            }

            FileStream attachmentStream = null;
            if (file != null && file.Length != 0)
            {
                attachmentStream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
                interviewsDTO.FileName = file.FileName;
                interviewsDTO.FileSize = file.Length;
                interviewsDTO.FileData = attachmentStream;
            }

            if (ModelState.IsValid)
            {
                Domain.Entities.Interviews currentInterview = await _interviewsRepository.GetById(interviewsDTO.InterviewsId);
                bool isHRFirstFlow = currentInterview?.StartFromHR == true;

                try
                {
                    Result<StatusDTO> newStatusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                    if (newStatusResult.IsSuccess)
                    {
                        StatusDTO newStatus = newStatusResult.Value;

                        if ((newStatus.Code == Domain.Enums.StatusCode.OnHold || newStatus.Code == Domain.Enums.StatusCode.Rejected))
                        {
                            await _notificationsService.CreateInterviewNotificationtoHrForOnHold(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                            if (newStatus.Code == Domain.Enums.StatusCode.OnHold)
                            {
                                string userNameForOnHold = _emailService.GetLoggedInUserName();
                                string HREmailForOnHold = await _emailService.GetHREmail();
                                
                                string hrOnHoldEmailBody = HRInvitationEmailTemplate.GetHROnHoldEmail(
                                    "Aseel",
                                    candidateNameresult,
                                    userNameForOnHold,
                                    "CMS"
                                );

                                EmailDTOs emailModel = new()
                                {
                                    EmailTo = [HREmailForOnHold],
                                    Subject = $"Interview On Hold ({candidateNameresult})",
                                    EmailBody = hrOnHoldEmailBody
                                };

                                if (!string.IsNullOrEmpty(HREmailForOnHold))
                                    await _emailService.SendEmailToInterviewer(HREmailForOnHold, interviewsDTO, emailModel);
                            }

                            string nextInterviewStatusCode = await _interviewsRepository.GetStatusOfNextInterview(interviewsDTO.CandidateId, interviewsDTO.InterviewsId);

                            if (currentInterview.Status.Code != Domain.Enums.StatusCode.Pending && currentInterview.Status.Code != Domain.Enums.StatusCode.Rejected)
                            {
                                int interviewCount = await _interviewsRepository.GetInterviewCountForCandidate(interviewsDTO.CandidateId);
                                
                                bool isGMFinalReviewStage = currentInterview.WorkflowStageId == (int)Domain.Enums.EnumWorkflowStage.GMFinalReview;
                                bool isFinalStage = nextInterviewStatusCode == null;
                                
                                bool isHRFirstFlowFinalStage = isHRFirstFlow && isGMFinalReviewStage && isFinalStage;
                                
                                bool isGMOrArchiInFinalStage = isHRFirstFlowFinalStage && 
                                    (User.IsInRole("General Manager") || User.IsInRole("Solution Architecture"));

                                if (newStatus.Code == Domain.Enums.StatusCode.Rejected && !User.IsInRole("HR Manager"))
                                {
                                    if (nextInterviewStatusCode != null && !nextInterviewStatusCode.Equals(Domain.Enums.StatusCode.Pending))
                                    {
                                        ModelState.AddModelError("StatusId", "Cannot set the interview status to Rejected because it has already been marked as done after the interview.");
                                        if (attachmentStream != null)
                                        {
                                            attachmentStream.Close();
                                            attachmentStream.Dispose();
                                        }
                                        return View(interviewsDTO);
                                    }
                                    else
                                    {
                                        bool interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(nextInterviewStatusCode, interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));
                                    }
                                }
                                else
                                {

                                    if ((interviewCount >= 1 && interviewCount <= 2) || ((interviewCount == 3 || interviewCount == 4) && User.IsInRole("General Manager")))
                                    {

                                        bool interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(nextInterviewStatusCode, interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));
                                        if (!interviewsDeleted)
                                        {
                                            ModelState.AddModelError("StatusId", "Cannot set the interview status to On Hold because it has already been marked as done after the interview.");
                                            if (attachmentStream != null)
                                            {
                                                attachmentStream.Close();
                                                attachmentStream.Dispose();
                                            }
                                            return View(interviewsDTO);
                                        }
                                    }
                                    else if (!User.IsInRole("HR Manager") && !isGMOrArchiInFinalStage)
                                    {
                                        ModelState.AddModelError("StatusId", "Cannot set the interview status to On Hold because it has already been marked as done after the interview.");
                                        if (attachmentStream != null)
                                        {
                                            attachmentStream.Close();
                                            attachmentStream.Dispose();
                                        }
                                        return View(interviewsDTO);
                                    }
                                }
                            }
                        }
                        else if (newStatus.Code == Domain.Enums.StatusCode.Approved && !User.IsInRole("HR Manager"))
                        {
                            string nextInterviewStatusCode = await _interviewsRepository.GetStatusOfNextInterview(interviewsDTO.CandidateId, interviewsDTO.InterviewsId);
                            if (nextInterviewStatusCode != null && nextInterviewStatusCode.Equals(Domain.Enums.StatusCode.Pending))
                            {
                                bool interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(nextInterviewStatusCode, interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));
                            }

                            else if (nextInterviewStatusCode != null && (nextInterviewStatusCode.Equals(Domain.Enums.StatusCode.Approved) || nextInterviewStatusCode.Equals(Domain.Enums.StatusCode.Rejected)))
                            {
                                ModelState.AddModelError("StatusId", "Cannot set the interview status to Approved or Change the result because it has already been marked as done after the interview.");
                                if (attachmentStream != null)
                                {
                                    attachmentStream.Close();
                                    attachmentStream.Dispose();
                                }
                                return View(interviewsDTO);
                            }

                        }

                        if (User.IsInRole("HR Manager"))
                        {
                            var currentInterviewForHR = await _interviewsRepository.GetById(interviewsDTO.InterviewsId);
                            if (currentInterviewForHR != null && currentInterviewForHR.StartFromHR == true)
                            {
                                string nextInterviewStatusCode = await _interviewsRepository.GetStatusOfNextInterview(interviewsDTO.CandidateId, interviewsDTO.InterviewsId);
                                
                                if (nextInterviewStatusCode != null && !nextInterviewStatusCode.Equals(Domain.Enums.StatusCode.Pending))
                                {
                                    ModelState.AddModelError("StatusId", "Cannot change the interview status because it has already been marked as done after the interview.");
                                    if (attachmentStream != null)
                                    {
                                        attachmentStream.Close();
                                        attachmentStream.Dispose();
                                    }
                                    return View(interviewsDTO);
                                }
                            }
                        }
                    }

                    IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                    var hrInterviewForReverseWorkflow = await _interviewsRepository.GetById(interviewsDTO.InterviewsId);
                    bool isReverseWorkflowHRApproval = hrInterviewForReverseWorkflow?.StartFromHR == true 
                        && await _userManager.IsInRoleAsync(currentUser, "HR Manager")
                        && interviewsDTO.StatusId.HasValue;

                    if (await _userManager.IsInRoleAsync(currentUser, "General Manager"))
                        await _interviewsService.ConductInterviewForGm(interviewsDTO);

                    else if (await _userManager.IsInRoleAsync(currentUser, "HR Manager"))
                    {
                        if (isReverseWorkflowHRApproval)
                        {
                            string secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                            string interviewerId = HttpContext.Session.GetString($"InterviewerId_{interviewsDTO.InterviewsId}");
                            await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);
                        }
                        else
                        {
                            string secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                            string interviewerId = HttpContext.Session.GetString($"InterviewerId_{interviewsDTO.InterviewsId}");
                            await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);
                        }
                    }
                    else if (await _userManager.IsInRoleAsync(currentUser, "Interviewer"))
                    {
                        string secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                        string interviewerId = HttpContext.Session.GetString($"InterviewerId_{interviewsDTO.InterviewsId}");
                        await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);
                    }
                    else if (await _userManager.IsInRoleAsync(currentUser, "Solution Architecture"))
                    {
                        Domain.Entities.Interviews archiInterview = await _interviewsRepository.GetById(interviewsDTO.InterviewsId);
                        
                        bool isCurrentUserFirstInterviewer = !string.IsNullOrEmpty(archiInterview?.InterviewerId) && 
                                                             archiInterview.InterviewerId == currentUser.Id;
                        
                        bool isCurrentUserSecondInterviewer = !string.IsNullOrEmpty(archiInterview?.SecondInterviewerId) && 
                                                              archiInterview.SecondInterviewerId == currentUser.Id;
                        
                        bool isCurrentUserArchitectureInterviewer = !string.IsNullOrEmpty(archiInterview?.ArchitectureInterviewerId) && 
                                                                    archiInterview.ArchitectureInterviewerId == currentUser.Id;

                        if (isCurrentUserFirstInterviewer || isCurrentUserSecondInterviewer || isCurrentUserArchitectureInterviewer)
                        {
                            await _interviewsService.ConductInterviewForArchi(interviewsDTO);
                        }
                        else
                        {
                            string secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                            string interviewerId = HttpContext.Session.GetString($"InterviewerId_{interviewsDTO.InterviewsId}");
                            
                            if (string.IsNullOrEmpty(interviewerId))
                                interviewerId = archiInterview?.InterviewerId;
                            if (string.IsNullOrEmpty(secondInterviewerId))
                                secondInterviewerId = archiInterview?.SecondInterviewerId;
                            
                            await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);
                        }
                    }
                    else
                    {
                        IdentityUser firstinterviewer = await _userManager.FindByIdAsync(interviewsDTO.InterviewerId);
                        string secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                        IdentityUser secondInterviewer = await _userManager.FindByIdAsync(secondInterviewerId);
                        string interviewerId = HttpContext.Session.GetString($"InterviewerId_{interviewsDTO.InterviewsId}");

                        if (secondInterviewer != null)
                        {
                            bool isInterviewerGMCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Solution Architecture", "General Manager");
                            bool isGMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "General Manager", "Solution Architecture");

                            if (isInterviewerGMCombo || isGMInterviewerCombo)
                                await _interviewsService.ConductInterviewForArchi(interviewsDTO);

                            else
                                await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);
                        }
                        else
                            await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);
                    }

                    if (isReverseWorkflowHRApproval && interviewsDTO.StatusId.HasValue)
                    {
                        Result<StatusDTO> statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                        if (statusResult.IsSuccess && statusResult.Value.Code == Domain.Enums.StatusCode.Approved)
                        {
                            if (hrInterviewForReverseWorkflow != null)
                            {
                                var selectedInterviewers = await _selectedInterviewersRepository.GetByInterviewIdAsync(hrInterviewForReverseWorkflow.InterviewsId);
                                
                                if (selectedInterviewers == null)
                                    return RedirectToAction(nameof(MyInterviews));

                                string firstInterviewerId = selectedInterviewers.FirstInterviewerId;
                                string secondInterviewerId = selectedInterviewers.SecondInterviewerId;
                                string architectureInterviewerId = selectedInterviewers.ArchitectureInterviewerId;

                                CandidateDTO candidateForEmail = await _candidateService.GetCandidateByIdAsync(interviewsDTO.CandidateId);
                                Result<PositionDTO> positionResultForEmail = await _positionService.GetById(interviewsDTO.PositionId);
                                string candidateNameForEmail = candidateForEmail.FullName;
                                string positionNameForEmail = positionResultForEmail.Value.Name;

                                DateTime interviewDate = hrInterviewForReverseWorkflow?.Date ?? interviewsDTO.Date;

                                if (!string.IsNullOrEmpty(firstInterviewerId))
                                {
                                    string firstInterviewerEmail = await _emailService.GetInterviewerEmail(firstInterviewerId);
                                    IdentityUser firstInterviewer = await _userManager.FindByIdAsync(firstInterviewerId);
                                    IdentityUser secondInterviewer = !string.IsNullOrEmpty(secondInterviewerId) 
                                        ? await _userManager.FindByIdAsync(secondInterviewerId) 
                                        : null;

                                    string firstInterviewerEmailBody = InterviewInvitationEmailTemplate.GetInvitationEmailTemplate(
                                        firstInterviewer?.UserName,
                                        secondInterviewer?.UserName,
                                        candidateNameForEmail,
                                        positionNameForEmail,
                                        interviewDate,
                                        interviewsDTO.InterviewsId
                                    );

                                    EmailDTOs emailModel = new()
                                    {
                                        EmailTo = [firstInterviewerEmail],
                                        Subject = $"Interview Invitation ({candidateNameForEmail})",
                                        EmailBody = firstInterviewerEmailBody
                                    };

                                    await _emailService.SendEmailToInterviewer(firstInterviewerEmail, interviewsDTO, emailModel);
                                    await _notificationsService.CreateInterviewNotificationForInterviewerAsync(
                                        interviewDate, 
                                        interviewsDTO.CandidateId, 
                                        interviewsDTO.PositionId, 
                                        new List<string> { firstInterviewerId }, 
                                        isCanceled: false);
                                }

                                if (!string.IsNullOrEmpty(secondInterviewerId))
                                {
                                    string secondInterviewerEmail = await _emailService.GetInterviewerEmail(secondInterviewerId);
                                    IdentityUser firstInterviewer = await _userManager.FindByIdAsync(firstInterviewerId);
                                    IdentityUser secondInterviewer = await _userManager.FindByIdAsync(secondInterviewerId);

                                    string secondInterviewerEmailBody = InterviewInvitationEmailTemplate.GetInvitationEmailTemplate(
                                        secondInterviewer?.UserName,
                                        firstInterviewer?.UserName,
                                        candidateNameForEmail,
                                        positionNameForEmail,
                                        interviewDate,
                                        interviewsDTO.InterviewsId
                                    );

                                    EmailDTOs emailModel2 = new()
                                    {
                                        EmailTo = [secondInterviewerEmail],
                                        Subject = $"Interview Invitation ({candidateNameForEmail})",
                                        EmailBody = secondInterviewerEmailBody
                                    };

                                    await _emailService.SendEmailToInterviewer(secondInterviewerEmail, interviewsDTO, emailModel2);
                                    await _notificationsService.CreateInterviewNotificationForInterviewerAsync(
                                        interviewDate, 
                                        interviewsDTO.CandidateId, 
                                        interviewsDTO.PositionId, 
                                        new List<string> { secondInterviewerId }, 
                                        isCanceled: false);
                                }
                            }
                        }
                    }

                    if (attachmentStream != null)
                    {
                        attachmentStream.Close();
                        attachmentStream.Dispose();
                        AttachmentHelper.removeFile(file.FileName, _attachmentStoragePath);
                    }

                    string userName = _emailService.GetLoggedInUserName();
                    string GMEmail = await _emailService.GetGMEmail();
                    string HREmail = await _emailService.GetHREmail();
                    string ArchiEmail = await _emailService.GetArchiEmail();

                    var gmUsers = await _userManager.GetUsersInRoleAsync("General Manager");
                    var gmUserIds = gmUsers.Select(u => u.Id).ToList();

                    IdentityUser userGM = await _userManager.FindByEmailAsync(GMEmail);
                    IdentityUser userHR = await _userManager.FindByEmailAsync(HREmail);
                    IdentityUser userArchi = await _userManager.FindByEmailAsync(ArchiEmail);

                    string hrApprovalEmailBody = HRInvitationEmailTemplate.GetHRApprovalEmail(
                                                                                                 "Aseel",
                                                                                                 candidateNameresult,
                                                                                                 userName
                                                                                             );
                    string hrRejectionEmailBody = HRInvitationEmailTemplate.GetHRRejectionEmail(
                                                                                                   "Aseel",
                                                                                                   candidateNameresult,
                                                                                                   userName,
                                                                                                   "CMS"
                                                                                               );

                    string hrInvitationEmailBody = HRInvitationEmailTemplate.GetFinalHRInterviewEmail(
                                                                                                        "Aseel",
                                                                                                        candidateNameresult,
                                                                                                        lastPositionName,
                                                                                                        "https://apps.sssprocess.com:6134/"
                                                                                                     );

                    string hrSecondInterviewApprovalEmailBody = HRInvitationEmailTemplate.GetHRSecondInterviewApprovalEmail(
                                                                                                        "Aseel",
                                                                                                        candidateNameresult,
                                                                                                        userName
                                                                                                     );

                    string gmInvitationEmailBody = GMInterviewInvitationEmailTemplate.GetGMInvitationEmail(
                                                                                                            userGM.UserName,
                                                                                                            candidateNameresult,
                                                                                                            lastPositionName,
                                                                                                            "https://apps.sssprocess.com:6134/"
                                                                                                          );

                    string architectureEmailBody = ArchitectureInterviewEmailTemplate.GetArchiAndGmInvitationEmail(
                                                                                                                      userArchi.UserName,
                                                                                                                      candidateNameresult,
                                                                                                                      lastPositionName,
                                                                                                                      "https://apps.sssprocess.com:6134/"
                                                                                                                  );

                    if (User.IsInRole("Interviewer"))
                    {

                        Result<StatusDTO> statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                        StatusDTO status = statusResult.Value;


                        if (status.Code == Domain.Enums.StatusCode.Rejected || status.Code == Domain.Enums.StatusCode.Approved)
                        {
                            if (status.Code == Domain.Enums.StatusCode.Approved)
                            {
                                if (isHRFirstFlow)
                                {
                                    IdentityUser currentApprovingUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                                    bool isApprovingUserGM = await _userManager.IsInRoleAsync(currentApprovingUser, "General Manager");
                                    bool isApprovingUserInterviewer = await _userManager.IsInRoleAsync(currentApprovingUser, "Interviewer");
                                    
                                    var originalHRInterview = currentInterview;
                                    while (originalHRInterview != null && originalHRInterview.ParentId != null)
                                    {
                                        var parentInterview = await _interviewsRepository.GetById(originalHRInterview.ParentId.Value);
                                        if (parentInterview == null)
                                            break;
                                        originalHRInterview = parentInterview;
                                    }
                                    
                                    string architectureInterviewerId = null;
                                    if (originalHRInterview != null)
                                    {
                                        var selectedInterviewers = await _selectedInterviewersRepository.GetByInterviewIdAsync(originalHRInterview.InterviewsId);
                                        architectureInterviewerId = selectedInterviewers?.ArchitectureInterviewerId;
                                    }

                                    bool isInterviewerGM = !string.IsNullOrEmpty(interviewsDTO.InterviewerId) && gmUserIds.Contains(interviewsDTO.InterviewerId);
                                    bool isSecondInterviewerGM = !string.IsNullOrEmpty(interviewsDTO.SecondInterviewerId) && gmUserIds.Contains(interviewsDTO.SecondInterviewerId);

                                    if (isApprovingUserGM)
                                        {
                                        
                                        if (!isInterviewerGM && !isSecondInterviewerGM)
                                        {
                                            await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, architectureInterviewerId);
                                            
                                            EmailDTOs emailModel = new EmailDTOs
                                            {
                                                EmailTo = [GMEmail],
                                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                EmailBody = gmInvitationEmailBody
                                            };
                                                
                                            if (!string.IsNullOrEmpty(GMEmail))
                                                await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModel);
                                        }
                                    
                                        if (!string.IsNullOrEmpty(architectureInterviewerId))
                                    {
                                            await _notificationsService.CreateNotificationForArchiAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);
                                            
                                                EmailDTOs architectureEmailModel = new()
                                                {
                                                EmailTo = [ArchiEmail],
                                                    Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                    EmailBody = architectureEmailBody
                                                };
                                            if (!string.IsNullOrEmpty(ArchiEmail))
                                                await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                            }
                                        
                                        return RedirectToAction(nameof(MyInterviews));
                                        }
                                    else
                                    {
                                        if (isApprovingUserInterviewer && isHRFirstFlow)
                                        {
                                            string approvingInterviewerName = currentApprovingUser?.UserName ?? userName;

                                            await _notificationsService.CreateInterviewNotificationForFinalHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                            EmailDTOs emailModelToHR = new()
                                            {
                                                EmailTo = [HREmail],
                                                Subject = $"Second Interview Approval ({candidateNameresult})",
                                                EmailBody = hrSecondInterviewApprovalEmailBody
                                            };

                                            if (!string.IsNullOrEmpty(HREmail))
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);
                                        }

                                        if (!isInterviewerGM && !isSecondInterviewerGM)
                                        {
                                            await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, architectureInterviewerId);

                                            EmailDTOs emailModel = new()
                                            {
                                                    EmailTo = [GMEmail],
                                                    Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                    EmailBody = gmInvitationEmailBody
                                                };
                                            
                                            if (!string.IsNullOrEmpty(GMEmail))
                                                await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModel);
                                        }
                                        
                                        if (!string.IsNullOrEmpty(architectureInterviewerId))
                                        {
                                            await _notificationsService.CreateNotificationForArchiAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);
                                            
                                            EmailDTOs architectureEmailModel = new()
                                            {
                                                EmailTo = [ArchiEmail],
                                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                EmailBody = architectureEmailBody
                                            };
                                            if (!string.IsNullOrEmpty(ArchiEmail))
                                                await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                    }
                                    
                                    return RedirectToAction(nameof(MyInterviews));
                                }
                                }

                                IdentityUser firstinterviewer = await _userManager.FindByIdAsync(interviewsDTO.InterviewerId);

                                IdentityUser secondInterviewer = await _userManager.FindByIdAsync(interviewsDTO.SecondInterviewerId);

                                if (secondInterviewer != null)
                                {
                                    bool isInterviewerGMCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Interviewer", "General Manager");
                                    bool isGMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "General Manager", "Interviewer");
                                    
                                    bool isFirstInterviewerGM = firstinterviewer != null && gmUserIds.Contains(firstinterviewer.Id);
                                    bool isSecondInterviewerGM = secondInterviewer != null && gmUserIds.Contains(secondInterviewer.Id);

                                    if (isInterviewerGMCombo || isGMInterviewerCombo || isFirstInterviewerGM || isSecondInterviewerGM)
                                    {
                                        var currentInterviewForGMCheck = await _interviewsRepository.GetById(interviewsDTO.InterviewsId);
                                        bool isHRFirstFlowForGM = currentInterviewForGMCheck?.StartFromHR == true;

                                        string approvingInterviewerName = "General Manager";

                                        if (isHRFirstFlowForGM)
                                        {
                                            await _notificationsService.CreateInterviewNotificationForFinalHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                            EmailDTOs emailModelToHR = new()
                                            {
                                                EmailTo = [HREmail],
                                                Subject = $"Second Interview Approval ({candidateNameresult})",
                                                EmailBody = hrSecondInterviewApprovalEmailBody
                                            };

                                            if (!string.IsNullOrEmpty(HREmail))
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);
                                        }
                                        else
                                        {
                                            if(!isHRFirstFlowForGM)
                                            {
                                                await _notificationsService.CreateInterviewNotificationForFinalHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                                EmailDTOs emailModels = new()
                                                {
                                                    EmailTo = [HREmail],
                                                    Subject = $"Interview Invitation ({candidateNameresult})",
                                                    EmailBody = hrInvitationEmailBody
                                                };

                                                if (!string.IsNullOrEmpty(HREmail))
                                                    await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModels);
                                            }

                                            EmailDTOs emailModelToHR = new()
                                            {
                                                EmailTo = [HREmail],
                                                Subject = $"Interview Approval ({candidateNameresult})",
                                                EmailBody = hrApprovalEmailBody
                                            };

                                         
                                            if (!string.IsNullOrEmpty(HREmail))
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);
                                        }
                                        
                                        return RedirectToAction(nameof(MyInterviews));
                                    }
                                    else
                                    {
                                        InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(interviewsDTO.CandidateId);
                                        string aechituciterId = archiIdd.ArchitectureInterviewerId;

                                        if (!isFirstInterviewerGM && !isSecondInterviewerGM)
                                        {
                                            await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, aechituciterId);

                                            EmailDTOs emailModel = new EmailDTOs
                                            {
                                                EmailTo = [GMEmail],
                                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                EmailBody = gmInvitationEmailBody
                                            };

                                            if (aechituciterId != null)
                                            {
                                                await _notificationsService.CreateNotificationForArchiAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                                EmailDTOs architectureEmailModel = new()
                                                {
                                                    EmailTo = [ArchiEmail],
                                                    Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                    EmailBody = architectureEmailBody
                                                };
                                                if (!string.IsNullOrEmpty(ArchiEmail))
                                                    await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                            }
                                            if (!string.IsNullOrEmpty(GMEmail))
                                                await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModel);
                                        }

                                        EmailDTOs emailModelToHR = new EmailDTOs
                                        {
                                            EmailTo = [HREmail],
                                            Subject = $"Interview Approval ({candidateNameresult})",
                                            EmailBody = hrApprovalEmailBody
                                        };

                                        if (!string.IsNullOrEmpty(HREmail))
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);

                                    }
                                }
                                else
                                {
                                    InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(interviewsDTO.CandidateId);
                                    string aechituciterId = archiIdd.ArchitectureInterviewerId;

                                    bool isFirstInterviewerGM = firstinterviewer != null && gmUserIds.Contains(firstinterviewer.Id);

                                    if (!isFirstInterviewerGM)
                                    {
                                        await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, aechituciterId);

                                        EmailDTOs emailModel = new()
                                        {
                                            EmailTo = [GMEmail],
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = gmInvitationEmailBody
                                        };

                                        if (!string.IsNullOrEmpty(GMEmail))
                                            await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModel);
                                    }

                                    EmailDTOs emailModelToHR = new()
                                    {
                                        EmailTo = [HREmail],
                                        Subject = $"Interview Approval ( {candidateNameresult} )",
                                        EmailBody = hrApprovalEmailBody
                                    };

                                    if ((aechituciterId != null) && status.Code == Domain.Enums.StatusCode.Approved)
                                    {
                                        await _notificationsService.CreateNotificationForArchiAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                        EmailDTOs architectureEmailModel = new EmailDTOs
                                        {
                                            EmailTo = [ArchiEmail],
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = architectureEmailBody
                                        };
                                        if (!string.IsNullOrEmpty(ArchiEmail))
                                            await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                    }

                                    if (!string.IsNullOrEmpty(HREmail))
                                        await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);

                                    return RedirectToAction(nameof(MyInterviews));
                                }
                            }

                            else if (status.Code == Domain.Enums.StatusCode.Rejected)
                            {
                                IdentityUser firstinterviewerRejected = await _userManager.FindByIdAsync(interviewsDTO.InterviewerId);
                                string secondInterviewerIdRejected = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                                bool isFirstInterviewerGM = firstinterviewerRejected != null && gmUserIds.Contains(firstinterviewerRejected.Id);
                                bool isSecondInterviewerGM = !string.IsNullOrEmpty(secondInterviewerIdRejected) && gmUserIds.Contains(secondInterviewerIdRejected);

                                if (!isFirstInterviewerGM && !isSecondInterviewerGM)
                                {
                                    await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, interviewsDTO.ArchitectureInterviewerId);
                                }
                                
                                EmailDTOs emailModel = new()
                                {
                                    EmailTo = [HREmail],
                                    Subject = $"Interview Rejection ({candidateNameresult})",
                                    EmailBody = hrRejectionEmailBody
                                };

                                if (!string.IsNullOrEmpty(HREmail))
                                    await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);

                                return RedirectToAction(nameof(MyInterviews));
                            }

                            else
                                return RedirectToAction(nameof(MyInterviews));
                        }
                        else
                            return RedirectToAction(nameof(MyInterviews));
                    }
                    else if (User.IsInRole("General Manager"))
                    {
                        Result<StatusDTO> statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                        StatusDTO status = statusResult.Value;

                        if ((status.Code == Domain.Enums.StatusCode.Rejected || status.Code == Domain.Enums.StatusCode.Approved))
                        {
                            await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                            if (status.Code == Domain.Enums.StatusCode.Approved)
                            {
                                var currentInterviewGM = await _interviewsRepository.GetById(interviewsDTO.InterviewsId);
                                bool isHRFirstFlowGM = currentInterviewGM?.StartFromHR == true;

                                EmailDTOs emailModelApproval = new()
                                {
                                    EmailTo = [HREmail],
                                    Subject = $"Interview Approval ({candidateNameresult})",
                                    EmailBody = hrApprovalEmailBody
                                };

                                if (!isHRFirstFlowGM)
                                {
                                    EmailDTOs emailModel = new()
                                    {
                                        EmailTo = [HREmail],
                                        Subject = $"Interview Invitation ( {candidateNameresult} )",
                                        EmailBody = hrInvitationEmailBody
                                    };

                                    if (!string.IsNullOrEmpty(HREmail))
                                        await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);
                                }

                                if (!string.IsNullOrEmpty(HREmail))
                                    await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);

                                return RedirectToAction(nameof(MyInterviews));
                            }

                            else if (status.Code == Domain.Enums.StatusCode.Rejected)
                            {
                                EmailDTOs emailModel = new()
                                {
                                    EmailTo = [HREmail],
                                    Subject = $"Interview Rejection ({candidateNameresult})",
                                    EmailBody = hrRejectionEmailBody
                                };

                                if (!string.IsNullOrEmpty(HREmail))
                                    await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);

                                return RedirectToAction(nameof(MyInterviews));
                            }

                            else
                                return RedirectToAction(nameof(MyInterviews));
                        }

                    }
                    else if (User.IsInRole("Solution Architecture"))
                    {
                        Result<StatusDTO> statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                        StatusDTO status = statusResult.Value;

                        if ((status.Code == Domain.Enums.StatusCode.Rejected || status.Code == Domain.Enums.StatusCode.Approved))
                        {
                            IdentityUser firstinterviewer = await _userManager.FindByIdAsync(interviewsDTO.InterviewerId);

                            string secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                            IdentityUser secondInterviewer = await _userManager.FindByIdAsync(interviewsDTO.SecondInterviewerId);
                            IdentityUser archiInterviewer = await _userManager.FindByIdAsync(interviewsDTO.ArchitectureInterviewerId);

                            if (status.Code == Domain.Enums.StatusCode.Approved)
                            {
                                if (secondInterviewer != null)
                                {
                                    bool isInterviewerGMCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Solution Architecture", "Interviewer");
                                    bool isGMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Interviewer", "Solution Architecture");

                                    if (isInterviewerGMCombo || isGMInterviewerCombo)
                                    {
                                        bool isFirstInterviewerGM = firstinterviewer != null && gmUserIds.Contains(firstinterviewer.Id);
                                        bool isSecondInterviewerGM = secondInterviewer != null && gmUserIds.Contains(secondInterviewer.Id);

                                        if (!isFirstInterviewerGM && !isSecondInterviewerGM)
                                        {
                                            await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, interviewsDTO.ArchitectureInterviewerId);

                                            EmailDTOs emailModels = new()
                                            {
                                                EmailTo = [GMEmail],
                                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                EmailBody = gmInvitationEmailBody
                                            };

                                            if (!string.IsNullOrEmpty(GMEmail))
                                                await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModels);

                                            if (archiInterviewer != null)
                                            {
                                                await _notificationsService.CreateNotificationForArchiAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                                EmailDTOs architectureEmailModel = new()
                                                {
                                                    EmailTo = [ArchiEmail],
                                                    Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                    EmailBody = architectureEmailBody
                                                };
                                                if (!string.IsNullOrEmpty(ArchiEmail))
                                                    await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                            }
                                        }

                                        EmailDTOs emailModelApproval = new()
                                        {
                                            EmailTo = [HREmail],
                                            Subject = $"Interview Approval ({candidateNameresult})",
                                            EmailBody = hrApprovalEmailBody
                                        };

                                        if (!string.IsNullOrEmpty(HREmail))
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);
                                    }

                                    bool isInterviewersolCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Solution Architecture", "General Manager");
                                    bool isGMMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "General Manager", "Solution Architecture");

                                    if (isInterviewersolCombo || isGMMInterviewerCombo)
                                    {
                                        if (!isHRFirstFlow)
                                        {
                                            await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                            EmailDTOs emailModel = new()
                                            {
                                                EmailTo = [HREmail],
                                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                EmailBody = hrInvitationEmailBody
                                            };

                                            if (!string.IsNullOrEmpty(HREmail))
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);
                                        }
                                       
                                        if (isHRFirstFlow)
                                        {
                                            await _notificationsService.CreateInterviewNotificationForFinalHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                            EmailDTOs emailModelApproval = new()
                                            {
                                                EmailTo = [HREmail],
                                                Subject = $"Second Interview Approval ({candidateNameresult})",
                                                EmailBody = hrSecondInterviewApprovalEmailBody
                                            };

                                            if (!string.IsNullOrEmpty(HREmail))
                                            {
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);
                                            }
                                        }
                                        else
                                        {
                                            EmailDTOs emailModelApproval = new()
                                            {
                                                EmailTo = [HREmail],
                                                Subject = $"Interview Approval ({candidateNameresult})",
                                                EmailBody = hrApprovalEmailBody
                                            };

                                            if (!string.IsNullOrEmpty(HREmail))
                                            {
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (secondInterviewer is null)
                                    {
                                        InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(interviewsDTO.CandidateId);
                                        string aechituciterId = archiIdd.ArchitectureInterviewerId;

                                        if (aechituciterId != null)
                                        {
                                            if (!isHRFirstFlow)
                                            {
                                                await _notificationsService.CreateInterviewNotificationForFinalHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                                EmailDTOs emailModels = new()
                                                {
                                                    EmailTo = [HREmail],
                                                    Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                    EmailBody = hrInvitationEmailBody
                                                };

                                                if (!string.IsNullOrEmpty(HREmail))
                                                    await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModels);
                                            }

                                            if(status.Code == Domain.Enums.StatusCode.Approved)
                                            {
                                                if (isHRFirstFlow)
                                                {
                                                    await _notificationsService.CreateInterviewNotificationForFinalHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                                    EmailDTOs emailModelApproval = new()
                                                    {
                                                        EmailTo = [HREmail],
                                                        Subject = $"Second Interview Approval ({candidateNameresult})",
                                                        EmailBody = hrSecondInterviewApprovalEmailBody
                                                    };

                                                    if (!string.IsNullOrEmpty(HREmail))
                                                        await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);
                                                }
                                                else
                                                {
                                                    EmailDTOs emailModelApproval = new()
                                                    {
                                                        EmailTo = [HREmail],
                                                        Subject = $"Interview Approval ({candidateNameresult})",
                                                        EmailBody = hrApprovalEmailBody
                                                    };

                                                    if (!string.IsNullOrEmpty(HREmail))
                                                        await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);
                                                }
                                            }

                                            if (status.Code == Domain.Enums.StatusCode.Rejected)
                                            {
                                                EmailDTOs emailModelRejection = new()
                                                {
                                                    EmailTo = [HREmail],
                                                    Subject = $"Interview Rejection ({candidateNameresult})",
                                                    EmailBody = hrRejectionEmailBody
                                                };

                                                if (!string.IsNullOrEmpty(HREmail))
                                                    await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelRejection);
                                            }
                                          
                                        }
                                        else
                                        {
                                            await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);
                                            
                                            bool isFirstInterviewerGM = firstinterviewer != null && gmUserIds.Contains(firstinterviewer.Id);
                                            
                                            if (!isFirstInterviewerGM)
                                            {
                                                await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, aechituciterId);
                                            }
                                        }

                                    }
                                    else if (!isHRFirstFlow)
                                    {
                                        await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                        EmailDTOs emailModel = new()
                                        {
                                            EmailTo = [HREmail],
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = hrInvitationEmailBody
                                        };
                                        if (!string.IsNullOrEmpty(HREmail))
                                        {
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);
                                        }
                                    }

                                }

                                return RedirectToAction(nameof(MyInterviews));
                            }

                            else if (status.Code == Domain.Enums.StatusCode.Rejected)
                            {
                                if (interviewsDTO.Notes != null)
                                {
                                    await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                    EmailDTOs emailModel = new EmailDTOs
                                    {
                                        EmailTo = [HREmail],
                                        Subject = $"Interview Rejection ({candidateNameresult})",
                                        EmailBody = hrRejectionEmailBody
                                    };

                                    if (!string.IsNullOrEmpty(HREmail))
                                        await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);

                                }
                                return RedirectToAction(nameof(MyInterviews));
                            }

                            else
                                return RedirectToAction(nameof(MyInterviews));
                        }
                    }
                    else
                        return RedirectToAction(nameof(MyInterviews));

                    return RedirectToAction(nameof(MyInterviews));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred: " + ex.Message);
                }
            }

            if (attachmentStream != null)
            {
                attachmentStream.Close();
                attachmentStream.Dispose();
            }
            return View(interviewsDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("isUserInRolesAsync")]
    public async Task<bool> IsUserInRolesAsync(string firstUserId, string secondUserId, string firstRole, string secondRole)
    {
        IdentityUser firstUser = await _userManager.FindByIdAsync(firstUserId);
        IdentityUser secondUser = await _userManager.FindByIdAsync(secondUserId);

        if (firstUser is null || secondUser is null)
            return false;

        bool isFirstUserInRoles = await _userManager.IsInRoleAsync(firstUser, firstRole);
        bool isSecondUserInRoles = await _userManager.IsInRoleAsync(secondUser, secondRole);
        return isFirstUserInRoles && isSecondUserInRoles;
    }

    [Route("{id}/assignArchitectureInterviewer")]
    public async Task<IActionResult> AssignArchitectureInterviewer(int id)
    {
        try
        {
            var interviewResult = await _interviewsService.GetInterviewDetails(id);
            if (!interviewResult.IsSuccess || interviewResult.Value == null)
            {
                return NotFound();
            }

            var architecturesResult = await _accountService.GetAllArchitectureInterviewers();
            if (!architecturesResult.IsSuccess)
            {
                ModelState.AddModelError("", architecturesResult.Error);
                return View(new List<SelectListItem>());
            }

            ViewBag.ArchitectureList = new SelectList(architecturesResult.Value, "Id", "UserName");

            ViewBag.AssignedArchitectureId = interviewResult.Value.SecondInterviewerId;
            ViewBag.InterviewId = id;

            return View();
        }
        catch (Exception ex)
        {
            return StatusCode(500);
        }
    }

    [HttpPost]
    [Route("{id}/assignArchitectureInterviewer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignArchitectureInterviewer(int interviewId, string architectureId, bool remove = false)
    {
        try
        {
            var architectureEmail = await _emailService.GetInterviewerEmail(architectureId);
            var architectureUser = await _userManager.FindByIdAsync(architectureId);

            if (interviewId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid interview ID.";
                return RedirectToAction(nameof(Index));
            }

            if (remove)
            {
                var removeResult = await _interviewsService.RemoveArchitectureInterviewer(interviewId);
                if (removeResult.IsSuccess)
                {
                    var interviewResult = await _interviewsService.GetInterviewDetails(interviewId);
                    if (interviewResult.IsSuccess && interviewResult.Value != null)
                    {
                        var interview = interviewResult.Value;
                        var candidateName = await _candidateService.GetCandidateByIdAsync(interview.CandidateId);

                        await _notificationsService.RemoveNotifyAssignArchiAsync(
                            interview.StatusId ?? 0,
                            "Interview assignment removed.",
                            interview.CandidateId,
                            interview.PositionId
                        );

                        string removalArchiEmailBody = ArchitectureInterviewAssignmentEmailTemplate.GetRemovalEmail(
                                                                                                                architectureUser.UserName,
                                                                                                                candidateName.FullName,
                                                                                                                "CMS"
                                                                                                              );
                        var emailModel = new EmailDTOs
                        {
                            EmailTo = [architectureEmail],
                            Subject = $"Interview Assignment Removed ({candidateName.FullName})",
                            EmailBody = removalArchiEmailBody
                        };


                        await _emailService.SendEmailToInterviewer(architectureEmail, interview, emailModel);
                    }

                    TempData["SuccessMessage"] = "Architecture Interviewer removed successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = removeResult.Error;
                    return RedirectToAction(nameof(AssignArchitectureInterviewer), new { id = interviewId });
                }
            }

            if (string.IsNullOrEmpty(architectureId))
            {
                TempData["ErrorMessage"] = "Please select an Architecture Interviewer.";
                return RedirectToAction(nameof(AssignArchitectureInterviewer), new { id = interviewId });
            }

            var result = await _interviewsService.AddOrUpdateArchitectureInterviewer(interviewId, architectureId);

            if (result.IsSuccess)
            {
                var interviewResult = await _interviewsService.GetInterviewDetails(interviewId);
                if (interviewResult.IsSuccess && interviewResult.Value != null)
                {
                    var interview = interviewResult.Value;

                    await _notificationsService.NotifyAssignArchiAsync(
                        interview.StatusId ?? 0,
                        "You have been assigned to a new interview.",
                        interview.CandidateId,
                        interview.PositionId
                    );

                    var candidateName = await _candidateService.GetCandidateByIdAsync(interview.CandidateId);
                    var formattedDate = interview.Date.ToString("dd/MM/yyyy hh:mm tt");

                    string assignmentArchiEmailBody = ArchitectureInterviewAssignmentEmailTemplate.GetAssignmentEmail(
                                                                                                                  architectureUser.UserName,
                                                                                                                  candidateName.FullName,
                                                                                                                  formattedDate,
                                                                                                                  $"https://apps.sssprocess.com:6134/interviews/{interviewId}/addingresult",
                                                                                                                  "CMS"
                                                                                                                );

                    var emailModel = new EmailDTOs
                    {
                        EmailTo = [architectureEmail],
                        Subject = $"New Architecture Interview Assigned ({candidateName.FullName})",
                        EmailBody = assignmentArchiEmailBody
                    };

                    await _emailService.SendEmailToInterviewer(architectureEmail, interview, emailModel);
                }

                TempData["SuccessMessage"] = "Architecture Interviewer updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = result.Error;
                return RedirectToAction(nameof(AssignArchitectureInterviewer), new { id = interviewId });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500);
        }
    }

    [HttpPost]
    public async Task<JsonResult> CheckExistingInterview(int candidateId)
    {
        bool interviewExists = await _interviewsService.DoesInterviewExistForCandidate(candidateId);
        return Json(new { exists = interviewExists });
    }
}