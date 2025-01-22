using System;
using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class CarrerOfferDTO
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Position is required.")]
    public int PositionId { get; set; }
    public string PositionName { get; set; }

    [Required(ErrorMessage = "YearsOfExperience is required.")]
    public int YearsOfExperience { get; set; }
    [Required(ErrorMessage = "LongDescription is required.")]
    public string LongDescription { get; set; }
    [Required(ErrorMessage = "CreatedBy is required.")]
    public string CreatedBy { get; set; }
    public DateTime CreatedDateTime { get; set; } = DateTime.Now;

}
