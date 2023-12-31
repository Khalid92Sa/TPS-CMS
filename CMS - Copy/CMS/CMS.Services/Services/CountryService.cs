﻿using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Implementation;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services
{
    public class CountryService:ICountryService
    {
        ICountryRepository _repository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CountryService(ICountryRepository repository,
            IHttpContextAccessor httpContextAccessor,UserManager<IdentityUser> userManager)
        {
            _repository = repository;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IEnumerable<Country>> GetAllCountriesAsync()
        {
            return await _repository.GetAllCountriesAsync();
        }

        public async Task<Country> GetCountryByNameAsync(string countryName)
        {
            return await _repository.GetCountryByNameAsync(countryName);
        }

        public async Task<Result<CountryDTO>> Delete(int id)
        {
            try
            {
                await _repository.Delete(id);
                return Result<CountryDTO>.Success(null);
            }
            catch (Exception ex)
            {
                return Result<CountryDTO>.Failure(null, $"An error occurred while deleting the country{ex.InnerException.Message}");
            }
        }

        public async Task<Result<List<CountryDTO>>> GetAll()
        {
            var countries=await _repository.GetAll();
            if(countries == null)
            {
                return Result<List<CountryDTO>>.Failure(null,"no countries found");
            }
            try
            {
                var countryDTOS = new List<CountryDTO>();
                foreach (var co in countries)
                {
                    countryDTOS.Add(new CountryDTO
                    {
                        Id = co.Id,
                        Name = co.Name,
                        companyDTOs = co.Companies.Select(com => new CompanyDTO
                        {
                            Id = com.Id,
                            Name = com.Name,
                            Email = com.Email,
                            Address = com.Address,
                            CountryId = com.CountryId,
                            PhoneNumber = com.PhoneNumber,
                            CountryName=com.Country.Name

                        }).ToList()
                    });
                    
                }
                return Result< List < CountryDTO >>.Success(countryDTOS);
            }
            catch(Exception ex)
            {
                return Result<List<CountryDTO>>.Failure(null, $"unable to get countries{ex.InnerException.Message}");
            }

        }

        public async Task<Result<CountryDTO>> GetById(int id)
        {
            if (id <= 0)
            {
                return Result<CountryDTO>.Failure(null, "Invalid company id");
            }
            try
            {
                var country=await _repository.GetById(id);
                var countryDTOS = new CountryDTO
                {
                    Id = country.Id,
                    Name = country.Name,
                    companyDTOs = country.Companies.Select(com => new CompanyDTO
                    {
                        Id = com.Id,
                        Name = com.Name,
                        Email = com.Email,
                        Address = com.Address,
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
            if(data == null)
            {
                return Result<CountryDTO>.Failure(data,"the country dto is null");
            }
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var country = new Country
            {
                Name = data.Name,
                CreatedBy= currentUser.Id,
                CreatedOn=DateTime.Now,
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
            if (data == null)
            {
                return Result<CountryDTO>.Failure(null, "can not update a null object");
            }
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var previousCountry=await _repository.GetById(data.Id);
            var country=new Country { 
                Name = data.Name,
                Id=data.Id,
                ModifiedBy= currentUser.Id,
                ModifiedOn=DateTime.Now,
                CreatedBy=previousCountry.CreatedBy,
                CreatedOn=previousCountry.CreatedOn,
            
            };
            try
            {
                await _repository.Update(country);
                return Result<CountryDTO>.Success(data);
            }
            catch (Exception ex)
            {
                return Result<CountryDTO>.Failure(data, $"unable to update the country: {ex.InnerException.Message}");
            }
        }
        public async Task<Result<CountryDTO>> GetCountryByName(string countryName)
        {
            try
            {
                var country = await _repository.GetCountryByNameAsync(countryName);

                if (country == null)
                {
                    return Result<CountryDTO>.Failure(null, "Country not found");
                }

                // Map the retrieved country to a DTO as needed
                var countryDTO = new CountryDTO
                {
                    Id = country.Id,
                    Name = country.Name,
                    // ...
                };

                return Result<CountryDTO>.Success(countryDTO);
            }
            catch (Exception ex)
            {
                return Result<CountryDTO>.Failure(null, $"An error occurred while getting the country by name: {ex.InnerException.Message}");
            }
        }
        public bool DoesCountryNameExist(string name)
        {
            return _repository.DoesCountryNameExist(name);
        }



    }
}
