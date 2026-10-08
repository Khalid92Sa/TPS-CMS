using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class DelegationDurationRepository : IDelegationDurationRepository
{
    private readonly ApplicationDbContext _context;

    public DelegationDurationRepository(ApplicationDbContext context) => _context = context;

    public Task<List<DelegationDuration>> GetAllAsync()
    {
        return _context.DelegationDurations
            .Where(d => !d.IsDelete)
            .OrderBy(d => d.IsUnlimited)
            .ThenBy(d => d.DurationMinutes)
            .ThenBy(d => d.Name)
            .ToListAsync();
    }

    public Task<List<DelegationDuration>> GetActiveAsync()
    {
        return _context.DelegationDurations
            .Where(d => d.IsActive && !d.IsDelete)
            .OrderBy(d => d.IsUnlimited)
            .ThenBy(d => d.DurationMinutes)
            .ThenBy(d => d.Name)
            .ToListAsync();
    }

    public Task<DelegationDuration> GetByIdAsync(int id)
    {
        return _context.DelegationDurations
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDelete);
    }

    public Task<DelegationDuration> GetActiveByIdAsync(int id)
    {
        return _context.DelegationDurations
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive && !d.IsDelete);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeId)
    {
        string normalized = name.Trim();
        return _context.DelegationDurations.AnyAsync(d =>
            !d.IsDelete
            && d.Name == normalized
            && (!excludeId.HasValue || d.Id != excludeId.Value));
    }

    public async Task AddAsync(DelegationDuration duration)
    {
        await _context.DelegationDurations.AddAsync(duration);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DelegationDuration duration)
    {
        DelegationDuration existing = await _context.DelegationDurations
            .FirstOrDefaultAsync(d => d.Id == duration.Id && !d.IsDelete)
            ?? throw new InvalidOperationException("Delegation duration was not found.");

        existing.Name = duration.Name;
        existing.Amount = duration.Amount;
        existing.Unit = duration.Unit;
        existing.DurationMinutes = duration.DurationMinutes;
        existing.IsUnlimited = duration.IsUnlimited;
        existing.IsActive = duration.IsActive;
        existing.ModifiedBy = duration.ModifiedBy;
        existing.ModifiedOn = duration.ModifiedOn;
        await _context.SaveChangesAsync();
    }
}
