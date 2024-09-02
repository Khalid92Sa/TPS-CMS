using CMS.Application.DTOs;
using CMS.Domain;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CMS.Web.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IReportingService _reportingService;
        private readonly ApplicationDbContext _context;
        private readonly ICountryService _countryService;
        private readonly IStatusRepository _statusRepository;
        private readonly ITrackService _trackService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        public DashboardController(IReportingService reportingService,
                                   ApplicationDbContext context,
                                   ICountryService countryService,
                                   IStatusRepository statusRepository,
                                   ITrackService trackService,
                                   RoleManager<IdentityRole> roleManager,
                                   UserManager<IdentityUser> userManager)
        {
            _reportingService = reportingService;
            _context = context;
            _countryService = countryService;
            _statusRepository = statusRepository;
            _trackService = trackService;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public void LogException(string methodName, Exception ex, string additionalInfo = null) => _countryService.LogException(methodName, ex, additionalInfo);

        public async Task<IActionResult> Index()
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("General Manager") || User.IsInRole("HR Manager"))
                {
                    var report = (await _reportingService.GetBusinessPerformanceReport()).Value;
                    double percentageFloat = ((double)report.NumberOfAccepted / report.NumberOfCandidates) * 100;
                    int acceptedPercentage = (int)percentageFloat;
                    ViewBag.AcceptedPercentage = acceptedPercentage;
                    double rejectedFloat = ((double)report.NumberOfRejected / report.NumberOfCandidates) * 100;
                    int rejectedPercentage = (int)rejectedFloat;
                    ViewBag.RejectedPercentage = rejectedPercentage;
                    ViewBag.PendingCount = report.NumberOfPending;

                    double onHoldPercentageFloat = ((double)report.NumberOfOnHold / report.NumberOfCandidates) * 100;
                    int onHoldPercentage = (int)onHoldPercentageFloat;
                    ViewBag.OnHoldPercentage = onHoldPercentage;

                    double StoppedCyclesFloat = ((double)report.NumberOfStoppedCycles / report.NumberOfCandidates) * 100;
                    int StoppedCyclesPercentage = (int)StoppedCyclesFloat;
                    ViewBag.StoppedCycles = StoppedCyclesPercentage;

                    double pendingPercentageFloat = ((double)report.NumberOfPending / report.NumberOfCandidates) * 100;
                    int pendingPercentage = (int)pendingPercentageFloat;
                    ViewBag.PendingPercentage = pendingPercentage;

                    var countries = await _countryService.GetAllCountriesAsync(); // Assuming you have a countryService instance

                    // Convert the list of countries to a JSON array for use in JavaScript
                    var countriesJson = JsonSerializer.Serialize(countries.Select(c => c.Name).ToList());

                    ViewBag.CountriesList = countriesJson; // Pass the JSON data to the view

                    var treeData = GetDataFromDatabase();

                    ViewBag.TreeData = treeData;

                    return View(report);
                }
                else
                {
                    // User is not in the Admin role, handle accordingly (redirect or show an error message)
                    if (User.Identity.IsAuthenticated)
                        return View("AccessDenied");

                    else
                        return RedirectToAction("Login", "Account");
                }
            }
            catch (Exception ex)
            {
                LogException(nameof(Index), ex, "Index page for Dashboard not working");
                throw;
            }
        }

        private static string ArrayToString(string[] array)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < array.Length; i++)
                {
                    sb.Append("'");
                    sb.Append(array[i]);
                    sb.Append("'");

                    if (i < array.Length - 1)
                        sb.Append(",");
                }
                sb.Append("]");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public IActionResult IndexForTree()
        {
            try
            {
                var treeData = GetDataFromDatabase(); // Retrieve data from the database
                return View(treeData);
            }
            catch (Exception ex)
            {
                LogException(nameof(IndexForTree), ex, "IndexForTree not working");
                throw;
            }
        }

        private List<PerformanceReportDTO> GetDataFromDatabase()
        {
            try
            {
                var candidateData = (from candidate in _context.Candidates
                                     join interview in _context.Interviews on candidate.Id equals interview.CandidateId
                                     orderby interview.ModifiedOn descending
                                     select new
                                     {
                                         PositionId = interview.Position.Id,
                                         PositionName = interview.Position.Name,
                                         CountryId = candidate.Company.Country.Id,
                                         CountryName = candidate.Company.Country.Name,
                                         CandidateName = candidate.FullName,
                                         StatusName = interview.Status.Name,
                                         interview.Score,
                                         interview.Date,
                                         interview.ModifiedOn,
                                         InterviewerName = interview.Interviewer.UserName // Include InterviewerName
                                     }).ToList();

                var latestInterviews = candidateData
                    .GroupBy(x => new
                    {
                        x.PositionId,
                        x.PositionName,
                        x.CountryId,
                        x.CountryName,
                        x.CandidateName
                    })
                    .Select(group => group.FirstOrDefault()) // Select the first interview for each candidate
                    .ToList();

                var positionsGroups = latestInterviews.GroupBy(x => new
                                                                    {
                                                                        x.PositionId,
                                                                        x.PositionName,
                                                                        x.CountryId,
                                                                        x.CountryName
                                                                    }
                                                              )
                                                      .Select(group => new PositionDTO
                                                      {
                                                          Id = group.Key.PositionId,
                                                          Name = group.Key.PositionName,
                                                          CountryId = group.Key.CountryId,
                                                          CountryName = group.Key.CountryName,
                                                          Candidates = group.Select(c => new CandidateDTO
                                                                                            {
                                                                                                Name = c.CandidateName,
                                                                                                Status = c.StatusName,
                                                                                                InterviewerName = c.InterviewerName,
                                                                                                Score = c.Score,
                                                                                            }
                                                                                   )
                                                                            .ToList()
                                                      })
                                                      .ToList();

                var result = positionsGroups.GroupBy(g => new
                {
                    g.CountryId,
                    g.CountryName
                })
                .Select(g => new PerformanceReportDTO()
                {
                    Name = g.Key.CountryName,
                    Positions = g.ToList()
                })
                .ToList();

                return result;
            }
            catch (Exception ex)
            {
                LogException(nameof(GetDataFromDatabase), ex, "GetDataFromDatabase not working");
                throw;
            }
        }

        public IActionResult AccessDenied()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(AccessDenied), ex, "Faild to load AccessDenied page");
                throw;
            }
        }

        // Modify the action to accept a candidateName parameter
        public async Task<IActionResult> AcceptedCandidates(string candidateName, int? trackFilter, string companyName)
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
                {
                    var HrId = string.Empty;

                    var Hr = await _roleManager.FindByNameAsync("HR Manager");

                    HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                    var acceptedCandidates = await _statusRepository.GetApprovedCandidatesByCode(CMS.Domain.Enums.StatusCode.Approved, HrId);

                    if (!string.IsNullOrEmpty(candidateName))
                    {
                        acceptedCandidates = acceptedCandidates
                            .Where(c => c.Name.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (trackFilter > 0)
                    {
                        acceptedCandidates = acceptedCandidates
                            .Where(i => i.TrackId == trackFilter.Value)
                            .ToList();
                    }

                    if (!string.IsNullOrEmpty(companyName))
                    {
                        acceptedCandidates = acceptedCandidates
                            .Where(c => c.CompanyName.Contains(companyName.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    acceptedCandidates = acceptedCandidates.OrderByDescending(c => c.CreatedOn)
                                                           .ToList();

                    var tracks = await _trackService.GetAll();
                    ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                    ViewData["candidateName"] = candidateName;
                    ViewData["companyName"] = companyName;
                    ViewBag.selectedTrack = trackFilter;

                    return View(acceptedCandidates);
                }
                else
                    return View("AccessDenied");
            }
            catch (Exception ex)
            {
                LogException(nameof(AcceptedCandidates), ex, "Failed to retrieve accepted candidates");
                throw;
            }
        }

        public async Task<IActionResult> PendingCandidates(string candidateName, int? trackFilter, string companyName)
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
                {
                    var pendingCandidates = await _statusRepository.GetPendingCandidatesByCode(CMS.Domain.Enums.StatusCode.Pending);

                    if (!string.IsNullOrEmpty(candidateName))
                    {
                        pendingCandidates = pendingCandidates
                            .Where(c => c.Name.Contains(candidateName.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (trackFilter > 0)
                    {
                        pendingCandidates = pendingCandidates
                            .Where(i => i.TrackId == trackFilter.Value)
                            .ToList();
                    }

                    if (!string.IsNullOrEmpty(companyName))
                    {
                        pendingCandidates = pendingCandidates
                            .Where(c => c.CompanyName.Contains(companyName.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    pendingCandidates = pendingCandidates.OrderByDescending(c => c.CreatedOn)
                                                         .ToList();

                    var tracks = await _trackService.GetAll();
                    ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                    ViewData["candidateName"] = candidateName;
                    ViewData["companyName"] = companyName;
                    ViewBag.selectedTrack = trackFilter;

                    return View(pendingCandidates);
                }
                else
                    return View("AccessDenied");
            }
            catch (Exception ex)
            {
                LogException(nameof(PendingCandidates), ex, "Failed to retrieve pending candidates");
                throw;
            }
        }

        public async Task<IActionResult> RejectedCandidates(string candidateName, int? trackFilter, string companyName)
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
                {
                    var rejectedCandidates = await _statusRepository.GetCandidatesByCode(CMS.Domain.Enums.StatusCode.Rejected);
                    if (!string.IsNullOrEmpty(candidateName))
                    {
                        rejectedCandidates = rejectedCandidates
                            .Where(c => c.Name.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (trackFilter > 0)
                    {
                        rejectedCandidates = rejectedCandidates
                            .Where(i => i.TrackId == trackFilter.Value)
                            .ToList();
                    }

                    if (!string.IsNullOrEmpty(companyName))
                    {
                        rejectedCandidates = rejectedCandidates
                            .Where(c => c.CompanyName.Contains(companyName.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    rejectedCandidates = rejectedCandidates.OrderByDescending(c => c.CreatedOn)
                                                           .ToList();

                    var tracks = await _trackService.GetAll();
                    ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                    ViewData["candidateName"] = candidateName;
                    ViewData["companyName"] = companyName;
                    ViewBag.selectedTrack = trackFilter;

                    return View(rejectedCandidates);
                }
                else
                    return View("AccessDenied");
            }
            catch (Exception ex)
            {
                LogException(nameof(RejectedCandidates), ex, "Failed to retrieve rejected candidates");
                throw;
            }
        }

        public async Task<IActionResult> OnHoldCandidates(string candidateName, int? trackFilter, string companyName)
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
                {
                    var onHoldCandidates = await _statusRepository.GetCandidatesByCode(CMS.Domain.Enums.StatusCode.OnHold);

                    if (!string.IsNullOrEmpty(candidateName))
                    {
                        onHoldCandidates = onHoldCandidates
                            .Where(c => c.Name.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (trackFilter > 0)
                    {
                        onHoldCandidates = onHoldCandidates
                            .Where(i => i.TrackId == trackFilter.Value)
                            .ToList();
                    }

                    if (!string.IsNullOrEmpty(companyName))
                    {
                        onHoldCandidates = onHoldCandidates
                            .Where(c => c.CompanyName.Contains(companyName.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    onHoldCandidates = onHoldCandidates.OrderByDescending(c => c.CreatedOn)
                                                       .ToList();

                    var tracks = await _trackService.GetAll();
                    ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                    ViewData["candidateName"] = candidateName;
                    ViewData["companyName"] = companyName;
                    ViewBag.selectedTrack = trackFilter;

                    return View(onHoldCandidates);
                }
                else
                    return View("AccessDenied");
            }
            catch (Exception ex)
            {
                LogException(nameof(OnHoldCandidates), ex, "Failed to retrieve on Hold candidates");
                throw;
            }

        }

        public async Task<IActionResult> StoppedCyclesCandidates(string candidateName, int? trackFilter, string companyName)
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
                {
                    var stoppedCyclesCandidates = await _statusRepository.GetStoppedCyclesCandidatesByNote();

                    if (!string.IsNullOrEmpty(candidateName))
                    {
                        stoppedCyclesCandidates = stoppedCyclesCandidates
                            .Where(c => c.Name.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (trackFilter > 0)
                    {
                        stoppedCyclesCandidates = stoppedCyclesCandidates
                            .Where(i => i.TrackId == trackFilter.Value)
                            .ToList();
                    }

                    if (!string.IsNullOrEmpty(companyName))
                    {
                        stoppedCyclesCandidates = stoppedCyclesCandidates
                            .Where(c => c.CompanyName.Contains(companyName.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    stoppedCyclesCandidates = stoppedCyclesCandidates.OrderByDescending(c => c.CreatedOn)
                                                                     .ToList();

                    var tracks = await _trackService.GetAll();
                    ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                    ViewData["candidateName"] = candidateName;
                    ViewData["companyName"] = companyName;
                    ViewBag.selectedTrack = trackFilter;

                    return View(stoppedCyclesCandidates);
                }
                else
                    return View("AccessDenied");
            }
            catch (Exception ex)
            {
                LogException(nameof(OnHoldCandidates), ex, "Failed to retrieve on Hold candidates");
                throw;
            }
        }
    }
}