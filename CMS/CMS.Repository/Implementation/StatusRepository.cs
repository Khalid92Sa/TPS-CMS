using CMS.Application.DTOs;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class StatusRepository : IStatusRepository
{
    readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public StatusRepository(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<List<Status>> GetAll()
    {
        try
        {
            return await _context.Statuses.AsNoTracking()
                                          .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Status> GetByCode(string co)
    {
        return await _context.Statuses.Where(c => c.Code == co)
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync();
    }

    public async Task<Status> GetById(int id)
    {
        return await _context.Statuses
                             .AsNoTracking()
                             .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> Insert(Status entity)
    {
        try
        {
            await _context.AddAsync(entity);
            return await _context.SaveChangesAsync();

        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Status> GetStatusByNameAsync(string statusName)
    {
        try
        {
            return await _context.Statuses.AsNoTracking()
                                          .FirstOrDefaultAsync(s => s.Name == statusName);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting status by name: {ex.Message}", ex);
        }
    }

    public async Task<List<CandidateDTO>> GetCandidatesByCode(string code)
    {
        List<CandidateDTO> candidates = await _context.Candidates.Include(c => c.Interviews)
                                                                    .ThenInclude(i => i.Status)
                                                                 .Where(c => c.Interviews.Any(i => i.Status.Code == code))
                                                                 .Select(candidate => new CandidateDTO
                                                                 {
                                                                     Name = candidate.FullName,
                                                                     CompanyId = candidate.CompanyId,
                                                                     CompanyName = candidate.Company.Name,
                                                                     CountryId = candidate.CountryId,
                                                                     CountryName = candidate.Country.Name,
                                                                     Experience = candidate.Experience,
                                                                     PositionId = candidate.PositionId,
                                                                     PositionName = candidate.Position.Name,
                                                                     TrackId = candidate.TrackId,
                                                                     TrackName = candidate.Track.Name,
                                                                     Phone = candidate.Phone,
                                                                     Status = code,
                                                                     CreatedOn = candidate.CreatedOn
                                                                 })
                                                                 .ToListAsync();

        return candidates;
    }


    public async Task<List<CandidateDTO>> GetApprovedCandidatesByCode(string code, string hrId)
    {
        IdentityRole GM = await _roleManager.FindByNameAsync("General Manager");
        string GMId = (await _userManager.GetUsersInRoleAsync(GM.Name))
                        .FirstOrDefault()?.Id;

        var allCandidates = await _context.Candidates
            .Include(c => c.Interviews)
                .ThenInclude(i => i.Status)
            .Include(c => c.Company)
            .Include(c => c.Country)
            .Include(c => c.Position)
            .Include(c => c.Track)
            .Where(c => c.Interviews.Any())
            .ToListAsync();

        var acceptedCandidates = allCandidates
            .Where(c =>
            {
                var interviews = c.Interviews.OrderBy(i => i.InterviewsId).ToList();
                int interviewCount = interviews.Count;

                if (interviewCount == 3 || interviewCount == 4)
                {
                    return interviews.All(i => i.Status.Code == StatusCode.Approved && i.StopCycleNote == null);
                }

                if (interviewCount == 2)
                {
                    var secondInterview = interviews.Skip(1).FirstOrDefault();
                    return secondInterview != null
                        && secondInterview.Status.Code == StatusCode.Approved
                        && secondInterview.StopCycleNote == null;
                }

                if (interviewCount == 1)
                {
                    var interview = interviews.First();

                    return interview.ParentId == null &&
                           interview.StopCycleNote == null &&
                           interview.Status.Code == StatusCode.Approved &&
                           (interview.InterviewerId == GMId ||
                            interview.SecondInterviewerId == GMId);
                }

                return false;
            })
            .Select(candidate => new CandidateDTO
            {
                Name = candidate.FullName,
                CompanyId = candidate.CompanyId,
                CompanyName = candidate.Company?.Name,
                CountryId = candidate.CountryId,
                CountryName = candidate.Country?.Name,
                Experience = candidate.Experience,
                PositionId = candidate.PositionId,
                PositionName = candidate.Position?.Name,
                TrackId = candidate.TrackId,
                TrackName = candidate.Track?.Name,
                Phone = candidate.Phone,
                Status = code,
                CreatedOn = candidate.CreatedOn
            })
            .ToList();

        return acceptedCandidates;
    }


    public async Task<List<CandidateDTO>> GetPendingCandidatesByCode(string code)
    {
        List<CandidateDTO> candidates = await _context.Candidates.Include(c => c.Interviews)
                                                                    .ThenInclude(i => i.Status)
                                                                 .Where(candidate =>
                                                                     candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.Pending)
                                                                 || !candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.Rejected)
                                                                 && !candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.Approved)
                                                                 && !candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.OnHold))
                                                                 .Select(candidate => new CandidateDTO
                                                                 {
                                                                     Name = candidate.FullName,
                                                                     CompanyId = candidate.CompanyId,
                                                                     CompanyName = candidate.Company.Name,
                                                                     CountryId = candidate.CountryId,
                                                                     CountryName = candidate.Country.Name,
                                                                     Experience = candidate.Experience,
                                                                     PositionId = candidate.PositionId,
                                                                     PositionName = candidate.Position.Name,
                                                                     TrackId = candidate.TrackId,
                                                                     TrackName = candidate.Track.Name,
                                                                     Phone = candidate.Phone,
                                                                     Status = code,
                                                                     CreatedOn = candidate.CreatedOn
                                                                 })
                                                                 .ToListAsync();

        return candidates;
    }

    public async Task<List<CandidateDTO>> GetStoppedCyclesCandidatesByNote()
    {
        List<CandidateDTO> candidates = await _context.Candidates.Include(c => c.Interviews)
                                                                    .ThenInclude(i => i.Status)
                                                                 .Where(candidate =>
                                                                     candidate.Interviews.Any(interview => interview.StopCycleNote != null))
                                                                 .Select(candidate => new CandidateDTO
                                                                 {
                                                                     Name = candidate.FullName,
                                                                     CompanyId = candidate.CompanyId,
                                                                     CompanyName = candidate.Company.Name,
                                                                     CountryId = candidate.CountryId,
                                                                     CountryName = candidate.Country.Name,
                                                                     Experience = candidate.Experience,
                                                                     PositionId = candidate.PositionId,
                                                                     PositionName = candidate.Position.Name,
                                                                     TrackId = candidate.TrackId,
                                                                     TrackName = candidate.Track.Name,
                                                                     Phone = candidate.Phone,
                                                                     CreatedOn = candidate.CreatedOn,
                                                                 })
                                                                 .ToListAsync();

        return candidates;
    }
}