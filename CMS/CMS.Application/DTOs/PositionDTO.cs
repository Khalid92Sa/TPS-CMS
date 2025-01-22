using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace CMS.Application.DTOs;

public class PositionDTO
{
    public int Id { set; get; }
    [Required]
    public string Name { set; get; }
    public int? EvaluationId { set; get; }
    public string FileName { get; set; }
    public long? FileSize { get; set; }
    public Stream FileData { get; set; }
    public List<CarrerOfferDTO> CarrerOfferDTO { get; set; }
    public List<InterviewsDTO> InterviewsDTO { get; set; }
    public List<CandidateDTO> Candidates { get; set; }
    public int? CountryId { get; set; }
    public string CountryName { get; set; }
    public DateTime CreatedOn { get; set; }
}
