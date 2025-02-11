using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

public class Interviews : BaseEntity
{
    public int InterviewsId { get; set; }

    [Range(0, 5, ErrorMessage = "Score must be between 0 and 5.")]
    public double? Score { get; set; }

    [RegularExpression(@"^(?:0(\.\d+)?|[1-9]\d*(\.\d+)?)$", ErrorMessage = "Experience must be a non-negative numeric value.")]
    public double? ActualExperience { get; set; }

    [Required(ErrorMessage = "Please select a Date.")]
    public DateTime Date { get; set; }

    [Required]
    public int StatusId { set; get; }

    public virtual Status Status { get; set; }

    [Required(ErrorMessage = "Please select a Candidate.")]
    public int CandidateId { get; set; }

    public virtual Candidate Candidate { get; set; }

    [Required(ErrorMessage = "Please select a Position.")]
    public int PositionId { get; set; }

    public virtual Position Position { get; set; }

    public int? AttachmentId { get; set; }

    public virtual Attachment Attachment { get; set; }

    [Required(ErrorMessage = "Please select a Interviewer.")]
    public string InterviewerId { get; set; }

    public virtual IdentityUser Interviewer { get; set; }

    public int? ParentId { get; set; }

#nullable enable
    public string? Notes { get; set; }

    public bool IsUpdated { get; set; }

    public string? SecondInterviewerId { get; set; }

    public string? ArchitectureInterviewerId { get; set; }

    [Required(ErrorMessage = "Please select a Track.")]
    public int TrackId { get; set; }

    public virtual Track Track { get; set; }

    public string? StopCycleNote { get; set; }
}
