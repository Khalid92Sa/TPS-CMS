using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace CMS.Application.DTOs;

public class InterviewsDTO
{
    public int InterviewsId { get; set; }

    [Range(0, 5, ErrorMessage = "Score must be between 0 and 5.")]
    public double? Score { get; set; }

    [Range(0, 5, ErrorMessage = "Score must be between 0 and 5.")]
    public double? FirstInterviewScore { get; set; }

    [Required(ErrorMessage = "Please select a Date.")]
    public DateTime Date { get; set; }

    public int? ParentId { get; set; }

    [Required(ErrorMessage = "Please select a Position.")]
    public int PositionId { get; set; }

    public string modifiedBy { set; get; }
    public DateTime ModifiedOn { set; get; }

    public string PositionName { get; set; }

    [Required(ErrorMessage = "Please select a Candidate.")]
    public int CandidateId { get; set; }

    public string CandidateName { get; set; }

    public int? StatusId { get; set; }

    public string StatusName { get; set; }

    [Required(ErrorMessage = "Please select a Interviewer.")]
    public string InterviewerId { get; set; }

    public string InterviewerName { get; set; }

    public string FullName { get; set; }

    public string Name { get; set; }

    public int? AttachmentId { set; get; }

    public int? EvalutaionFormId { set; get; }

    public int? CandidateCVAttachmentId { set; get; }

    public string CandidateCVFileName { get; set; }

    public string FileName { get; set; }

    public long? FileSize { get; set; }

    public Stream FileData { get; set; }

    public bool isUpdated { set; get; } = false;

    public string? Notes { get; set; }

    public string? InterviewerRole { get; set; }


    [RegularExpression(@"^(?:0(\.\d+)?|[1-9]\d*(\.\d+)?)$", ErrorMessage = "Experience must be a non-negative numeric value.")]
    public double? ActualExperience { get; set; }

    public string? SecondInterviewerId { get; set; }

    public string? SecondInterviewerName { get; set; }

    public string? ArchitectureInterviewerId { get; set; }

    public string? ArchitectureInterviewerName { get; set; }

    public bool isArchitectureUpdated { get; set; }

    public int TrackId { get; set; }

    public string? TrackName { get; set; }

    public string? StopCycleNote { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CompanyName { get; set; }

    public double? SecondInterviewActualExperience { get; set; }

    public string? SecondInterviewNotes { get; set; }

    public string? SecondInterviewInterviewerName { get; set; }

    public string? HRNotes { get; set; }

    public string? firstNote { get; set; }

    public int? WorkflowStageId { get; set; }

    public string? StageName { get; set; }

    public bool StartFromHR { get; set; } = false;
}
