using CMS.Application.DTOs;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace CMS.Application.Helpers;

public static class ExcelHelper
{
    public static async Task<byte[]> GenerateExcelFileAsync(IEnumerable<InterviewsDTO> data, Func<int, Task<double?>> getScoreCallback)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Interviews");

            // Add headers
            worksheet.Cells["A1"].Value = "Candidate Name";
            worksheet.Cells["B1"].Value = "Position";
            worksheet.Cells["C1"].Value = "Track";
            worksheet.Cells["D1"].Value = "Interviewer/s Name";
            worksheet.Cells["E1"].Value = "Date and Time";
            worksheet.Cells["F1"].Value = "Score";
            worksheet.Cells["G1"].Value = "Status";
            worksheet.Cells["H1"].Value = "Notes";

            // Style headers
            using (var headerRange = worksheet.Cells["A1:H1"])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                headerRange.Style.Font.Color.SetColor(Color.Black);
            }

            // Fill data rows
            int row = 2;
            foreach (var item in data)
            {
                double? score = await getScoreCallback(item.InterviewsId);

                if (score != null)
                    worksheet.Cells[row, 6].Value = score;
                else
                    worksheet.Cells[row, 6].Value = "N/A";

                string interviewers = item.InterviewerName;
                if (!string.IsNullOrEmpty(item.SecondInterviewerName) && item.SecondInterviewerName != "User not found")
                    interviewers += " && " + item.SecondInterviewerName;

                worksheet.Cells[row, 1].Value = item.FullName;
                worksheet.Cells[row, 2].Value = item.Name;
                worksheet.Cells[row, 3].Value = item.TrackName;
                worksheet.Cells[row, 4].Value = interviewers;
                worksheet.Cells[row, 5].Style.Numberformat.Format = "yyyy-mm-dd";
                worksheet.Cells[row, 5].Value = item.Date;
                worksheet.Cells[row, 7].Value = item.StatusName;
                worksheet.Cells[row, 8].Value = item.Notes;

                row++;
            }

            return package.GetAsByteArray();
        }
        catch (Exception)
        {
            throw;
        }
    }
}
