using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using CMS.Services.Interfaces;
using CMS.Web.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("positions")]
public class PositionController : Controller
{
    private readonly IPositionService _positionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _attachmentStoragePath;

    public PositionController(
        IPositionService positionService,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor)
    {
        _positionService = positionService;
        _httpContextAccessor = httpContextAccessor;
        _attachmentStoragePath = Path.Combine(env.WebRootPath, "attachments");

        if (!Directory.Exists(_attachmentStoragePath))
            Directory.CreateDirectory(_attachmentStoragePath);
    }

    [Route("create")]
    public IActionResult AddPosition()
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
    public async Task<IActionResult> AddPosition(PositionDTO positionDTO, IFormFile file)
    {
        try
        {
            if (ModelState.IsValid)
            {
                if (_positionService.DoesPositionNameExist(positionDTO.Name))
                {
                    ModelState.AddModelError("Name", "A position with the same name already exists.");
                    return View(positionDTO);
                }

                FileStream attachmentStream = null;
                if (file != null && file.Length != 0)
                {
                    attachmentStream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
                    positionDTO.FileName = file.FileName;
                    positionDTO.FileSize = file.Length;
                    positionDTO.FileData = attachmentStream;
                }


                Result<PositionDTO> result = await _positionService.Insert(positionDTO);
                if (attachmentStream != null)
                {
                    attachmentStream.Close();
                    attachmentStream.Dispose();
                    AttachmentHelper.removeFile(file.FileName, _attachmentStoragePath);
                }

                if (result.IsSuccess)
                    return RedirectToAction(nameof(GetPositions));

                ModelState.AddModelError(string.Empty, result.Error);
            }
            else
                ModelState.AddModelError(string.Empty, "error validating the model");

            return View(positionDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("getPositions")]
    public async Task<IActionResult> GetPositions(string positionName, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
                ViewBag.positionNameFilter = positionName;

                Result<IEnumerable<PositionDTO>> result = await _positionService.GetAll();

                if (result.IsSuccess)
                {
                    List<PositionDTO> positionsDTOs = [.. result.Value
                                                                .Where(x => string.IsNullOrEmpty(positionName) || x.Name.Contains(positionName, StringComparison.OrdinalIgnoreCase))
                                                                .OrderByDescending(x => x.CreatedOn)
                                                      ];

                    List<PositionDTO> paginatedPositions = positionsDTOs
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    PaginatedList<PositionDTO> paginatedList = new(paginatedPositions, positionsDTOs.Count, pageNumber, pageSize);
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
    public async Task<IActionResult> GetDeleteConfirmation(int id)
    {
        try
        {
            if (id <= 0)
                return NotFound();

            Result<PositionDTO> result = await _positionService.GetById(id);
            PositionDTO positionDTO = result.Value;

            if (positionDTO == null)
                return NotFound();

            return View(positionDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/delete")]
    public async Task<IActionResult> DeletePosition(PositionDTO positionDTO)
    {
        try
        {
            if (positionDTO is null || positionDTO.Id <= 0)
                return BadRequest("invalid position id");

            Result<PositionDTO> result = await _positionService.Delete(positionDTO.Id);
            
            if (result.IsSuccess)
                return RedirectToAction(nameof(GetPositions));

            ModelState.AddModelError(string.Empty, result.Error);
            return View("DeleteConfirmation", positionDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet]
    [Route("{id}/update")]
    public async Task<IActionResult> UpdatePosition(int id)
    {
        try
        {
            if (id <= 0)
                return NotFound();

            Result<PositionDTO> result = await _positionService.GetById(id);
            PositionDTO positionDTO = result.Value;
            
            if (positionDTO is null)
                return NotFound();
            
            return View(positionDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/update")]
    public async Task<IActionResult> UpdatePosition(PositionDTO positionDTO)
    {
        try
        {
            if (ModelState.IsValid)
            {
                Result<PositionDTO> result = await _positionService.Update(positionDTO);

                if (result.IsSuccess)
                    return RedirectToAction(nameof(GetPositions));
            
                ModelState.AddModelError(string.Empty, result.Error);
                return RedirectToAction(nameof(GetPositions));
            }
            else
                ModelState.AddModelError(string.Empty, $"the model state is not valid");
         
            return View(positionDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/updateAttachment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttachment(int id, IFormFile file)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Please choose a file to upload.");
                return View();
            }
            
            if (ModelState.IsValid)
            {
                FileStream stream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
                try
                {
                    await _positionService.UpdatePositionEvaluationAsync(id, file.FileName, file.Length, stream);
                    return RedirectToAction(nameof(GetPositions));
                }
                finally
                {
                    stream.Close();
                    AttachmentHelper.removeFile(file.FileName, _attachmentStoragePath);
                }

            }
            return RedirectToAction(nameof(UpdatePosition), new { id = id });
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    public IActionResult CheckPositionName([FromBody] string name)
    {
        try
        {
            bool exists = _positionService.DoesPositionNameExist(name);
            return Ok(new { exists });
        }
        catch (Exception)
        {
            throw;
        }
    }
}