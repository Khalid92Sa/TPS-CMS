using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class DelegationDurationDTO
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; }

    [Range(1, 10000, ErrorMessage = "Enter a duration greater than zero.")]
    public int Amount { get; set; }

    [Required(ErrorMessage = "Unit is required.")]
    public string Unit { get; set; }

    public int DurationMinutes { get; set; }

    [Display(Name = "Enabled")]
    public bool IsActive { get; set; } = true;
}
