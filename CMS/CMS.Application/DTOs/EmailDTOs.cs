using System.Collections.Generic;

namespace CMS.Application.DTOs;

public class EmailDTOs
{
    public List<string> EmailTo { get; set; }
    public string EmailBody { get; set; }
    public string Subject { get; set; }
}
