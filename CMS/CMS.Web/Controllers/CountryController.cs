using CMS.Application.CustomRoleAuth;
using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using CMS.Services.Interfaces;
using CMS.Web.Customes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("countries")]
[AuthorizeRoles("Admin", "HR Manager")]
public class CountryController : Controller
{
    private readonly ICountryService _countryService;

    public CountryController(ICountryService countryService) => _countryService = countryService;

    [HttpGet]
    [Route("getCountries")]
    public async Task<IActionResult> GetCountries(string countryName, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
                ViewBag.countryNameFilter = countryName;

                Result<List<CountryDTO>> result = await _countryService.GetAll();

                if (result.IsSuccess)
                {
                    List<CountryDTO> CountriesDTOs = [.. result.Value
                                                               .Where(x => string.IsNullOrEmpty(countryName) || x.Name.Contains(countryName, StringComparison.OrdinalIgnoreCase))
                                                               .OrderByDescending(x => x.CreatedOn)
                                                     ];

                    List<CountryDTO> paginatedCountries = CountriesDTOs
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    PaginatedList<CountryDTO> paginatedList = new(paginatedCountries, CountriesDTOs.Count, pageNumber, pageSize);

                    return View(paginatedList);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.Error);
                    return View();
                }
        }
        catch (Exception)
        {
            throw;
        }
    }


    [HttpGet]
    [Route("create")]
    public IActionResult AddCountry()
    {
        try
        {
            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> AddCountry(CountryDTO countryDTO)
    {
        try
        {
            if (ModelState.IsValid)
            {
                countryDTO.Name = StringHelper.ToUpperFirstLetter(countryDTO.Name);

                if (_countryService.DoesCountryNameExist(countryDTO.Name))
                {
                    ModelState.AddModelError("Name", "A country with the same name already exists.");
                    return View(countryDTO);
                }

                Result<CountryDTO> result = await _countryService.Insert(countryDTO);

                if (result.IsSuccess)
                    return RedirectToAction(nameof(GetCountries));

                ModelState.AddModelError(string.Empty, result.Error);
            }
            else
                ModelState.AddModelError(string.Empty, "Error validating the model");

            return View(countryDTO);
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

            Result<CountryDTO> result = await _countryService.GetById(id);
            CountryDTO countryDTO = result.Value;

            if (countryDTO is null)
                return NotFound();

            return View(countryDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/delete")]
    public IActionResult DeleteCountry(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest("Invalid country id");

                Result<CountryDTO> result = _countryService.Delete(id);

                if (result.IsSuccess)
                    return RedirectToAction(nameof(GetCountries));

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
    public async Task<IActionResult> UpdateCountry(int id)
    {
        try
        {
            if (id <= 0)
                return NotFound();

            Result<CountryDTO> result = await _countryService.GetById(id);
            CountryDTO countryDTO = result.Value;

            if (countryDTO is null)
                return NotFound();

            return View(countryDTO);
        }
        catch (Exception)
        {
            throw;
        }

    }

    [HttpPost]
    [Route("{id}/update")]
    public async Task<IActionResult> UpdateCountry(CountryDTO countryDTO)
    {
        try
        {
            if (ModelState.IsValid)
            {
                Result<CountryDTO> result = await _countryService.Update(countryDTO);

                if (result.IsSuccess)
                    return RedirectToAction(nameof(GetCountries));

                ModelState.AddModelError(string.Empty, result.Error);
                return View(countryDTO);
            }
            else
                ModelState.AddModelError(string.Empty, $"the model state is not valid");

            return View(countryDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("{id}/details")]
    public async Task<IActionResult> ShowCompanies(int id)
    {
        try
        {
            Result<CountryDTO> result = await _countryService.GetById(id);
            if (result.IsSuccess)
            {
                CountryDTO countryDTO = result.Value;
                return View(countryDTO);
            }

            else
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    public IActionResult CheckCountryName(string name)
    {
        try
        {
            bool exists = _countryService.DoesCountryNameExist(name);
            return Ok(new { exists });
        }
        catch (Exception)
        {
            throw;
        }
    }
}