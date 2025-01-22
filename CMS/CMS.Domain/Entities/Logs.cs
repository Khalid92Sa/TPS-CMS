using System;

namespace CMS.Domain.Entities;

public class Logs
{
    public int Id { get; set; }
    public string MethodName { get; set; }
    public string ExceptionMessage { get; set; }
    public string StackTrace { get; set; }
    public DateTime LogTime { get; set; }
    public string CreatedByUserId { get; set; }
    public string AdditionalInfo { get; set; }
}
