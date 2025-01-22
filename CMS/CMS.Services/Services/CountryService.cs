using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class CountryService : ICountryService
{
    ICountryRepository _repository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public CountryService(
        ICountryRepository repository,
        IHttpContextAccessor httpContextAccessor,
        UserManager<IdentityUser> userManager)
    {
        _repository = repository;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }


    public Result<CountryDTO> Delete(int id)
    {
        try
        {
            _repository.Delete(id);
            return Result<CountryDTO>.Success(null);
        }
        catch (Exception ex)
        {
            return Result<CountryDTO>.Failure(null, $"An error occurred while deleting the country: {ex.Message}");
        }
    }

    public async Task<Result<List<CountryDTO>>> GetAll()
    {
        List<Country> countries = await _repository.GetAll();

        if (countries is null)
            return Result<List<CountryDTO>>.Failure(null, "no countries found");

        try
        {
            List<CountryDTO> countryDTOS = new List<CountryDTO>();

            foreach (Country co in countries)
            {
                countryDTOS.Add(new CountryDTO
                {
                    Id = co.Id,
                    Name = co.Name,
                    CreatedOn = co.CreatedOn,
                    companyDTOs = co.Companies.Select(com => new CompanyDTO
                    {
                        Id = com.Id,
                        Name = com.Name,
                        Email = com.Email,
                        PersonName = com.PersonName,
                        CountryId = com.CountryId,
                        PhoneNumber = com.PhoneNumber,
                        CountryName = com.Country.Name

                    }).ToList()
                });

            }
            return Result<List<CountryDTO>>.Success(countryDTOS);
        }
        catch (Exception ex)
        {
            return Result<List<CountryDTO>>.Failure(null, $"unable to get countries{ex.InnerException.Message}");
        }
    }

    public async Task<Result<CountryDTO>> GetById(int id)
    {
        if (id <= 0)
            return Result<CountryDTO>.Failure(null, "Invalid company id");

        try
        {

            Country country = await _repository.GetById(id);
            CountryDTO countryDTOS = new CountryDTO
            {
                Id = country.Id,
                Name = country.Name,
                CreatedOn= country.CreatedOn,
                companyDTOs = country.Companies.Select(com => new CompanyDTO
                {
                    Id = com.Id,
                    Name = com.Name,
                    Email = com.Email,
                    PersonName = com.PersonName,
                    CountryId = com.CountryId,
                    PhoneNumber = com.PhoneNumber,

                }).ToList()

            };
            return Result<CountryDTO>.Success(countryDTOS);
        }
        catch (Exception ex)
        {
            return Result<CountryDTO>.Failure(null, $"unable to retrieve the country from the repository{ex.InnerException.Message}");
        }
    }

    public async Task<Result<CountryDTO>> Insert(CountryDTO data)
    {
        if (data is null)
            return Result<CountryDTO>.Failure(data, "the country dto is null");

        IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        Country country = new()
        {
            Name = data.Name,
            CreatedBy = currentUser.Id,
            CreatedOn = DateTime.Now,
        };
        try
        {
            await _repository.Insert(country);
            return Result<CountryDTO>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<CountryDTO>.Failure(data, $"unable to insert a country: {ex.InnerException.Message}");
        }
    }

    public async Task<Result<CountryDTO>> Update(CountryDTO data)
    {
        try
        {
            if (data is null)
                return Result<CountryDTO>.Failure(null, "can not update a null object");

            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            Country previousCountry = await _repository.GetById(data.Id);
            Country country = new Country
            {
                Name = data.Name,
                Id = data.Id,
                ModifiedBy = currentUser.Id,
                ModifiedOn = DateTime.Now,
                CreatedBy = previousCountry.CreatedBy,
                CreatedOn = previousCountry.CreatedOn,
            };

            await _repository.Update(country);
            return Result<CountryDTO>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<CountryDTO>.Failure(data, $"unable to update the country: {ex.InnerException.Message}");
        }
    }

    public bool DoesCountryNameExist(string name)
    {
        try
        {
            return _repository.DoesCountryNameExist(name);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<Country>> GetAllCountriesAsync()
    {
        try
        {
            return await _repository.GetAllCountriesAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}