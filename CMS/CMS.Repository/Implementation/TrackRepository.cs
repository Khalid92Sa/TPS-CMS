using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class TrackRepository : ITrackRepository
{
    private readonly ApplicationDbContext _context;
    public TrackRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<Track>> GetAll()
    {
        try
        {
            return await _context.Tracks.AsNoTracking().ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Track> GetById(int? id) => await _context.Tracks.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
}