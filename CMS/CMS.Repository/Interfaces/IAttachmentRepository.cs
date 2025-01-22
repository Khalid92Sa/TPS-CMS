using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces;

public interface IAttachmentRepository
{
    Task<IEnumerable<Domain.Entities.Attachment>> GetAllAttachmentsAsync();
    Task<Domain.Entities.Attachment> GetAttachmentByIdAsync(int id);
    Task<int> CreateAttachmentAsync(Domain.Entities.Attachment attachment);
    Task DeleteAttachmentAsync(int id);
}
