using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using CMS.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("dashboard")]
public class DashboardController : Controller
{
    private readonly IReportingService _reportingService;
    private readonly ApplicationDbContext _context;
    private readonly ICountryService _countryService;
    private readonly IStatusRepository _statusRepository;
    private readonly ITrackService _trackService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICompanyService _companyService;
    private readonly UserManager<IdentityUser> _userManager;
    public DashboardController(
        IReportingService reportingService,
        ApplicationDbContext context,
        ICountryService countryService,
        IStatusRepository statusRepository,
        ITrackService trackService,
        ICompanyService companyService,
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
        _companyService = companyService;
    }


    [Route("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("General Manager") || User.IsInRole("HR Manager"))
            {
                PerformanceReportDTO report = (await _reportingService.GetBusinessPerformanceReport()).Value;

                double totalPercentage = 0;

                double percentageFloat = ((double)report.NumberOfAccepted / report.NumberOfCandidates) * 100;
                int acceptedPercentage = (int)Math.Round(percentageFloat);
                totalPercentage += acceptedPercentage;
                ViewBag.AcceptedPercentage = acceptedPercentage;

                double rejectedFloat = ((double)report.NumberOfRejected / report.NumberOfCandidates) * 100;
                int rejectedPercentage = (int)Math.Round(rejectedFloat);
                totalPercentage += rejectedPercentage;
                ViewBag.RejectedPercentage = rejectedPercentage;

                double onHoldPercentageFloat = ((double)report.NumberOfOnHold / report.NumberOfCandidates) * 100;
                int onHoldPercentage = (int)Math.Round(onHoldPercentageFloat);
                totalPercentage += onHoldPercentage;
                ViewBag.OnHoldPercentage = onHoldPercentage;

                double StoppedCyclesFloat = ((double)report.NumberOfStoppedCycles / report.NumberOfCandidates) * 100;
                int StoppedCyclesPercentage = (int)Math.Round(StoppedCyclesFloat);
                totalPercentage += StoppedCyclesPercentage;
                ViewBag.StoppedCycles = StoppedCyclesPercentage;

                int pendingPercentage = 100 - (int)totalPercentage;
                ViewBag.PendingPercentage = pendingPercentage;

                IEnumerable<Country> countries = await _countryService.GetAllCountriesAsync();

                string countriesJson = NetJSON.NetJSON.Serialize(countries.Select(c => c.Name).ToList());

                ViewBag.CountriesList = countriesJson;

                List<PerformanceReportDTO> treeData = GetDataFromDatabase();
                ViewBag.TreeData = treeData;

                return View(report);
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

    private static string ArrayToString(string[] array)
    {
        if (array is null || array.Length == 0)
            return "[]";

        string joinedStrings = string.Join(",", array.Select(item => $"'{item}'"));

        return $"[{joinedStrings}]";
    }

    [Route("indexForTree")]
    public IActionResult IndexForTree()
    {
        try
        {
            List<PerformanceReportDTO> treeData = GetDataFromDatabase();
            return View(treeData);
        }
        catch (Exception)
        {
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
                                     InterviewerName = interview.Interviewer.UserName,
                                     interview.Score,
                                     interview.Date,
                                     interview.ModifiedOn
                                 }).ToList();

            List<PerformanceReportDTO> result = candidateData
                .GroupBy(x => new
                {
                    x.CountryId,
                    x.CountryName
                })
                .Select(countryGroup => new PerformanceReportDTO
                {
                    Name = countryGroup.Key.CountryName,
                    Positions = countryGroup
                        .GroupBy(c => new
                        {
                            c.PositionId,
                            c.PositionName
                        })
                        .Select(positionGroup => new PositionDTO
                        {
                            Id = positionGroup.Key.PositionId,
                            Name = positionGroup.Key.PositionName,
                            CountryId = countryGroup.Key.CountryId,
                            CountryName = countryGroup.Key.CountryName,
                            Candidates = positionGroup
                                .GroupBy(p => p.CandidateName)
                                .Select(g => g.OrderByDescending(i => i.ModifiedOn).First())
                                .Select(c => new CandidateDTO
                                {
                                    Name = c.CandidateName,
                                    Status = c.StatusName,
                                    InterviewerName = c.InterviewerName,
                                    Score = c.Score
                                }).ToList()
                        }).ToList()
                }).ToList();

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("accessDenied")]
    public IActionResult AccessDenied()
    {
        try
        {
            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("acceptedCandidates")]
    public async Task<IActionResult> AcceptedCandidates(string candidateName, int? companyFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
            {
                string HrId = "";

                IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");

                HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                List<CandidateDTO> acceptedCandidates = await _statusRepository.GetApprovedCandidatesByCode(Domain.Enums.StatusCode.Approved, HrId);

                if (!string.IsNullOrEmpty(candidateName))
                {
                    acceptedCandidates = acceptedCandidates
                        .Where(c => c.Name.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (companyFilter.HasValue && companyFilter > 0)
                {
                    acceptedCandidates = acceptedCandidates
                        .Where(i => i.CompanyId == companyFilter.Value)
                        .ToList();
                }

                if (trackFilter > 0)
                {
                    acceptedCandidates = acceptedCandidates
                        .Where(i => i.TrackId == trackFilter.Value)
                        .ToList();
                }

                acceptedCandidates = [.. acceptedCandidates.OrderByDescending(c => c.CreatedOn)];

                PaginatedList<CandidateDTO> paginatedAcceptedCandidates = new(
                                                                acceptedCandidates.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
                                                                acceptedCandidates.Count,
                                                                pageNumber,
                                                                pageSize
                                                            );

                Result<List<TrackDTO>> tracks = await _trackService.GetAll();
                ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                ViewData["candidateName"] = candidateName;
                ViewBag.selectedTrack = trackFilter;

                Result<List<CompanyDTO>> allCompanies = await _companyService.GetAll();
                List<CompanyDTO> companies = allCompanies.Value;
                ViewBag.CompanyList = new SelectList(companies, "Id", "Name");

                return View(paginatedAcceptedCandidates);
            }
            else
                return View("AccessDenied");
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("pendingCandidates")]
    public async Task<IActionResult> PendingCandidates(string candidateName, int? companyFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
            {
                List<CandidateDTO> pendingCandidates = await _statusRepository.GetPendingCandidatesByCode(Domain.Enums.StatusCode.Pending);

                if (!string.IsNullOrEmpty(candidateName))
                {
                    pendingCandidates = pendingCandidates
                        .Where(c => c.Name.Contains(candidateName.Trim(), StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (companyFilter.HasValue && companyFilter > 0)
                {
                    pendingCandidates = pendingCandidates
                        .Where(i => i.CompanyId == companyFilter.Value)
                        .ToList();
                }

                if (trackFilter > 0)
                {
                    pendingCandidates = pendingCandidates
                        .Where(i => i.TrackId == trackFilter.Value)
                        .ToList();
                }

                pendingCandidates = [.. pendingCandidates.OrderByDescending(c => c.CreatedOn)];

                PaginatedList<CandidateDTO> paginatedPendingCandidates = new(
                                                                                pendingCandidates.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
                                                                                pendingCandidates.Count,
                                                                                pageNumber,
                                                                                pageSize
                                                                            );

                Result<List<TrackDTO>> tracks = await _trackService.GetAll();
                ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                ViewData["candidateName"] = candidateName;
                ViewBag.selectedTrack = trackFilter;

                Result<List<CompanyDTO>> allCompanies = await _companyService.GetAll();
                List<CompanyDTO> companies = allCompanies.Value;
                ViewBag.CompanyList = new SelectList(companies, "Id", "Name");

                return View(paginatedPendingCandidates);
            }
            else
                return View("AccessDenied");
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("rejectedCandidates")]
    public async Task<IActionResult> RejectedCandidates(string candidateName, int? companyFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
            {
                List<CandidateDTO> rejectedCandidates = await _statusRepository.GetCandidatesByCode(Domain.Enums.StatusCode.Rejected);
                if (!string.IsNullOrEmpty(candidateName))
                {
                    rejectedCandidates = rejectedCandidates
                        .Where(c => c.Name.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (companyFilter.HasValue && companyFilter > 0)
                {
                    rejectedCandidates = rejectedCandidates
                        .Where(i => i.CompanyId == companyFilter.Value)
                        .ToList();
                }

                if (trackFilter > 0)
                {
                    rejectedCandidates = rejectedCandidates
                        .Where(i => i.TrackId == trackFilter.Value)
                        .ToList();
                }

                rejectedCandidates = [.. rejectedCandidates.OrderByDescending(c => c.CreatedOn)];


                PaginatedList<CandidateDTO> paginatedRejectedCandidatess = new(
                                                                rejectedCandidates.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
                                                                rejectedCandidates.Count,
                                                                pageNumber,
                                                                pageSize
                                                            );

                Result<List<TrackDTO>> tracks = await _trackService.GetAll();
                ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                ViewData["candidateName"] = candidateName;
                ViewBag.selectedTrack = trackFilter;

                Result<List<CompanyDTO>> allCompanies = await _companyService.GetAll();
                List<CompanyDTO> companies = allCompanies.Value;
                ViewBag.CompanyList = new SelectList(companies, "Id", "Name");

                return View(paginatedRejectedCandidatess);
            }
            else
                return View("AccessDenied");
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("onHoldCandidates")]
    public async Task<IActionResult> OnHoldCandidates(string candidateName, int? companyFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
            {
                List<CandidateDTO> onHoldCandidates = await _statusRepository.GetCandidatesByCode(Domain.Enums.StatusCode.OnHold);

                if (!string.IsNullOrEmpty(candidateName))
                {
                    onHoldCandidates = onHoldCandidates
                        .Where(c => c.Name.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (companyFilter.HasValue && companyFilter > 0)
                {
                    onHoldCandidates = onHoldCandidates
                        .Where(i => i.CompanyId == companyFilter.Value)
                        .ToList();
                }

                if (trackFilter > 0)
                {
                    onHoldCandidates = onHoldCandidates
                        .Where(i => i.TrackId == trackFilter.Value)
                        .ToList();
                }

                onHoldCandidates = [.. onHoldCandidates.OrderByDescending(c => c.CreatedOn)];

                PaginatedList<CandidateDTO> paginatedOnHoldCandidatess = new(
                                                onHoldCandidates.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
                                                onHoldCandidates.Count,
                                                pageNumber,
                                                pageSize
                                            );

                Result<List<TrackDTO>> tracks = await _trackService.GetAll();
                ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                ViewData["candidateName"] = candidateName;
                ViewBag.selectedTrack = trackFilter;

                Result<List<CompanyDTO>> allCompanies = await _companyService.GetAll();
                List<CompanyDTO> companies = allCompanies.Value;
                ViewBag.CompanyList = new SelectList(companies, "Id", "Name");

                return View(paginatedOnHoldCandidatess);
            }
            else
                return View("AccessDenied");
        }
        catch (Exception)
        {
            throw;
        }

    }

    [Route("stoppedCyclesCandidates")]
    public async Task<IActionResult> StoppedCyclesCandidates(string candidateName, int? companyFilter, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager") || User.IsInRole("General Manager"))
            {
                List<CandidateDTO> stoppedCyclesCandidates = await _statusRepository.GetStoppedCyclesCandidatesByNote();

                if (!string.IsNullOrEmpty(candidateName))
                {
                    stoppedCyclesCandidates = stoppedCyclesCandidates
                        .Where(c => c.Name.Contains(candidateName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (companyFilter.HasValue && companyFilter > 0)
                {
                    stoppedCyclesCandidates = stoppedCyclesCandidates
                        .Where(i => i.CompanyId == companyFilter.Value)
                        .ToList();
                }

                if (trackFilter > 0)
                {
                    stoppedCyclesCandidates = stoppedCyclesCandidates
                        .Where(i => i.TrackId == trackFilter.Value)
                        .ToList();
                }

                stoppedCyclesCandidates = [.. stoppedCyclesCandidates.OrderByDescending(c => c.CreatedOn)];

                PaginatedList<CandidateDTO> paginatedStoppedCyclesCandidates = new(
                                                                                    stoppedCyclesCandidates.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
                                                                                    stoppedCyclesCandidates.Count,
                                                                                    pageNumber,
                                                                                    pageSize
                                                                                  );

                Result<List<TrackDTO>> tracks = await _trackService.GetAll();
                ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                ViewData["candidateName"] = candidateName;
                ViewBag.selectedTrack = trackFilter;

                Result<List<CompanyDTO>> allCompanies = await _companyService.GetAll();
                List<CompanyDTO> companies = allCompanies.Value;
                ViewBag.CompanyList = new SelectList(companies, "Id", "Name");

                return View(paginatedStoppedCyclesCandidates);
            }
            else
                return View("AccessDenied");
        }
        catch (Exception)
        {
            throw;
        }
    }
}