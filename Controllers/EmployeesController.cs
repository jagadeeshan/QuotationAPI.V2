using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Employee;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private const decimal DefaultMonthlyDivisorDays = 30m;
    private const decimal WorkHoursPerDay = 8m;
    private const decimal MaxAttendanceHoursPerDay = 16m;

    private readonly QuotationDbContext _db;
    public EmployeesController(QuotationDbContext db) => _db = db;

    private static decimal NormalizeAttendanceHours(string? status, decimal attendanceHours)
    {
        var normalizedHours = Math.Clamp(attendanceHours, 0m, MaxAttendanceHoursPerDay);

        var normalizedStatus = (status ?? string.Empty).Trim().ToLowerInvariant();

        if (normalizedStatus is "absent" or "leave" or "half-day" or "half day" or "halfday")
        {
            return 0m;
        }

        if (normalizedStatus is "weekoff" or "holiday")
        {
            return WorkHoursPerDay;
        }

        if (normalizedStatus == "present" && normalizedHours <= 0m)
        {
            return WorkHoursPerDay;
        }

        return normalizedHours;
    }

    private static decimal CalculateOtHours(decimal attendanceHours) =>
        decimal.Round(Math.Max(0m, attendanceHours - WorkHoursPerDay), 2, MidpointRounding.AwayFromZero);

    private static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static int ParseEmployeeCodeSerial(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return 0;
        }

        var normalized = employeeCode.Trim().ToUpperInvariant();
        if (!normalized.StartsWith("EMP", StringComparison.Ordinal))
        {
            return 0;
        }

        var numeric = normalized[3..];
        return int.TryParse(numeric, out var parsed) ? parsed : 0;
    }

    private async Task<string> GenerateNextEmployeeCodeAsync(CancellationToken cancellationToken = default)
    {
        var existingCodes = await _db.Employees
            .AsNoTracking()
            .Select(item => item.EmployeeCode)
            .ToListAsync(cancellationToken);

        var maxSerial = existingCodes
            .Select(ParseEmployeeCodeSerial)
            .DefaultIfEmpty(0)
            .Max();

        return $"EMP{(maxSerial + 1):000000}";
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            return postgresException.SqlState == "23505";
        }

        return false;
    }

    private async Task<(Dictionary<string, EmpEmployee> EmployeesById, Dictionary<string, EmpSalaryMaster> SalaryMastersByEmployeeId)> LoadAttendanceCalculationDataAsync()
    {
        var employees = await _db.Employees
            .AsNoTracking()
            .ToDictionaryAsync(item => item.Id);

        var salaryMasters = (await _db.SalaryMasters
                .AsNoTracking()
                .ToListAsync())
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => group.First());

        return (employees, salaryMasters);
    }

    private static decimal ResolveMonthlySalary(
        string employeeId,
        IReadOnlyDictionary<string, EmpEmployee> employeesById,
        IReadOnlyDictionary<string, EmpSalaryMaster> salaryMastersByEmployeeId)
    {
        if (salaryMastersByEmployeeId.TryGetValue(employeeId, out var salaryMaster))
        {
            return Math.Max(0m, salaryMaster.BasicSalary + salaryMaster.Hra + salaryMaster.Allowance - salaryMaster.Deduction);
        }

        return employeesById.TryGetValue(employeeId, out var employee) ? employee.MonthlySalary : 0m;
    }

    private static decimal ResolveBasicPay(
        string employeeId,
        IReadOnlyDictionary<string, EmpEmployee> employeesById,
        IReadOnlyDictionary<string, EmpSalaryMaster> salaryMastersByEmployeeId)
    {
        if (salaryMastersByEmployeeId.TryGetValue(employeeId, out var salaryMaster))
        {
            return Math.Max(0m, salaryMaster.BasicSalary);
        }

        return employeesById.TryGetValue(employeeId, out var employee) ? Math.Max(0m, employee.MonthlySalary) : 0m;
    }

    private static decimal ResolveOtMultiplier(string employeeId, IReadOnlyDictionary<string, EmpSalaryMaster> salaryMastersByEmployeeId) =>
        salaryMastersByEmployeeId.TryGetValue(employeeId, out var salaryMaster) ? salaryMaster.OtMultiplier : 1m;

    private static decimal ResolveOtBaseHourlyRate(
        string employeeId,
        IReadOnlyDictionary<string, EmpEmployee> employeesById,
        IReadOnlyDictionary<string, EmpSalaryMaster> salaryMastersByEmployeeId)
    {
        var basicPay = ResolveBasicPay(employeeId, employeesById, salaryMastersByEmployeeId);
        return basicPay / (DefaultMonthlyDivisorDays * WorkHoursPerDay);
    }

    private static string NormalizeSalaryType(string? salaryType)
    {
        var normalized = (salaryType ?? string.Empty).Trim().ToLowerInvariant();
        return normalized == "weekly" ? "weekly" : "monthly";
    }

    private static int? NormalizeWeekNumber(int? weekNumber, string? salaryType)
    {
        if (NormalizeSalaryType(salaryType) != "weekly")
        {
            return null;
        }

        if (!weekNumber.HasValue || weekNumber.Value <= 0)
        {
            return 1;
        }

        return weekNumber.Value;
    }

    private static decimal ResolveHourlyRate(
        string employeeId,
        IReadOnlyDictionary<string, EmpEmployee> employeesById,
        IReadOnlyDictionary<string, EmpSalaryMaster> salaryMastersByEmployeeId)
    {
        if (salaryMastersByEmployeeId.TryGetValue(employeeId, out var salaryMaster))
        {
            if (NormalizeSalaryType(salaryMaster.SalaryType) == "weekly")
            {
                return RoundMoney(Math.Max(0m, salaryMaster.BasicSalary) / WorkHoursPerDay);
            }

            var monthlySalary = ResolveMonthlySalary(employeeId, employeesById, salaryMastersByEmployeeId);
            return RoundMoney(monthlySalary / (DefaultMonthlyDivisorDays * WorkHoursPerDay));
        }

        return employeesById.TryGetValue(employeeId, out var employee)
            ? RoundMoney(Math.Max(0m, employee.MonthlySalary) / (DefaultMonthlyDivisorDays * WorkHoursPerDay))
            : 0m;
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool IsFutureDate(string date)
    {
        var parsed = ParseDateOnly(date);
        return parsed.HasValue && parsed.Value > DateOnly.FromDateTime(DateTime.Today);
    }

    private static bool IsBeforeJoiningDate(EmpEmployee employee, string date)
    {
        var target = ParseDateOnly(date);
        var joining = ParseDateOnly(employee.JoiningDate);

        if (!target.HasValue || !joining.HasValue)
        {
            return false;
        }

        return target.Value < joining.Value;
    }

    private static bool IsFutureMonth(string month)
    {
        if (string.IsNullOrWhiteSpace(month) || month.Length < 7)
        {
            return false;
        }

        var monthPrefix = month[..7];
        var currentMonth = DateTime.Today.ToString("yyyy-MM");
        return string.CompareOrdinal(monthPrefix, currentMonth) > 0;
    }

    private static EmpAttendanceRecord NormalizeAttendanceRecord(
        EmpAttendanceRecord record,
        IReadOnlyDictionary<string, EmpEmployee> employeesById,
        IReadOnlyDictionary<string, EmpSalaryMaster> salaryMastersByEmployeeId)
    {
        var attendanceHours = NormalizeAttendanceHours(record.Status, record.AttendanceHours);
        var otHours = CalculateOtHours(attendanceHours);
        var regularHours = Math.Min(attendanceHours, WorkHoursPerDay);
        var hourlyRate = ResolveHourlyRate(record.EmployeeId, employeesById, salaryMastersByEmployeeId);
        var otMultiplier = ResolveOtMultiplier(record.EmployeeId, salaryMastersByEmployeeId);
        var otBaseHourlyRate = ResolveOtBaseHourlyRate(record.EmployeeId, employeesById, salaryMastersByEmployeeId);
        var otRate = RoundMoney(otBaseHourlyRate * otMultiplier);
        var regularPay = RoundMoney(regularHours * hourlyRate);
        var otPay = RoundMoney(otBaseHourlyRate * otHours * otMultiplier);

        return new EmpAttendanceRecord
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            Date = record.Date,
            Status = record.Status,
            AttendanceHours = attendanceHours,
            OtHours = otHours,
            HourlyRate = hourlyRate,
            OtRate = otRate,
            RegularPay = regularPay,
            OtPay = otPay,
            TotalPay = RoundMoney(regularPay + otPay),
            Notes = record.Notes
        };
    }

    private async Task<EmpAttendanceRecord> UpsertAttendanceRecordAsync(EmpAttendanceRecord normalizedRecord, CancellationToken cancellationToken = default)
    {
        var existing = _db.AttendanceRecords.Local.FirstOrDefault(a =>
                a.EmployeeId == normalizedRecord.EmployeeId && a.Date == normalizedRecord.Date)
            ?? await _db.AttendanceRecords.FirstOrDefaultAsync(
                a => a.EmployeeId == normalizedRecord.EmployeeId && a.Date == normalizedRecord.Date,
                cancellationToken);

        if (existing != null)
        {
            existing.Status = normalizedRecord.Status;
            existing.AttendanceHours = normalizedRecord.AttendanceHours;
            existing.OtHours = normalizedRecord.OtHours;
            existing.HourlyRate = normalizedRecord.HourlyRate;
            existing.OtRate = normalizedRecord.OtRate;
            existing.RegularPay = normalizedRecord.RegularPay;
            existing.OtPay = normalizedRecord.OtPay;
            existing.TotalPay = normalizedRecord.TotalPay;
            existing.Notes = normalizedRecord.Notes;
            return existing;
        }

        normalizedRecord.Id = Guid.NewGuid().ToString();
        _db.AttendanceRecords.Add(normalizedRecord);
        return normalizedRecord;
    }

    // ── Employees ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetEmployees() =>
        Ok(await _db.Employees.Where(e => !e.IsDeleted).OrderBy(e => e.EmployeeCode).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(string id)
    {
        var emp = await _db.Employees.FindAsync(id);
        return emp == null ? NotFound() : Ok(emp);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmployee([FromBody] EmpEmployee emp)
    {
        emp.Id = Guid.NewGuid().ToString();

        // Employee code generation is server-owned to avoid UI-side collisions.
        const int maxAttempts = 5;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            emp.EmployeeCode = await GenerateNextEmployeeCodeAsync();
            _db.Employees.Add(emp);

            try
            {
                await _db.SaveChangesAsync();
                return Ok(emp);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _db.Entry(emp).State = EntityState.Detached;
            }
        }

        return Conflict("Unable to generate unique employee code. Please retry.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(string id, [FromBody] EmpEmployee updated)
    {
        var emp = await _db.Employees.FindAsync(id);
        if (emp == null) return NotFound();
        emp.FullName = updated.FullName;
        emp.Phone = updated.Phone;
        emp.Designation = updated.Designation;
        emp.JoiningDate = updated.JoiningDate;
        emp.MonthlySalary = updated.MonthlySalary;
        emp.Status = updated.Status;
        emp.Department = updated.Department;
        await _db.SaveChangesAsync();
        return Ok(emp);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(string id)
    {
        var emp = await _db.Employees.FindAsync(id);
        if (emp == null) return NotFound();

        emp.IsDeleted = true;
        emp.Status = "inactive";

        var attendanceRows = await _db.AttendanceRecords.Where(x => x.EmployeeId == id).ToListAsync();
        foreach (var row in attendanceRows)
        {
            row.IsDeleted = true;
        }

        var salaryMasters = await _db.SalaryMasters.Where(x => x.EmployeeId == id).ToListAsync();
        foreach (var row in salaryMasters)
        {
            row.IsDeleted = true;
        }

        var salaryAdvances = await _db.SalaryAdvances.Where(x => x.EmployeeId == id).ToListAsync();
        foreach (var row in salaryAdvances)
        {
            row.IsDeleted = true;
        }

        var salaryCalcs = await _db.MonthlySalaryCalcs.Where(x => x.EmployeeId == id).ToListAsync();
        foreach (var row in salaryCalcs)
        {
            row.IsDeleted = true;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Attendance ─────────────────────────────────────────────────────────

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance()
    {
        var records = await _db.AttendanceRecords
            .AsNoTracking()
            .OrderBy(a => a.Date)
            .ToListAsync();
        var (employeesById, salaryMastersByEmployeeId) = await LoadAttendanceCalculationDataAsync();

        return Ok(records
            .Select(record => NormalizeAttendanceRecord(record, employeesById, salaryMastersByEmployeeId))
            .ToList());
    }

    [HttpGet("attendance/date/{date}")]
    public async Task<IActionResult> GetAttendanceByDate(string date)
    {
        var records = await _db.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Date == date)
            .ToListAsync();
        var (employeesById, salaryMastersByEmployeeId) = await LoadAttendanceCalculationDataAsync();

        return Ok(records
            .Select(record => NormalizeAttendanceRecord(record, employeesById, salaryMastersByEmployeeId))
            .ToList());
    }

    [HttpPost("attendance")]
    public async Task<IActionResult> MarkAttendance([FromBody] EmpAttendanceRecord record)
    {
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == record.EmployeeId);
        if (employee == null)
        {
            return BadRequest("Employee not found.");
        }

        if (IsFutureDate(record.Date))
        {
            return BadRequest("Future date attendance is not allowed.");
        }

        if (IsBeforeJoiningDate(employee, record.Date))
        {
            return BadRequest("Attendance before joining date is not allowed.");
        }

        var (employeesById, salaryMastersByEmployeeId) = await LoadAttendanceCalculationDataAsync();
        var normalizedRecord = NormalizeAttendanceRecord(record, employeesById, salaryMastersByEmployeeId);
        var savedRecord = await UpsertAttendanceRecordAsync(normalizedRecord);
        await _db.SaveChangesAsync();
        return Ok(savedRecord);
    }

    [HttpPost("attendance/bulk")]
    public async Task<IActionResult> MarkAttendanceBulk([FromBody] EmpAttendanceBulkUpsertRequest request)
    {
        if (request.Records == null || request.Records.Count == 0)
        {
            return BadRequest("Attendance records are required.");
        }

        var employeeIds = request.Records
            .Select(record => record.EmployeeId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var employees = await _db.Employees
            .AsNoTracking()
            .Where(employee => employeeIds.Contains(employee.Id))
            .ToDictionaryAsync(employee => employee.Id);

        var (employeesById, salaryMastersByEmployeeId) = await LoadAttendanceCalculationDataAsync();
        var savedRecords = new List<EmpAttendanceRecord>(request.Records.Count);

        foreach (var record in request.Records)
        {
            if (!employees.TryGetValue(record.EmployeeId, out var employee))
            {
                return BadRequest($"Employee not found for attendance date {record.Date}.");
            }

            if (IsFutureDate(record.Date))
            {
                return BadRequest($"Future date attendance is not allowed for {record.Date}.");
            }

            if (IsBeforeJoiningDate(employee, record.Date))
            {
                return BadRequest($"Attendance before joining date is not allowed for {record.Date}.");
            }

            var normalizedRecord = NormalizeAttendanceRecord(record, employeesById, salaryMastersByEmployeeId);
            var savedRecord = await UpsertAttendanceRecordAsync(normalizedRecord);
            savedRecords.Add(savedRecord);
        }

        await _db.SaveChangesAsync();
        return Ok(savedRecords);
    }

    [HttpDelete("attendance/{id}")]
    public async Task<IActionResult> DeleteAttendance(string id)
    {
        var record = await _db.AttendanceRecords.FindAsync(id);
        if (record == null) return NotFound();
        record.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Holidays ───────────────────────────────────────────────────────────

    [HttpGet("holidays")]
    public async Task<IActionResult> GetHolidays([FromQuery] int? year)
    {
        var query = _db.Holidays.AsQueryable();
        if (year.HasValue)
        {
            query = query.Where(h => h.Year == year.Value);
        }

        var holidays = await query
            .OrderBy(h => h.Date)
            .ToListAsync();

        return Ok(holidays);
    }

    [HttpGet("holidays/{id}")]
    public async Task<IActionResult> GetHoliday(string id)
    {
        var holiday = await _db.Holidays.FindAsync(id);
        return holiday == null ? NotFound() : Ok(holiday);
    }

    [HttpPost("holidays")]
    public async Task<IActionResult> UpsertHoliday([FromBody] EmpHoliday holiday)
    {
        if (string.IsNullOrWhiteSpace(holiday.Date) || string.IsNullOrWhiteSpace(holiday.Name))
        {
            return BadRequest("Holiday date and name are required.");
        }

        holiday.Year = string.IsNullOrWhiteSpace(holiday.Date) ? holiday.Year : int.Parse(holiday.Date.Substring(0, 4));
        var existing = !string.IsNullOrWhiteSpace(holiday.Id)
            ? await _db.Holidays.FindAsync(holiday.Id)
            : await _db.Holidays.FirstOrDefaultAsync(h => h.Date == holiday.Date);

        if (existing != null)
        {
            existing.Date = holiday.Date;
            existing.Year = holiday.Year;
            existing.Name = holiday.Name;
            existing.Description = holiday.Description;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        holiday.Id = Guid.NewGuid().ToString();
        _db.Holidays.Add(holiday);
        await _db.SaveChangesAsync();
        return Ok(holiday);
    }

    [HttpDelete("holidays/{id}")]
    public async Task<IActionResult> DeleteHoliday(string id)
    {
        var holiday = await _db.Holidays.FindAsync(id);
        if (holiday == null) return NotFound();

        holiday.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Salary Masters ─────────────────────────────────────────────────────

    [HttpGet("salary-masters")]
    public async Task<IActionResult> GetSalaryMasters() =>
        Ok(await _db.SalaryMasters.ToListAsync());

    [HttpGet("salary-masters/history/{employeeId}")]
    public async Task<IActionResult> GetSalaryMasterHistory(string employeeId) =>
        Ok(await _db.SalaryMasters.Where(s => s.EmployeeId == employeeId).OrderByDescending(s => s.EffectiveFrom).ToListAsync());

    [HttpPost("salary-masters")]
    public async Task<IActionResult> UpsertSalaryMaster([FromBody] EmpSalaryMaster master)
    {
        master.SalaryType = NormalizeSalaryType(master.SalaryType);
        if (master.SalaryType == "weekly")
        {
            master.Hra = 0m;
            master.Allowance = 0m;
        }

        var existing = await _db.SalaryMasters.FindAsync(master.Id);
        if (existing == null)
            existing = await _db.SalaryMasters.FirstOrDefaultAsync(s => s.EmployeeId == master.EmployeeId);

        if (existing != null)
        {
            existing.BasicSalary = master.BasicSalary;
            existing.Hra = master.Hra;
            existing.Allowance = master.Allowance;
            existing.Deduction = master.Deduction;
            existing.SalaryType = master.SalaryType;
            existing.OtMultiplier = master.OtMultiplier;
            existing.OtRatePerHour = master.OtRatePerHour;
            existing.EffectiveFrom = master.EffectiveFrom;
            existing.DeductionsJson = master.DeductionsJson;
            existing.Description = master.Description;
            existing.UpdatedDate = DateTime.UtcNow.ToString("o");
            await _db.SaveChangesAsync();
            return Ok(existing);
        }
        
        master.Id = Guid.NewGuid().ToString();
        master.CreatedDate = DateTime.UtcNow.ToString("o");
        master.UpdatedDate = DateTime.UtcNow.ToString("o");
        _db.SalaryMasters.Add(master);
        await _db.SaveChangesAsync();
        return Ok(master);
    }

    [HttpDelete("salary-masters/{id}")]
    public async Task<IActionResult> DeleteSalaryMaster(string id)
    {
        var master = await _db.SalaryMasters.FindAsync(id);
        if (master == null) return NotFound();
        master.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Salary Advances ────────────────────────────────────────────────────

    [HttpGet("salary-advances")]
    public async Task<IActionResult> GetSalaryAdvances() =>
        Ok(await _db.SalaryAdvances.ToListAsync());

    [HttpPost("salary-advances")]
    public async Task<IActionResult> CreateSalaryAdvance([FromBody] EmpSalaryAdvance advance)
    {
        advance.Id = Guid.NewGuid().ToString();
        if (string.IsNullOrWhiteSpace(advance.PaymentMode))
        {
            advance.PaymentMode = "cash";
        }

        _db.SalaryAdvances.Add(advance);
        await _db.SaveChangesAsync();
        return Ok(advance);
    }

    [HttpPut("salary-advances/{id}")]
    public async Task<IActionResult> UpdateSalaryAdvance(string id, [FromBody] EmpSalaryAdvance updated)
    {
        var advance = await _db.SalaryAdvances.FindAsync(id);
        if (advance == null) return NotFound();
        advance.Amount = updated.Amount;
        advance.RequestDate = updated.RequestDate;
        advance.Reason = updated.Reason;
        advance.Status = updated.Status;
        advance.PaymentMode = string.IsNullOrWhiteSpace(updated.PaymentMode) ? "cash" : updated.PaymentMode;
        // Update ExpenseId if provided (links the advance to an expense record)
        if (!string.IsNullOrWhiteSpace(updated.ExpenseId))
        {
            advance.ExpenseId = updated.ExpenseId;
        }
        await _db.SaveChangesAsync();
        return Ok(advance);
    }

    [HttpDelete("salary-advances/{id}")]
    public async Task<IActionResult> DeleteSalaryAdvance(string id)
    {
        var advance = await _db.SalaryAdvances.FindAsync(id);
        if (advance == null) return NotFound();
        advance.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("salary-advances/{id}/expense")]
    public async Task<IActionResult> LinkExpenseToAdvance(string id, [FromBody] LinkExpenseRequest req)
    {
        var advance = await _db.SalaryAdvances.FindAsync(id);
        if (advance == null) return NotFound();
        advance.ExpenseId = req.ExpenseId;
        await _db.SaveChangesAsync();
        return Ok(advance);
    }

    // ── Monthly Salary Calculations ─────────────────────────────────────────

    [HttpGet("monthly-salary-calcs")]
    public async Task<IActionResult> GetMonthlySalaryCalcs() =>
        Ok(await _db.MonthlySalaryCalcs.ToListAsync());

    [HttpPost("monthly-salary-calcs")]
    public async Task<IActionResult> UpsertMonthlySalaryCalc([FromBody] EmpMonthlySalaryCalc calc)
    {
        calc.SalaryType = NormalizeSalaryType(calc.SalaryType);
        calc.WeekNumber = NormalizeWeekNumber(calc.WeekNumber, calc.SalaryType);
        if (calc.SalaryType == "weekly")
        {
            calc.Hra = 0m;
            calc.Allowance = 0m;
        }

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == calc.EmployeeId);
        if (employee == null)
        {
            return BadRequest("Employee not found.");
        }

        if (IsFutureMonth(calc.Month))
        {
            return BadRequest("Future month salary calculation is not allowed.");
        }

        var joiningMonth = string.IsNullOrWhiteSpace(employee.JoiningDate) || employee.JoiningDate.Length < 7
            ? string.Empty
            : employee.JoiningDate[..7];

        var targetMonth = string.IsNullOrWhiteSpace(calc.Month) || calc.Month.Length < 7
            ? string.Empty
            : calc.Month[..7];

        if (!string.IsNullOrWhiteSpace(joiningMonth) && !string.IsNullOrWhiteSpace(targetMonth) &&
            string.CompareOrdinal(targetMonth, joiningMonth) < 0)
        {
            return BadRequest("Salary calculation before joining month is not allowed.");
        }

        var existing = string.IsNullOrEmpty(calc.Id) ? null : await _db.MonthlySalaryCalcs.FindAsync(calc.Id);
        if (existing == null)
        {
            var normalizedWeek = NormalizeWeekNumber(calc.WeekNumber, calc.SalaryType);
            if (calc.SalaryType == "weekly")
            {
                existing = await _db.MonthlySalaryCalcs.FirstOrDefaultAsync(c =>
                    c.EmployeeId == calc.EmployeeId
                    && c.Month == calc.Month
                    && c.SalaryType == "weekly"
                    && (c.WeekNumber ?? 1) == (normalizedWeek ?? 1));
            }
            else
            {
                existing = await _db.MonthlySalaryCalcs.FirstOrDefaultAsync(c =>
                    c.EmployeeId == calc.EmployeeId
                    && c.Month == calc.Month
                    && (c.SalaryType == null || c.SalaryType == "" || c.SalaryType == "monthly"));
            }
        }

        if (existing != null)
        {
            existing.WeekNumber = calc.WeekNumber;
            existing.SalaryType = calc.SalaryType;
            existing.BasicSalary = calc.BasicSalary;
            existing.Hra = calc.Hra;
            existing.Allowance = calc.Allowance;
            existing.BonusPay = calc.BonusPay;
            existing.PerformancePay = calc.PerformancePay;
            existing.SalaryMasterDeduction = calc.SalaryMasterDeduction;
            existing.TotalEarnings = calc.TotalEarnings;
            existing.PresentDays = calc.PresentDays;
            existing.AbsentDays = calc.AbsentDays;
            existing.LeaveDays = calc.LeaveDays;
            existing.TotalOtHours = calc.TotalOtHours;
            existing.OtEarnings = calc.OtEarnings;
            existing.AttendanceDeduction = calc.AttendanceDeduction;
            existing.OtherDeductions = calc.OtherDeductions;
            existing.OtherDeductionsJson = calc.OtherDeductionsJson;
            existing.SalaryAdvanceDeduction = calc.SalaryAdvanceDeduction;
            existing.TotalDeductions = calc.TotalDeductions;
            existing.NetSalary = calc.NetSalary;
            existing.CalcStatus = calc.CalcStatus;
            existing.UpdatedDate = DateTime.UtcNow.ToString("o");
            await _db.SaveChangesAsync();
            return Ok(existing);
        }
        calc.Id = Guid.NewGuid().ToString();
        calc.CreatedDate = DateTime.UtcNow.ToString("o");
        calc.UpdatedDate = DateTime.UtcNow.ToString("o");
        _db.MonthlySalaryCalcs.Add(calc);
        await _db.SaveChangesAsync();
        return Ok(calc);
    }

    [HttpDelete("monthly-salary-calcs/{id}")]
    public async Task<IActionResult> DeleteMonthlySalaryCalc(string id)
    {
        var calc = await _db.MonthlySalaryCalcs.FindAsync(id);
        if (calc == null) return NotFound();
        calc.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
