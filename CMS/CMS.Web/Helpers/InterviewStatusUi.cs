using System;
using System.Net;
using Microsoft.AspNetCore.Html;

namespace CMS.Web.Helpers;

/// <summary>
/// Single source for how an interview status is shown. Accepts a status name ("Approved")
/// or code ("APR") and maps it to one of the fixed kinds styled by the tps-status classes in
/// wwwroot/css/tps-design.css (colors: --tps-status-*). The same mapping exists in
/// wwwroot/js/tps-ui.js (TPS.statusKind) for client-rendered controls.
/// </summary>
public static class InterviewStatusUi
{
    public enum Kind
    {
        Unknown,
        Pending,
        Approved,
        Rejected,
        OnHold
    }

    public static Kind Resolve(string nameOrCode)
    {
        string value = (nameOrCode ?? string.Empty).Trim().ToLowerInvariant();
        if (value.Length == 0)
            return Kind.Unknown;

        if (value == "pnd" || value.StartsWith("pend"))
            return Kind.Pending;
        if (value == "apr" || value.StartsWith("approv") || value.StartsWith("accept"))
            return Kind.Approved;
        if (value == "rej" || value.StartsWith("reject"))
            return Kind.Rejected;
        if (value == "hold" || value.Contains("hold"))
            return Kind.OnHold;

        return Kind.Unknown;
    }

    /// <summary>The CSS modifier for the status, e.g. "pending".</summary>
    public static string Key(string nameOrCode) => Resolve(nameOrCode) switch
    {
        Kind.Pending => "pending",
        Kind.Approved => "approved",
        Kind.Rejected => "rejected",
        Kind.OnHold => "hold",
        _ => "neutral"
    };

    /// <summary>Readable label; turns codes such as "APR" into "Approved".</summary>
    public static string Label(string nameOrCode) => Resolve(nameOrCode) switch
    {
        Kind.Pending => "Pending",
        Kind.Approved => "Approved",
        Kind.Rejected => "Rejected",
        Kind.OnHold => "On hold",
        _ => string.IsNullOrWhiteSpace(nameOrCode) ? "—" : nameOrCode
    };

    public static string CssClass(string nameOrCode) => $"tps-status tps-status--{Key(nameOrCode)}";

    /// <summary>Status pill: colored dot plus text, so color is never the only signal.</summary>
    public static IHtmlContent Badge(string nameOrCode, string text = null)
    {
        string label = WebUtility.HtmlEncode(text ?? Label(nameOrCode));
        return new HtmlString($"<span class=\"{CssClass(nameOrCode)}\" data-status=\"{Key(nameOrCode)}\">{label}</span>");
    }

    /// <summary>All four statuses side by side, for table legends.</summary>
    public static IHtmlContent Legend()
    {
        return new HtmlString(
            "<div class=\"tps-status-legend\" aria-label=\"Status colors\">"
            + Badge("Pending") + Badge("Approved") + Badge("Rejected") + Badge("On hold")
            + "</div>");
    }
}
