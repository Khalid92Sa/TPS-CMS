using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("companies")]
public class CompanyController : Controller
{
    private readonly ICompanyService _companyService;
    private readonly ICountryService _countryService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CompanyController(
        ICompanyService companyService,
        ICountryService countryService,
        IHttpContextAccessor httpContextAccessor)
    {
        _companyService = companyService;
        _countryService = countryService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    [Route("create")]
    public async Task<IActionResult> AddCompany()
    {
        try
        {
            Result<List<CountryDTO>> CountriesDTOs = await _countryService.GetAll();
            ViewBag.CountriesDTOs = new SelectList(CountriesDTOs.Value, "Id", "Name");

            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> AddCompany(CompanyDTO companyDTO)
    {
        try
        {
            Result<List<CountryDTO>> CountriesDTOs = await _countryService.GetAll();
            ViewBag.CountriesDTOs = new SelectList(CountriesDTOs.Value, "Id", "Name");

            if (ModelState.IsValid)
            {
                if (_companyService.DoesCompanyNameExist(companyDTO.Name, companyDTO.CountryId))
                {
                    ModelState.AddModelError("Name", "A company with the same name already exists in the selected country.");
                    return View(companyDTO);
                }

                Result<CompanyDTO> result = await _companyService.Insert(companyDTO);

                if (result.IsSuccess)
                    return RedirectToAction(nameof(GetCompanies));

                ModelState.AddModelError(string.Empty, result.Error);
            }
            else
                ModelState.AddModelError(string.Empty, "error validating the model");

            return View(companyDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("getCompanies")]
    public async Task<IActionResult> GetCompanies(string companyName, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
                ViewBag.companyNameFilter = companyName;

                Result<List<CompanyDTO>> result = await _companyService.GetAll();

                if (result.IsSuccess)
                {
                    List<CompanyDTO> companiesDTOs = [.. result.Value
                                                               .Where(x => string.IsNullOrEmpty(companyName) || x.Name.Contains(companyName, StringComparison.OrdinalIgnoreCase))
                                                               .OrderByDescending(x => x.CreatedOn)
                                                     ];

                    List<CompanyDTO> paginatedCompanies = companiesDTOs
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    PaginatedList<CompanyDTO> paginatedList = new(paginatedCompanies, companiesDTOs.Count, pageNumber, pageSize);

                    return View(paginatedList);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.Error);
                    return View();
                }
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");
                else
                    return RedirectToAction("login", "users");
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("{id}/delete")]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        try
        {
            if (id <= 0)
                return NotFound();

            Result<CompanyDTO> result = await _companyService.GetById(id);
            CompanyDTO companyDTO = result.Value;

            if (companyDTO is null)
                return NotFound();

            return View(companyDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/delete")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest("Invalid company id");

                Result<CompanyDTO> result = await _companyService.Delete(id);

                if (result.IsSuccess)
                    return RedirectToAction(nameof(GetCompanies));

                ModelState.AddModelError(string.Empty, result.Error);
            
            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("{id}/update")]
    public async Task<IActionResult> UpdateCompany(int id)
    {
        try
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Result<CompanyDTO> result = await _companyService.GetById(id);
            CompanyDTO companyDTO = result.Value;

            if (companyDTO is null)
                return NotFound();

            Result<List<CountryDTO>> CountriesDTOs = await _countryService.GetAll();
            ViewBag.CountriesDTOs = new SelectList(CountriesDTOs.Value, "Id", "Name");

            return View(companyDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/update")]
    public async Task<IActionResult> UpdateCompany(CompanyDTO companyDTO)
    {
        try
        {
            if (companyDTO is null)
            {
                ModelState.AddModelError(string.Empty, $"The company DTO you are trying to update is null.");
                return RedirectToAction("Index");
            }

            Result<List<CountryDTO>> CountriesDTOs = await _countryService.GetAll();
            ViewBag.CountriesDTOs = new SelectList(CountriesDTOs.Value, "Id", "Name");

            if (ModelState.IsValid)
            {
                Result<CompanyDTO> result = await _companyService.Update(companyDTO);

                if (result.IsSuccess)
                    return RedirectToAction(nameof(GetCompanies));

                ModelState.AddModelError(string.Empty, result.Error);
                return View(companyDTO);
            }
            else
                ModelState.AddModelError(string.Empty, $"The model state is not valid.");

            return View(companyDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/details")]
    public async Task<ActionResult> Details(int id)
    {
        try
        {
            Result<CompanyDTO> result = await _companyService.GetById(id);

            if (result.IsSuccess)
                return View(result.Value);
            else
                return NotFound();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    public IActionResult CheckCompanyName([FromBody] CompanyDTO companyDTO)
    {
        try
        {
            if (companyDTO != null)
            {
                bool exists = _companyService.DoesCompanyNameExist(companyDTO.Name, companyDTO.CountryId);
                return Ok(new { exists });
            }

            return BadRequest("Invalid data");
        }
        catch (Exception)
        {
            throw;
        }
    }
}