using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class TrackDTO
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
    public List<InterviewsDTO> InterviewsDTO { get; set; }
    public List<CandidateDTO> Candidates { get; set; }
    public int CandidateCount { get; set; }
}
