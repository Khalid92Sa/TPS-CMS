using CMS.Application.DTOs;
using CMS.Services.Interfaces;
using CMS.Web.Customes;
using CMS.Web.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers
{
    public class CandidatesController : Controller
    {
        private readonly ICandidateService _candidateService;
        private readonly string _attachmentStoragePath;
        private readonly IPositionService _positionService;
        private readonly ICompanyService _companyService;
        private readonly ICountryService _countryService;
        private readonly ITrackService _trackService;

        public CandidatesController(ICandidateService candidateService,
                                    IWebHostEnvironment env,
                                    IPositionService positionService,
                                    ICompanyService companyService,
                                    ICountryService countryService,
                                    ITrackService trackService)
        {
            _candidateService = candidateService;
            _attachmentStoragePath = Path.Combine(env.WebRootPath, "attachments");

            if (!Directory.Exists(_attachmentStoragePath))
            {
                Directory.CreateDirectory(_attachmentStoragePath);
            }
            _positionService = positionService;
            _companyService = companyService;
            _countryService = countryService;
            _trackService = trackService;
        }

        public void LogException(string methodName, Exception ex, string additionalInfo = null) => _candidateService.LogException(methodName, ex, additionalInfo);

        public async Task<IActionResult> Index(string FullName, string Phone, int? trackFilter)
        {
            try
            {
                ViewBag.candidateFilter = FullName;

                if (User.IsInRole("Admin") || User.IsInRole("HR Manager"))
                {
                    var candidates = await _candidateService.GetAllCandidatesAsync();

                    var tracks = await _trackService.GetAll();
                    ViewBag.TrackList = new SelectList(tracks.Value, "Id", "Name");

                    if (!string.IsNullOrEmpty(Phone))
                        candidates = candidates
                            .Where(i => i.Phone.ToString().Contains(Phone))
                            .ToList();

                    if (!string.IsNullOrEmpty(FullName))
                        candidates = candidates
                            .Where(i => i.FullName.Contains(FullName, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                    if (trackFilter.HasValue && trackFilter.Value > 0)
                        candidates = candidates
                            .Where(i => i.TrackId == trackFilter.Value)
                            .ToList();

                    return View(candidates);
                }
                else if (User.Identity.IsAuthenticated)
                    return View("AccessDenied");

                else
                    return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                LogException(nameof(Index), ex, "Error in CandidatesController.Index");
                throw;
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var Country = await _countryService.GetAll();
                ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

                var Position = await _positionService.GetAll();
                ViewBag.positions = new SelectList(Position.Value, "Id", "Name");

                var candidate = await _candidateService.GetCandidateByIdAsync(id);

                if (candidate is null)
                    return NotFound();

                return View(candidate);
            }
            catch (Exception ex)
            {
                LogException(nameof(Details), ex, $"Error in CandidatesController.Details for ID: {id}");
                throw;
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                var positions = await _positionService.GetAll();
                ViewBag.positions = new SelectList(positions.Value, "Id", "Name");

                var CompaniesDTOs = await _companyService.GetAll();
                ViewBag.CompaniesDTOs = new SelectList(CompaniesDTOs.Value, "Id", "Name");

                var Country = await _countryService.GetAll();
                ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

                var tracks = await _trackService.GetAll();
                ViewBag.Tracks = new SelectList(tracks.Value, "Id", "Name");

                return View();
            }
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, "Error in CandidatesController.Create (GET)");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CandidateCreateDTO candidateDTO, IFormFile file)
        {
            try
            {
                var positions = await _positionService.GetAll();
                ViewBag.positions = new SelectList(positions.Value, "Id", "Name");

                var CompaniesDTOs = await _companyService.GetAll();
                ViewBag.CompaniesDTOs = new SelectList(CompaniesDTOs.Value, "Id", "Name");

                var Country = await _countryService.GetAll();
                ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

                var tracks = await _trackService.GetAll();
                ViewBag.Tracks = new SelectList(tracks.Value, "Id", "Name");

                FileStream attachmentStream = null;

                if (file?.Length > 0)
                {
                    // Check file extension and size
                    var allowedExtensions = new[] { ".pdf", ".docx", ".png", ".jpg" };
                    const int maxFileSize = 9 * 1024 * 1024; // 9MB

                    var fileExtension = Path.GetExtension(file.FileName).ToLower();

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
            catch (Exception ex)
            {
                LogException(nameof(Create), ex, "Error in CandidatesController.Create (POST)");
                throw;
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var candidate = await _candidateService.GetCandidateByIdAsync(id);

                if (candidate is null)
                    return NotFound();

                var positions = await _positionService.GetAll();
                ViewBag.positions = new SelectList(positions.Value, "Id", "Name");
                var CompaniesDTOs = await _companyService.GetAll();
                ViewBag.CompaniesDTOs = new SelectList(CompaniesDTOs.Value, "Id", "Name");

                var Country = await _countryService.GetAll();
                ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

                var tracks = await _trackService.GetAll();
                ViewBag.Tracks = new SelectList(tracks.Value, "Id", "Name");

                return View(candidate);
            }
            catch (Exception ex)
            {
                LogException(nameof(Edit), ex, $"Error in CandidatesController.Edit (GET) for ID: {id}");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CandidateDTO candidateDTO, IFormFile file)
        {
            try
            {
                if (id != candidateDTO.Id)
                    return NotFound();

                var positions = await _positionService.GetAll();
                ViewBag.positions = new SelectList(positions.Value, "Id", "Name");

                var CompaniesDTOs = await _companyService.GetAll();
                ViewBag.CompaniesDTOs = new SelectList(CompaniesDTOs.Value, "Id", "Name");

                var Country = await _countryService.GetAll();
                ViewBag.CountryDTOs = new SelectList(Country.Value, "Id", "Name");

                var tracks = await _trackService.GetAll();
                ViewBag.Tracks = new SelectList(tracks.Value, "Id", "Name");

                if (file?.Length > 0)
                {
                    // Check file extension and size
                    var allowedExtensions = new[] { ".pdf", ".docx", ".png", ".jpg" };
                    var maxFileSize = 9 * 1024 * 1024; // 4MB

                    var fileExtension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                        ModelState.AddModelError("File", "Invalid file format. Allowed formats are PDF, DOCX, PNG, and JPG.");

                    else if (file.Length < 1024 * 1024) // 1MB
                        ModelState.AddModelError("File", "File size is too small. Minimum size allowed is 1MB.");

                    else if (file.Length > maxFileSize)
                        ModelState.AddModelError("File", "File size exceeds the maximum allowed size (9MB).");

                    if (ModelState.IsValid)
                    {
                        var stream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
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
            catch (Exception ex)
            {
                LogException(nameof(Edit), ex, $"Error in CandidatesController.Edit (POST) for ID: {id}");
                throw;
            }
        }

        [HttpPost]
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
                    var stream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
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
            catch (Exception ex)
            {
                LogException(nameof(UpdateAttachment), ex, $"Error in CandidatesController.UpdateAttachment for ID: {id}");
                throw;
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var candidate = await _candidateService.GetCandidateByIdAsync(id);

                if (candidate is null)
                    return NotFound();

                return View(candidate);
            }
            catch (Exception ex)
            {
                LogException(nameof(DeleteConfirmed), ex, $"Error in CandidatesController.Delete (GET) for ID: {id}");
                throw;
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _candidateService.DeleteCandidateAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogException(nameof(DeleteConfirmed), ex, $"Error in CandidatesController.DeleteConfirmed (POST) for ID: {id}");
                throw;
            }
        }
    }
}