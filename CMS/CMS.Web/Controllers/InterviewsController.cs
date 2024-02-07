using Microsoft.AspNetCore;
using CMS.Application.DTOs;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Services.Interfaces;
using CMS.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using CMS.Web.Utils;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using CMS.Domain;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Habanero.Util;
using CMS.Repository.Interfaces;
using Hangfire;
using System.Security.Claims;
using CMS.Domain.Migrations;
using CMS.Repository.Implementation;

namespace CMS.Web.Controllers
{

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
        private readonly ICompanyService _companyService;
        private readonly ITrackService _trackService;

        public InterviewsController(IInterviewsService interviewsService, ICandidateService candidateService,
            IPositionService positionService, IStatusService statusService, IWebHostEnvironment env,
            IAccountService accountService, INotificationsService notificationsService,
            IInterviewsRepository interviewsRepository, IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityUser> userManager, IEmailService emailService, IAttachmentService attachmentService,
            SignInManager<IdentityUser> signInManager, ICompanyService companyService, ITrackService trackService)
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
            _companyService = companyService;
            _trackService = trackService;
            _accountService = accountService;
            _notificationsService = notificationsService;
            _interviewsRepository = interviewsRepository;
            _attachmentStoragePath = Path.Combine(env.WebRootPath, "attachments");

            if (!Directory.Exists(_attachmentStoragePath))
            {
                Directory.CreateDirectory(_attachmentStoragePath);
            }
        }

        public void LogException(string methodName, Exception ex, string additionalInfo = null)
        {
            _interviewsService.LogException(methodName, ex, additionalInfo);
        }

        public async Task<ActionResult> MyInterviews(int? statusFilter, int? companyFilter, int? trackFilter)
        {
            try
            {


                if (User.IsInRole("Interviewer") || User.IsInRole("General Manager") || User.IsInRole("HR Manager") || User.IsInRole("Solution Architecture"))
                {
                    // Get all statuses
                    var statusesResult = await _StatusService.GetAll();
                    if (!statusesResult.IsSuccess)
                    {
                        ModelState.AddModelError("", statusesResult.Error);
                        return View(new List<InterviewsDTO>()); // Return an empty list if there was an error
                    }

                    var companiesResult = await _companyService.GetAll();
                    if (!companiesResult.IsSuccess)
                    {
                        ModelState.AddModelError("", companiesResult.Error);
                        return View(new List<InterviewsDTO>());
                    }
                    var companies = companiesResult.Value;
                    ViewBag.CompanyList = new SelectList(companies, "Id", "Name");

                    var statuses = statusesResult.Value;
                    ViewBag.StatusList = new SelectList(statuses, "Id", "Name");

                    // Default to "Pending" status if no filter is specified
                    if (!statusFilter.HasValue)
                    {
                        statusFilter = await _StatusService.GetStatusIdByName("Pending");
                    }

                    var tracksResult = await _trackService.GetAll();
                    if (!tracksResult.IsSuccess)
                    {
                        ModelState.AddModelError("", tracksResult.Error);
                        return View(new List<InterviewsDTO>());
                    }

                    var tracks = tracksResult.Value;
                    ViewBag.TrackList = new SelectList(tracks, "Id", "Name");



                    var result = await _interviewsService.MyInterviews(companyFilter, trackFilter);
                    if (!result.IsSuccess)
                    {
                        ModelState.AddModelError("", result.Error);
                        return View();
                    }

                    var interviewsDTOs = result.Value;

                    // Filter interviews based on the selected status if a filter is applied
                    if (statusFilter.HasValue && statusFilter.Value > 0)
                    {
                        interviewsDTOs = interviewsDTOs
                            .Where(i => i.StatusId == statusFilter.Value)
                            .ToList();
                    }

                    interviewsDTOs = interviewsDTOs.OrderBy(i => i.Date).ToList();

                    return View(interviewsDTOs);
                }
                else
                {
                    return View("AccessDenied");
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(MyInterviews), ex, "Faild to load MyInterviews");
                throw ex;
            }
        }



        // GET: InterviewsController
        public async Task<ActionResult> Index(int? statusFilter, string candidateFilter, int? trackFilter)
        {
            try
            {
                ViewBag.statusFilter = statusFilter;
                ViewBag.candidateFilter = candidateFilter;

                if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
                {
                    var statusesResult = await _StatusService.GetAll();

                    if (!statusesResult.IsSuccess)
                    {
                        ModelState.AddModelError("", statusesResult.Error);
                        return View(new List<InterviewsDTO>());
                    }
                    var tracks = await _trackService.GetAll();
                    ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                    var statuses = statusesResult.Value;
                    ViewBag.StatusList = new SelectList(statuses, "Id", "Name");

                    var candidatesDTO = await _candidateService.GetAllCandidatesAsync();
                    ViewBag.CandidateList = new SelectList(candidatesDTO, "Id", "FullName");


                    // Apply filters and retrieve filtered interviews
                    var filteredInterviews = await ApplyFiltersAndRetrieveData(statusFilter, candidateFilter, trackFilter);

                    return View(filteredInterviews);
                }
                else
                {
                    return View("AccessDenied");
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(Index), ex, "Failed to load Interviews index page");
                throw ex;
            }
        }

        private async Task<IEnumerable<InterviewsDTO>> ApplyFiltersAndRetrieveData(int? statusFilter, string candidateFilter, int? trackFilter)
        {
            try
            {


                var interviewsResult = await _interviewsService.GetAll();

                if (!interviewsResult.IsSuccess)
                {
                    ModelState.AddModelError("", interviewsResult.Error);
                    return new List<InterviewsDTO>();
                }

                var interviews = interviewsResult.Value;

                // Apply your filters to the data
                int statusId = Convert.ToInt32(statusFilter);

                var filteredInterviews = interviews
                    .GroupBy(i => i.CandidateId)
                    .Select(group => group.OrderByDescending(i => i.InterviewsId).FirstOrDefault())
                    .ToList();



                // Filter by status if the statusFilter parameter is provided
                if (statusFilter.HasValue && statusFilter.Value > 0)
                {
                    filteredInterviews = filteredInterviews
                        .Where(i => i.StatusId == statusFilter.Value)
                        .ToList();
                }

                if (trackFilter.HasValue && trackFilter.Value > 0)
                {
                    filteredInterviews = filteredInterviews
                        .Where(i => i.TrackId == trackFilter.Value)
                        .ToList();
                }


                // Filter by candidate if the candidateFilter parameter is provided
                if (!string.IsNullOrEmpty(candidateFilter))
                {
                    filteredInterviews = filteredInterviews
                        .Where(i => i.FullName.Contains(candidateFilter, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }


                return filteredInterviews;
            }
            catch (Exception ex)
            {
                LogException(nameof(ApplyFiltersAndRetrieveData), ex, "Faild to apply filter");
                throw ex;
            }
        }


        public async Task<IActionResult> StopCycle(int id)
        {
            try
            {
                if (_signInManager.IsSignedIn(User))
                {
                    var result = await _interviewsService.GetById(id);
                    var interviewsDTO = result.Value;

                    return View(interviewsDTO);
                }
                else
                {
                    TempData["ErrorMessage"] = "You must log in first.";
                    return RedirectToAction("Login", "Account");
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(StopCycle), ex, "StopCycle not working");
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopCycle(int id, InterviewsDTO collection)
        {
            try
            {
                if (_signInManager.IsSignedIn(User))
                {
                    // Server-side validation for Notes
                    if (string.IsNullOrWhiteSpace(collection.StopCycleNote))
                    {
                        ModelState.AddModelError("StopCycleNote", "Please add a note.");
                    }

                    if (ModelState.IsValid)
                    {
                        // Save the note to the database
                        var saveNoteResult = await _interviewsService.SaveStopCycleNote(collection.CandidateId, collection.StopCycleNote);

                        if (saveNoteResult.IsSuccess)
                        {
                            // Delete pending interviews for the same candidate
                            var deletePendingResult = await _interviewsService.DeletePendingInterviews(collection.CandidateId, collection);

                            if (deletePendingResult)
                            {
                                // Redirect to a success page or display a success message
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                ModelState.AddModelError("", "Error deleting pending interviews.");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", saveNoteResult.Error);
                        }
                    }

                    var result = await _interviewsService.GetById(id);
                    var interviewsDTO = result.Value;

                    return View(interviewsDTO);
                }
                else
                {
                    TempData["ErrorMessage"] = "You must log in first.";
                    return RedirectToAction("Login", "Account");
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(StopCycle), ex, "StopCycle not working");
                throw ex;
            }
        }




        // GET: InterviewsController/Details/5
        public async Task<ActionResult> Details(int id, string previousAction)
        {
            try
            {

                ViewBag.PreviousAction = previousAction;
                var result = await _interviewsService.GetById(id);

                //if (result == null)
                //{
                //    // Redirect to the custom 404 page
                //    return RedirectToAction("NotFound", "Home");
                //}


                await LoadSelectionLists();

                if (result.IsSuccess)
                {
                    var interviewsDTO = result.Value;

                    interviewsDTO.InterviewerName = await _interviewsService.GetInterviewerName(interviewsDTO.InterviewerId);


                    return View(interviewsDTO);
                }


                else
                {
                    ModelState.AddModelError("", result.Error);
                    return View();
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(Details), ex, $"Faild to load Interview details with ID: {id} ");
                throw ex;
            }
        }

        public async Task<ActionResult> ShowHistory(int id)
        {
            try
            {


                var result = await _interviewsService.ShowHistory(id);

                if (result.IsSuccess)
                {
                    var interviewsDTOs = result.Value;

                    var interviews = await _interviewsService.GetById(id);
                    var interviewsResult = interviews.Value;

                    if (interviewsResult != null)
                    {
                        var candidateId = interviewsResult.CandidateId;

                        var candidate = await _candidateService.GetCandidateByIdAsync(candidateId);
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
            catch (Exception ex)
            {
                LogException(nameof(ShowHistory), ex, "Faild to load show history page");
                throw ex;
            }
        }

        //[Authorize(Roles = "General Manager")]
        // GET: InterviewsController/Create
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
                    return View("AccessDenied");
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, "Faild to load interview create page");
                throw ex;
            }
        }
        private async Task LoadSelectionLists()
        {
            try
            {

                var positions = await _positionService.GetAll();
                ViewBag.positionList = new SelectList(positions.Value, "Id", "Name");
                var candidates = await _candidateService.GetAllCandidatesAsync();
                ViewBag.candidateList = new SelectList(candidates, "Id", "FullName");
                var interviewers = await _accountService.GetAllInterviewers();
                ViewBag.interviewersList = new SelectList(interviewers.Value, "Id", "UserName");
                var architectures = await _accountService.GetAllArchitectureInterviewers();
                ViewBag.architecturesList = new SelectList(architectures.Value, "Id", "UserName");
                var statuses = await _StatusService.GetAll();
                ViewBag.statusList = new SelectList(statuses.Value, "Id", "Name");

                var tracks = await _trackService.GetAll();
                ViewBag.Tracks = new SelectList(tracks.Value, "Id", "Name");
            }
            catch (Exception ex)
            {
                LogException(nameof(LoadSelectionLists), ex, "Faild to LoadSelectionLists");
                throw ex;
            }
        }
        // POST: InterviewsController/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(InterviewsDTO collection)
        {
            try
            {
                var firstInterviewerRoles = await _interviewsService.GetInterviewerRole(collection.InterviewerId);
                var secondInterviewerRoles = await _interviewsService.GetInterviewerRole(collection.SecondInterviewerId);

                await LoadSelectionLists();

                if (ModelState.IsValid)
                {
                    var result = await _interviewsService.Insert(collection);

                    if (result.IsSuccess)
                    {
                        if (User.IsInRole("HR Manager") || User.IsInRole("Admin"))
                        {
                            var selectedInterviewerId = collection.InterviewerId;
                            var insertedInterview = result.Value;
                            collection.InterviewsId = insertedInterview.InterviewsId;

                            HttpContext.Session.SetString($"SecondInterviewerId_{collection.InterviewsId}", collection.SecondInterviewerId ?? "");
                            HttpContext.Session.SetString($"InterviewerId_{collection.InterviewsId}", collection.InterviewerId ?? "");
                            //HttpContext.Session.SetString($"ArchitectureInterviewerId_{collection.InterviewsId}", collection.ArchitectureInterviewerId ?? "");
                            // Get Candidate Name By Id
                            var candidateName = await _candidateService.GetCandidateByIdAsync(collection.CandidateId);
                            var candidateNameresult = candidateName.FullName;

                            // Get Position Name By Id
                            var positionName = await _positionService.GetById(collection.PositionId);
                            var positionNameresult = positionName.Value;
                            var lastPositionName = positionNameresult.Name;

                            string userName = _emailService.GetLoggedInUserName();

                            string userSecondInterviewer = null;

                            var secondInterviewerEmail = await _emailService.GetInterviewerEmail(collection.SecondInterviewerId);
                            if (secondInterviewerEmail != null)
                            {
                                var userSecondInterviewerObj = await _userManager.FindByEmailAsync(secondInterviewerEmail);
                                userSecondInterviewer = userSecondInterviewerObj.UserName;
                            }

                            var formattedDate = collection.Date.ToString("dd/MM/yyyy hh:mm tt");

                            // Prepare the email model for the first interviewer
                            var interviewerEmail = await _emailService.GetInterviewerEmail(collection.InterviewerId);
                            var userInterviewer = await _userManager.FindByEmailAsync(interviewerEmail);
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
                            {
                                // Send email only to the first interviewer if the second interviewer is not selected
                                await _emailService.SendEmailToInterviewer(interviewerEmail, collection, emailModel);
                            }

                            return RedirectToAction("Index");
                        }
                        else
                        {
                            return RedirectToAction("Index");
                        }
                    }

                    ModelState.AddModelError("", result.Error);
                }
                else
                {
                    ModelState.AddModelError("", "Error validating the model");
                }

                return View(collection);
            }
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, $"Failed to create interview. Additional Details data :{collection}");
                throw ex;
            }
        }



        // GET: InterviewsController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            try
            {

                if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
                {

                    if (id <= 0)
                    {
                        return NotFound();
                    }
                    var StatusDTOs = await _StatusService.GetAll();
                    ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");
                    var result = await _interviewsService.GetById(id);
                    var interviewDTO = result.Value;
                    if (interviewDTO == null)
                    {
                        return NotFound();
                    }
                    await LoadSelectionLists();

                    return View(interviewDTO);
                }
                else
                {
                    return View("AccessDenied");
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(Edit), ex, "Faild to load edit interview page");
                throw ex;
            }
        }

        // POST: InterviewsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, InterviewsDTO collection)
        {
            try
            {

                if (collection == null)
                {
                    ModelState.AddModelError("", $"The interview DTO you are trying to update is null ");
                    return RedirectToAction("Index");
                }

                var StatusDTOs = await _StatusService.GetAll();
                ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

                await LoadSelectionLists();

                //if (collection.ArchitectureInterviewerId != null)
                //{
                //HttpContext.Session.SetString($"ArchitectureInterviewerId_{collection.InterviewsId}", collection.ArchitectureInterviewerId ?? "");
                //}

                if (!collection.StatusId.HasValue)
                {
                    ModelState.AddModelError("StatusId", "Please select a status.");
                    return View(collection);
                }

                var statusResult = await _StatusService.GetById((int)collection.StatusId);
                var status = statusResult.Value;

                if (status.Code == Domain.Enums.StatusCode.Rejected && collection.Notes == null)
                {
                    ModelState.AddModelError("Notes", "Please add a note explaining why it was rejected.");
                }




                if (ModelState.IsValid)
                {
                    var previousInterviewerId = HttpContext.Session.GetString($"InterviewerId_{collection.InterviewsId}");
                    var previousSecondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{collection.InterviewsId}");
                    if (status.Code == Domain.Enums.StatusCode.Pending)
                    {

                        await _notificationsService.CreateInterviewNotificationForInterviewerAsync(
                          collection.Date,
                          collection.CandidateId,
                          collection.PositionId,
                          new List<string> { previousInterviewerId, previousSecondInterviewerId },
                          isCanceled: true);

                    }



                    var result = await _interviewsService.Update(collection);

                    HttpContext.Session.SetString($"SecondInterviewerId_{collection.InterviewsId}", collection.SecondInterviewerId ?? "");
                    HttpContext.Session.SetString($"InterviewerId_{collection.InterviewsId}", collection.InterviewerId ?? "");
                    //HttpContext.Session.SetString($"ArchitectureInterviewerId_{collection.InterviewsId}", collection.ArchitectureInterviewerId ?? "");

                    if (result.IsSuccess)
                    {
                        if (User.IsInRole("HR Manager") || User.IsInRole("Admin"))
                        {
                            var selectedInterviewerId = collection.InterviewerId;
                            var insertedInterview = result.Value;
                            collection.InterviewsId = insertedInterview.InterviewsId;

                            HttpContext.Session.SetString($"SecondInterviewerId_{collection.InterviewsId}", collection.SecondInterviewerId ?? "");
                            HttpContext.Session.SetString($"InterviewerId_{collection.InterviewsId}", collection.InterviewerId ?? "");
                            //HttpContext.Session.SetString($"ArchitectureInterviewerId_{collection.InterviewsId}", collection.ArchitectureInterviewerId ?? "");
                            // Get Candidate Name By Id
                            var candidateName = await _candidateService.GetCandidateByIdAsync(collection.CandidateId);
                            var candidateNameresult = candidateName.FullName;

                            // Get Position Name By Id
                            var positionName = await _positionService.GetById(collection.PositionId);
                            var positionNameresult = positionName.Value;
                            var lastPositionName = positionNameresult.Name;

                            string userName = _emailService.GetLoggedInUserName();

                            string userSecondInterviewer = null;

                            var secondInterviewerEmail = await _emailService.GetInterviewerEmail(collection.SecondInterviewerId);
                            if (secondInterviewerEmail != null)
                            {
                                var userSecondInterviewerObj = await _userManager.FindByEmailAsync(secondInterviewerEmail);
                                userSecondInterviewer = userSecondInterviewerObj.UserName;
                            }

                            var formattedDate = collection.Date.ToString("dd/MM/yyyy hh:mm tt");

                            // Prepare the email model for the first interviewer
                            var interviewerEmail = await _emailService.GetInterviewerEmail(collection.InterviewerId);
                            var userInterviewer = await _userManager.FindByEmailAsync(interviewerEmail);

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
                            {
                                // Send email only to the first interviewer if the second interviewer is not selected
                                await _emailService.SendEmailToInterviewer(interviewerEmail, collection, emailModel);
                            }

                            return RedirectToAction("Index");
                        }
                        else
                        {
                            return RedirectToAction("Index");
                        }
                    }


                    ModelState.AddModelError("", result.Error);
                    return View(collection);

                }
                else
                {
                    ModelState.AddModelError("", $"");
                }

                return View(collection);
            }
            catch (Exception ex)
            {
                LogException(nameof(Edit), ex, "Failed to edit interview");
                throw ex;
            }
        }


        // GET: InterviewsController/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            try
            {



                var result = await _interviewsService.GetById(id);
                if (result.IsSuccess)
                {
                    var interviewDTO = result.Value;

                    interviewDTO.InterviewerName = await _interviewsService.GetInterviewerName(interviewDTO.InterviewerId);



                    return View(interviewDTO);
                }


                else
                {
                    ModelState.AddModelError("", result.Error);
                    return View();
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(Delete), ex, "Faild to load delete interview page");
                throw ex;
            }
        }

        // POST: InterviewsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, InterviewsDTO collection)
        {
            try
            {



                if (id <= 0)
                {
                    return BadRequest("invalid career offer id");
                }
                var result = await _interviewsService.Delete(id);
                if (result.IsSuccess)
                {
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("", result.Error);
                return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(Delete), ex, "Faild to delete interview");
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttachment(int id, IFormFile file)
        {

            try
            {



                if (file == null || file.Length == 0)
                {
                    ModelState.AddModelError("File", "Please choose a file to upload.");
                    return View();
                }
                if (ModelState.IsValid)
                {
                    var stream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
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
            catch (Exception ex)
            {
                LogException(nameof(UpdateAttachment), ex, "UpdateAttachment not working");
                throw ex;
            }
        }

        public async Task<IActionResult> UpdateAfterInterviewForEdit(int id)
        {
            try
            {



                var StatusDTOs = await _StatusService.GetAll();
                ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

                var result = await _interviewsService.GetById(id);
                var InterviewsDTO = result.Value;

                if (InterviewsDTO.AttachmentId != null)
                {
                    ViewBag.ExistingAttachmentId = InterviewsDTO.AttachmentId;
                }
                else
                {

                    if (InterviewsDTO.AttachmentId.HasValue)
                    {
                        var existingAttachment = await _attachmentService.GetAttachmentByIdAsync(InterviewsDTO.AttachmentId.Value);

                        if (existingAttachment != null)
                        {
                            var attachmentDTO = existingAttachment;
                            InterviewsDTO.FileName = attachmentDTO.FileName;
                            InterviewsDTO.FileSize = attachmentDTO.FileSize;
                        }
                        ViewBag.FileName = InterviewsDTO.FileName;
                        // ViewBag.FileSize = InterviewsDTO.FileSize;
                    }

                    return View(InterviewsDTO);




                }

                return View(InterviewsDTO);
            }
            catch (Exception ex)
            {
                LogException(nameof(UpdateAfterInterviewForEdit), ex, "UpdateAfterInterviewForEdit not working");
                throw ex;
            }
        }

        public async Task<IActionResult> UpdateAfterInterview(int id)
        {
            try
            {
                if (_signInManager.IsSignedIn(User))
                {

                    var StatusDTOs = await _StatusService.GetAll();
                    ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

                    var result = await _interviewsService.GetById(id);
                    var InterviewsDTO = result.Value;

                    return View(InterviewsDTO);
                }
                else
                {
                    // Redirect to a login page with a message
                    TempData["ErrorMessage"] = "You must log in first.";
                    return RedirectToAction("Login", "Account"); // Adjust "Login" and "Account" according to your actual login route/controller
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(UpdateAfterInterview), ex, "UpdateAfterInterview not working");
                throw ex;
            }
        }


        [HttpPost]
        public async Task<IActionResult> UpdateAfterInterview(InterviewsDTO interviewsDTO, IFormFile file)
        {
            try
            {
                if (!interviewsDTO.StatusId.HasValue)
                {
                    ModelState.AddModelError("StatusId", "Please select a status.");
                }

                //if (interviewsDTO.ArchitectureInterviewerId != null)
                //{
                //    HttpContext.Session.SetString($"ArchitectureInterviewer2Id_{interviewsDTO.InterviewsId}", interviewsDTO.ArchitectureInterviewerId ?? "");
                //}

                var firstInterviewerRoles = await _interviewsService.GetInterviewerRole(interviewsDTO.InterviewerId);
                var secondInterviewerRoles = await _interviewsService.GetInterviewerRole(interviewsDTO.SecondInterviewerId);


                //Get Candidate Name By Id
                var candidateName = await _candidateService.GetCandidateByIdAsync(interviewsDTO.CandidateId);
                var candidateNameresult = candidateName.FullName;

                //Get Position Name By Id
                var positionName = await _positionService.GetById(interviewsDTO.PositionId);
                var positionNameresult = positionName.Value;
                var lastPositionName = positionNameresult.Name;

                var StatusDTOs = await _StatusService.GetAll();
                ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

                var validationErrors = new List<string>();

                if ((file == null || file.Length == 0) && User.IsInRole("Interviewer"))
                {
                    ModelState.AddModelError("AttachmentId", "Please choose a file to upload.");
                }

                if (User.IsInRole("Interviewer") || User.IsInRole("General Manager") || User.IsInRole("Solution Architecture"))
                {
                    if (interviewsDTO.ActualExperience == null)
                    {
                        ModelState.AddModelError("ActualExperience", "Please add the actual experience.");
                    }
                }


                if (!interviewsDTO.StatusId.HasValue)
                {
                    ModelState.AddModelError("StatusId", "Please select a status.");
                }
                else
                {
                    var statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                    if (!statusResult.IsSuccess)
                    {
                        ModelState.AddModelError("StatusId", "Invalid status selected.");
                    }
                    else
                    {
                        var status = statusResult.Value;
                        if (status.Code == Domain.Enums.StatusCode.Rejected && string.IsNullOrWhiteSpace(interviewsDTO.Notes))
                        {
                            ModelState.AddModelError("Notes", "Please add a note for why it was rejected.");
                        }
                    }
                }


                if (interviewsDTO.Score == null && User.IsInRole("Interviewer"))
                {
                    ModelState.AddModelError("Score", "Please add a score.");
                }

                if (interviewsDTO.StatusId == null)
                {
                    ModelState.AddModelError("StatusId", "Please select a status.");
                }

                if (validationErrors.Count() > 0)
                {
                    foreach (var validation in validationErrors)
                    {
                        ModelState.AddModelError("", validation);
                    }
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
                        var newStatusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                        if (newStatusResult.IsSuccess)
                        {
                            var newStatus = newStatusResult.Value;
                            // Check if the new status is On Hold
                            if ((newStatus.Code == Domain.Enums.StatusCode.OnHold || newStatus.Code == Domain.Enums.StatusCode.Rejected) && !User.IsInRole("HR Manager"))
                            {
                                await _notificationsService.CreateInterviewNotificationtoHrForOnHold(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                // Get the current interview status
                                var currentInterview = await _interviewsRepository.GetById(interviewsDTO.InterviewsId); // Assuming you have a method to get the interview by its ID

                                // Check if the current interview status is not pending
                                if (currentInterview.Status.Code != Domain.Enums.StatusCode.Pending && currentInterview.Status.Code != Domain.Enums.StatusCode.Rejected)
                                {
                                    // Continue with the logic only if the current interview status is not pending
                                    var interviewCount = await _interviewsRepository.GetInterviewCountForCandidate(interviewsDTO.CandidateId);

                                    if (newStatus.Code == Domain.Enums.StatusCode.Rejected && !User.IsInRole("HR Manager"))
                                    {
                                        // Check if the next interview is pending
                                        var nextInterviewStatusCode = await _interviewsRepository.GetStatusOfNextInterview(interviewsDTO.CandidateId, interviewsDTO.InterviewsId);

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
                                            var interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));
                                        }
                                    }
                                    else
                                    {



                                        if ((interviewCount >= 1 && interviewCount <= 2) || ((interviewCount == 3 || interviewCount == 4) && User.IsInRole("General Manager")))
                                        {



                                            var interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));
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
                                //else
                                //{
                                //    // Handle the case where the current interview status is pending
                                //    ModelState.AddModelError("StatusId", "Cannot put a pending interview on hold.");
                                //    return View(interviewsDTO);
                                //}
                            }
                            else if (newStatus.Code == Domain.Enums.StatusCode.Approved && !User.IsInRole("HR Manager"))
                            {
                                var nextInterviewStatusCode = await _interviewsRepository.GetStatusOfNextInterview(interviewsDTO.CandidateId, interviewsDTO.InterviewsId);
                                if (nextInterviewStatusCode != null && nextInterviewStatusCode.Equals(Domain.Enums.StatusCode.Pending))
                                {
                                    var interviewsDeleted = await _interviewsRepository.DeletePendingInterviews(interviewsDTO.CandidateId, interviewsDTO.PositionId, userId: User.FindFirstValue(ClaimTypes.NameIdentifier));

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




                        var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

                        if (await _userManager.IsInRoleAsync(currentUser, "General Manager"))
                        {
                            await _interviewsService.ConductInterviewForGm(interviewsDTO);

                        }
                        else if (await _userManager.IsInRoleAsync(currentUser, "Interviewer"))
                        {
                            var secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                            var interviewerId = HttpContext.Session.GetString($"InterviewerId_{interviewsDTO.InterviewsId}");
                            //var archiId = HttpContext.Session.GetString($"ArchitectureInterviewerId_{interviewsDTO.InterviewsId}");

                            await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);
                        }
                        else
                        {
                            var firstinterviewer = await _userManager.FindByIdAsync(interviewsDTO.InterviewerId);

                            var secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                            var secondInterviewer = await _userManager.FindByIdAsync(secondInterviewerId);
                            var interviewerId = HttpContext.Session.GetString($"InterviewerId_{interviewsDTO.InterviewsId}");
                            //var archiId = HttpContext.Session.GetString($"ArchitectureInterviewerId_{interviewsDTO.InterviewsId}");

                            if (secondInterviewer != null)
                            {
                                var isInterviewerGMCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Solution Architecture", "General Manager");
                                var isGMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "General Manager", "Solution Architecture");





                                if (isInterviewerGMCombo || isGMInterviewerCombo)
                                {
                                    await _interviewsService.ConductInterviewForArchi(interviewsDTO);
                                }
                                else
                                {
                                    await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);

                                }
                            }
                            else
                            {
                                await _interviewsService.ConductInterview(interviewsDTO, interviewerId, secondInterviewerId);

                            }
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

                            var statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                            var status = statusResult.Value;


                            if (status.Code == Domain.Enums.StatusCode.Rejected || status.Code == Domain.Enums.StatusCode.Approved)
                            {


                                //var architectureInterviewerId = HttpContext.Session.GetString($"ArchitectureInterviewerId_{interviewsDTO.InterviewsId}");
                                //if(architectureInterviewerId != "" || architectureInterviewerId != null)
                                //{
                                //    await _notificationsService.CreateNotificationForArchiAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);
                                //}


                                if (status.Code == Domain.Enums.StatusCode.Approved)
                                {
                                    string userName = _emailService.GetLoggedInUserName();
                                    var GMEmail = await _emailService.GetGMEmail();
                                    var HREmail = await _emailService.GetHREmail();
                                    var ArchiEmail = await _emailService.GetArchiEmail();

                                    var userGM = await _userManager.FindByEmailAsync(GMEmail);
                                    var userHR = await _userManager.FindByEmailAsync(HREmail);

                                    var firstinterviewer = await _userManager.FindByIdAsync(interviewsDTO.InterviewerId);

                                    var secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                                    var secondInterviewer = await _userManager.FindByIdAsync(secondInterviewerId);

                                    if (secondInterviewer != null)
                                    {


                                        var isInterviewerGMCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Interviewer", "General Manager");
                                        var isGMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "General Manager", "Interviewer");


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
                                                   Dear {userHR.UserName.Replace("_", " ")},
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
                                                Subject = $"Interview Approval",
                                                EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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
                                            {
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModels);
                                            }
                                            if (!string.IsNullOrEmpty(HREmail))
                                            {
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);
                                            }
                                        }
                                        else
                                        {
                                            var archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(interviewsDTO.CandidateId);

                                            var aechituciterId = archiIdd.ArchitectureInterviewerId;

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
                                                Subject = $"Interview Approval",
                                                EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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
                                                var userArchi = await _userManager.FindByEmailAsync(ArchiEmail);
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
                                                {
                                                    //Send an Email to the Archi if it was selceted
                                                    await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                                }
                                            }
                                            if (!string.IsNullOrEmpty(GMEmail))
                                            {
                                                await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModel);
                                            }

                                            if (!string.IsNullOrEmpty(HREmail))
                                            {
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);
                                            }

                                        }
                                    }
                                    else
                                    {
                                        var archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(interviewsDTO.CandidateId);

                                        var aechituciterId = archiIdd.ArchitectureInterviewerId;

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
                                            Subject = $"Interview Approval",
                                            EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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
                                            var userArchi = await _userManager.FindByEmailAsync(ArchiEmail);
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
                                            {
                                                //Send an Email to the Archi if it was selceted
                                                await _emailService.SendEmailToInterviewer(ArchiEmail, interviewsDTO, architectureEmailModel);
                                            }
                                        }

                                        //var reminderJobId = BackgroundJob.Schedule(() => ReminderJobAsync(GMEmail, interviewsDTO), TimeSpan.FromHours(hoursUntil3PM));

                                        if (!string.IsNullOrEmpty(GMEmail))
                                        {
                                            await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModel);
                                        }

                                        if (!string.IsNullOrEmpty(HREmail))
                                        {
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelToHR);
                                        }


                                        return RedirectToAction(nameof(MyInterviews));
                                    }
                                }

                                else if (status.Code == Domain.Enums.StatusCode.Rejected)
                                {
                                    await _notificationsService.CreateNotificationForGeneralManagerAsync(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId, interviewsDTO.ArchitectureInterviewerId);
                                    string userName = _emailService.GetLoggedInUserName();
                                    var HREmail = await _emailService.GetHREmail();

                                    EmailDTOs emailModel = new EmailDTOs
                                    {
                                        EmailTo = new List<string> { HREmail },
                                        Subject = "Interview Rejection",
                                        EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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
                                    {
                                        await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);
                                    }

                                    return RedirectToAction(nameof(MyInterviews));
                                }

                                else
                                {
                                    return RedirectToAction(nameof(MyInterviews));

                                }


                            }
                            else
                            {
                                return RedirectToAction(nameof(MyInterviews));

                            }
                        }
                        else if (User.IsInRole("General Manager"))
                        {
                            var statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                            var status = statusResult.Value;

                            if ((status.Code == Domain.Enums.StatusCode.Rejected || status.Code == Domain.Enums.StatusCode.Approved))
                            {
                                await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);


                                if (status.Code == Domain.Enums.StatusCode.Approved)
                                {
                                    string userName = _emailService.GetLoggedInUserName();
                                    var HREmail = await _emailService.GetHREmail();
                                    var userHR = await _userManager.FindByEmailAsync(HREmail);
                                    EmailDTOs emailModel = new EmailDTOs
                                    {
                                        EmailTo = new List<string> { HREmail },
                                        Subject = $"Interview Invitation ( {candidateNameresult} )",
                                        EmailBody = $@"<html>
                                           <body style='font-family: Arial, sans-serif;'>
                                               <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                   <p style='font-size: 18px; color: #333;'>
                                                       Dear {userHR.UserName.Replace("_", " ")},
                                                   </p>
                                                   <p style='font-size: 16px; color: #555;'>
                                                       You are assigned to have a thierd interview for candidate: {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
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
                                        Subject = "Interview Approval",
                                        EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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

                                    //var reminderJobId = BackgroundJob.Schedule(() => ReminderJobAsync(HREmail, interviewsDTO), interviewsDTO.Date.AddHours(16));

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
                                    var HREmail = await _emailService.GetHREmail();
                                    EmailDTOs emailModel = new EmailDTOs
                                    {
                                        EmailTo = new List<string> { HREmail },
                                        Subject = "Interview Rejection",
                                        EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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
                                    {
                                        await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);
                                    }

                                    return RedirectToAction(nameof(MyInterviews));
                                }

                                else
                                {
                                    return RedirectToAction(nameof(MyInterviews));

                                }
                            }

                        }
                        else if (User.IsInRole("Solution Architecture"))
                        {
                            var statusResult = await _StatusService.GetById(interviewsDTO.StatusId.Value);
                            var status = statusResult.Value;



                            if ((status.Code == Domain.Enums.StatusCode.Rejected || status.Code == Domain.Enums.StatusCode.Approved))
                            {

                                var firstinterviewer = await _userManager.FindByIdAsync(interviewsDTO.InterviewerId);

                                var secondInterviewerId = HttpContext.Session.GetString($"SecondInterviewerId_{interviewsDTO.InterviewsId}");
                                var secondInterviewer = await _userManager.FindByIdAsync(secondInterviewerId);





                                if (status.Code == Domain.Enums.StatusCode.Approved)
                                {
                                    string userName = _emailService.GetLoggedInUserName();
                                    var HREmail = await _emailService.GetHREmail();
                                    var GMEmail = await _emailService.GetGMEmail();


                                    var userHR = await _userManager.FindByEmailAsync(HREmail);
                                    var userGM = await _userManager.FindByEmailAsync(GMEmail);

                                    if (secondInterviewer != null)
                                    {


                                        var isInterviewerGMCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Solution Architecture", "Interviewer");
                                        var isGMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Interviewer", "Solution Architecture");


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
                                                Subject = "Interview Approval",
                                                EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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
                                            {
                                                await _emailService.SendEmailToInterviewer(GMEmail, interviewsDTO, emailModels);
                                            }

                                            if (!string.IsNullOrEmpty(HREmail))
                                            {
                                                await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModelApproval);

                                            }

                                        }


                                        var isInterviewersolCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "Solution Architecture", "General Manager");
                                        var isGMMInterviewerCombo = await IsUserInRolesAsync(firstinterviewer.Id, secondInterviewer.Id, "General Manager", "Solution Architecture");

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
                                                       Dear {userHR.UserName.Replace("_", " ")},
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
                                                Subject = "Interview Approval",
                                                EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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

                                        if (secondInterviewer == null)
                                        {
                                            //var architectureInterviewerId = HttpContext.Session.GetString($"ArchitectureInterviewerId_{interviewsDTO.InterviewsId}");

                                            var archiIdd = _interviewsRepository.GetInterviewByCandidateIdWithParentId(interviewsDTO.CandidateId);
                                            var aechituciterId = archiIdd.ArchitectureInterviewerId;

                                            //if (architectureInterviewerId == "")
                                            //{
                                            //    architectureInterviewerId = null;
                                            //}

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
                                                                   Dear {userGM.UserName.Replace("_", " ")},
                                                               </p>
                                                               <p style='font-size: 16px; color: #555;'>
                                                                   You are assigned to have a thierd interview for candidate : {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
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

                                                //EmailDTOs emailModels = new EmailDTOs
                                                //{
                                                //    EmailTo = new List<string> { HREmail },
                                                //    Subject = "Interview Invitation",
                                                //    EmailBody = $@"<html>
                                                //       <body style='font-family: Arial, sans-sexrif;'>
                                                //           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                                //               <p style='font-size: 18px; color: #333;'>
                                                //                   Dear {userHR},
                                                //               </p>
                                                //               <p style='font-size: 16px; color: #555;'>
                                                //                   You are assigned to have a thierd interview for candidate : {candidateNameresult} with position: {lastPositionName},<br><br>kindly <a href='https://apps.sssprocess.com:6134/'>Click here</a> to see the invitation details.
                                                //               </p>
                                                //               <p style='font-size: 14px; color: #777;'>
                                                //                   Regards,
                                                //               </p>
                                                //           </div>
                                                //       </body>
                                                //    </html>"
                                                //};
                                                //if (!string.IsNullOrEmpty(HREmail))
                                                //{
                                                //    await SendEmailToInterviewer(HREmail, interviewsDTO, emailModels);
                                                //}
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
                                                       Dear {userHR.UserName.Replace("_", " ")},
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


                                    //var reminderJobId = BackgroundJob.Schedule(() => ReminderJobAsync(HREmail, interviewsDTO), interviewsDTO.Date.AddHours(16));


                                    return RedirectToAction(nameof(MyInterviews));
                                }

                                else if (status.Code == Domain.Enums.StatusCode.Rejected)
                                {
                                    if (interviewsDTO.Notes != null)
                                    {
                                        await _notificationsService.CreateInterviewNotificationForHRInterview(interviewsDTO.StatusId.Value, interviewsDTO.Notes, interviewsDTO.CandidateId, interviewsDTO.PositionId);

                                        string userName = _emailService.GetLoggedInUserName();
                                        var HREmail = await _emailService.GetHREmail();
                                        EmailDTOs emailModel = new EmailDTOs
                                        {
                                            EmailTo = new List<string> { HREmail },
                                            Subject = "Interview Rejection",
                                            EmailBody = $@"<html>
                                       <body style='font-family: Arial, sans-serif;'>
                                           <div style='background-color: #f5f5f5; padding: 20px; border-radius: 10px;'>
                                               <p style='font-size: 18px; color: #333;'>
                                                   Dear HR,
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
                                        {
                                            await _emailService.SendEmailToInterviewer(HREmail, interviewsDTO, emailModel);
                                        }
                                    }
                                    return RedirectToAction(nameof(MyInterviews));
                                }

                                else
                                {
                                    return RedirectToAction(nameof(MyInterviews));

                                }
                            }

                        }
                        else
                        {
                            return RedirectToAction(nameof(MyInterviews));
                        }

                        // Redirect to the appropriate action
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
            catch (Exception ex)
            {
                LogException(nameof(UpdateAfterInterview), ex, "Faild to save a status");
                throw ex;
            }
        }



        public async Task<bool> IsUserInRolesAsync(string firstUserId, string secondUserId, string firstRole, string secondRole)
        {
            var firstUser = await _userManager.FindByIdAsync(firstUserId);
            var secondUser = await _userManager.FindByIdAsync(secondUserId);

            if (firstUser == null || secondUser == null)
            {
                // Handle the case when the user is not found
                return false;
            }

            var isFirstUserInRoles = await _userManager.IsInRoleAsync(firstUser, firstRole);
            var isSecondUserInRoles = await _userManager.IsInRoleAsync(secondUser, secondRole);

            return isFirstUserInRoles && isSecondUserInRoles;
        }



    }
}