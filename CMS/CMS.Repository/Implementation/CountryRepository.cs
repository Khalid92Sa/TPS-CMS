using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CMS.Repository.Implementation;

public class CountryRepository : ICountryRepository
{
    readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CountryRepository(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public int Delete(int id)
    {
        try
        {
            Country country = _context.Countries
                                      .Include(c => c.Companies)
                                      .FirstOrDefault(c => c.Id == id);

            if (country != null)
            {
                foreach (Company com in country.Companies)
                    _context.Companies.Remove(com);

                _context.Countries.Remove(country);
                return _context.SaveChanges();
            }

            return 0;
        }
        catch (Exception)
        {
            throw;
        }
    }


    public async Task<List<Country>> GetAll()
    {
        try
        {
          return await _context.Countries.Include(c=>c.Companies).AsNoTracking().ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Country> GetById(int id)
    {
        try
        {
            Country country = await _context.Countries
                                            .Include(c=>c.Companies)
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(c=>c.Id==id);
            return country;
        }
        catch (Exception)
        {
            throw;
        }

    }

    public async Task<int> Insert(Country entity)
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

    public async Task<int> Update(Country entity)
    {
        try
        {
            _context.Update(entity);
          return  await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }


    public bool DoesCountryNameExist(string name)
    {
        try
        {
            return _context.Countries.Any(x => x.Name == name);
        }
        catch (Exception)
        {
            throw;
        }

    }


    public async Task<IEnumerable<Country>> GetAllCountriesAsync()
    {
        try
        {
            return await _context.Countries.ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    
}
