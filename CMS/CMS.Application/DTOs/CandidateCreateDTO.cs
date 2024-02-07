using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace CMS.Application.DTOs
{
    public class CandidateCreateDTO
    {
        public int CandidateId { get; set; }
        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; }

        [RegularExpression(@"^(\+[0-9]{1,})?[0-9]+$", ErrorMessage = "Please enter a valid format")]
        public string? Phone { get; set; }

        //[Required(ErrorMessage = "DesiredPosition is required.")]
        public int? PositionId { get; set; }

        [Required(ErrorMessage = "company is required.")]
        public int CompanyId { set; get; }

        public string CompanyName { set; get; }

        //[DataType(DataType.EmailAddress, ErrorMessage = "E-mail is not valid")]
        //public string Email { get; set; }

        //public string Address { get; set; }

        [Required(ErrorMessage = "Experience is required.")]
        [RegularExpression(@"^(?:\d{1,2}(\.\d{1})?)$", ErrorMessage = "Experience must be a non-negative numeric value with up to 1 decimal place.")]
        public double Experience { get; set; }

        //[Url(ErrorMessage = "Invalid LinkedIn URL.")]

        //public string LinkedInUrl { get; set; }
        public int? CountryId { get; set; }
        public string CountryName { get; set; }
        public int? CVAttachmentId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        //[Required(ErrorMessage = "CVAttachment is required.")]
        public Stream FileData { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<InterviewsDTO> InterviewsDTO { get; set; }

        public string LastInterviewStatus { get; set; } // Add this property

        public string InterviewerName { get; set; } // Include InterviewerName

        public int? TrackId { get; set; }
        public bool IsBlacklisted { get; set; }


    }
}
