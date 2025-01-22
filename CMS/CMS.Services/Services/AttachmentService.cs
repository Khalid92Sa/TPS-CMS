using CMS.Application.DTOs;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AttachmentService(
        IAttachmentRepository attachmentRepository,
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _attachmentRepository = attachmentRepository;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<IEnumerable<AttachmentDTO>> GetAllAttachmentsAsync()
    {
        try
        {
            IEnumerable<Attachment> attachments = await _attachmentRepository.GetAllAttachmentsAsync();
            return attachments.Select(a => new AttachmentDTO
                                            {
                                                Id = a.Id,
                                                FileName = a.FileName,
                                                FileSize = a.FileSize,
                                                FileData = a.FileData,
                                                CreatedOn = a.CreatedOn
                                            }
                                     );
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<AttachmentDTO> GetAttachmentByIdAsync(int id)
    {
        try
        {
            Attachment attachment = await _attachmentRepository.GetAttachmentByIdAsync(id);
            
            if (attachment is null)
                return null;

            return new AttachmentDTO
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FileSize = attachment.FileSize,
                FileData = attachment.FileData,
                CreatedOn = attachment.CreatedOn
            };
        }
        catch (Exception)
        {
            throw;
        }
    }
    public async Task<int> CreateAttachmentAsync(string fileName, long fileSize, Stream fileStream)
    {
        try
        {
            IdentityUser currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            Attachment attachment = new Attachment
            {
                FileName = fileName,
                FileSize = fileSize,
                FileData = ReadStream(fileStream),
                CreatedOn = DateTime.Now,
                CreatedBy = currentUser.Id
            };

            return await _attachmentRepository.CreateAttachmentAsync(attachment);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task DeleteAttachmentAsync(int id)
    {
        try
        {
            await _attachmentRepository.DeleteAttachmentAsync(id);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<byte[]> GetAttachmentFileDataAsync(int id)
    {
        try
        {
            Attachment attachment = await _attachmentRepository.GetAttachmentByIdAsync(id);
            return attachment?.FileData;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public Task UpdateAttachmentAsync(int id, AttachmentDTO attachmentDTO) => throw new NotImplementedException();

    private static byte[] ReadStream(Stream stream)
    {
        using MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}