using System;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

public class Attachment : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public byte[] FileData { get; set; }
    public DateTime CreatedOn { get; set; }
}
