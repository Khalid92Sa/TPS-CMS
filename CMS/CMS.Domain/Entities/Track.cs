using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

public class Track
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
    public virtual ICollection<Candidate> Candidates { get; set; }
    public virtual ICollection<Interviews> Interviews { get; set; }
}