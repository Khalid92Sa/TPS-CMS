using System;

namespace CMS.Domain.Entities;

public class BaseEntity
{
    public string CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }
    public bool IsActive { get; set; }
    public bool IsDelete { get; set; }
}
