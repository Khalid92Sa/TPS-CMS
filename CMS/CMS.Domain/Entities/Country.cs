using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

public class Country : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
    public virtual ICollection<Company> Companies { get; set; }
    public virtual ICollection<Candidate> Candidates { get; set; }
}
