using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace CMS.Web.Controllers;

public class StatusController : Controller
{
    private readonly IStatusService _statusService;

    public StatusController(IStatusService statusService) => _statusService = statusService;

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(StatusDTO statusDTO)
    {
        if (ModelState.IsValid)
        {
            Result<StatusDTO> result = await _statusService.Insert(statusDTO);

            if (result.IsSuccess)
                return RedirectToAction("Get");

            ModelState.AddModelError(string.Empty, result.Error);
        }
        else
            ModelState.AddModelError(string.Empty, "error validating the model");

        return View(statusDTO);
    }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        Result<List<StatusDTO>> result = await _statusService.GetAll();
        
        if (result.IsSuccess)
        {
            List<StatusDTO> StatusDTOs = result.Value;
            return View(StatusDTOs);
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View();
        }
    }
}