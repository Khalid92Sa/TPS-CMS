using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class StatusDTO
{
    public int Id { set; get; }
    [Required]
    public string Name { set; get; }
    [Required]
    public string Code { set; get; }
}
