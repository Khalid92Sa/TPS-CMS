using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class CandidateDTO
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone must be a numeric value.")]
    public string Phone { get; set; }
    public int? PositionId { get; set; }
    public string Name { get; set; }

    [Required(ErrorMessage = "company is required.")]
    public int CompanyId { set; get; }
    public string CompanyName { set; get; }

    [Required(ErrorMessage = "Experience is required.")]
    [RegularExpression(@"^(?:\d{1,2}(\.\d{1})?)$", ErrorMessage = "Experience must be a non-negative numeric value with up to 1 decimal place.")]
    public double Experience { get; set; }

    public int? CVAttachmentId { get; set; }
    public int? CountryId { get; set; }
    public string CountryName { get; set; }
    public List<InterviewsDTO> InterviewsDTO { get; set; }
    public string? PositionName { get; set; }
    public InterviewsDTO LastInterview { get; set; }
    public string Status { get; set; }
    public double? Score { get; set; }
    public string LastInterviewStatus { get; set; }
    public string InterviewerName { get; set; }
    public int? TrackId { get; set; }
    public string? TrackName { get; set; }
    public bool IsBlacklisted { get; set; }
    public DateTime CreatedOn { get; set; }
}
