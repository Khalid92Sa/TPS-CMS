﻿using CMS.Application.DTOs;
using CMS.Services.Interfaces;
using CMS.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CMS.Web.Controllers
{
    public class CountryController : Controller
    {
        //

        private readonly ICountryService _countryService;
        public CountryController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        public void LogException(string methodName, Exception ex, string additionalInfo = null)
        {
            var createdByUserId = GetUserId();
            _countryService.LogException(methodName, ex, createdByUserId, additionalInfo);
        }
        public string GetUserId()
        {
            try
            {
                var userId = _countryService.GetUserId();
                return userId;
            }
            catch (Exception ex)
            {
                LogException(nameof(GetUserId), ex, null);
                throw ex;
            }
        }
        public IActionResult Index()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(Index), ex, null);
                throw ex;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            try
            {

            
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
                var result = await _countryService.GetAll(GetUserId());
                if (result.IsSuccess)
                {
                    var CountriesDTOs = result.Value;

                    return View(CountriesDTOs);
                }
                else
                {
                    ModelState.AddModelError("", result.Error);
                    return View();
                }
            }
            else
            {
                return View("AccessDenied");
            }
            }
            catch(Exception ex)
            {
                LogException(nameof(GetCountries), ex, null);
                throw ex;
            }
        }

        [HttpGet]
        public IActionResult AddCountry()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(AddCountry), ex, null);
                throw ex;
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddCountry(CountryDTO countryDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_countryService.DoesCountryNameExist(countryDTO.Name))
                    {
                        ModelState.AddModelError("Name", "A country with the same name already exists.");
                        return View(countryDTO);
                    }

                    var result = await _countryService.Insert(countryDTO, GetUserId());

                    if (result.IsSuccess)
                    {
                        return RedirectToAction("GetCountries");
                    }

                    ModelState.AddModelError("", result.Error);
                }
                else
                {
                    ModelState.AddModelError("", "Error validating the model");
                }

                return View(countryDTO);
            }
            catch (Exception ex)
            {
                LogException(nameof(AddCountry), ex, "Faild to add country");
                throw ex;
            }
        }


        [HttpGet]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            try
            {

            
            if (id <= 0)
            {
                return NotFound();
            }

            var result = await _countryService.GetById(id, GetUserId());
            var countryDTO = result.Value;

            if (countryDTO == null)
            {
                return NotFound();
            }

            return View(countryDTO);
            }
            catch (Exception ex)
            {
                LogException(nameof(ConfirmDelete), ex, "Faild to load delete page of the country");
                throw ex;
            }
        }



        [HttpPost]
        public IActionResult DeleteCountry(int id)
        {
            try
            {

            
            if (id <= 0)
            {
                return BadRequest("Invalid country id");
            }

            if (HttpContext.Request.Method == "POST")
            {
                // Handle the actual deletion
                var result = _countryService.Delete(id, GetUserId());

                if (result.IsSuccess)
                {
                    return RedirectToAction("GetCountries");
                }

                ModelState.AddModelError("", result.Error);
            }
            else
            {
                // For GET requests, show the confirmation page
                return RedirectToAction("ConfirmDelete", new { id });
            }

            return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(DeleteCountry), ex, "Faild to delete country");
                throw ex;
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCountry(int id)
        {
            try
            {

            
            if (id <= 0)
            {
                return NotFound();
            }
            var result = await _countryService.GetById(id, GetUserId());
            var countryDTO = result.Value;
            if (countryDTO == null)
            {
                return NotFound();
            }
            return View(countryDTO);
            }
            catch (Exception ex)
            {
                LogException(nameof(UpdateCountry), ex, "Faild to laod edit page in country");
                throw ex;
            }

        }

        [HttpPost]
        public async Task<IActionResult> UpdateCountry(CountryDTO countryDTO)
        {
            try
            {

            
            if (ModelState.IsValid)
            {
                var result = await _countryService.Update(countryDTO, GetUserId());

                if (result.IsSuccess)
                {
                    return RedirectToAction("GetCountries");
                }
                ModelState.AddModelError("", result.Error);
                return View(countryDTO);  
            }
            else
            {
                ModelState.AddModelError("", $"the model state is not valid");
            }
            return View(countryDTO);
            }
            catch (Exception ex)
            {
                LogException(nameof(UpdateCountry), ex, "Faild to edit country");
                throw ex;
            }

        }
        [HttpGet]
        public async Task<IActionResult> ShowCompanies(int id)
        {
            try
            {

            
            var result = await _countryService.GetById(id, GetUserId());
            if (result.IsSuccess)
            {
                var countryDTO = result.Value;
                return View(countryDTO);
            }
           
           
             else
            {
                ModelState.AddModelError("", result.Error);
                return View();
            }
            }
            catch (Exception ex)
            {
                LogException(nameof(ShowCompanies), ex, "Faild to Show Companies details");
                throw ex;
            }
        }



        [HttpPost]
        public IActionResult CheckCountryName([FromBody] string name)
        {
            try
            {

            
            bool exists = _countryService.DoesCountryNameExist(name);
            return Ok(new { exists });
            }
            catch (Exception ex)
            {
                LogException(nameof(CheckCountryName), ex, "Faild to Check Country Name validation");
                throw ex;
            }
        }



    }
}
