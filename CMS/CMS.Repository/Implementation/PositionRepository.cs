using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class PositionRepository : IPositionRepository
{
    readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PositionRepository(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Delete(int id)
    {
        try
        {
            Position position = await _context.Positions.Include(c => c.Interviews)
                                                        .Include(x => x.Candidates)
                                                        .FirstOrDefaultAsync(c => c.Id == id);

            if (position.Interviews != null && position.Interviews.Any())
            {
                foreach (Interviews c in position.Interviews.ToList())
                    _context.Interviews.Remove(c);
            }

            if (position.Candidates != null && position.Candidates.Any())
            {
                foreach (Candidate c in position.Candidates.ToList())
                    _context.Candidates.Remove(c);
            }

            _context.Positions.Remove(position);
            return await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Position>> GetAll()
    {
        try
        {
            return await _context.Positions.Include(c => c.CarrerOffer).Include(c => c.Interviews).Include(c => c.Candidates).AsNoTracking().ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Position> GetById(int id)
    {
        try
        {
            Position postion = await _context.Positions.Include(c => c.CarrerOffer)
                                                       .Include(c => c.Interviews)
                                                       .Include(c => c.Candidates)
                                                       .AsNoTracking()
                                                       .FirstOrDefaultAsync(c => c.Id == id);
            return postion;
        }
        catch (Exception)
        {
            throw;
        }

    }

    public async Task<int> Insert(Position entity)
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

    public async Task<int> Update(Position entity)
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

    public bool DoesPositionNameExist(string name)
    {
        try
        {
            return _context.Positions.Any(x => x.Name == name);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
