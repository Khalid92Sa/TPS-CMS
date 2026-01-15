
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
    private readonly RoleManager<IdentityRole> _roleManager;

    public CandidateRepository(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _roleManager = roleManager;
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
            IdentityRole GM = await _roleManager.FindByNameAsync("General Manager");
            string GMId = (await _userManager.GetUsersInRoleAsync(GM.Name))
                            .FirstOrDefault()?.Id;

            int candidateCounts = await _dbContext.Candidates
                .Where(a =>
                    (a.Interviews.Count == 3 || a.Interviews.Count == 4) &&
                    a.Interviews.All(i =>
                        i.Status.Code == StatusCode.Approved &&
                        i.StopCycleNote == null))
                .CountAsync();

            int candidateCountsWithTwoAccepted = await _dbContext.Candidates
                .Where(a =>
                    a.Interviews.Count == 2 &&
                    a.Interviews.Skip(1).All(i =>
                        i.Status.Code == StatusCode.Approved &&
                        i.StopCycleNote == null))
                .CountAsync();

            var singleInterviewCandidateIds =_dbContext.Interviews.GroupBy(i => i.CandidateId)
                                                                  .Where(g => g.Count() == 1)
                                                                  .Select(g => g.Key);

            int gmInterviewCount = await _dbContext.Interviews
                                                    .Where(i =>
                                                        i.ParentId == null &&
                                                        i.StopCycleNote == null &&
                                                        i.Status.Code == StatusCode.Approved &&
                                                        (i.InterviewerId == GMId || i.SecondInterviewerId == GMId) &&
                                                        singleInterviewCandidateIds.Contains(i.CandidateId))
                                                    .CountAsync();



            return candidateCounts
                 + candidateCountsWithTwoAccepted
                 + gmInterviewCount;
        }
        catch
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
            int stoppedCyclesCount = await _dbContext.Candidates
                                                    .Include(a => a.Interviews)
                                                    .Where(candidate => candidate.Interviews.Any(interview => interview.StopCycleNote != null))
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