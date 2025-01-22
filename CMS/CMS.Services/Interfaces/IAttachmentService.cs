using CMS.Application.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface IAttachmentService
{
    Task<IEnumerable<AttachmentDTO>> GetAllAttachmentsAsync();
    Task<AttachmentDTO> GetAttachmentByIdAsync(int id);
    Task<int> CreateAttachmentAsync(string fileName, long fileSize, Stream fileStream);
    Task DeleteAttachmentAsync(int id);
    Task<byte[]> GetAttachmentFileDataAsync(int id);
    Task UpdateAttachmentAsync(int id, AttachmentDTO attachmentDTO);
}
