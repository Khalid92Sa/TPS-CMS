using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Application.DTOs;

public class CountryDTO
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
    public List<CompanyDTO> companyDTOs { get; set; }
    public DateTime CreatedOn { get; set; }
}
