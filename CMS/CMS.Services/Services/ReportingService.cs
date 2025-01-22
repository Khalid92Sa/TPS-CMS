using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class ReportingService : IReportingService
{
    private readonly ApplicationDbContext _dbContext;
    ICarrerOfferRepository _carrerOfferRepository;
    ICandidateRepository _candidateRepository;
    private ICountryService _countryService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReportingService(
        ApplicationDbContext dbContext,
        ICarrerOfferRepository carrerOfferRepository,
        ICandidateRepository candidateRepository,
        ICountryService countryService,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _carrerOfferRepository = carrerOfferRepository;
        _candidateRepository = candidateRepository;
        _countryService = countryService;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<Result<PerformanceReportDTO>> GetBusinessPerformanceReport()
    {
        try
        {
            int candidatesCount = await _candidateRepository.CountAllAsync();
            int acceptedCount = await _candidateRepository.CountAcceptedAsync();
            int rejectedCount = await _candidateRepository.CountRejectedAsync();
            int pendingCount = await _candidateRepository.CountPendingAsync(); // New line to count pending candidates
            int onHold = await _candidateRepository.CountOnHoldAsync();
            int stoppedcycles = await _candidateRepository.CountStoppedCyclesAsync();

            PerformanceReportDTO report = new PerformanceReportDTO
            {
                NumberOfAccepted = acceptedCount,
                NumberOfCandidates = candidatesCount,
                NumberOfRejected = rejectedCount,
                NumberOfPending = pendingCount,
                NumberOfOnHold = onHold,
                NumberOfStoppedCycles = stoppedcycles
            };
            return Result<PerformanceReportDTO>.Success(report);
        }
        catch (Exception)
        {
            throw;
        }
    }
}