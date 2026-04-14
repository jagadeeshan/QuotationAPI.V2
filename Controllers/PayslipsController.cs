using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuotationAPI.V2.Models.Employee;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/payslips")]
public class PayslipsController : ControllerBase
{
    static PayslipsController()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [HttpPost("pdf")]
    public IActionResult GeneratePayslipPdf([FromBody] PayslipPdfRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeName) ||
            string.IsNullOrWhiteSpace(request.EmployeeCode) ||
            string.IsNullOrWhiteSpace(request.Month))
        {
            return BadRequest("Employee name, employee code and month are required.");
        }

        var generatedAt = request.GeneratedDate ?? DateTime.UtcNow;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text(string.IsNullOrWhiteSpace(request.CompanyName) ? "Company" : request.CompanyName)
                        .FontSize(18).Bold();

                    if (!string.IsNullOrWhiteSpace(request.CompanyAddress))
                    {
                        column.Item().Text(request.CompanyAddress);
                    }

                    if (!string.IsNullOrWhiteSpace(request.CompanyGstNo))
                    {
                        column.Item().Text($"GST No: {request.CompanyGstNo}");
                    }

                    column.Item().PaddingTop(8).LineHorizontal(1);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Payslip").FontSize(16).Bold();
                            left.Item().Text($"Month: {request.Month}");
                            if (request.WeekNumber.HasValue && request.WeekNumber.Value > 0)
                            {
                                left.Item().Text($"Week: {request.WeekNumber.Value}");
                            }
                            left.Item().Text($"Generated On: {generatedAt:yyyy-MM-dd HH:mm}");
                        });
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text($"Employee Name: {request.EmployeeName}");
                            left.Item().Text($"Employee Code: {request.EmployeeCode}");
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text($"Designation: {request.Designation}");
                            right.Item().Text($"Department: {request.Department}");
                        });
                    });

                    column.Item().PaddingTop(6).Text("Attendance").Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(110);
                        });

                        table.Cell().Element(CellStyle).Text("Present Days");
                        table.Cell().Element(CellStyle).AlignRight().Text(request.PresentDays.ToString("0.##"));

                        table.Cell().Element(CellStyle).Text("Leave Days");
                        table.Cell().Element(CellStyle).AlignRight().Text(request.LeaveDays.ToString("0.##"));

                        table.Cell().Element(CellStyle).Text("Absent Days");
                        table.Cell().Element(CellStyle).AlignRight().Text(request.AbsentDays.ToString("0.##"));

                        table.Cell().Element(CellStyle).Text("Total OT Hours");
                        table.Cell().Element(CellStyle).AlignRight().Text(request.TotalOtHours.ToString("0.##"));
                    });

                    column.Item().PaddingTop(6).Text("Earnings").Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(120);
                        });

                        table.Cell().Element(CellStyle).Text("Basic Salary");
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatInr(request.BasicSalary));

                        if (request.Hra > 0)
                        {
                            table.Cell().Element(CellStyle).Text("HRA");
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatInr(request.Hra));
                        }

                        if (request.Allowance > 0)
                        {
                            table.Cell().Element(CellStyle).Text("Allowance");
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatInr(request.Allowance));
                        }

                        foreach (var item in request.Earnings.Where(x => x.Amount > 0))
                        {
                            table.Cell().Element(CellStyle).Text(item.Description);
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatInr(item.Amount));
                        }

                        table.Cell().Element(CellStyle).Text("Total Earnings").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatInr(request.GrossSalary)).Bold();
                    });

                    column.Item().PaddingTop(6).Text("Deductions").Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(120);
                        });

                        if (request.SalaryMasterDeduction > 0)
                        {
                            table.Cell().Element(CellStyle).Text("Salary Master Deduction");
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatInr(request.SalaryMasterDeduction));
                        }

                        foreach (var item in request.Deductions.Where(x => x.Amount > 0))
                        {
                            table.Cell().Element(CellStyle).Text(item.Description);
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatInr(item.Amount));
                        }

                        table.Cell().Element(CellStyle).Text("Total Deductions").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatInr(request.TotalDeductions)).Bold();
                    });
                });

                page.Footer()
                    .AlignRight()
                    .Text($"Net Salary: {FormatInr(request.NetSalary)}")
                    .Bold()
                    .FontSize(12);
            });
        }).GeneratePdf();

        var safeCode = MakeSafeFileToken(request.EmployeeCode);
        var safeMonth = MakeSafeFileToken(request.Month);
        return File(pdfBytes, "application/pdf", $"Payslip_{safeCode}_{safeMonth}.pdf");
    }

    private static string FormatInr(decimal amount) => $"INR {amount:0.00}";

    private static string MakeSafeFileToken(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container
            .PaddingVertical(4)
            .PaddingHorizontal(6)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2);
    }
}
