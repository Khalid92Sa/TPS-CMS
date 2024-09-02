
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
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CMS.Repository.Implementation
{
    public class CandidateRepository : ICandidateRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CandidateRepository(ApplicationDbContext dbContext,UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async void LogException(string methodName, Exception ex, string additionalInfo)
        {
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var userId = currentUser?.Id;
            _dbContext.Logs.Add(new Log
            {
                MethodName = methodName,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace,CreatedByUserId = userId,
                LogTime = DateTime.Now,
                
                AdditionalInfo = additionalInfo
            });
            _dbContext.SaveChanges();
        }




        public async Task<IEnumerable<Candidate>> GetAllCandidatesAsync()
        {
            try
            {

            
            return await _dbContext.Candidates
                .Include(c => c.Position)
                .Include(c => c.Company)
                .Include(c => c.Country)
                .Include(c=>c.Track)
                .AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                LogException(nameof(GetAllCandidatesAsync), ex, "Enable to Get All Candidates");
                throw ex;
            }
        }

        public async Task<Candidate> GetCandidateByIdAsync(int id)
        {
            try
            {

           
            return await _dbContext.Candidates
                .Include(c => c.Position)
                .Include(c => c.Company)
                 .Include(c => c.Country)
                 .Include(c => c.Track)
                .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                LogException(nameof(GetCandidateByIdAsync), ex, $"Error retrieving candidate with ID: {id}");
                throw ex;
            }
        }

        public async Task CreateCandidateAsync(Candidate candidate)
        {
            try
            {

           
            _dbContext.Candidates.Add(candidate);
            await _dbContext.SaveChangesAsync();
 }
             catch (Exception ex)
            {
                LogException(nameof(CreateCandidateAsync), ex,  $"Candiate inserted with ID: {candidate.Id}");
                throw ex;
            }
        }

        public async Task UpdateCandidateAsync(Candidate candidate)
        {
            try
            {

            _dbContext.Entry(candidate).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            }

            catch (Exception ex)
            {
                LogException(nameof(UpdateCandidateAsync), ex,  $"Error updating candiate with ID: {candidate.Id}");
                throw ex;
            }
        }


        public async Task DeleteCandidateAsync(Candidate candidate)
        {

            try
            {

            _dbContext.Candidates.Remove(candidate);
            await _dbContext.SaveChangesAsync();
            }

            catch (Exception ex)
            {
                LogException(nameof(DeleteCandidateAsync), null, $"Candidate deleted with ID: {candidate.Id}");
                throw ex;
            }
        }
        public async Task<int> CountAllAsync()
        {
            try
            {

            return await _dbContext.Candidates.CountAsync();
            }

            catch (Exception ex)
            {
                LogException(nameof(CountAllAsync), ex,null);
                throw ex;
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
            catch (Exception ex)
            {
                LogException(nameof(CountAcceptedAsync), ex, "Unable to count the accepted interviews");
                throw ex;
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

            catch (Exception ex)
            {
                LogException(nameof(CountRejectedAsync), ex, "Enable to count the rejected interviews");
                throw ex;
            }
        }
        public async Task<int> CountPendingAsync()
        {
            try
            {

            int candidateCounts = await _dbContext.Candidates
        .Include(a => a.Interviews)
        .ThenInclude(a => a.Status)
        .Where(candidate =>
                            candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.Pending) ||
                            candidate.Interviews.All(interview => interview.Status.Code != StatusCode.Rejected) &&
                            candidate.Interviews.All(interview => interview.Status.Code != StatusCode.Approved) &&
                            candidate.Interviews.All(interview => interview.Status.Code != StatusCode.OnHold) &&
                            candidate.Interviews.All(interview => interview.StopCycleNote == null)
              )
        .CountAsync();

            return candidateCounts;
            }

            catch (Exception ex)
            {
                LogException(nameof(CountPendingAsync), ex, "Enable to count the pending interviews");
                throw ex;
            }

        }

        public async Task<int> CountOnHoldAsync()
        {
            try
            {
                int onHoldCount = await _dbContext.Candidates
                    .Include(a => a.Interviews)
                    .ThenInclude(a => a.Status)
                    .Where(candidate =>
                        candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.OnHold && interview.StopCycleNote == null))
                    .CountAsync();

                return onHoldCount;
            }
            catch (Exception ex)
            {
                LogException(nameof(CountOnHoldAsync), ex, "Unable to count the on hold interviews");
                throw ex;
            }
        }

        public async Task<int> CountStoppedCyclesAsync()
        {
            try
            {
                int stoppedCyclesCount = await _dbContext.Interviews
                    .Where(i => i.StopCycleNote != null)
                    .CountAsync();

                return stoppedCyclesCount;
            }
            catch (Exception ex)
            {
                LogException(nameof(CountStoppedCyclesAsync), ex, "Unable to count the stopped cycles interviews");
                throw ex;
            }
        }


        public async Task<int?> GetCVAttachmentIdByCandidateId(int candidateId)
        {
            try
            {
                var candidate = await _dbContext.Candidates
                    .Include(c => c.CV)
                    .FirstOrDefaultAsync(c => c.Id == candidateId);

                if (candidate != null && candidate.CV != null)
                {
                    return candidate.CV.Id;
                }

                return null;
            }
            catch (Exception ex)
            {
                LogException(nameof(GetCVAttachmentIdByCandidateId), ex, "Enable to Get CVAttachmentId By CandidateId");
                throw ex;
            }
        }


    }
}
