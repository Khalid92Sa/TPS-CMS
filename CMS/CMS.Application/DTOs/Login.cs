using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class Login : IdentityUser
{
    public string LoginId { get; set; }
    [Required(ErrorMessage = "User Email is required.")]
    public string UserEmail { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}
