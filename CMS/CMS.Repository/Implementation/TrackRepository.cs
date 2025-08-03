using CMS.Application.DTOs;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation
{
    public class TrackRepository : ITrackRepository
    {
        private readonly ApplicationDbContext _context;
        public TrackRepository(ApplicationDbContext context) => _context = context;

        public async Task<List<Track>> GetAll()
        {
            try
            {
                return await _context.Tracks.Include(t => t.Candidates)
                                            .AsNoTracking()
                                            .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<TrackDTO>> GetAllWithCandidateCountAsync()
        {
            return await _context.Tracks
                .Select(t => new TrackDTO
                {
                    Id = t.Id,
                    Name = t.Name,
                    CandidateCount = t.Candidates.Count()
                })
                .AsNoTracking()
                .ToListAsync();
        }


        public async Task<Track> GetById(int? id)
        {
            return await _context.Tracks.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Track> Create(Track track)
        {
            try
            {
                await _context.Tracks.AddAsync(track);
                await _context.SaveChangesAsync();
                return track;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> Update(Track track)
        {
            try
            {
                var existingTrack = await _context.Tracks.FindAsync(track.Id);
                if (existingTrack == null)
                    return false;

                _context.Entry(existingTrack).CurrentValues.SetValues(track);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> Delete(int id)
        {
            try
            {
                var track = await _context.Tracks
                                          .Include(t => t.Candidates)
                                          .FirstOrDefaultAsync(t => t.Id == id);

                if (track == null)
                    return false;

                if (track.Candidates != null && track.Candidates.Any())
                {
                    foreach (var candidate in track.Candidates.ToList())
                    {
                        _context.Candidates.Remove(candidate);
                    }
                }

                _context.Tracks.Remove(track);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}