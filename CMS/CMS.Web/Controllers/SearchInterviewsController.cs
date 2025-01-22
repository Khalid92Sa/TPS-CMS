using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("interviewsFilteration")]
public class SearchInterviewsController : Controller
{


    private readonly ICandidateService _candidateService;
    private readonly IPositionService _positionService;
    private readonly IStatusService _StatusService;
    private readonly INotificationsService _notificationsService;
    private readonly ISearchInterviewsService _searchInterviewsService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITrackService _trackService;
    private readonly string _attachmentStoragePath;

    public SearchInterviewsController(
        ICandidateService candidateService,
        IPositionService positionService,
        IStatusService statusService,
        IWebHostEnvironment env,
        INotificationsService notificationsService,
        ISearchInterviewsService searchInterviewsService,
        IHttpContextAccessor httpContextAccessor,
        ITrackService trackService)
    {
        _candidateService = candidateService;
        _positionService = positionService;
        _StatusService = statusService;
        _notificationsService = notificationsService;
        _searchInterviewsService = searchInterviewsService;
        _httpContextAccessor = httpContextAccessor;
        _trackService = trackService;
        _attachmentStoragePath = Path.Combine(env.WebRootPath, "attachments");

        if (!Directory.Exists(_attachmentStoragePath))
            Directory.CreateDirectory(_attachmentStoragePath);
    }

    [Route("index")]
    public async Task<ActionResult> Index(string positionFilter,
                                      int? scoreFilter,
                                      int? statusFilter,
                                      string candidateFilter,
                                      string interviewerFilter,
                                      DateTime? fromDate,
                                      DateTime? toDate,
                                      string export,
                                      int? trackFilterDropdown,
                                      int pageNumber = 1,
                                      int pageSize = 5)
    {
        try
        {
            ViewBag.positionFilter = positionFilter;
            ViewBag.scoreFilter = scoreFilter;
            ViewBag.statusFilter = statusFilter;
            ViewBag.candidateFilter = candidateFilter;
            ViewBag.interviewerFilter = interviewerFilter;
            ViewBag.fromDate = fromDate;
            ViewBag.toDate = toDate;
            ViewBag.TrackList = trackFilterDropdown;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;

            if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
            {
                Result<IEnumerable<PositionDTO>> positionsDTO = await _positionService.GetAll();
                ViewBag.PositionList = new SelectList(positionsDTO.Value, "Id", "Name");

                Result<List<StatusDTO>> statusesResult = await _StatusService.GetAll();
                if (!statusesResult.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, statusesResult.Error);
                    return View(new List<InterviewsDTO>());
                }

                List<StatusDTO> statuses = statusesResult.Value;
                ViewBag.StatusList = new SelectList(statuses, "Id", "Name");

                IEnumerable<CandidateDTO> candidatesDTO = await _candidateService.GetAllCandidatesAsync();
                ViewBag.CandidateList = new SelectList(candidatesDTO, "Id", "FullName");

                List<UsersDTO> interviewersDTO = await _searchInterviewsService.GetAllInterviewers();
                ViewBag.InterviewerList = new SelectList(interviewersDTO, "Id", "Name");

                Result<List<TrackDTO>> tracks = await _trackService.GetAll();
                ViewBag.TrackListDropdown = new SelectList(tracks.Value, "Id", "Name");

                TempData["PositionFilter"] = positionFilter;
                TempData["ScoreFilter"] = scoreFilter;
                TempData["StatusFilter"] = statusFilter;
                TempData["CandidateFilter"] = candidateFilter;
                TempData["InterviewerFilter"] = interviewerFilter;
                TempData["FromDate"] = fromDate;
                TempData["ToDate"] = toDate;
                TempData["TrackFilterDropdown"] = trackFilterDropdown;

                IEnumerable<InterviewsDTO> paginatedInterviews = await ApplyFiltersAndRetrieveData(
                    positionFilter, scoreFilter, statusFilter, candidateFilter, interviewerFilter, fromDate, toDate, trackFilterDropdown, pageNumber, pageSize
                );

                if (!string.IsNullOrEmpty(export) && export == "excel")
                {
                    byte[] excelData = await ExcelHelper.GenerateExcelFileAsync(
                        paginatedInterviews,
                        async (interviewId) =>
                        {
                            Result<InterviewsDTO> interviewResult = await _searchInterviewsService.GetById(interviewId);
                            return interviewResult.IsSuccess ? interviewResult.Value.FirstInterviewScore : (double?)null;
                        });

                    return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Interviews.xlsx");
                }
                else
                    return View(paginatedInterviews);

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


    private async Task<PaginatedList<InterviewsDTO>> ApplyFiltersAndRetrieveData(string positionFilter,
        int? scoreFilter,
        int? statusFilter,
        string candidateFilter,
        string interviewerFilter,
        DateTime? fromDate,
        DateTime? toDate,
        int? trackFilter,
        int pageNumber,
        int pageSize)
    {
        try
        {
            Result<List<InterviewsDTO>> interviewsResult = await _searchInterviewsService.GetAll();

            if (!interviewsResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, interviewsResult.Error);
                return new PaginatedList<InterviewsDTO>([], 0, pageNumber, pageSize);
            }

            List<InterviewsDTO> interviews = interviewsResult.Value;

            if (!string.IsNullOrEmpty(positionFilter) && positionFilter != "All Positions")
            {
                int positionId = Convert.ToInt32(positionFilter);
                interviews = interviews.Where(i => i.PositionId == positionId).ToList();
            }

            if (scoreFilter.HasValue)
                interviews = interviews.Where(i => i.Score == scoreFilter.Value).ToList();

            if (statusFilter.HasValue && statusFilter.Value > 0)
                interviews = interviews.Where(i => i.StatusId == statusFilter.Value).ToList();

            if (!string.IsNullOrEmpty(candidateFilter))
                interviews = interviews.Where(i => i.FullName.Contains(candidateFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(interviewerFilter) && interviewerFilter != "All Interviewers")
            {
                interviews = interviews.Where(i =>
                    i.InterviewerId == interviewerFilter ||
                    i.SecondInterviewerId == interviewerFilter).ToList();
            }

            if (fromDate.HasValue)
                interviews = interviews.Where(i => i.Date.Date >= fromDate.Value.Date).ToList();

            if (fromDate.HasValue && toDate.HasValue)
            {
                toDate = toDate.Value.AddDays(1);
                interviews = interviews.Where(i => i.Date >= fromDate.Value && i.Date <= toDate.Value).ToList();
            }

            if (trackFilter.HasValue && trackFilter.Value > 0)
                interviews = interviews.Where(i => i.TrackId == trackFilter.Value).ToList();

            List<InterviewsDTO> orderedInterviews = interviews
                .OrderByDescending(i => i.InterviewsId)
                .GroupBy(i => i.CandidateId)
                .Select(group => group.First())
                .ToList();

            int totalCount = orderedInterviews.Count;

            List<InterviewsDTO> paginatedInterviews = orderedInterviews
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            foreach (var interview in paginatedInterviews)
            {
                int? candidateCVAttachmentId = await _candidateService.GetCVAttachmentIdByCandidateId(interview.CandidateId);
                interview.CandidateCVAttachmentId = candidateCVAttachmentId;
            }

            return new PaginatedList<InterviewsDTO>(paginatedInterviews, totalCount, pageNumber, pageSize);
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
            string positionFilter = TempData["PositionFilter"] as string;
            int? scoreFilter = TempData["ScoreFilter"] as int?;
            int? statusFilter = TempData["StatusFilter"] as int?;
            string candidateFilter = TempData["CandidateFilter"] as string;
            string interviewerFilter = TempData["InterviewerFilter"] as string;
            DateTime? fromDate = TempData["FromDate"] as DateTime?;
            DateTime? toDate = TempData["ToDate"] as DateTime?;
            int? trackFilterDropdown = TempData["TrackFilterDropdown"] as int?;

            // Pass filter values to the view
            ViewBag.PositionFilter = positionFilter;
            ViewBag.ScoreFilter = scoreFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CandidateFilter = candidateFilter;
            ViewBag.InterviewerFilter = interviewerFilter;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.TrackList = trackFilterDropdown;


            Result<List<InterviewsDTO>> result = await _searchInterviewsService.ShowHistory(id);

            if (result.IsSuccess)
            {
                List<InterviewsDTO> interviewsDTOs = result.Value;
                Result<InterviewsDTO> interviews = await _searchInterviewsService.GetById(id);
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
                ModelState.AddModelError(string.Empty, result.Error);
                return View();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("exportFilteredData")]
    public async Task<ActionResult> ExportFilteredData(string positionFilter, int? scoreFilter, int? statusFilter, string candidateFilter, string interviewerFilter, DateTime? fromDate, DateTime? toDate, int? trackFilter)
    {
        try
        {
            IEnumerable<InterviewsDTO> filteredInterviews = await ApplyFiltersAndRetrieveData(positionFilter, scoreFilter, statusFilter, candidateFilter, interviewerFilter, fromDate, toDate, trackFilter, 1, int.MaxValue);

            byte[] excelData = await ExcelHelper.GenerateExcelFileAsync(filteredInterviews, async (interviewId) =>
            {
                Result<InterviewsDTO> scoreResult = await _searchInterviewsService.GetById(interviewId);
                return scoreResult.IsSuccess ? scoreResult.Value.FirstInterviewScore : (double?)null;
            });

            string fileName = "Interviews_Report_" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".xlsx";

            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/details")]
    public async Task<ActionResult> Details(int id)
    {
        try
        {
            string positionFilter = TempData["PositionFilter"] as string;
            int? scoreFilter = TempData["ScoreFilter"] as int?;
            int? statusFilter = TempData["StatusFilter"] as int?;
            string candidateFilter = TempData["CandidateFilter"] as string;
            string interviewerFilter = TempData["InterviewerFilter"] as string;
            DateTime? fromDate = TempData["FromDate"] as DateTime?;
            DateTime? toDate = TempData["ToDate"] as DateTime?;
            int? trackFilterDropdown = TempData["TrackFilterDropdown"] as int?;

            Result<InterviewsDTO> result = await _searchInterviewsService.GetById(id);

            Result<IEnumerable<PositionDTO>> positionsDTO = await _positionService.GetAll();
            ViewBag.positionDTOs = new SelectList(positionsDTO.Value, "Id", "Name");

            IEnumerable<CandidateDTO> candidateDTOs = await _candidateService.GetAllCandidatesAsync();
            ViewBag.candidateDTOs = new SelectList(candidateDTOs, "Id", "FullName");

            ViewBag.PositionFilter = positionFilter;
            ViewBag.ScoreFilter = scoreFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CandidateFilter = candidateFilter;
            ViewBag.InterviewerFilter = interviewerFilter;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.TrackList = trackFilterDropdown;

            if (result.IsSuccess)
            {
                InterviewsDTO interviewsDTO = result.Value;
                interviewsDTO.InterviewerName = await _searchInterviewsService.GetInterviewerName(interviewsDTO.InterviewerId);
                return View(interviewsDTO);
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View();
            }
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
            if (id <= 0)
                return NotFound();

            Result<InterviewsDTO> result = await _searchInterviewsService.GetById(id);
            InterviewsDTO interviewDTO = result.Value;

            if (interviewDTO is null)
                return NotFound();

            Result<IEnumerable<PositionDTO>> positionDTOs = await _positionService.GetAll();
            ViewBag.positionDTOs = new SelectList(positionDTOs.Value, "Id", "Name");

            IEnumerable<CandidateDTO> candidateDTOs = await _candidateService.GetAllCandidatesAsync();
            ViewBag.candidateDTOs = new SelectList(candidateDTOs, "Id", "FullName");

            Result<List<StatusDTO>> StatusDTOs = await _StatusService.GetAll();
            ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

            List<UsersDTO> interviewersDTOs = await _searchInterviewsService.GetInterviewers();
            ViewBag.interviewersDTOs = new SelectList(interviewersDTOs, "Id", "Name");

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
                ModelState.AddModelError(string.Empty, $"the interview dto you are trying to update is null ");
                return RedirectToAction(nameof(Index));
            }

            Result<IEnumerable<PositionDTO>> positionDTOs = await _positionService.GetAll();
            ViewBag.positionDTOs = new SelectList(positionDTOs.Value, "Id", "Name");

            IEnumerable<CandidateDTO> candidateDTOs = await _candidateService.GetAllCandidatesAsync();
            ViewBag.candidateDTOs = new SelectList(candidateDTOs, "Id", "FullName");

            Result<List<StatusDTO>> StatusDTOs = await _StatusService.GetAll();
            ViewBag.StatusDTOs = new SelectList(StatusDTOs.Value, "Id", "Name");

            List<UsersDTO> interviewersDTOs = await _searchInterviewsService.GetInterviewers();
            ViewBag.interviewersDTOs = new SelectList(interviewersDTOs, "Id", "Name");

            if (ModelState.IsValid)
            {
                Result<InterviewsDTO> result = await _searchInterviewsService.Update(collection);

                if (result.IsSuccess)
                    return RedirectToAction(nameof(Index));

                ModelState.AddModelError(string.Empty, result.Error);
                return View(collection);
            }
            else
                ModelState.AddModelError(string.Empty, $"the model state is not valid");

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
            Result<InterviewsDTO> result = await _searchInterviewsService.GetById(id);
           
            if (result.IsSuccess)
            {
                InterviewsDTO interviewDTO = result.Value;
                interviewDTO.InterviewerName = await _searchInterviewsService.GetInterviewerName(interviewDTO.InterviewerId);
                return View(interviewDTO);
            }

            else
            {
                ModelState.AddModelError(string.Empty, result.Error);
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

            Result<InterviewsDTO> result = await _searchInterviewsService.Delete(id);
            if (result.IsSuccess)
                return RedirectToAction(nameof(Index));
        
            ModelState.AddModelError(string.Empty, result.Error);
            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }
}