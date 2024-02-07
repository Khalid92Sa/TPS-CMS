using CMS.Application.DTOs;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Domain.Enums;
using CMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation
{
    public class StatusRepository : IStatusRepository
    {
        readonly ApplicationDbContext _context;
        public StatusRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<Status>> GetAll()
        {

            try
            {
                return await _context.Statuses.AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async  Task<Status> GetByCode(string co)
        {
             return await _context.Statuses.Where(c => c.Code == co).AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task<Status> GetById(int id)
        {
           return await _context.Statuses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<int> Insert(Status entity)
        {
            try
            {
                _context.Add(entity);
                return await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }




        public async Task<Status> GetStatusByNameAsync(string statusName)
        {
            try
            {
                return await _context.Statuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Name == statusName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting status by name: {ex.Message}", ex);
            }
        }

        public async Task<List<CandidateDTO>> GetCandidatesByCode(string code)
        {
            var candidates = await _context.Candidates
                .Include(c => c.Interviews)
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
                })
                .ToListAsync();

            return candidates;
        }


        public async Task<List<CandidateDTO>> GetApprovedCandidatesByCode(string code, string hrId)
        {
            var candidates = await _context.Candidates
                .Include(c => c.Interviews)
                .ThenInclude(i => i.Status)
                .Where(c => c.Interviews.Any(i => i.Status.Code == code && i.InterviewerId == hrId))
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
                })
                .ToListAsync();

            return candidates;
        }


        public async Task<List<CandidateDTO>> GetPendingCandidatesByCode(string code)
        {
            var candidates = await _context.Candidates
                .Include(c => c.Interviews)
                .ThenInclude(i => i.Status)
                .Where(candidate =>
                   candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.Pending) ||
                   !candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.Rejected) &&
                   !candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.Approved) &&
                   !candidate.Interviews.Any(interview => interview.Status.Code == StatusCode.OnHold))
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
                })
                .ToListAsync();

            return candidates;
        }


    }
}
