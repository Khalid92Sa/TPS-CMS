using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CMS.Application.EmailTemplates;

/// <summary>Colour of the email's accent bar, label and status badge.</summary>
public enum EmailTone
{
    Primary,
    Success,
    Danger,
    Warning,
    Info,
    Neutral
}

/// <summary>Everything one email needs; <see cref="EmailLayout.Render"/> turns it into HTML.</summary>
public sealed class EmailContent
{
    /// <summary>Document title, also used as the hidden inbox preview text when <see cref="Preheader"/> is empty.</summary>
    public string Title { get; set; }
    public string Preheader { get; set; }
    public EmailTone Tone { get; set; } = EmailTone.Primary;

    /// <summary>Small upper-case label above the heading, e.g. "Interview invitation".</summary>
    public string Eyebrow { get; set; }

    /// <summary>Optional status badge next to the label, e.g. "Approved".</summary>
    public string Badge { get; set; }

    public string Heading { get; set; }

    /// <summary>Shown as "Dear {name},". Leave empty for a neutral "Hello,".</summary>
    public string RecipientName { get; set; }

    /// <summary>Body paragraphs as HTML. Build dynamic parts with <see cref="EmailLayout.Strong"/> or <see cref="EmailLayout.Encode"/>.</summary>
    public string IntroHtml { get; set; }

    public string DetailsTitle { get; set; }
    public List<EmailDetail> Details { get; } = new();

    /// <summary>Optional second details table under the first, e.g. the result that led to this email.</summary>
    public string SecondaryDetailsTitle { get; set; }
    public List<EmailDetail> SecondaryDetails { get; } = new();

    public string ButtonText { get; set; }
    public string ButtonUrl { get; set; }

    /// <summary>Muted call-out under the button, as HTML.</summary>
    public string NoteHtml { get; set; }

    public string SystemName { get; set; } = "CMS";
}

public sealed class EmailDetail
{
    public EmailDetail(string label, string value, bool monospace = false)
    {
        Label = label;
        Value = value;
        Monospace = monospace;
    }

    public string Label { get; }
    public string Value { get; }
    public bool Monospace { get; }
}

/// <summary>
/// The single design used by every system email: a dark TPS header, a white card with a
/// coloured accent bar, a details table, one call-to-action button and a footer.
/// Table-based with inline styles so it renders in Outlook, Gmail and mobile clients.
/// </summary>
public static class EmailLayout
{
    // Brand colours from wwwroot/css/tps-design.css
    private const string Ink = "#0e1a2b";
    private const string Heading = "#0f1a2a";
    private const string Text = "#172234";
    private const string Muted = "#5a6880";
    private const string Subtle = "#8692a6";
    private const string Border = "#e1e6ee";
    private const string Page = "#f3f5f9";
    private const string Panel = "#f7f9fc";
    private const string Primary = "#2856c8";
    private const string FontStack = "'Segoe UI', Roboto, 'Helvetica Neue', Helvetica, Arial, sans-serif";

    public static string Encode(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

    /// <summary>Encoded text in the heading colour, for names and key facts inside a sentence.</summary>
    public static string Strong(string value) =>
        $"<strong style=\"color:{Heading};font-weight:600;\">{Encode(value)}</strong>";

    /// <summary>
    /// A coloured status banner for the intro, with a round icon (e.g. a check mark) beside a bold
    /// title and an optional muted line. Text is encoded; use it inside <see cref="EmailContent.IntroHtml"/>.
    /// </summary>
    public static string StatusBanner(EmailTone tone, string icon, string title, string subtitle = null)
    {
        var (color, soft) = Palette(tone);
        string sub = string.IsNullOrWhiteSpace(subtitle)
            ? ""
            : $@"<br><span style=""font-size:13px;line-height:19px;font-weight:400;color:{Muted};"">{Encode(subtitle)}</span>";

        return $@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:0 0 18px;""><tr>
<td style=""background:{soft};border:1px solid {color}33;border-left:4px solid {color};border-radius:10px;padding:14px 16px;"">
<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr>
<td width=""32"" style=""width:32px;height:32px;background:{color};border-radius:50%;text-align:center;vertical-align:middle;font-family:{FontStack};font-size:17px;line-height:32px;font-weight:700;color:#ffffff;"">{Encode(icon)}</td>
<td style=""padding-left:12px;font-family:{FontStack};font-size:15px;line-height:21px;font-weight:700;color:{color};vertical-align:middle;"">{Encode(title)}{sub}</td>
</tr></table>
</td></tr></table>";
    }

    private static (string Color, string Soft) Palette(EmailTone tone) => tone switch
    {
        EmailTone.Success => ("#178a5a", "#e3f4ec"),
        EmailTone.Danger => ("#cf3b3b", "#fbe7e7"),
        EmailTone.Warning => ("#a8660a", "#fdf1dc"),
        EmailTone.Info => ("#2878c0", "#e4f0fa"),
        EmailTone.Neutral => ("#5d6a80", "#eceff4"),
        _ => (Primary, "#e8eefb")
    };

    private static void AppendDetails(StringBuilder html, string title, List<EmailDetail> details)
    {
        if (details.Count == 0)
            return;

        html.Append($@"
<tr><td class=""tps-pad"" style=""background:#ffffff;padding:12px 32px 4px;font-family:{FontStack};"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:{Panel};border:1px solid {Border};border-radius:10px;"">");

        if (!string.IsNullOrWhiteSpace(title))
            html.Append($@"<tr><td colspan=""2"" style=""padding:14px 18px 4px;font-size:11px;line-height:16px;font-weight:700;letter-spacing:1px;text-transform:uppercase;color:{Subtle};"">{Encode(title)}</td></tr>");

        for (int i = 0; i < details.Count; i++)
        {
            EmailDetail d = details[i];
            string top = i == 0 && string.IsNullOrWhiteSpace(title) ? "14px" : "8px";
            string bottom = i == details.Count - 1 ? "14px" : "8px";
            string divider = i == 0 ? "" : $"border-top:1px solid {Border};";
            string valueFont = d.Monospace ? "font-family:Consolas,'Courier New',monospace;" : "";
            html.Append($@"<tr class=""tps-row"">
  <td class=""tps-label"" width=""38%"" style=""{divider}padding:{top} 12px {bottom} 18px;font-size:13px;line-height:20px;color:{Muted};vertical-align:top;"">{Encode(d.Label)}</td>
  <td class=""tps-value"" style=""{divider}padding:{top} 18px {bottom} 12px;font-size:14px;line-height:20px;font-weight:600;color:{Heading};vertical-align:top;word-break:break-word;{valueFont}"">{Encode(d.Value)}</td>
</tr>");
        }

        html.Append("</table></td></tr>");
    }

    public static string Render(EmailContent c)
    {
        var (accent, accentSoft) = Palette(c.Tone);
        string system = string.IsNullOrWhiteSpace(c.SystemName) ? "CMS" : c.SystemName;
        string preheader = string.IsNullOrWhiteSpace(c.Preheader) ? c.Title : c.Preheader;
        string greeting = string.IsNullOrWhiteSpace(c.RecipientName) ? "Hello," : $"Dear {Strong(c.RecipientName)},";

        var html = new StringBuilder();

        html.Append($@"<!DOCTYPE html>
<html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
<meta name=""x-apple-disable-message-reformatting"">
<meta name=""color-scheme"" content=""light"">
<meta name=""supported-color-schemes"" content=""light"">
<title>{Encode(c.Title)}</title>
<style>
  body {{ margin:0 !important; padding:0 !important; width:100% !important; background:{Page}; }}
  a {{ color:{Primary}; }}
  @media only screen and (max-width:620px) {{
    .tps-shell {{ width:100% !important; }}
    .tps-pad {{ padding-left:20px !important; padding-right:20px !important; }}
    .tps-outer {{ padding:12px 8px !important; }}
    .tps-h1 {{ font-size:20px !important; line-height:28px !important; }}
    .tps-btn, .tps-btn tbody, .tps-btn tr, .tps-btn td, .tps-btn a {{ display:block !important; width:100% !important; box-sizing:border-box; text-align:center !important; }}
    .tps-row td {{ display:block !important; width:100% !important; box-sizing:border-box; padding:2px 16px !important; border-top:0 !important; }}
    .tps-row td.tps-label {{ padding-top:10px !important; }}
    .tps-row + .tps-row td.tps-label {{ border-top:1px solid {Border} !important; }}
    .tps-row td.tps-value {{ padding-bottom:10px !important; }}
  }}
</style>
</head>
<body style=""margin:0;padding:0;background:{Page};"">
<div style=""display:none;max-height:0;overflow:hidden;opacity:0;mso-hide:all;"">{Encode(preheader)}</div>
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:{Page};"">
<tr><td align=""center"" class=""tps-outer"" style=""padding:32px 16px;"">
<table role=""presentation"" class=""tps-shell"" width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width:600px;max-width:600px;"">");

        // Header: brand mark + product name
        html.Append($@"
<tr><td style=""background:{Ink};border-radius:12px 12px 0 0;padding:20px 32px;"" class=""tps-pad"">
  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr>
    <td style=""width:36px;height:36px;background:{Primary};border-radius:9px;text-align:center;vertical-align:middle;font-family:{FontStack};font-size:12px;font-weight:800;color:#ffffff;letter-spacing:0.5px;"">TPS</td>
    <td style=""padding-left:12px;font-family:{FontStack};"">
      <div style=""font-size:15px;line-height:20px;font-weight:700;color:#ffffff;"">Candidate Management</div>
      <div style=""font-size:11px;line-height:16px;color:#8fa0b8;letter-spacing:1px;text-transform:uppercase;"">TechProcess Solutions</div>
    </td>
  </tr></table>
</td></tr>
<tr><td style=""background:{accent};height:4px;line-height:4px;font-size:0;"">&nbsp;</td></tr>");

        // Card body
        html.Append($@"
<tr><td class=""tps-pad"" style=""background:#ffffff;padding:32px 32px 8px;font-family:{FontStack};color:{Text};"">");

        if (!string.IsNullOrWhiteSpace(c.Eyebrow) || !string.IsNullOrWhiteSpace(c.Badge))
        {
            html.Append(@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin-bottom:10px;""><tr>");
            if (!string.IsNullOrWhiteSpace(c.Eyebrow))
                html.Append($@"<td style=""font-size:11px;line-height:16px;font-weight:700;letter-spacing:1.2px;text-transform:uppercase;color:{accent};padding-right:10px;"">{Encode(c.Eyebrow)}</td>");
            if (!string.IsNullOrWhiteSpace(c.Badge))
                html.Append($@"<td style=""background:{accentSoft};color:{accent};border-radius:999px;padding:3px 10px;font-size:11px;line-height:16px;font-weight:700;"">{Encode(c.Badge)}</td>");
            html.Append("</tr></table>");
        }

        if (!string.IsNullOrWhiteSpace(c.Heading))
            html.Append($@"<h1 class=""tps-h1"" style=""margin:0 0 20px;font-size:22px;line-height:30px;font-weight:700;color:{Heading};"">{Encode(c.Heading)}</h1>");

        html.Append($@"<p style=""margin:0 0 14px;font-size:15px;line-height:24px;"">{greeting}</p>");

        if (!string.IsNullOrWhiteSpace(c.IntroHtml))
            html.Append($@"<div style=""font-size:15px;line-height:24px;color:{Text};"">{c.IntroHtml}</div>");

        html.Append("</td></tr>");

        // Details tables
        AppendDetails(html, c.DetailsTitle, c.Details);
        AppendDetails(html, c.SecondaryDetailsTitle, c.SecondaryDetails);

        // Call to action
        if (!string.IsNullOrWhiteSpace(c.ButtonText) && !string.IsNullOrWhiteSpace(c.ButtonUrl))
        {
            string url = Encode(c.ButtonUrl);
            html.Append($@"
<tr><td class=""tps-pad"" style=""background:#ffffff;padding:24px 32px 4px;font-family:{FontStack};"">
<table role=""presentation"" class=""tps-btn"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr>
  <td align=""center"" bgcolor=""{Primary}"" style=""background:{Primary};border-radius:8px;"">
    <a href=""{url}"" target=""_blank"" style=""display:inline-block;padding:13px 28px;font-family:{FontStack};font-size:15px;line-height:20px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:8px;"">{Encode(c.ButtonText)} &rarr;</a>
  </td>
</tr></table>
<p style=""margin:12px 0 0;font-size:12px;line-height:18px;color:{Subtle};"">If the button doesn't work, copy this link into your browser:<br><a href=""{url}"" target=""_blank"" style=""color:{Primary};word-break:break-all;"">{url}</a></p>
</td></tr>");
        }

        // Note
        if (!string.IsNullOrWhiteSpace(c.NoteHtml))
        {
            html.Append($@"
<tr><td class=""tps-pad"" style=""background:#ffffff;padding:20px 32px 0;font-family:{FontStack};"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr>
  <td style=""border-left:3px solid {accent};background:{accentSoft};border-radius:0 8px 8px 0;padding:12px 16px;font-size:13px;line-height:20px;color:{Text};"">{c.NoteHtml}</td>
</tr></table>
</td></tr>");
        }

        // Sign-off
        html.Append($@"
<tr><td class=""tps-pad"" style=""background:#ffffff;border-radius:0 0 12px 12px;padding:28px 32px 32px;font-family:{FontStack};font-size:15px;line-height:24px;color:{Text};"">
  Regards,<br><strong style=""color:{Heading};"">{Encode(system)}</strong> <span style=""color:{Muted};"">&middot; Candidate Management System</span>
</td></tr>");

        // Footer
        html.Append($@"
<tr><td align=""center"" style=""padding:20px 24px 0;font-family:{FontStack};font-size:12px;line-height:18px;color:{Subtle};"">
  This is an automated message from the TPS Candidate &amp; Interview Management System.<br>
  Please do not reply to this email. For questions, contact the HR team.<br>
  &copy; TechProcess Solutions
</td></tr>
</table>
</td></tr>
</table>
</body>
</html>");

        return html.ToString();
    }
}
