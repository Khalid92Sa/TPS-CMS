using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

public class Company : BaseEntity
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string? Email { get; set; }
    public string PersonName { get; set; }
    public string PhoneNumber { set; get; }
    [Required(ErrorMessage = "Please select a country")]
    public int CountryId { get; set; }
    public virtual Country Country { get; set; }

    public virtual ICollection<Candidate> Candidates { get; set; }
}
