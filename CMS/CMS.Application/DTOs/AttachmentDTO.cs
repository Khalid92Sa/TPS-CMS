using System;

namespace CMS.Application.DTOs;

public class AttachmentDTO
{
    public int Id { get; set; }

    public string FileName { get; set; }

    public long FileSize { get; set; }

    public byte[] FileData { get; set; }

    public DateTime CreatedOn { get; set; }
}
