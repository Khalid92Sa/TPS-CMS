using CMS.Domain.Enums;
using System.Collections.Generic;

namespace CMS.Domain.Entities;

public class Templates : BaseEntity
{
    public int TemplatesId { get; set; }
    public TemplatesName Name { get; set; }
    public string Title { get; set; }
    public string BodyDesc { get; set; }
    public virtual ICollection<Notifications> Notifications { get; set; }
}
