using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Application.Helpers;
using CMS.Domain.Entities;
using CMS.Services.Interfaces;
using CMS.Web.Customes;
using CMS.Web.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

[Route("candidates")]
public class CandidatesController : Controller
{
    private readonly ICandidateService _candidateService;
    private readonly string _attachmentStoragePath;
    private readonly IPositionService _positionService;
    private readonly ICompanyService _companyService;
    private readonly ICountryService _countryService;
    private readonly ITrackService _trackService;

    public CandidatesController(
        ICandidateService candidateService,
        IWebHostEnvironment env,
        IPositionService positionService,
        ICompanyService companyService,
        ICountryService countryService,
        ITrackService trackService)
    {
        _candidateService = candidateService;
        _attachmentStoragePath = Path.Combine(env.WebRootPath, "attachments");

        if (!Directory.Exists(_attachmentStoragePath))
            Directory.CreateDirectory(_attachmentStoragePath);

        _positionService = positionService;
        _companyService = companyService;
        _countryService = countryService;
        _trackService = trackService;
    }


    [Route("index")]
    public async Task<IActionResult> Index(string FullName, string Phone, int? trackFilter, int pageNumber = 1, int pageSize = 5)
    {
        try
        {
            ViewBag.candidateFilter = FullName;

            if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
            {
                IEnumerable<CandidateDTO> candidates = await _candidateService.GetAllCandidatesAsync();

                Result<List<TrackDTO>> tracks = await _trackService.GetAll();
                ViewBag.TrackList = new SelectList(tracks.Value.OrderBy(x => x.Name), "Id", "Name");

                if (!string.IsNullOrEmpty(Phone))
                    candidates = candidates.Where(i => i.Phone.ToString().Contains(Phone))
                                           .ToList();

                if (!string.IsNullOrEmpty(FullName))
                    candidates = candidates.Where(i => i.FullName.Contains(FullName, StringComparison.OrdinalIgnoreCase))
                                           .ToList();

                if (trackFilter.HasValue && trackFilter.Value > 0)
                    candidates = candidates.Where(i => i.TrackId == trackFilter.Value)
                                           .ToList();

                List<CandidateDTO> candidatesList = candidates.ToList();

                IEnumerable<CandidateDTO> filteredcandidates = candidates.OrderByDescending(i => i.CreatedOn);
                                               
                int totalCount = filteredcandidates.Count();

                List<CandidateDTO> paginatedCandidates = filteredcandidates
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

                PaginatedList<CandidateDTO> paginatedList = new(paginatedCandidates, totalCount, pageNumber, pageSize);

                return View(paginatedList);
            }

            else if (User.Identity.IsAuthenticated)
                return View("AccessDenied");

            else
                return Redirect(Url.Action("login", "users"));
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/details")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            Result<List<CountryDTO>> Country = await _countryService.GetAll();
            ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

            Result<IEnumerable<PositionDTO>> Position = await _positionService.GetAll();
            ViewBag.positions = new SelectList(Position.Value, "Id", "Name");

            CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(id);

            if (candidate is null)
                return NotFound();

            return View(candidate);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("create")]
    public async Task<IActionResult> Create()
    {
        try
        {
            Result<IEnumerable<PositionDTO>> positions = await _positionService.GetAll();
            ViewBag.positions = new SelectList(positions.Value.OrderBy(x => x.Name), "Id", "Name");

            Result<List<CompanyDTO>> CompaniesDTOs = await _companyService.GetAll();
            ViewBag.CompaniesDTOs = new SelectList(CompaniesDTOs.Value, "Id", "Name");

            Result<List<CountryDTO>> Country = await _countryService.GetAll();
            ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

            Result<List<TrackDTO>> tracks = await _trackService.GetAll();
            ViewBag.Tracks = new SelectList(tracks.Value.OrderBy(x => x.Name), "Id", "Name");

            return View();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CandidateCreateDTO candidateDTO, IFormFile file)
    {
        try
        {
            Result<IEnumerable<PositionDTO>> positions = await _positionService.GetAll();
            ViewBag.positions = new SelectList(positions.Value, "Id", "Name");

            Result<List<CompanyDTO>> CompaniesDTOs = await _companyService.GetAll();
            ViewBag.CompaniesDTOs = new SelectList(CompaniesDTOs.Value, "Id", "Name");

            Result<List<CountryDTO>> Country = await _countryService.GetAll();
            ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

            Result<List<TrackDTO>> tracks = await _trackService.GetAll();
            ViewBag.Tracks = new SelectList(tracks.Value, "Id", "Name");

            FileStream attachmentStream = null;

            if (file?.Length > 0)
            {
                // Check file extension and size
                string[] allowedExtensions = new[] { ".pdf", ".docx", ".png", ".jpg" };
                const int maxFileSize = 9 * 1024 * 1024; // 9MB

                string fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                    ModelState.AddModelError("File", "Invalid file format. Allowed formats are PDF, DOCX, PNG, and JPG.");

                else if (file.Length > maxFileSize)
                    ModelState.AddModelError("File", "File size exceeds the maximum allowed size (9MB).");

                attachmentStream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
                candidateDTO.FileName = file.FileName;
                candidateDTO.FileSize = file.Length;
                candidateDTO.FileData = attachmentStream;
            }

            if (ModelState.IsValid)
            {
                candidateDTO.FullName = StringHelper.ToUpperFirstLetter(candidateDTO.FullName);

                try
                {
                    await _candidateService.CreateCandidateAsync(candidateDTO);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (attachmentStream != null)
                    {
                        attachmentStream.Close();
                        await attachmentStream.DisposeAsync(); // Dispose the stream to release the file
                        AttachmentHelper.removeFile(file.FileName, _attachmentStoragePath);
                    }
                }
            }

            else
                ModelState.AddModelError(string.Empty, string.Empty);

            if (attachmentStream != null)
            {
                attachmentStream.Close();
                await attachmentStream.DisposeAsync();
            }

            return View(candidateDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/update")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(id);

            if (candidate is null)
                return NotFound();

            Result<IEnumerable<PositionDTO>> positions = await _positionService.GetAll();
            ViewBag.positions = new SelectList(positions.Value.OrderBy(x => x.Name), "Id", "Name");

            Result<List<CompanyDTO>> CompaniesDTOs = await _companyService.GetAll();
            ViewBag.CompaniesDTOs = new SelectList(CompaniesDTOs.Value, "Id", "Name");

            Result<List<CountryDTO>> Country = await _countryService.GetAll();
            ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

            Result<List<TrackDTO>> tracks = await _trackService.GetAll();
            ViewBag.Tracks = new SelectList(tracks.Value.OrderBy(x => x.Name), "Id", "Name");

            return View(candidate);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [Route("{id}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CandidateDTO candidateDTO, IFormFile file)
    {
        try
        {
            if (id != candidateDTO.Id)
                return NotFound();

            Result<IEnumerable<PositionDTO>> positions = await _positionService.GetAll();
            ViewBag.positions = new SelectList(positions.Value, "Id", "Name");

            Result<List<CompanyDTO>> CompaniesDTOs = await _companyService.GetAll();
            ViewBag.CompaniesDTOs = new SelectList(CompaniesDTOs.Value, "Id", "Name");

            Result<List<CountryDTO>> Country = await _countryService.GetAll();
            ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

            Result<List<TrackDTO>> tracks = await _trackService.GetAll();
            ViewBag.Tracks = new SelectList(tracks.Value, "Id", "Name");

            if (file?.Length > 0)
            {
                string[] allowedExtensions = new[] { ".pdf", ".docx", ".png", ".jpg" };
                int maxFileSize = 9 * 1024 * 1024; // 4MB

                string fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                    ModelState.AddModelError("File", "Invalid file format. Allowed formats are PDF, DOCX, PNG, and JPG.");

                else if (file.Length < 1024 * 1024) // 1MB
                    ModelState.AddModelError("File", "File size is too small. Minimum size allowed is 1MB.");

                else if (file.Length > maxFileSize)
                    ModelState.AddModelError("File", "File size exceeds the maximum allowed size (9MB).");

                if (ModelState.IsValid)
                {
                    FileStream stream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
                    try
                    {
                        await _candidateService.UpdateCandidateCVAsync(id, file.FileName, file.Length, stream);
                    }
                    finally
                    {
                        stream.Close();
                        AttachmentHelper.removeFile(file.FileName, _attachmentStoragePath);
                    }
                }
            }
            else if (ModelState.IsValid) // No file uploaded, but other data is valid
                await _candidateService.UpdateCandidateAsync(id, candidateDTO);

            return RedirectToAction(nameof(Index));
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
                    await _candidateService.UpdateCandidateCVAsync(id, file.FileName, file.Length, stream);
                    return RedirectToAction(nameof(Index));
                }
                finally
                {
                    stream.Close();
                    AttachmentHelper.removeFile(file.FileName, _attachmentStoragePath);
                }

            }
            return RedirectToAction(nameof(Edit), new { id = id });
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Route("{id}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            CandidateDTO candidate = await _candidateService.GetCandidateByIdAsync(id);

            if (candidate is null)
                return NotFound();

            return View(candidate);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost, ActionName("Delete")]
    [Route("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _candidateService.DeleteCandidateAsync(id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            throw;
        }
    }
}