using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class DelegationDurationDTO : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; }

    // Amount and Unit are only required for a limited duration; see Validate.
    public int Amount { get; set; }

    public string Unit { get; set; }

    public int DurationMinutes { get; set; }

    [Display(Name = "Unlimited duration")]
    public bool IsUnlimited { get; set; }

    [Display(Name = "Enabled")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsUnlimited)
            yield break;

        if (Amount < 1 || Amount > 10000)
            yield return new ValidationResult("Enter a duration greater than zero.", [nameof(Amount)]);

        if (string.IsNullOrWhiteSpace(Unit))
            yield return new ValidationResult("Unit is required.", [nameof(Unit)]);
    }
}
