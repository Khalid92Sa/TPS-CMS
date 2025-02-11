using CMS.Application.DTOs;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Repositories;

public class InterviewsRepository : IInterviewsRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InterviewsRepository(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Delete(int id)
    {
        try
        {
            Interviews interviews = await _context.Interviews.FindAsync(id)
;
            _context.Interviews.Remove(interviews);
            return await _context.SaveChangesAsync();

        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Interviews>> GetAll()
    {
        try
        {
            return await _context.Interviews.Include(c => c.Position)
                                            .Include(c => c.Candidate)
                                            .Include(c => c.Status)
                                            .Include(c => c.Track)
                                            .AsNoTracking()
                                            .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Interviews> GetById(int id)
    {
        try
        {
            Interviews interview = await _context.Interviews.FirstOrDefaultAsync(c => c.InterviewsId == id);
            return interview;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetRoleById(string id)
    {
        try
        {
            IdentityRole role = await _roleManager.FindByIdAsync(id);

            if (role != null)
                return role.Name;

            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Interviews> GetByIdForEdit(int id)
    {

        try
        {
            Interviews interview = await _context.Interviews.Include(c => c.Position)
                                                            .Include(c => c.Candidate)
                                                            .Include(c => c.Track)
                                                            .Include(c => c.Status)
                                                            .AsNoTracking()
                                                            .FirstOrDefaultAsync(c => c.InterviewsId == id);
            return interview;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Interviews>> GetCurrentInterviews(string userId, int? companyFilter, int? trackFilter)
    {
        try
        {
            IQueryable<Interviews> interviewsQuery = _context.Interviews.Include(c => c.Position)
                                                                        .Include(c => c.Candidate)
                                                                        .Include(c => c.Status)
                                                                        .Include(c => c.Track)
                                                                        .Where(c => c.InterviewerId == userId || c.SecondInterviewerId == userId)
                                                                        .AsQueryable();

            if (companyFilter.HasValue)
                interviewsQuery = interviewsQuery.Where(c => c.Candidate.CompanyId == companyFilter.Value);

            if (trackFilter.HasValue)
                interviewsQuery = interviewsQuery.Where(c => c.TrackId == trackFilter.Value);

            return await interviewsQuery.AsNoTracking().ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }
    public async Task<int> Insert(Interviews entity)
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

    public async Task<int> Update(Interviews entity)
    {
        try
        {
            _context.Update(entity);
            return await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetInterviewerEmail(string interviewerId)
    {
        try
        {
            IdentityUser interviewer = await _context.Users.FindAsync(interviewerId);

            if (interviewer != null)
                return interviewer.Email;

            else
                return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetGeneralManagerEmail()
    {
        try
        {
            string generalManagerRoleId = await _context.Roles.Where(r => r.Name == "General Manager")
                                                              .Select(r => r.Id).FirstOrDefaultAsync();

            if (generalManagerRoleId != null)
            {
                string generalManagerEmail = await _context.UserRoles.Where(ur => ur.RoleId == generalManagerRoleId)
                                                                     .Join(_context.Users, ur => ur.UserId, user => user.Id, (ur, user) => user.Email)
                                                                     .FirstOrDefaultAsync();

                return generalManagerEmail;
            }
            else
                return null;

        }
        catch (Exception)
        {
            throw;

        }
    }

    //Get HrManager Email
    public async Task<string> GetHREmail()
    {
        try
        {
            string hrRoleId = await _context.Roles.Where(r => r.Name == "HR Manager")
                                                  .Select(r => r.Id)
                                                  .FirstOrDefaultAsync();

            if (hrRoleId != null)
            {
                string hrManagerEmail = await _context.UserRoles.Where(ur => ur.RoleId == hrRoleId)
                                                                .Join(_context.Users, ur => ur.UserId,
                                                                                      user => user.Id,
                                                                                      (ur, user) => user.Email
                                                                     )
                                                                .FirstOrDefaultAsync();

                return hrManagerEmail;
            }
            else
            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> GetArchiEmail()
    {
        try
        {
            string archiRoleId = await _context.Roles.Where(r => r.Name == "Solution Architecture")
                                                     .Select(r => r.Id)
                                                     .FirstOrDefaultAsync();

            if (archiRoleId != null)
            {
                string archiEmail = await _context.UserRoles.Where(ur => ur.RoleId == archiRoleId)
                                                            .Join(_context.Users, ur => ur.UserId,
                                                                                  user => user.Id,
                                                                                 (ur, user) => user.Email)
                                                            .FirstOrDefaultAsync();

                return archiEmail;
            }
            else
                return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> HasGivenStatusAsync(string interviewerId, int interviewId)
    {
        try
        {
            Interviews interview = await _context.Interviews.FirstOrDefaultAsync(c => c.InterviewsId == interviewId 
                                                                                   && c.InterviewerId == interviewerId
                                                                                );

            if (interview != null)
                return interview.Status.Code == StatusCode.Pending;
            
            return false;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Interviews> GetGeneralManagerInterviewForCandidate(int candidateId)
    {
        try
        {
            string generalManagerRoleId = (await _roleManager.FindByNameAsync("General Manager"))?.Id;

            if (!string.IsNullOrEmpty(generalManagerRoleId))
            {
                string generalManagerId = (await _userManager.GetUsersInRoleAsync("General Manager")).FirstOrDefault()?.Id;

                if (!string.IsNullOrEmpty(generalManagerId))
                    return await _context.Interviews.Where(i => i.CandidateId == candidateId && i.InterviewerId == generalManagerId)
                                                    .FirstOrDefaultAsync();
            }

            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Interviews> GetArchiInterviewForCandidate(int candidateId)
    {
        try
        {
            string archiRoleId = (await _roleManager.FindByNameAsync("Solution Architecture"))?.Id;

            if (!string.IsNullOrEmpty(archiRoleId))
            {
                string archiId = (await _userManager.GetUsersInRoleAsync("Solution Architecture")).FirstOrDefault()?.Id;

                if (!string.IsNullOrEmpty(archiId))
                {
                    return await _context.Interviews.Where(i => i.CandidateId == candidateId && i.InterviewerId == archiId)
                                                    .FirstOrDefaultAsync();
                }
            }

            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Interviews> GetinterviewerInterviewForCandidate(int candidateId)
    {
        try
        {
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            string interviewerRoleId = (await _userManager.GetRolesAsync(currentUser)).FirstOrDefault();

            if (!string.IsNullOrEmpty(interviewerRoleId))
            {
                string interviewerId = currentUser.Id;

                return await _context.Interviews.Where(i => i.CandidateId == candidateId && i.InterviewerId == interviewerId)
                                                .FirstOrDefaultAsync();
            }

            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<int> GetInterviewCountForCandidate(int candidateId)
    {
        try
        {
            int interviewCount = await _context.Interviews.Where(i => i.CandidateId == candidateId)
                                                          .CountAsync();

            return interviewCount;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> DeletePendingInterviews(int candidateId, int positionId, string userId)
    {
        try
        {
            List<string> approvedInterviewsCreatedByUser = await _context.Interviews
                                                                         .Where(i => i.CandidateId == candidateId
                                                                                  && i.PositionId == positionId 
                                                                                  && i.Status.Code == StatusCode.Approved 
                                                                                  && (i.InterviewerId == userId || i.SecondInterviewerId == userId)
                                                                               )
                                                                         .Select(i => i.InterviewerId)
                                                                         .Distinct()
                                                                         .ToListAsync();

            List<Interviews> pendingInterviews = await _context.Interviews
                                                               .Where(i => i.CandidateId == candidateId 
                                                                        && i.PositionId == positionId 
                                                                        && i.Status.Code == StatusCode.Pending
                                                                     )
                                                               .ToListAsync();

            if (pendingInterviews.Count > 0)
            {
                // Delete pending interviews
                _context.Interviews.RemoveRange(pendingInterviews);

                // Delete associated notifications created by the user who approved the interview
                foreach (string createdByUser in approvedInterviewsCreatedByUser)
                {
                    string HrId = "";

                    IdentityRole Hr = await _roleManager.FindByNameAsync("HR Manager");

                    HrId = (await _userManager.GetUsersInRoleAsync(Hr.Name)).FirstOrDefault().Id;

                    List<Notifications> notificationsToDelete = _context.Notifications
                                                                        .Where(n => n.CreatedBy == createdByUser 
                                                                                 && n.ReceiverId != HrId 
                                                                                 && n.CandidateId == candidateId
                                                                              )
                                                                        .ToList();

                    if (notificationsToDelete.Count > 0)
                        _context.Notifications.RemoveRange(notificationsToDelete);
                }

                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string?> GetStatusOfNextInterview(int candidateId, int currentInterviewId)
    {
        Interviews nextInterview = await _context.Interviews
                                                 .Where(i => i.CandidateId == candidateId && i.InterviewsId > currentInterviewId)
                                                 .OrderBy(i => i.InterviewsId)
                                                 .FirstOrDefaultAsync();

        return nextInterview?.Status?.Code;
    }

    public InterviewsDTO GetInterviewByCandidateIdWithParentId(int candidateId)
    {
        InterviewsDTO intervew = _context.Interviews.Include(i => i.Candidate)
                                                    .Where(i => i.ParentId == null && i.CandidateId == candidateId)
                                                    .Select(i => new InterviewsDTO
                                                                    {
                                                                        InterviewsId = i.InterviewsId,
                                                                        ArchitectureInterviewerId = i.ArchitectureInterviewerId,
                                                                        CandidateId = i.CandidateId,

                                                                    }
                                                           )
                                                    .FirstOrDefault();
        return intervew;

    }

    public async Task<bool> DeletePendingInterviewsforStopCycle(int candidateId, int positionId)
    {
        try
        {
            List<Interviews> pendingInterviews = await _context.Interviews
                                                               .Where(i => i.CandidateId == candidateId 
                                                                        && i.PositionId == positionId 
                                                                        && i.Status.Code == StatusCode.Pending
                                                                     )
                                                               .ToListAsync();

            if (pendingInterviews.Count > 0)
            {
                _context.Interviews.RemoveRange(pendingInterviews);

                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Interviews> GetLastInterviewBeforePendingByCandidateId(int candidateId)
    {
        try
        {
            Interviews pendingInterview = await _context.Interviews
                                                        .Where(i => i.CandidateId == candidateId && i.Status.Code == StatusCode.Pending)
                                                        .OrderByDescending(i => i.InterviewsId)
                                                        .FirstOrDefaultAsync();

            if (pendingInterview != null)
            {
                Interviews previousInterview = await _context.Interviews
                                                             .Where(i => i.CandidateId == candidateId && i.InterviewsId < pendingInterview.InterviewsId)
                                                             .OrderByDescending(i => i.InterviewsId)
                                                             .FirstOrDefaultAsync();

                return previousInterview ?? pendingInterview;
            }

            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Interviews> GetByParentIdAsync(int candidateId)
    {
        try
        {
            return await _context.Interviews.FirstOrDefaultAsync(i => i.CandidateId == candidateId && i.ParentId == null);
        }
        catch (Exception)
        {
            throw;

        }
    }

    public async Task<Interviews> GetByInterviewerRoleAsync(int candidateId, string roleName)
    {
        try
        {
            // Find the user with the specified role
            IList<IdentityUser> usersWithRole = await _userManager.GetUsersInRoleAsync(roleName);

            // Get the user IDs of users with the specified role
            List<string> userIds = usersWithRole.Select(u => u.Id).ToList();

            // Retrieve the interview where the candidate is the interviewee
            // and the interviewer has the specified role
            Interviews interview = await _context.Interviews
                                                 .Include(i => i.Interviewer)
                                                 .FirstOrDefaultAsync(i => i.CandidateId == candidateId &&
                                                                            userIds.Contains(i.InterviewerId));

            return interview;
        }
        catch (Exception)
        {
            throw;

        }
    }

    public async Task<Interviews> GetThirdInterviewAsync(int candidateId)
    {
        try
        {
            return await _context.Interviews
                                 .Where(i => i.CandidateId == candidateId && i.ActualExperience == null)
                                 .OrderByDescending(i => i.Date)
                                 .FirstOrDefaultAsync();
        }
        catch (Exception)
        {
            throw;

        }
    }

    public async Task<bool> DoesInterviewExistForCandidateAsync(int candidateId) => await _context.Interviews.AnyAsync(i => i.CandidateId == candidateId);

    public async Task<List<Interviews>> GetFirstInterviews() => await _context.Interviews
                                                                              .Where(i => i.ParentId == null)
                                                                              .ToListAsync();

}
