using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

public class Candidate : BaseEntity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "FullName is required.")]
    public string FullName { get; set; }
    public string? Phone { get; set; }
    public int? PositionId { get; set; }

    public virtual Position Position { get; set; }

    [Required(ErrorMessage = "Company is required.")]
    public int CompanyId { set; get; }

    public virtual Company Company { set; get; }

    [Required(ErrorMessage = "Experience is required.")]
    [RegularExpression(@"^(?:\d{1,2}(\.\d{1})?)$", ErrorMessage = "Experience must be a non-negative numeric value with up to 1 decimal place.")]
    public double Experience { get; set; }

    public int? CVAttachmentId { get; set; }
    public virtual Attachment CV { get; set; }
    public int? CountryId { get; set; }
    public virtual Country Country { get; set; }
    public virtual ICollection<Interviews> Interviews { get; set; }
    public int? TrackId { get; set; }
    public virtual Track Track { get; set; }
}
