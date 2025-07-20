using CMS.Application.DTOs;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace CMS.Application.Helpers
{
    public static class ExcelHelper
    {
        public static async Task<byte[]> GenerateExcelFileAsync(
            IEnumerable<InterviewsDTO> data,
            Func<int, Task<double?>> getScoreCallback,
            Func<int, Task<string>> getAllStatusesCallback
        )
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.Commercial;

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Interviews");

                string[] headers = {
                    "Candidate Name", "Position", "Track",
                    "Interviewer/s Name", "Date and Time",
                    "Score", "Statuses (All)", "Notes"
                };
                int[] columnWidths = { 25, 20, 20, 30, 18, 10, 40, 50 };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.DarkGray);
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Column(i + 1).Width = columnWidths[i];
                }

                int row = 2;
                foreach (var item in data)
                {
                    double? score = await getScoreCallback(item.InterviewsId);

                    string interviewers = item.InterviewerName;
                    if (!string.IsNullOrEmpty(item.SecondInterviewerName) &&
                        item.SecondInterviewerName != "User not found")
                    {
                        interviewers += " && " + item.SecondInterviewerName;
                    }

                    string allStatuses = await getAllStatusesCallback(item.CandidateId);

                    worksheet.Cells[row, 1].Value = item.FullName;
                    worksheet.Cells[row, 2].Value = item.Name;
                    worksheet.Cells[row, 3].Value = item.TrackName;
                    worksheet.Cells[row, 4].Value = interviewers;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "yyyy-mm-dd";
                    worksheet.Cells[row, 5].Value = item.Date;
                    if (score != null)
                        worksheet.Cells[row, 6].Value = score;
                    else
                        worksheet.Cells[row, 6].Value = "N/A";
                    worksheet.Cells[row, 7].Value = allStatuses;
                    worksheet.Cells[row, 8].Value = item.Notes;

                    for (int col = 1; col <= headers.Length; col++)
                    {
                        worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[row, col].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        if (col == 7 || col == 8)
                        {
                            worksheet.Cells[row, col].Style.WrapText = true;
                        }
                    }

                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                worksheet.Column(7).Width = columnWidths[6];
                worksheet.Column(8).Width = columnWidths[7];

                using (var range = worksheet.Cells[1, 1, row - 1, headers.Length])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                return package.GetAsByteArray();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
