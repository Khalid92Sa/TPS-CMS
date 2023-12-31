﻿using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CMS.Services.Services
{
    public class CompanyService: ICompanyService
    {
        ICompanyRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        public CompanyService(ICompanyRepository repository,
            IHttpContextAccessor httpContextAccessor,UserManager<IdentityUser> userManager)
        {
           _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
         
        }

        public async Task<Result<CompanyDTO>> Delete(int id)
        {
         
            try
            {
                await _repository.Delete(id);
                return Result<CompanyDTO>.Success(null);
            }

            catch (Exception ex)
            {
                return Result<CompanyDTO>.Failure(null, $"An error occurred while deleting the company{ex.InnerException.Message}");
            }
        }

        public async Task<Result<List<CompanyDTO>>> GetAll()
        {
            try
            {
                var companies=await _repository.GetAll();
            if(companies == null)
            {
                return Result<List<CompanyDTO>>.Failure(null, "No companies found");
            }
                
                var companyDTOs=new List<CompanyDTO>();
                foreach(var c in companies)
                {
                
                    var com = new CompanyDTO
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Address = c.Address,
                        Email = c.Email,
                        PhoneNumber = c.PhoneNumber,
                        CountryId = c.CountryId,
                        CountryName = c.Country.Name,
                       
                    };
                    companyDTOs.Add(com);

                }
                return Result<List<CompanyDTO>>.Success(companyDTOs);
           

            }
            catch(Exception ex)
            {
                return Result<List<CompanyDTO>>.Failure(null, $"Unable to get companies: {ex.InnerException.Message}");
            }

        }

        public async  Task<Result<CompanyDTO>> GetById(int id)
        {
            if (id <= 0)
            {
                return Result<CompanyDTO>.Failure(null, "Invalid company id");
            }
            try {
                var company = await _repository.GetById(id);
                var companyDTO = new CompanyDTO
                {
                    Id= company.Id,
                    Name = company.Name,
                    Address = company.Address,
                    Email = company.Email,
                    PhoneNumber = company.PhoneNumber,
                  //  CountryId = company.CountryId,
                    CountryId=company.CountryId,
                    CountryName= company.Country.Name,
                    
                };
                return Result<CompanyDTO>.Success(companyDTO);
            }
            catch (Exception ex)
            {
                return Result<CompanyDTO>.Failure(null, $"unable to retrieve the company from the repository{ex.InnerException.Message}");
            }
            
        }

        public async Task<Result<CompanyDTO>> Insert(CompanyDTO data)
        {
            if(data == null)
            {
                 return Result<CompanyDTO>.Failure(data, "the company DTO is null");
            }
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

            var company = new Company {
                Name = data.Name,
                Email = data.Email,
                Address = data.Address,
                PhoneNumber = data.PhoneNumber,
                CountryId= data.CountryId,
                CreatedBy= currentUser.Id,
                CreatedOn=DateTime.Now,
               
            };

            try
            {
                await _repository.Insert(company);
                
                return Result<CompanyDTO>.Success(data); 

            }
            catch (Exception ex)
            {
                return Result<CompanyDTO>.Failure(data,$"unable to insert a company: {ex.InnerException.Message}");

            }
          
        }

        public async Task<Result<CompanyDTO>> Update(CompanyDTO data)
        {
            if(data == null)
            {
                return Result<CompanyDTO>.Failure(data, "can not update a null object");
            }
            var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var previousCompany = await _repository.GetById(data.Id);
            var company = new Company
            {
                Id = data.Id,
                Name = data.Name,
                Address = data.Address,
                Email = data.Email,
                PhoneNumber= data.PhoneNumber,
                CountryId= data.CountryId,
                ModifiedOn= DateTime.Now,
                ModifiedBy = currentUser.Id,
                CreatedOn =previousCompany.CreatedOn,
                CreatedBy=previousCompany.CreatedBy
               
            };
          
            try
            {
                await _repository.Update(company);
                return Result<CompanyDTO>.Success(data);
            }
            catch (Exception ex)
            {
                return Result<CompanyDTO>.Failure(data, $"error updating the company {ex.Message}");
            }
        }



        public bool DoesCompanyNameExist(string name)
        {
            return _repository.DoesCompanyNameExist(name);
        }


    }
}
