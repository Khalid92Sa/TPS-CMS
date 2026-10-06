namespace CMS.Domain.Entities;

/// <summary>
/// A duration an administrator can offer when delegating a pending interview.
/// </summary>
public class DelegationDuration : BaseEntity
{
    public const string UnitMinutes = "Minutes";
    public const string UnitHours = "Hours";
    public const string UnitDays = "Days";
    public const string UnitWeeks = "Weeks";

    public int Id { get; set; }

    public string Name { get; set; }

    public int Amount { get; set; }

    public string Unit { get; set; }

    public int DurationMinutes { get; set; }
}
