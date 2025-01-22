using CMS.Application.DTOs;
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
                                ITrackService trackService)
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
        _attachmentStoragePath = Path.Combine(env.WebRootPath, "attachments");

        if (!Directory.Exists(_attachmentStoragePath))
            Directory.CreateDirectory(_attachmentStoragePath);
    }


    [Route("myInterviews")]
    public async Task<ActionResult> MyInterviews(int? statusFilter, int? companyFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Interviewer") || User.IsInRole("General Manager") || User.IsInRole("HR Manager") || User.IsInRole("Solution Architecture"))
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
                ViewBag.CompanyList = new SelectList(companies, "Id", "Name");

                List<StatusDTO> statuses = statusesResult.Value;
                ViewBag.StatusList = new SelectList(statuses, "Id", "Name");

                if (!statusFilter.HasValue)
                    statusFilter = await _StatusService.GetStatusIdByName("Pending");

                Result<List<TrackDTO>> tracksResult = await _trackService.GetAll();
                if (!tracksResult.IsSuccess)
                {
                    ModelState.AddModelError("", tracksResult.Error);
                    return View(new PaginatedList<InterviewsDTO>(new List<InterviewsDTO>(), 0, pageNumber, pageSize));
                }

                List<TrackDTO> tracks = tracksResult.Value;
                ViewBag.TrackList = new SelectList(tracks, "Id", "Name");

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
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("")]
    public async Task<ActionResult> Index(int? statusFilter, string candidateFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            ViewBag.statusFilter = statusFilter;
            ViewBag.candidateFilter = candidateFilter;

            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
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

                ViewBag.TrackList = new SelectList(tracksResult.Value, "Id", "Name");

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
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
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

    [Route("{id}/stopCycle")]
    public async Task<IActionResult> StopCycle(int id)
    {
        try
        {
            if (_signInManager.IsSignedIn(User))
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
            if (_signInManager.IsSignedIn(User))
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
    public async Task<ActionResult> Details(int id, string previousAction)
    {
        try
        {
            ViewBag.PreviousAction = previousAction;
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

    [Route("create")]
    public async Task<ActionResult> Create()
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
                await LoadSelectionLists();
                return View();
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
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
            ViewBag.positionList = new SelectList(positions.Value, "Id", "Name");

            IEnumerable<CandidateDTO> candidates = await _candidateService.GetAllCandidatesAsync();
            IOrderedEnumerable<CandidateDTO> sortedCandidates = candidates.OrderByDescending(x => x.Id);
            ViewBag.candidateList = new SelectList(sortedCandidates, "Id", "FullName");

            Result<IList<IdentityUser>> interviewers = await _accountService.GetAllInterviewers();
            ViewBag.interviewersList = new SelectList(interviewers.Value, "Id", "UserName");

            Result<IList<IdentityUser>> architectures = await _accountService.GetAllArchitectureInterviewers();
            ViewBag.architecturesList = new SelectList(architectures.Value, "Id", "UserName");

            Result<List<StatusDTO>> statuses = await _StatusService.GetAll();
            ViewBag.statusList = new SelectList(statuses.Value, "Id", "Name");

            Result<List<TrackDTO>> tracks = await _trackService.GetAll();
            ViewBag.Tracks = new SelectList(tracks.Value, "Id", "Name");
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

            if (ModelState.IsValid)
            {
                Result<InterviewsDTO> result = await _interviewsService.Insert(collection);

                if (result.IsSuccess)
                {
                    if (User.IsInRole("HR Manager") || User.IsInRole("Admin"))
                    {
                        string selectedInterviewerId = collection.InterviewerId;
                        InterviewsDTO insertedInterview = result.Value;
                        collection.InterviewsId = insertedInterview.InterviewsId;

                        HttpContext.Session.SetString($"SecondInterviewerId_{collection.InterviewsId}", collection.SecondInterviewerId ?? "");
                        HttpContext.Session.SetString($"InterviewerId_{collection.InterviewsId}", collection.InterviewerId ?? "");

                        CandidateDTO candidateName = await _candidateService.GetCandidateByIdAsync(collection.CandidateId);
                        string candidateNameresult = candidateName.FullName;

                        Result<PositionDTO> positionName = await _positionService.GetById(collection.PositionId);
                        PositionDTO positionNameresult = positionName.Value;
                        string lastPositionName = positionNameresult.Name;
                        string userName = _emailService.GetLoggedInUserName();
                        string userSecondInterviewer = null;

                        string secondInterviewerEmail = await _emailService.GetInterviewerEmail(collection.SecondInterviewerId);

                        if (secondInterviewerEmail != null)
                        {
                            IdentityUser userSecondInterviewerObj = await _userManager.FindByEmailAsync(secondInterviewerEmail);
                            userSecondInterviewer = userSecondInterviewerObj.UserName;
                        }

                        string formattedDate = collection.Date.ToString("dd/MM/yyyy hh:mm tt");

                        string interviewerEmail = await _emailService.GetInterviewerEmail(collection.InterviewerId);
                        IdentityUser userInterviewer = await _userManager.FindByEmailAsync(interviewerEmail);

                        EmailDTOs emailModel = new EmailDTOs
                        {
                            EmailTo = new List<string> { interviewerEmail },
                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                            EmailBody = $@"<html>
                            <body style='font-family: Arial, sans-serif;'>
                                <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                    <p style='font-size: 18px; color: #333;'>
                                        Dear {userInterviewer.UserName.Replace("_", " ")},
                                    </p>
                                    <p style='font-size: 16px; color: #555;'>
                                {(collection.SecondInterviewerId != null ? $"You and {userSecondInterviewer} are" : "You are")} assigned to have a first interview for {candidateNameresult} scheduled on {formattedDate} for the {lastPositionName} position,<br><br>kindly <a href='https://apps.sssprocess.com:6134/Interviews/UpdateAfterInterview/{collection.InterviewsId}'>Click here</a> to see the invitation details.
                                    </p>
                                    <p style='font-size: 14px; color: #777;'>
                                        Regards,
                                    </p>

                                    <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                </div>
                            </body>
                        </html>"
                        };

                        await _notificationsService.CreateInterviewNotificationForInterviewerAsync(collection.Date, collection.CandidateId, collection.PositionId, new List<string> { collection.InterviewerId, collection.SecondInterviewerId }, isCanceled: false);

                        if (collection.SecondInterviewerId != null)
                        {
                            EmailDTOs emailModel2 = new EmailDTOs
                            {
                                EmailTo = new List<string> { secondInterviewerEmail },
                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                EmailBody = $@"<html>
                                <body style='font-family: Arial, sans-serif;'>
                                    <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                        <p style='font-size: 18px; color: #333;'>
                                            Dear {userSecondInterviewer.Replace("_", " ")},
                                        </p>
                                        <p style='font-size: 16px; color: #555;'>
                                            You and {userInterviewer} are assigned to have a first interview with {candidateNameresult} for the {lastPositionName} position scheduled on {collection.Date},<br><br>kindly <a href='https://apps.sssprocess.com:6134/Interviews/UpdateAfterInterview/{collection.InterviewsId}'>Click here</a> to see the invitation details.
                                        </p>
                                        <p style='font-size: 14px; color: #777;'>
                                            Regards,
                                        </p>

                                        <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                    </div>
                                </body>
                            </html>"
                            };

                            // Send emails to both first and second interviewers
                            await _emailService.SendEmailToInterviewer(secondInterviewerEmail, collection, emailModel2);
                            await _emailService.SendEmailToInterviewer(interviewerEmail, collection, emailModel);
                        }
                        else
                            await _emailService.SendEmailToInterviewer(interviewerEmail, collection, emailModel);

                        return RedirectToAction(nameof(Index));
                    }
                    else
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
    public async Task<ActionResult> Edit(int id)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
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
            else
            {
                if (User.Identity.IsAuthenticated)
                {
                    return View("AccessDenied");
                }
                else
                {
                    return RedirectToAction("login", "users");
                }
            }
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

            if (ModelState.IsValid)
            {
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

                HttpContext.Session.SetString($"SecondInterviewerId_{collection.InterviewsId}", collection.SecondInterviewerId ?? "");
                HttpContext.Session.SetString($"InterviewerId_{collection.InterviewsId}", collection.InterviewerId ?? "");

                if (result.IsSuccess)
                {
                    if (User.IsInRole("HR Manager") || User.IsInRole("Admin"))
                    {
                        string selectedInterviewerId = collection.InterviewerId;
                        InterviewsDTO insertedInterview = result.Value;
                        collection.InterviewsId = insertedInterview.InterviewsId;

                        HttpContext.Session.SetString($"SecondInterviewerId_{collection.InterviewsId}", collection.SecondInterviewerId ?? "");
                        HttpContext.Session.SetString($"InterviewerId_{collection.InterviewsId}", collection.InterviewerId ?? "");

                        CandidateDTO candidateName = await _candidateService.GetCandidateByIdAsync(collection.CandidateId);
                        string candidateNameresult = candidateName.FullName;

                        Result<PositionDTO> positionName = await _positionService.GetById(collection.PositionId);
                        PositionDTO positionNameresult = positionName.Value;
                        string lastPositionName = positionNameresult.Name;
                        string userName = _emailService.GetLoggedInUserName();
                        string userSecondInterviewer = null;
                        string secondInterviewerEmail = await _emailService.GetInterviewerEmail(collection.SecondInterviewerId);

                        if (secondInterviewerEmail != null)
                        {
                            IdentityUser userSecondInterviewerObj = await _userManager.FindByEmailAsync(secondInterviewerEmail);
                            userSecondInterviewer = userSecondInterviewerObj.UserName;
                        }

                        string formattedDate = collection.Date.ToString("dd/MM/yyyy hh:mm tt");

                        // Prepare the email model for the first interviewer
                        string interviewerEmail = await _emailService.GetInterviewerEmail(collection.InterviewerId);
                        IdentityUser userInterviewer = await _userManager.FindByEmailAsync(interviewerEmail);

                        EmailDTOs emailModel = new EmailDTOs
                        {
                            EmailTo = new List<string> { interviewerEmail },
                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                            EmailBody = $@"<html>
                            <body style='font-family: Arial, sans-serif;'>
                                <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                    <p style='font-size: 18px; color: #333;'>
                                        Dear {userInterviewer.UserName.Replace("_", " ")},
                                    </p>
                                    <p style='font-size: 16px; color: #555;'>
                                {(collection.SecondInterviewerId != null ? $"You and {userSecondInterviewer} are" : "You are")} assigned to have a first interview for {candidateNameresult} scheduled on {formattedDate} for the {lastPositionName} position,<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                    </p>
                                    <p style='font-size: 14px; color: #777;'>
                                        Regards,
                                    </p>

                                        <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                </div>
                            </body>
                        </html>"
                        };


                        await _notificationsService.CreateInterviewNotificationForInterviewerAsync(collection.Date, collection.CandidateId, collection.PositionId, new List<string> { collection.InterviewerId, collection.SecondInterviewerId }, isCanceled: false);

                        // Prepare the email model for the second interviewer if selected
                        if (collection.SecondInterviewerId != null)
                        {
                            EmailDTOs emailModel2 = new EmailDTOs
                            {
                                EmailTo = new List<string> { secondInterviewerEmail },
                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                EmailBody = $@"<html>
                                <body style='font-family: Arial, sans-serif;'>
                                    <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                        <p style='font-size: 18px; color: #333;'>
                                            Dear {userSecondInterviewer.Replace("_", " ")},
                                        </p>
                                        <p style='font-size: 16px; color: #555;'>
                                            You and {userInterviewer} are assigned to have a first interview with {candidateNameresult} for the {lastPositionName} position scheduled on {collection.Date} ,<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                        </p>
                                        <p style='font-size: 14px; color: #777;'>
                                            Regards,
                                        </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                    </div>
                                </body>
                            </html>"
                            };

                            // Send emails to both first and second interviewers
                            await _emailService.SendEmailToInterviewer(secondInterviewerEmail, collection, emailModel2);
                            await _emailService.SendEmailToInterviewer(interviewerEmail, collection, emailModel);
                        }
                        else
                            await _emailService.SendEmailToInterviewer(interviewerEmail, collection, emailModel);

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }

                ModelState.AddModelError("", result.Error);
                return View(collection);
            }
            else
                ModelState.AddModelError("", $"");

            return View(collection);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/delete")]
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
    public async Task<IActionResult> UpdateAfterInterviewForEdit(int id)
    {
        try
        {
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
    public async Task<IActionResult> UpdateAfterInterview(int id)
    {
        try
        {
            if (_signInManager.IsSignedIn(User))
            {

                Result<List<StatusDTO>> StatusDTOs = await _StatusService.GetAll();
                ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

                Result<InterviewsDTO> result = await _interviewsService.GetInterviewDetails(id);
                InterviewsDTO InterviewsDTO = result.Value;

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

            //Get Candidate Name By Id
            CandidateDTO candidateName = await _candidateService.GetCandidateByIdAsync(interviewsDTO.CandidateId);
            string candidateNameresult = candidateName.FullName;

            //Get Position Name By Id
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
                try
                {
                    Result<StatusDTO> newStatusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                    if (newStatusResult.IsSuccess)
                    {
                        StatusDTO newStatus = newStatusResult.Value;

                        // Check if the new status is On Hold
                        if ((newStatus.Code == Domain.Enums.StatusCode.OnHold || newStatus.Code == Domain.Enums.StatusCode.Rejected) && !User.IsInRole("HR Manager"))
                        {
                            await _notificationsService.CreateInterviewNotificationtoHrForOnHold(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                            // Get the current interview status
                            Domain.Entities.Interviews currentInterview = await _interviewsRepository.GetById(interviewsDTO.InterviewsId); // Assuming you have a method to get the interview by its ID

                            // Check if the current interview status is not pending
                            if (currentInterview.Status.Code != Domain.Enums.StatusCode.Pending && currentInterview.Status.Code != Domain.Enums.StatusCode.Rejected)
                            {
                                // Continue with the logic only if the current interview status is not pending
                                int interviewCount = await _interviewsRepository.GetInterviewCountForCandidate(interviewsDTO.CandidateId);

                                if (newStatus.Code == Domain.Enums.StatusCode.Rejected && !User.IsInRole("HR Manager"))
                                {
                                    // Check if the next interview is pending
                                    string nextInterviewStatusCode = await _interviewsRepository.GetStatusOfNextInterview(interviewsDTO.CandidateId, interviewsDTO.InterviewsId);

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
                                        bool interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));
                                    }
                                }
                                else
                                {

                                    if ((interviewCount >= 1 && interviewCount <= 2) || ((interviewCount == 3 || interviewCount == 4) && User.IsInRole("General Manager")))
                                    {
                                        bool interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));
                                        if (!interviewsDeleted)
                                        {
                                            // Show a pop-up or handle the case where there are no pending interviews to delete
                                            ModelState.AddModelError("StatusId", "Cannot set the interview status to On Hold because it has already been marked as done after the interview.");
                                            if (attachmentStream != null)
                                            {
                                                attachmentStream.Close();
                                                attachmentStream.Dispose();
                                            }
                                            return View(interviewsDTO);
                                        }
                                    }
                                    else if (!User.IsInRole("HR Manager"))
                                    {
                                        // Show a pop-up or handle the case where there's only one interview
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
                                bool interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));
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
                    }

                    IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                    if (await _userManager.IsInRoleAsync(currentUser, "General Manager"))
                        await _interviewsService.ConductInterviewForGm(interviewsDTO);

                    else if (await _userManager.IsInRoleAsync(currentUser, "Interviewer"))
                    {
                        string secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                        string interviewerId = HttpContext.Session.GetString($"InterviewerId_{interviewsDTO.InterviewsId}");
                        await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);
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

                    if (attachmentStream != null)
                    {
                        // Close the file stream and release the file
                        attachmentStream.Close();
                        attachmentStream.Dispose();
                        AttachmentHelper.removeFile(file.FileName, _attachmentStoragePath);
                    }

                    if (User.IsInRole("Interviewer"))
                    {

                        Result<StatusDTO> statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                        StatusDTO status = statusResult.Value;

                        if (status.Code == Domain.Enums.StatusCode.Rejected || status.Code == Domain.Enums.StatusCode.Approved)
                        {
                            if (status.Code == Domain.Enums.StatusCode.Approved)
                            {
                                string userName = _emailService.GetLoggedInUserName();
                                string GMEmail = await _emailService.GetGMEmail();
                                string HREmail = await _emailService.GetHREmail();
                                string ArchiEmail = await _emailService.GetArchiEmail();

                                IdentityUser userGM = await _userManager.FindByEmailAsync(GMEmail);
                                IdentityUser userHR = await _userManager.FindByEmailAsync(HREmail);

                                IdentityUser firstinterviewer = await _userManager.FindByIdAsync(interviewsDTO.InterviewerId);

                                string secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                                IdentityUser secondInterviewer = await _userManager.FindByIdAsync(secondInterviewerId);

                                if (secondInterviewer != null)
                                {
                                    bool isInterviewerGMCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Interviewer", "General Manager");
                                    bool isGMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "General Manager", "Interviewer");

                                    if (isInterviewerGMCombo || isGMInterviewerCombo)
                                    {
                                        await _notificationsService.CreateInterviewNotificationForFinalHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);
                                        EmailDTOs emailModels = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { HREmail },
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                               You are assigned to have a Final interview for {candidateNameresult} with {lastPositionName} position,<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
                                        };

                                        EmailDTOs emailModelToHR = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { HREmail },
                                            Subject = $"Interview Approval ({candidateNameresult})",
                                            EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The first interview with {candidateNameresult} Approved by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
                                        };

                                        if (!string.IsNullOrEmpty(HREmail))
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModels);

                                        if (!string.IsNullOrEmpty(HREmail))
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);
                                    }
                                    else
                                    {
                                        InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(interviewsDTO.CandidateId);
                                        string aechituciterId = archiIdd.ArchitectureInterviewerId;

                                        await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, aechituciterId);

                                        //from interviewer to GM
                                        EmailDTOs emailModel = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { GMEmail },
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear {userGM.UserName.Replace("_", " ")},
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                               You are assigned to have a second interview for {candidateNameresult} with {lastPositionName} position,<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
                                        };


                                        EmailDTOs emailModelToHR = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { HREmail },
                                            Subject = $"Interview Approval ({candidateNameresult})",
                                            EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The first interview with {candidateNameresult} Approved by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
                                        };



                                        if (aechituciterId != null)
                                        {
                                            IdentityUser userArchi = await _userManager.FindByEmailAsync(ArchiEmail);
                                            await _notificationsService.CreateNotificationForArchiAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                            EmailDTOs architectureEmailModel = new EmailDTOs
                                            {
                                                EmailTo = new List<string> { ArchiEmail },
                                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                EmailBody = $@"<html>
                                           <body style='font-family: Arial, sans-serif;'>
                                               <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                   <p style='font-size: 18px; color: #333;'>
                                                       Dear {userArchi.UserName.Replace("_", " ")},
                                                   </p>
                                                   <p style='font-size: 16px; color: #555;'>
                                                       An interview has been scheduled with Saeed, and you are assigned as the Architecture Interviewer for the {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                                   </p>
                                                   <p style='font-size: 14px; color: #777;'>
                                                       Regards,
                                                   </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                               </div>
                                           </body>
                                        </html>"

                                            };
                                            if (!string.IsNullOrEmpty(ArchiEmail))
                                                //Send an Email to the Archi if it was selceted
                                                await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                        }
                                        if (!string.IsNullOrEmpty(GMEmail))
                                            await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModel);

                                        if (!string.IsNullOrEmpty(HREmail))
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);

                                    }
                                }
                                else
                                {
                                    InterviewsDTO archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(interviewsDTO.CandidateId);
                                    string aechituciterId = archiIdd.ArchitectureInterviewerId;

                                    await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, aechituciterId);

                                    //from interviewer to GM
                                    EmailDTOs emailModel = new EmailDTOs
                                    {
                                        EmailTo = new List<string> { GMEmail },
                                        Subject = $"Interview Invitation ( {candidateNameresult} )",
                                        EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear {userGM.UserName.Replace("_", " ")},
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                               You are assigned to have a second interview for {candidateNameresult} with {lastPositionName} position,<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
                                    };


                                    EmailDTOs emailModelToHR = new EmailDTOs
                                    {
                                        EmailTo = new List<string> { HREmail },
                                        Subject = $"Interview Approval ( {candidateNameresult} )",
                                        EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The first interview with {candidateNameresult} Approved by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
                                    };


                                    if ((aechituciterId != null) && status.Code == Domain.Enums.StatusCode.Approved)
                                    {
                                        IdentityUser userArchi = await _userManager.FindByEmailAsync(ArchiEmail);
                                        await _notificationsService.CreateNotificationForArchiAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                        EmailDTOs architectureEmailModel = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { ArchiEmail },
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = $@"<html>
                                           <body style='font-family: Arial, sans-serif;'>
                                               <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                   <p style='font-size: 18px; color: #333;'>
                                                       Dear {userArchi.UserName.Replace("_", " ")},
                                                   </p>
                                                   <p style='font-size: 16px; color: #555;'>
                                                       An interview has been scheduled with Saeed, and you are assigned as the Architecture Interviewer for the {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                                   </p>
                                                   <p style='font-size: 14px; color: #777;'>
                                                       Regards,
                                                   </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                               </div>
                                           </body>
                                        </html>"

                                        };
                                        if (!string.IsNullOrEmpty(ArchiEmail))
                                            //Send an Email to the Archi if it was selceted
                                            await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                    }


                                    if (!string.IsNullOrEmpty(GMEmail))
                                        await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModel);

                                    if (!string.IsNullOrEmpty(HREmail))
                                        await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);

                                    return RedirectToAction(nameof(MyInterviews));
                                }
                            }

                            else if (status.Code == Domain.Enums.StatusCode.Rejected)
                            {
                                await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, interviewsDTO.ArchitectureInterviewerId);
                                string userName = _emailService.GetLoggedInUserName();
                                string HREmail = await _emailService.GetHREmail();

                                EmailDTOs emailModel = new EmailDTOs
                                {
                                    EmailTo = new List<string> { HREmail },
                                    Subject = $"Interview Rejection ({candidateNameresult})",
                                    EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The first interview with {candidateNameresult} Rejected by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"

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
                                string userName = _emailService.GetLoggedInUserName();
                                string HREmail = await _emailService.GetHREmail();
                                IdentityUser userHR = await _userManager.FindByEmailAsync(HREmail);
                                EmailDTOs emailModel = new EmailDTOs
                                {
                                    EmailTo = new List<string> { HREmail },
                                    Subject = $"Interview Invitation ( {candidateNameresult} )",
                                    EmailBody = $@"<html>
                                           <body style='font-family: Arial, sans-serif;'>
                                               <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                   <p style='font-size: 18px; color: #333;'>
                                                       Dear Sajeda,
                                                   </p>
                                                   <p style='font-size: 16px; color: #555;'>
                                                       You are assigned to have a third interview for candidate: {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                                   </p>
                                                   <p style='font-size: 14px; color: #777;'>
                                                       Regards,
                                                   </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                               </div>
                                           </body>
                                        </html>"
                                };

                                EmailDTOs emailModelApproval = new EmailDTOs
                                {
                                    EmailTo = new List<string> { HREmail },
                                    Subject = $"Interview Approval ({candidateNameresult})",
                                    EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The Second interview with {candidateNameresult} Approved by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"

                                };

                                if (!string.IsNullOrEmpty(HREmail))
                                {
                                    await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);
                                    await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);
                                }

                                return RedirectToAction(nameof(MyInterviews));
                            }

                            else if (status.Code == Domain.Enums.StatusCode.Rejected)
                            {
                                string userName = _emailService.GetLoggedInUserName();
                                string HREmail = await _emailService.GetHREmail();

                                EmailDTOs emailModel = new EmailDTOs
                                {
                                    EmailTo = new List<string> { HREmail },
                                    Subject = $"Interview Rejection ({candidateNameresult})",
                                    EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The Second interview with {candidateNameresult} Rejected by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
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
                            IdentityUser secondInterviewer = await _userManager.FindByIdAsync(secondInterviewerId);

                            if (status.Code == Domain.Enums.StatusCode.Approved)
                            {
                                string userName = _emailService.GetLoggedInUserName();
                                string HREmail = await _emailService.GetHREmail();
                                string GMEmail = await _emailService.GetGMEmail();
                                IdentityUser userHR = await _userManager.FindByEmailAsync(HREmail);
                                IdentityUser userGM = await _userManager.FindByEmailAsync(GMEmail);

                                if (secondInterviewer != null)
                                {

                                    bool isInterviewerGMCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Solution Architecture", "Interviewer");
                                    bool isGMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Interviewer", "Solution Architecture");

                                    if (isInterviewerGMCombo || isGMInterviewerCombo)
                                    {

                                        await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, interviewsDTO.ArchitectureInterviewerId);

                                        EmailDTOs emailModels = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { GMEmail },
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = $@"<html>
                                           <body style='font-family: Arial, sans-serif;'>
                                               <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                   <p style='font-size: 18px; color: #333;'>
                                                       Dear {userGM.UserName.Replace("_", " ")},
                                                   </p>
                                                   <p style='font-size: 16px; color: #555;'>
                                                       You are assigned to have a Second interview for candidate : {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                                   </p>
                                                   <p style='font-size: 14px; color: #777;'>
                                                       Regards,
                                                   </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                               </div>
                                           </body>
                                        </html>"
                                        };
                                        EmailDTOs emailModelApproval = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { HREmail },
                                            Subject = $"Interview Approval ({candidateNameresult})",
                                            EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The First interview with {candidateNameresult} Approved by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
                                        };
                                        if (!string.IsNullOrEmpty(GMEmail))
                                            await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModels);

                                        if (!string.IsNullOrEmpty(HREmail))
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);
                                    }


                                    bool isInterviewersolCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Solution Architecture", "General Manager");
                                    bool isGMMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "General Manager", "Solution Architecture");

                                    if (isInterviewersolCombo || isGMMInterviewerCombo)
                                    {
                                        await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                        //from Archi to HR
                                        EmailDTOs emailModel = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { HREmail },
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = $@"<html>
                                           <body style='font-family: Arial, sans-serif;'>
                                               <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                   <p style='font-size: 18px; color: #333;'>
                                                       Dear Sajeda,
                                                   </p>
                                                   <p style='font-size: 16px; color: #555;'>
                                                       You are assigned to have a Final interview for candidate : {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                                   </p>
                                                   <p style='font-size: 14px; color: #777;'>
                                                       Regards,
                                                   </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                               </div>
                                           </body>
                                        </html>"
                                        };

                                        EmailDTOs emailModelApproval = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { HREmail },
                                            Subject = $"Interview Approval ({candidateNameresult})",
                                            EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The First interview with {candidateNameresult} Approved by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
                                        };

                                        if (!string.IsNullOrEmpty(HREmail))
                                        {
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);
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
                                            await _notificationsService.CreateInterviewNotificationForFinalHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                            EmailDTOs emailModels = new EmailDTOs
                                            {
                                                EmailTo = new List<string> { HREmail },
                                                Subject = $"Interview Invitation ( {candidateNameresult} )",
                                                EmailBody = $@"<html>
                                                       <body style='font-family: Arial, sans-serif;'>
                                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                               <p style='font-size: 18px; color: #333;'>
                                                                   Dear Sajeda,
                                                               </p>
                                                               <p style='font-size: 16px; color: #555;'>
                                                                   You are assigned to have a third interview for candidate : {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                                               </p>
                                                               <p style='font-size: 14px; color: #777;'>
                                                                   Regards,
                                                               </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                                           </div>
                                                       </body>
                                                    </html>"
                                            };
                                            if (!string.IsNullOrEmpty(HREmail))
                                            {
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModels);
                                            }
                                        }
                                        else
                                        {
                                            await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);
                                            await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, aechituciterId);
                                        }

                                    }
                                    else
                                    {


                                        await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);
                                        //from Archi to HR
                                        EmailDTOs emailModel = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { HREmail },
                                            Subject = $"Interview Invitation ( {candidateNameresult} )",
                                            EmailBody = $@"<html>
                                           <body style='font-family: Arial, sans-serif;'>
                                               <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                   <p style='font-size: 18px; color: #333;'>
                                                       Dear Sajeda,
                                                   </p>
                                                   <p style='font-size: 16px; color: #555;'>
                                                       You are assigned to have a Final interview for candidate : {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                                   </p>
                                                   <p style='font-size: 14px; color: #777;'>
                                                       Regards,
                                                   </p>

                                       <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                               </div>
                                           </body>
                                        </html>"
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

                                    string userName = _emailService.GetLoggedInUserName();
                                    string HREmail = await _emailService.GetHREmail();
                                    EmailDTOs emailModel = new EmailDTOs
                                    {
                                        EmailTo = new List<string> { HREmail },
                                        Subject = $"Interview Rejection ({candidateNameresult})",
                                        EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear Sajeda,
                                               </p>
                                               <p style='font-size: 16px; color: #555;'>
                                                    The Second interview with {candidateNameresult} rejected by {userName}
                                               </p>
                                               <p style='font-size: 14px; color: #777;'>
                                                   Regards,
                                               </p>

                                      <p style='font-size: 14px; color: #777;'>Sent by: CMS</p>
                                           </div>
                                       </body>
                                    </html>"
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

            // Set the currently assigned Architecture Interviewer
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
                // Remove the assigned Architecture Interviewer
                var removeResult = await _interviewsService.RemoveArchitectureInterviewer(interviewId);
                if (removeResult.IsSuccess)
                {
                    // Send removal notification and email
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

                        var emailModel = new EmailDTOs
                        {
                            EmailTo = new List<string> { architectureEmail },
                            Subject = $"Interview Assignment Removed ({candidateName.FullName})",
                            EmailBody = $@"<html>
                                <body style='font-family: Arial, sans-serif;'>
                                    <p style='font-size: 16px;'>Dear {architectureUser.UserName.Replace("_", " ")},</p>
                                    <p style='font-size: 16px;'>Your assignment for the Architecture Interview with candidate <strong>{candidateName.FullName}</strong> has been removed.</p>
                                    <p style='font-size: 14px;'>If you have any questions, please contact the HR department.</p>
                                    <p>Thank you.</p>
                                </body>
                            </html>"
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
                // Send addition notification and email
                var interviewResult = await _interviewsService.GetInterviewDetails(interviewId);
                if (interviewResult.IsSuccess && interviewResult.Value != null)
                {
                    var interview = interviewResult.Value;

                    // Send notification
                    await _notificationsService.NotifyAssignArchiAsync(
                        interview.StatusId ?? 0,
                        "You have been assigned to a new interview.",
                        interview.CandidateId,
                        interview.PositionId
                    );

                    // Send email
                    var candidateName = await _candidateService.GetCandidateByIdAsync(interview.CandidateId);
                    var formattedDate = interview.Date.ToString("dd/MM/yyyy hh:mm tt");

                    var emailModel = new EmailDTOs
                    {
                        EmailTo = new List<string> { architectureEmail },
                        Subject = $"New Architecture Interview Assigned ({candidateName.FullName})",
                        EmailBody = $@"<html>
                        <body style='font-family: Arial, sans-serif;'>
                            <p style='font-size: 16px;'>Dear {architectureUser.UserName.Replace("_", " ")},</p>
                            <p style='font-size: 16px;'>You have been assigned with the GM to interview {candidateName.FullName}, scheduled on {formattedDate}.</p>
                            <p><a href='https://apps.sssprocess.com:6134/Interviews/Details/{interviewId}'>Click here</a> for more details.</p>
                            <p>Thank you.</p>
                        </body>
                    </html>"
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

