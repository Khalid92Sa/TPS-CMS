using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class SelectedInterviewersRepository : ISelectedInterviewersRepository
{
    private readonly ApplicationDbContext _context;

    public SelectedInterviewersRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SelectedInterviewers> GetByInterviewIdAsync(int interviewId)
    {
        return await _context.SelectedInterviewers
            .FirstOrDefaultAsync(s => s.InterviewId == interviewId);
    }

    public async Task<int> InsertAsync(SelectedInterviewers entity)
    {
        await _context.SelectedInterviewers.AddAsync(entity);
        return await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SelectedInterviewers entity)
    {
        _context.SelectedInterviewers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.SelectedInterviewers.FindAsync(id);
        if (entity != null)
        {
            _context.SelectedInterviewers.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}

