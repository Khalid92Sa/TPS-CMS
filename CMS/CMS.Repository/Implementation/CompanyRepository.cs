using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace CMS.Repository.Implementation;

public class CompanyRepository : ICompanyRepository
{
   private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CompanyRepository(
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
            Company company = await _context.Companies.Include(c => c.Candidates)
                                                      .FirstOrDefaultAsync(c=>c.Id==id);

            if (company.Candidates != null && company.Candidates.Any())
            {
                foreach (Candidate c in company.Candidates.ToList())
                    _context.Candidates.Remove(c);
            }

            _context.Companies.Remove(company);
          
            return  await _context.SaveChangesAsync();
            
        }
        catch(Exception)
        {
            throw;
        }
    }

    public async Task<List<Company>> GetAll()
    {
        try
        {
          return await _context.Companies.Include(c=>c.Country)
                                         .Include(c=>c.Candidates)
                                         .AsNoTracking()
                                         .ToListAsync();
        }
        catch(Exception)
        {
            throw;
        }
    }

    public async Task<Company> GetById(int id)
    {
        try
        {
            Company company = await _context.Companies
                                            .Include(c=>c.Country)
                                            .Include(c=>c.Candidates)
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(c=>c.Id==id);
            
            return company;
        }
        catch (Exception) {
            throw;
        }
    }

    public async Task<int> Insert(Company entity)
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

    public async Task<int> Update(Company entity)
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

    public bool DoesCompanyNameExist(string name, int countryId)
    {
        try
        {
            return _context.Companies.Any(x => x.Name == name && x.CountryId == countryId);
        }

        catch (Exception)
        {
            throw;
        }
    }
}
