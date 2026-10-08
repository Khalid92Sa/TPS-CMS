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
    private readonly IStatusRepository _statusRepository;

    public InterviewsRepository(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor,
        IStatusRepository statusRepository)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
        _statusRepository = statusRepository;
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
                                            // Read by callers; without the include every row lazy-loads it (one query per interview)
                                            .Include(c => c.WorkflowStage)
                                            .AsNoTracking()
                                            .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// The interview list pages show one row per candidate: the candidate's latest (highest id)
    /// interview among those matching the filter, newest first. Filtering, grouping, counting and
    /// paging run in SQL, and only the rows of the requested page are loaded.
    /// </summary>
    public async Task<(List<Interviews> Items, int TotalCount)> GetLatestInterviewPerCandidatePageAsync(
        InterviewListFilter filter,
        int pageNumber,
        int pageSize)
    {
        filter ??= new InterviewListFilter();
        IQueryable<Interviews> query = _context.Interviews.AsNoTracking();

        if (filter.StatusId.HasValue)
            query = query.Where(i => i.StatusId == filter.StatusId.Value);

        if (filter.TrackId.HasValue)
            query = query.Where(i => i.TrackId == filter.TrackId.Value);

        if (filter.PositionId.HasValue)
            query = query.Where(i => i.PositionId == filter.PositionId.Value);

        if (filter.Score.HasValue)
            query = query.Where(i => i.Score == filter.Score.Value);

        if (!string.IsNullOrEmpty(filter.InterviewerId))
            query = query.Where(i => i.InterviewerId == filter.InterviewerId || i.SecondInterviewerId == filter.InterviewerId);

        if (!string.IsNullOrEmpty(filter.CandidateName))
            query = query.Where(i => i.Candidate.FullName.Contains(filter.CandidateName));

        if (filter.FromDay.HasValue)
        {
            DateTime fromDay = filter.FromDay.Value.Date;
            query = query.Where(i => i.Date >= fromDay);
        }

        if (filter.From.HasValue)
            query = query.Where(i => i.Date >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(i => i.Date <= filter.To.Value);

        IQueryable<int> latestIds = query
            .GroupBy(i => i.CandidateId)
            .Select(g => g.Max(i => i.InterviewsId));

        int totalCount = await latestIds.CountAsync();

        int skip = Math.Max(0, (pageNumber - 1) * pageSize);
        List<int> pageIds = await latestIds
            .OrderByDescending(id => id)
            .Skip(skip)
            .Take(Math.Max(0, pageSize))
            .ToListAsync();

        if (pageIds.Count == 0)
            return ([], totalCount);

        List<Interviews> items = await _context.Interviews
            .AsNoTracking()
            .Include(i => i.Position)
            .Include(i => i.Candidate)
            .Include(i => i.Status)
            .Include(i => i.Track)
            .Include(i => i.WorkflowStage)
            .Where(i => pageIds.Contains(i.InterviewsId))
            .ToListAsync();

        return (items.OrderByDescending(i => i.InterviewsId).ToList(), totalCount);
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
            IQueryable<InterviewDelegation> visibleDelegations = VisibleDelegationsForUser(userId, DateTime.Now);
            IQueryable<Interviews> interviewsQuery = _context.Interviews.Include(c => c.Position)
                                                                        .Include(c => c.Candidate)
                                                                        .Include(c => c.Status)
                                                                        .Include(c => c.Track)
                                                                        .Where(c => 
                                                                            (
                                                                                (c.InterviewerId == userId || c.SecondInterviewerId == userId) ||
                                                                                (c.ArchitectureInterviewerId == userId && 
                                                                                 (
                                                                                     (c.StartFromHR == true && c.WorkflowStageId == (int)EnumWorkflowStage.GMFinalReview) ||
                                                                                     (c.StartFromHR == false && c.WorkflowStageId.HasValue && c.WorkflowStageId >= (int)EnumWorkflowStage.ManagementReview) ||
                                                                                     c.ModifiedBy == userId
                                                                                 ))
                                                                                || visibleDelegations.Any(d => d.InterviewId == c.InterviewsId)
                                                                            )
                                                                        )
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

    public async Task<bool> DeletePendingInterviews(string nextInterviewStatusCode, int candidateId, int positionId, string userId)
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
                _context.Interviews.RemoveRange(pendingInterviews);

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
            else if (nextInterviewStatusCode is null)
                return true;

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
            IList<IdentityUser> usersWithRole = await _userManager.GetUsersInRoleAsync(roleName);

            List<string> userIds = usersWithRole.Select(u => u.Id).ToList();

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

    /// <summary>
    /// Interviews that are due a result reminder. Only the candidate's current step counts: the
    /// candidate's latest interview record and its parallel siblings (same parent and stage). Of
    /// those, only records still Pending with no score, scheduled between <paramref name="notBefore"/>
    /// and <paramref name="dueBy"/>, and not already reminded since <paramref name="remindedBefore"/>.
    /// An older record never qualifies once the candidate has moved on, whatever its status.
    /// </summary>
    public async Task<List<Interviews>> GetCurrentPendingInterviewsDueReminderAsync(
        int pendingStatusId,
        DateTime notBefore,
        DateTime dueBy,
        DateTime remindedBefore)
    {
        IQueryable<int> latestIds = _context.Interviews
            .GroupBy(i => i.CandidateId)
            .Select(g => g.Max(i => i.InterviewsId));
        IQueryable<Interviews> latest = _context.Interviews.Where(i => latestIds.Contains(i.InterviewsId));

        return await _context.Interviews
            .AsNoTracking()
            .Include(i => i.Candidate)
            .Include(i => i.Position)
            .Where(i => i.StatusId == pendingStatusId
                && i.Score == null
                && i.Date >= notBefore
                && i.Date <= dueBy
                && (i.LastReminderSentOn == null || i.LastReminderSentOn < remindedBefore)
                && latest.Any(l => l.CandidateId == i.CandidateId
                    && l.ParentId == i.ParentId
                    && l.WorkflowStageId == i.WorkflowStageId))
            .ToListAsync();
    }

    public async Task<bool> IsStillPendingAsync(int interviewId, int pendingStatusId)
    {
        return await _context.Interviews
            .AsNoTracking()
            .AnyAsync(i => i.InterviewsId == interviewId && i.StatusId == pendingStatusId && i.Score == null);
    }

    /// <summary>Stamps the reminder time only; the interview's audit fields are left untouched.</summary>
    public async Task MarkReminderSentAsync(int interviewId, DateTime sentOn)
    {
        await _context.Interviews
            .Where(i => i.InterviewsId == interviewId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.LastReminderSentOn, sentOn));
    }

    public async Task<List<Interviews>> GetInterviewsByCandidateIdAsync(int candidateId)
    {
        try
        {
            return await _context.Interviews
                                .Include(i => i.Position)
                                .Include(i => i.Candidate)
                                .Include(i => i.Status)
                                .Include(i => i.Track)
                                .Include(i => i.Interviewer)
                                .Include(i => i.WorkflowStage)
                                .Where(i => i.CandidateId == candidateId)
                                .OrderBy(i => i.WorkflowStageId ?? 0)
                                .ThenBy(i => i.InterviewsId)
                                .AsNoTracking()
                                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Interviews>> GetChildInterviewsByParentIdAsync(int parentInterviewId)
    {
        try
        {
            return await _context.Interviews
                .Where(i => i.ParentId == parentInterviewId)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> DeleteChildInterviewsAndNotificationsAsync(int parentInterviewId, int candidateId)
    {
        try
        {
            // Get all child interviews recursively
            var allChildInterviews = new List<Interviews>();
            var childInterviews = await GetChildInterviewsByParentIdAsync(parentInterviewId);
            allChildInterviews.AddRange(childInterviews);

            // Recursively get grandchildren
            foreach (var child in childInterviews)
            {
                var grandchildren = await GetChildInterviewsByParentIdAsync(child.InterviewsId);
                allChildInterviews.AddRange(grandchildren);
                
                // Continue recursively for deeper levels
                var queue = new Queue<Interviews>(grandchildren);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var nextLevel = await GetChildInterviewsByParentIdAsync(current.InterviewsId);
                    allChildInterviews.AddRange(nextLevel);
                    foreach (var item in nextLevel)
                        queue.Enqueue(item);
                }
            }

            if (allChildInterviews.Count > 0)
            {
                // Collect all interviewer IDs from child interviews for notification deletion
                var interviewerIds = allChildInterviews
                    .SelectMany(i => new[] { i.InterviewerId, i.SecondInterviewerId, i.ArchitectureInterviewerId })
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                // Get interview IDs to delete
                var interviewIds = allChildInterviews.Select(i => i.InterviewsId).ToList();

                // Delete notifications related to these interviews
                var notificationsToDelete = _context.Notifications
                    .Where(n => n.CandidateId == candidateId && 
                                interviewerIds.Contains(n.ReceiverId))
                    .ToList();

                if (notificationsToDelete.Count > 0)
                {
                    _context.Notifications.RemoveRange(notificationsToDelete);
                }

                // Delete all child interviews by ID (to avoid tracking conflicts)
                var interviewsToDelete = await _context.Interviews
                    .Where(i => interviewIds.Contains(i.InterviewsId))
                    .ToListAsync();

                if (interviewsToDelete.Count > 0)
                {
                    _context.Interviews.RemoveRange(interviewsToDelete);
                }

                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> DeleteNotificationsByCandidateAndReceiversAsync(int candidateId, List<string> receiverIds)
    {
        try
        {
            if (receiverIds == null || receiverIds.Count == 0)
                return true;

            // Filter out null or empty receiver IDs
            var validReceiverIds = receiverIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            
            if (validReceiverIds.Count == 0)
                return true;

            // Delete notifications related to these receivers for this candidate
            var notificationsToDelete = _context.Notifications
                .Where(n => n.CandidateId == candidateId && 
                            validReceiverIds.Contains(n.ReceiverId))
                .ToList();

            if (notificationsToDelete.Count > 0)
            {
                _context.Notifications.RemoveRange(notificationsToDelete);
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task ReloadAsync(Interviews interview)
    {
        if (interview == null)
            return;

        await _context.Entry(interview).ReloadAsync();
    }

    public async Task<string> TryCompletePendingIfAssignedAsync(Interviews interview, string actingUserId)
    {
        if (interview == null || string.IsNullOrEmpty(actingUserId))
            return "NotAllowed";

        DateTime now = DateTime.Now;
        Status pendingStatus = await _statusRepository.GetByCode(StatusCode.Pending);
        string activeStatus = InterviewDelegation.StatusActive;

        // The assigned interviewers can always submit. While a delegation is in force the
        // delegate can submit too; whoever submits first completes the interview, and the
        // conditional UPDATE makes sure only one submission is saved.
        int rows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Interviews
            SET StatusId = {interview.StatusId},
                Score = {interview.Score},
                Notes = {interview.Notes},
                StopCycleNote = {interview.StopCycleNote},
                ActualExperience = {interview.ActualExperience},
                AttachmentId = {interview.AttachmentId},
                ModifiedBy = {interview.ModifiedBy},
                ModifiedOn = {interview.ModifiedOn},
                IsUpdated = {interview.IsUpdated}
            WHERE InterviewsId = {interview.InterviewsId}
              AND StatusId = {pendingStatus.Id}
              AND IsUpdated = 0
              AND (
                    InterviewerId = {actingUserId}
                    OR SecondInterviewerId = {actingUserId}
                    OR ArchitectureInterviewerId = {actingUserId}
                    OR EXISTS (
                        SELECT 1
                        FROM InterviewDelegations d
                        WHERE d.InterviewId = Interviews.InterviewsId
                          AND d.DelegatedUserId = {actingUserId}
                          AND d.IsActive = 1
                          AND d.IsDelete = 0
                          AND d.Status = {activeStatus}
                          AND d.StartDate <= {now}
                          AND d.ExpirationDate > {now}
                    )
                  )");

        if (rows == 1)
        {
            _context.Entry(interview).State = EntityState.Detached;

            // The interview is scored: close every open delegation on it. The acting
            // delegate's own delegation is Completed; any other open one (including one whose
            // original interviewer submitted first) is Closed.
            string completedStatus = InterviewDelegation.StatusCompleted;
            string closedStatus = InterviewDelegation.StatusClosed;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE InterviewDelegations
                SET Status = CASE
                                WHEN DelegatedUserId = {actingUserId} AND StartDate <= {now} THEN {completedStatus}
                                ELSE {closedStatus}
                             END,
                    ModifiedBy = {actingUserId},
                    ModifiedOn = {now}
                WHERE InterviewId = {interview.InterviewsId}
                  AND IsActive = 1
                  AND IsDelete = 0
                  AND Status = {activeStatus}
                  AND ExpirationDate > {now}");

            return "Saved";
        }

        Interviews fresh = await _context.Interviews.AsNoTracking()
            .FirstOrDefaultAsync(i => i.InterviewsId == interview.InterviewsId);
        if (fresh == null || fresh.StatusId != pendingStatus.Id || fresh.IsUpdated)
            return "NotPending";

        List<InterviewDelegation> delegations = await GetUserDelegationsForInterviewAsync(interview.InterviewsId, actingUserId);
        if (delegations.Any(d => d.IsActive && !d.IsDelete && d.Status == InterviewDelegation.StatusActive && now < d.StartDate))
            return "NotStarted";
        if (delegations.Any(d => d.IsActive && !d.IsDelete && d.Status == InterviewDelegation.StatusActive && now >= d.ExpirationDate))
            return "Expired";

        return "NotAllowed";
    }

    public async Task<List<InterviewDelegation>> GetDelegationsByInterviewIdAsync(int interviewId)
    {
        return await _context.InterviewDelegations
            .AsNoTracking()
            .Where(d => d.InterviewId == interviewId && !d.IsDelete)
            .OrderByDescending(d => d.CreatedOn)
            .ToListAsync();
    }

    public async Task<List<InterviewDelegation>> GetInForceDelegationsAsync(int interviewId, DateTime asOf)
    {
        return await _context.InterviewDelegations
            .AsNoTracking()
            .Where(d => d.InterviewId == interviewId
                && d.IsActive
                && !d.IsDelete
                && d.Status == InterviewDelegation.StatusActive
                && d.StartDate <= asOf
                && d.ExpirationDate > asOf)
            .OrderByDescending(d => d.CreatedOn)
            .ToListAsync();
    }

    public async Task<List<Interviews>> GetPendingChildInterviewsAsync(int parentInterviewId)
    {
        Status pendingStatus = await _statusRepository.GetByCode(StatusCode.Pending);
        return await _context.Interviews
            .Where(i => i.ParentId == parentInterviewId && i.StatusId == pendingStatus.Id)
            .ToListAsync();
    }

    public async Task<InterviewDelegation> GetActiveDelegationAsync(int interviewId, string userId, DateTime asOf)
    {
        return await _context.InterviewDelegations
            .AsNoTracking()
            .Where(d => d.InterviewId == interviewId
                && d.DelegatedUserId == userId
                && d.IsActive
                && !d.IsDelete
                && d.Status == InterviewDelegation.StatusActive
                && d.StartDate <= asOf
                && d.ExpirationDate > asOf)
            .OrderByDescending(d => d.ExpirationDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<InterviewDelegation>> GetUserDelegationsForInterviewAsync(int interviewId, string userId)
    {
        return await _context.InterviewDelegations
            .AsNoTracking()
            .Where(d => d.InterviewId == interviewId && d.DelegatedUserId == userId && !d.IsDelete)
            .ToListAsync();
    }

    /// <summary>
    /// Delegations that keep an interview visible to the delegate: still open (Active) or already
    /// used by the delegate to submit the result (Completed), from the start date until the period
    /// ends. Unlimited delegations never reach their expiration date. Only Active delegations allow
    /// submitting; see <see cref="GetActiveDelegationAsync"/>.
    /// </summary>
    private IQueryable<InterviewDelegation> VisibleDelegationsForUser(string userId, DateTime asOf)
    {
        return _context.InterviewDelegations
            .Where(d => d.DelegatedUserId == userId
                && d.IsActive
                && !d.IsDelete
                && (d.Status == InterviewDelegation.StatusActive || d.Status == InterviewDelegation.StatusCompleted)
                && d.StartDate <= asOf
                && d.ExpirationDate > asOf);
    }

    public async Task<bool> HasVisibleDelegationAsync(string userId, DateTime asOf)
    {
        return await VisibleDelegationsForUser(userId, asOf).AnyAsync();
    }

    public async Task<List<InterviewDelegation>> GetVisibleDelegationsForUserAsync(string userId, IEnumerable<int> interviewIds, DateTime asOf)
    {
        List<int> ids = interviewIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return [];

        return await VisibleDelegationsForUser(userId, asOf)
            .AsNoTracking()
            .Where(d => ids.Contains(d.InterviewId))
            .OrderByDescending(d => d.ExpirationDate)
            .ToListAsync();
    }

    /// <summary>
    /// The delegation whose delegate submitted this interview's result, or null when the result
    /// was not submitted through a delegation.
    /// </summary>
    public async Task<InterviewDelegation> GetCompletedDelegationAsync(int interviewId)
    {
        return await _context.InterviewDelegations
            .AsNoTracking()
            .Where(d => d.InterviewId == interviewId
                && !d.IsDelete
                && d.Status == InterviewDelegation.StatusCompleted)
            .OrderByDescending(d => d.ModifiedOn)
            .FirstOrDefaultAsync();
    }

    public async Task<string> InsertInterviewDelegationAsync(
        int interviewId,
        string originalInterviewerId,
        bool secondSlot,
        InterviewDelegation delegation,
        int? replaceDelegationId = null)
    {
        Status pendingStatus = await _statusRepository.GetByCode(StatusCode.Pending);
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            Interviews interview = await _context.Interviews
                .FirstOrDefaultAsync(i => i.InterviewsId == interviewId);

            if (interview == null)
            {
                await transaction.RollbackAsync();
                return "NotFound";
            }

            if (interview.StatusId != pendingStatus.Id || interview.IsUpdated)
            {
                await transaction.RollbackAsync();
                return "NotPending";
            }

            string currentInterviewerId = secondSlot ? interview.SecondInterviewerId : interview.InterviewerId;
            if (!string.Equals(currentInterviewerId, originalInterviewerId, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync();
                return "InterviewerChanged";
            }

            if (string.Equals(interview.InterviewerId, delegation.DelegatedUserId, StringComparison.Ordinal)
                || string.Equals(interview.SecondInterviewerId, delegation.DelegatedUserId, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync();
                return "AlreadyAssigned";
            }

            DateTime now = DateTime.Now;
            string slot = secondSlot ? InterviewDelegation.SlotSecond : InterviewDelegation.SlotPrimary;

            // Changing a delegation: the one being replaced must still be open on the same slot.
            InterviewDelegation replaced = null;
            if (replaceDelegationId.HasValue)
            {
                replaced = await _context.InterviewDelegations.FirstOrDefaultAsync(d =>
                    d.Id == replaceDelegationId.Value
                    && d.InterviewId == interviewId
                    && d.InterviewerSlot == slot
                    && d.IsActive
                    && !d.IsDelete
                    && d.Status == InterviewDelegation.StatusActive
                    && d.ExpirationDate > now);

                if (replaced == null)
                {
                    await transaction.RollbackAsync();
                    return "ReplaceNotOpen";
                }
            }

            int? replacedId = replaced?.Id;

            // One open delegation per interviewer slot.
            bool slotTaken = await _context.InterviewDelegations.AnyAsync(d =>
                d.InterviewId == interviewId
                && d.InterviewerSlot == slot
                && d.Id != replacedId
                && d.IsActive
                && !d.IsDelete
                && d.Status == InterviewDelegation.StatusActive
                && d.ExpirationDate > now);

            if (slotTaken)
            {
                await transaction.RollbackAsync();
                return "SlotDelegated";
            }

            // The same person cannot hold two open delegations on one interview (e.g. both slots).
            bool overlaps = await _context.InterviewDelegations.AnyAsync(d =>
                d.InterviewId == interviewId
                && d.DelegatedUserId == delegation.DelegatedUserId
                && d.Id != replacedId
                && d.IsActive
                && !d.IsDelete
                && d.Status == InterviewDelegation.StatusActive
                && d.StartDate < delegation.ExpirationDate
                && delegation.StartDate < d.ExpirationDate);

            if (overlaps)
            {
                await transaction.RollbackAsync();
                return "Overlap";
            }

            if (replaced != null)
                await MarkDelegationRemovedAsync(replaced, delegation.DelegatedByUserId, now);

            delegation.CreatedOn = now;
            delegation.ModifiedOn = now;
            delegation.IsActive = true;
            delegation.IsDelete = false;
            delegation.Status = InterviewDelegation.StatusActive;

            await _context.InterviewDelegations.AddAsync(delegation);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return "Success";
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> CancelInterviewDelegationAsync(int interviewId, int delegationId, string removedByUserId)
    {
        Status pendingStatus = await _statusRepository.GetByCode(StatusCode.Pending);
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            InterviewDelegation delegation = await _context.InterviewDelegations
                .FirstOrDefaultAsync(d => d.Id == delegationId && d.InterviewId == interviewId && !d.IsDelete);

            if (delegation == null)
            {
                await transaction.RollbackAsync();
                return "NotFound";
            }

            if (!delegation.IsActive || delegation.Status == InterviewDelegation.StatusCancelled)
            {
                await transaction.RollbackAsync();
                return "AlreadyRemoved";
            }

            if (delegation.Status == InterviewDelegation.StatusCompleted
                || delegation.Status == InterviewDelegation.StatusClosed)
            {
                await transaction.RollbackAsync();
                return "ScoreSubmitted";
            }

            Interviews interview = await _context.Interviews
                .FirstOrDefaultAsync(i => i.InterviewsId == interviewId);

            if (interview == null)
            {
                await transaction.RollbackAsync();
                return "NotFound";
            }

            if (interview.StatusId != pendingStatus.Id || interview.IsUpdated)
            {
                await transaction.RollbackAsync();
                return "ScoreSubmitted";
            }

            DateTime now = DateTime.Now;
            if (delegation.ExpirationDate <= now)
            {
                await transaction.RollbackAsync();
                return "Expired";
            }

            await MarkDelegationRemovedAsync(delegation, removedByUserId, now);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return "Success";
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<InterviewDelegation>> GetOpenDelegationsForInterviewsAsync(IEnumerable<int> interviewIds, DateTime asOf)
    {
        List<int> ids = interviewIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
            return [];

        return await _context.InterviewDelegations
            .AsNoTracking()
            .Where(d => ids.Contains(d.InterviewId)
                && d.IsActive
                && !d.IsDelete
                && d.Status == InterviewDelegation.StatusActive
                && d.ExpirationDate > asOf)
            .ToListAsync();
    }

    /// <summary>
    /// Revokes a tracked delegation and withdraws the "temporary access" notification
    /// that was sent for it. The caller saves the changes.
    /// </summary>
    private async Task MarkDelegationRemovedAsync(InterviewDelegation delegation, string removedByUserId, DateTime now)
    {
        delegation.Status = InterviewDelegation.StatusCancelled;
        delegation.IsActive = false;
        delegation.RemovedByUserId = removedByUserId;
        delegation.RemovedOn = now;
        delegation.ModifiedBy = removedByUserId;
        delegation.ModifiedOn = now;

        // Matches the window text written by NotificationsService.CreatePendingInterviewDelegationNotificationAsync.
        string expires = delegation.IsUnlimited
            ? $"from {delegation.StartDate:dd/MM/yyyy hh:mm tt}, with no end date"
            : delegation.ExpirationDate.ToString("dd/MM/yyyy hh:mm tt");
        List<Notifications> notices = await _context.Notifications
            .Where(n => n.ReceiverId == delegation.DelegatedUserId
                && n.CandidateId == delegation.CandidateId
                && n.Title.StartsWith("Temporary access to score "))
            .ToListAsync();

        List<Notifications> matched = notices
            .Where(n => n.BodyDesc != null && n.BodyDesc.Contains(expires))
            .ToList();
        if (matched.Count > 0)
            _context.Notifications.RemoveRange(matched);
    }

}
