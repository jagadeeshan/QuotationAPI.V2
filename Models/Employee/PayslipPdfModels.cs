namespace QuotationAPI.V2.Models.Employee;

public class PayslipPdfRequest
{
    public string CompanyName { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string CompanyGstNo { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string Designation { get; set; } = "";
    public string Department { get; set; } = "";
    public string Month { get; set; } = "";
    public int? WeekNumber { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Hra { get; set; }
    public decimal Allowance { get; set; }
    public decimal SalaryMasterDeduction { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }
    public decimal PresentDays { get; set; }
    public decimal LeaveDays { get; set; }
    public decimal AbsentDays { get; set; }
    public decimal TotalOtHours { get; set; }
    public DateTime? GeneratedDate { get; set; }
    public List<PayslipPdfLineItem> Earnings { get; set; } = new();
    public List<PayslipPdfLineItem> Deductions { get; set; } = new();
}

public class PayslipPdfLineItem
{
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}
