namespace QuotationAPI.V2.Models.Employee;

public class EmpEmployee
{
    public string Id { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Designation { get; set; } = "";
    public string JoiningDate { get; set; } = "";
    public decimal MonthlySalary { get; set; }
    public string Status { get; set; } = "active";
    public string? Department { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class EmpAttendanceRecord
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string Date { get; set; } = "";
    public string Status { get; set; } = "present";
    public decimal AttendanceHours { get; set; }
    public decimal OtHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal OtRate { get; set; }
    public decimal RegularPay { get; set; }
    public decimal OtPay { get; set; }
    public decimal TotalPay { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class EmpHoliday
{
    public string Id { get; set; } = "";
    public int Year { get; set; }
    public string Date { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class EmpSalaryMaster
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string SalaryType { get; set; } = "monthly";
    public decimal BasicSalary { get; set; }
    public decimal Hra { get; set; }
    public decimal Allowance { get; set; }
    public decimal Deduction { get; set; }
    public decimal OtMultiplier { get; set; } = 1;
    public decimal? OtRatePerHour { get; set; }
    public string EffectiveFrom { get; set; } = "";
    public string? DeductionsJson { get; set; }
    public string? Description { get; set; }
    public string? CreatedDate { get; set; }
    public string? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class EmpSalaryAdvance
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public decimal Amount { get; set; }
    public string RequestDate { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "requested";
    public bool IsDeleted { get; set; } = false;
}

public class EmpMonthlySalaryCalc
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Designation { get; set; } = "";
    public string Month { get; set; } = "";
    public int? WeekNumber { get; set; }
    public string SalaryType { get; set; } = "monthly";
    public decimal BasicSalary { get; set; }
    public decimal Hra { get; set; }
    public decimal Allowance { get; set; }
    public decimal BonusPay { get; set; }
    public decimal PerformancePay { get; set; }
    public decimal SalaryMasterDeduction { get; set; }
    public decimal TotalEarnings { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LeaveDays { get; set; }
    public decimal TotalOtHours { get; set; }
    public decimal OtEarnings { get; set; }
    public decimal AttendanceDeduction { get; set; }
    public decimal? OtherDeductions { get; set; }
    public string? OtherDeductionsJson { get; set; }
    public decimal? SalaryAdvanceDeduction { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }
    public string CalcStatus { get; set; } = "draft";
    public string? CreatedDate { get; set; }
    public string? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; } = false;
}
