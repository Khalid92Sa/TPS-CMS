using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

public class Status : BaseEntity
{
    public int Id { set; get; }

    [Required]
    public string Name { set; get; }

    [Required]
    public string Code { set; get; }
    public virtual ICollection<Interviews> Interviews { get; set; }
}