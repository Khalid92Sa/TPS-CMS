using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

public class Position : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
    public int? EvaluationId { set; get; }
    public virtual Attachment Evaluation { get; set; }
    public virtual ICollection<Interviews> Interviews { get; set; }
    public virtual ICollection<Candidate> Candidates { get; set; }
    public virtual ICollection<CarrerOffer> CarrerOffer { get; set; }
}
