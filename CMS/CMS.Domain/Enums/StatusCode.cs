namespace CMS.Domain.Enums;

public static class StatusCode
{
    public static string Pending = "PND";
    public static string Approved = "APR";
    public static string Rejected = "REJ";
    public static string OnHold = "HOLD";

    public static string GetName(string code)
    {
        return code switch
        {
            "PND" => "Pending",
            "APR" => "Approved",
            "REJ" => "Rejected",
            "HOLD" => "On hold",
            _ => "Unknown",
        };
    }
}
