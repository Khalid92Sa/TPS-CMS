using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;


public class SelectedInterviewers : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public int InterviewId { get; set; }

    public virtual Interviews Interview { get; set; }

    public string? FirstInterviewerId { get; set; }

    public string? SecondInterviewerId { get; set; }

    public string? ArchitectureInterviewerId { get; set; }
}

