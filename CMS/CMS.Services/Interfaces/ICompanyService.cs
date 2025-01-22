using CMS.Application.DTOs;
using CMS.Application.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface ICompanyService
{
    Task<Result<CompanyDTO>> Insert(CompanyDTO data);
    Task<Result<List<CompanyDTO>>> GetAll();
    Task<Result<CompanyDTO>> Delete(int id);
    Task<Result<CompanyDTO>> GetById(int id);
    Task<Result<CompanyDTO>> Update(CompanyDTO data);
    bool DoesCompanyNameExist(string name, int countryId);
}
