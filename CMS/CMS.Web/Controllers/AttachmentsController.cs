using CMS.Application.DTOs;
using CMS.Services.Interfaces;
using CMS.Web.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

public class AttachmentsController : Controller
{
    private readonly IAttachmentService _attachmentService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _attachmentStoragePath;

    public AttachmentsController(
        IAttachmentService attachmentService,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor)
    {
        _attachmentService = attachmentService;
        _httpContextAccessor = httpContextAccessor;
        _attachmentStoragePath = Path.Combine(env.WebRootPath, "attachments");

        if (!Directory.Exists(_attachmentStoragePath))
            Directory.CreateDirectory(_attachmentStoragePath);
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            IEnumerable<AttachmentDTO> attachments = await _attachmentService.GetAllAttachmentsAsync();
            return View(attachments);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile file)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Please choose a file to upload.");
                return View();
            }

            FileStream attachmentStream = await AttachmentHelper.handleUpload(file, _attachmentStoragePath);
            int attachmentId = await _attachmentService.CreateAttachmentAsync(file.FileName, file.Length, attachmentStream);
            attachmentStream.Close();
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IActionResult> Download(int id)
    {
        try
        {
            AttachmentDTO attachment = await _attachmentService.GetAttachmentByIdAsync(id);
            
            if (attachment is null)
                return NotFound();

            string contentType = "application/octet-stream";
            FileContentResult result = new(attachment.FileData, contentType)
            {
                FileDownloadName = attachment.FileName
            };

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            AttachmentDTO attachment = await _attachmentService.GetAttachmentByIdAsync(id);
            
            if (attachment is null)
                return NotFound();

            return View(attachment);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AttachmentDTO attachmentDTO)
    {
        try
        {
            if (id != attachmentDTO.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                await _attachmentService.UpdateAttachmentAsync(id, attachmentDTO);
                return RedirectToAction(nameof(Index));
            }

            return View(attachmentDTO);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            AttachmentDTO attachment = await _attachmentService.GetAttachmentByIdAsync(id);
            
            if (attachment is null)
                return NotFound();

            return View(attachment);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _attachmentService.DeleteAttachmentAsync(id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            throw;
        }
    }
}