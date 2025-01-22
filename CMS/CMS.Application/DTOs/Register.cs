using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class Register
{
    public string RegisterrId { get; set; }
    [Required]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }
    [Required]
    public string UserName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Confirm Password Not Match Password")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Please select a role.")]
    [Display(Name = "Role")]
    public string SelectedRole { get; set; }
}
