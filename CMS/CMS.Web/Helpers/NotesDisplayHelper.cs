using System;
using System.Text.RegularExpressions;

namespace CMS.Web.Helpers;

public static class NotesDisplayHelper
{
    public static string GetCardColumnClass(string? notes, string compactClass = "col-md-3 col-sm-6")
    {
        if (string.IsNullOrWhiteSpace(notes))
            return $"{compactClass} interview-card-col";

        var plainText = GetPlainText(notes);
        var tableColumnCount = GetMaxTableColumnCount(notes);
        var hasTable = tableColumnCount > 0;

        if (hasTable && tableColumnCount >= 4)
            return "col-12 interview-card-col interview-card-col-wide";

        if (hasTable || plainText.Length > 250)
            return "col-12 interview-card-col interview-card-col-wide";

        if (plainText.Length > 120)
            return "col-md-6 col-lg-6 interview-card-col interview-card-col-medium";

        return $"{compactClass} interview-card-col";
    }

    public static string GetHrCardColumnClass(string? notes)
        => GetCardColumnClass(notes, "col-md-4 col-sm-6");

    private static string GetPlainText(string html)
    {
        var plainText = Regex.Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ")
            .Trim();

        return Regex.Replace(plainText, @"\s+", " ");
    }

    private static int GetMaxTableColumnCount(string html)
    {
        if (!html.Contains("<table", StringComparison.OrdinalIgnoreCase))
            return 0;

        var rowMatches = Regex.Matches(html, @"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var maxCols = 0;

        foreach (Match row in rowMatches)
        {
            var colCount = Regex.Matches(row.Groups[1].Value, @"<t[hd]\b", RegexOptions.IgnoreCase).Count;
            if (colCount > maxCols)
                maxCols = colCount;
        }

        return maxCols;
    }
}
