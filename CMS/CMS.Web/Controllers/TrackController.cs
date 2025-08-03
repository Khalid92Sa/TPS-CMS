using CMS.Application.CustomRoleAuth;
using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("tracks")]
[AuthorizeRoles("Admin", "HR Manager")]
public class TrackController : Controller
{
    private readonly ITrackService _trackService;

    public TrackController(ITrackService trackService)
    {
        _trackService = trackService;
    }

    [HttpGet("")]
    [HttpGet("list")]
    public async Task<IActionResult> Index(string trackName, int pageNumber = 1, int pageSize = 10)
    {
            Result<List<TrackDTO>> result = await _trackService.GetAll();

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(new PaginatedList<TrackDTO>(new List<TrackDTO>(), 0, pageNumber, pageSize));
            }

            List<TrackDTO> filtered = [.. result.Value
                .Where(x => string.IsNullOrEmpty(trackName) || x.Name.Contains(trackName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Id)];

            List<TrackDTO> paginated = [.. filtered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)];

            ViewBag.trackNameFilter = trackName;

            PaginatedList<TrackDTO> paginatedList = new(paginated, filtered.Count, pageNumber, pageSize);

            return View(paginatedList);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(TrackDTO dto)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid input");
            return View(dto);
        }

        Result<TrackDTO> result = await _trackService.Create(dto);

        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError(string.Empty, result.Error);
        return View(dto);
    }

    [HttpGet("{id}/update")]
    public async Task<IActionResult> Update(int id)
    {
        Result<TrackDTO> result = await _trackService.GetById(id);

        if (!result.IsSuccess || result.Value == null)
            return NotFound();

        return View(result.Value);
    }

    [HttpPost("{id}/update")]
    public async Task<IActionResult> Update(TrackDTO dto)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid input");
            return View(dto);
        }

        Result<bool> result = await _trackService.Update(dto);

        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError(string.Empty, result.Error);
        return View(dto);
    }

    [HttpGet("{id}/delete")]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        Result<TrackDTO> result = await _trackService.GetById(id);

        if (!result.IsSuccess || result.Value == null)
            return NotFound();

        return View(result.Value);
    }

    [HttpPost("{id}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        Result<bool> result = await _trackService.Delete(id);

        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError(string.Empty, result.Error);
        return View();
    }

    [HttpGet("{id}/details")]
    public async Task<IActionResult> Details(int id)
    {
        Result<TrackDTO> result = await _trackService.GetById(id);

        if (result.IsSuccess && result.Value != null)
            return View(result.Value);

        return NotFound();
    }
}