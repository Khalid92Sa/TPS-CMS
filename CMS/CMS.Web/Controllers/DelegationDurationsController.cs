using CMS.Application.CustomRoleAuth;
using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("delegation-durations")]
[AuthorizeRoles("Admin", "HR Manager")]
public class DelegationDurationsController : Controller
{
    private readonly IDelegationDurationService _durationService;

    public DelegationDurationsController(IDelegationDurationService durationService)
    {
        _durationService = durationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        Result<List<DelegationDurationDTO>> result = await _durationService.GetAllAsync();
        if (!result.IsSuccess)
        {
            ViewBag.ErrorMessage = result.Error;
            return View(new List<DelegationDurationDTO>());
        }

        return View(result.Value);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        PopulateUnits();
        return View(new DelegationDurationDTO { IsActive = true, Unit = DelegationDuration.UnitHours });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DelegationDurationDTO dto)
    {
        PopulateUnits();
        if (!ModelState.IsValid)
            return View(dto);

        Result<DelegationDurationDTO> result = await _durationService.CreateAsync(dto);
        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError(string.Empty, result.Error);
        return View(dto);
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        Result<DelegationDurationDTO> result = await _durationService.GetByIdAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        PopulateUnits();
        return View(result.Value);
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DelegationDurationDTO dto)
    {
        dto.Id = id;
        PopulateUnits();
        if (!ModelState.IsValid)
            return View(dto);

        Result<DelegationDurationDTO> result = await _durationService.UpdateAsync(dto);
        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError(string.Empty, result.Error);
        return View(dto);
    }

    [HttpPost("{id}/active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        Result<bool> result = await _durationService.SetActiveAsync(id, isActive);
        if (!result.IsSuccess)
            TempData["ErrorMessage"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    private void PopulateUnits()
    {
        ViewBag.Units = new List<SelectListItem>
        {
            new(DelegationDuration.UnitMinutes, DelegationDuration.UnitMinutes),
            new(DelegationDuration.UnitHours, DelegationDuration.UnitHours),
            new(DelegationDuration.UnitDays, DelegationDuration.UnitDays),
            new(DelegationDuration.UnitWeeks, DelegationDuration.UnitWeeks)
        };
    }
}
