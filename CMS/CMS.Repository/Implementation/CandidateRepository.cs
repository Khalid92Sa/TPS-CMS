
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class CandidateRepository : ICandidateRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CandidateRepository(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<IEnumerable<Candidate>> GetAllCandidatesAsync()
    {
        try
        {
            return await _dbContext.Candidates.Include(c => c.Position)
                                              .Include(c => c.Company)
                                              .Include(c => c.Country)
                                              .Include(c => c.Track)
                                              .AsNoTracking()
                                              .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Candidate> GetCandidateByIdAsync(int id)
    {
        try
        {
            return await _dbContext.Candidates.Include(c => c.Position)
                                              .Include(c => c.Company)
                                              .Include(c => c.Country)
                                              .Include(c => c.Track)
                                              .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task CreateCandidateAsync(Candidate candidate)
    {
        try
        {
            await _dbContext.Candidates.AddAsync(candidate);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateCandidateAsync(Candidate candidate)
    {
        try
        {
            _dbContext.Entry(candidate).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task DeleteCandidateAsync(Candidate candidate)
    {

        try
        {
            _dbContext.Candidates.Remove(candidate);
            await _dbContext.SaveChangesAsync();
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int> CountAllAsync()
    {
        try
        {
            return await _dbContext.Candidates.CountAsync();
        }

        catch (Exception)
        {
            throw;
        }

    }


    public async Task<int> CountAcceptedAsync()
    {
        try
        {
            int candidateCounts = await _dbContext.Candidates
                                                  .Include(a => a.Interviews)
                                                      .ThenInclude(a => a.Status)
                                                  .Where(a => (a.Interviews.Count == 3 || a.Interviews.Count == 4) && a.Interviews.All(a => a.Status.Code == StatusCode.Approved && a.StopCycleNote == null))
                                                  .CountAsync();

            int candidateCountsWithTwoAccepted = await _dbContext.Candidates
                                                                 .Include(a => a.Interviews)
                                                                     .ThenInclude(a => a.Status)
                                                                 .Where(a => a.Interviews.Count == 2 && a.Interviews.Skip(1).All(i => i.Status.Code == StatusCode.Approved && i.StopCycleNote == null))
                                                                 .CountAsync();

            return candidateCounts + candidateCountsWithTwoAccepted;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int> CountRejectedAsync()
    {
        try
        {
            int rejectedCount = await _dbContext.Candidates
                                                .Include(a => a.Interviews)
                                                  .ThenInclude(a => a.Status)
                                                .Where(a => a.Interviews.Count > 0 && a.Interviews.Any(a => a.Status.Code == StatusCode.Rejected && a.StopCycleNote == null))
                                                .CountAsync();

            return rejectedCount;
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int> CountPendingAsync()
    {
        try
        {

            int candidateCounts = await _dbContext.Candidates.Include(a => a.Interviews)
                                                                .ThenInclude(a => a.Status)
                                                             .Where(candidate =>
                                                                                  candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.Pending)
                                                                               || candidate.Interviews.All(interview => interview.Status.Code != StatusCode.Rejected) 
                                                                               && candidate.Interviews.All(interview => interview.Status.Code != StatusCode.Approved)
                                                                               && candidate.Interviews.All(interview => interview.Status.Code != StatusCode.OnHold) 
                                                                               && candidate.Interviews.All(interview => interview.StopCycleNote == null)
                                                                   )
                                                             .CountAsync();

            return candidateCounts;
        }

        catch (Exception)
        {
            throw;
        }

    }

    public async Task<int> CountOnHoldAsync()
    {
        try
        {
            int onHoldCount = await _dbContext.Candidates.Include(a => a.Interviews)
                                                            .ThenInclude(a => a.Status)
                                                         .Where(candidate =>
                                                             candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.OnHold && interview.StopCycleNote == null))
                                                         .CountAsync();

            return onHoldCount;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int> CountStoppedCyclesAsync()
    {
        try
        {
            int stoppedCyclesCount = await _dbContext.Interviews.Where(i => i.StopCycleNote != null)
                                                                .CountAsync();

            return stoppedCyclesCount;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int?> GetCVAttachmentIdByCandidateId(int candidateId)
    {
        try
        {
            Candidate candidate = await _dbContext.Candidates
                                                  .Include(c => c.CV)
                                                  .FirstOrDefaultAsync(c => c.Id == candidateId);

            if (candidate != null && candidate.CV != null)
                return candidate.CV.Id;

            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }
}