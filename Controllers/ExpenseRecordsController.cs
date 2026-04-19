using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Accounts;
using QuotationAPI.V2.Models.Expense;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/expense-records")]
public class ExpenseRecordsController : ControllerBase
{
    private readonly QuotationDbContext _db;
    private const string ExpenseRecordReferencePrefix = "EXPREC:";

    public ExpenseRecordsController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseRecord>>> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? sourceModule,
        [FromQuery] string? sourceId)
    {
        var query = _db.ExpenseRecords.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(sourceModule))
        {
            query = query.Where(x => x.SourceModule == sourceModule);
        }

        if (!string.IsNullOrWhiteSpace(sourceId))
        {
            query = query.Where(x => x.SourceId == sourceId);
        }

        var results = await query
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseRecord>> GetById(string id)
    {
        var record = await _db.ExpenseRecords.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return record == null ? NotFound() : Ok(record);
    }

    [HttpGet("total")]
    public async Task<ActionResult<decimal>> GetTotal([FromQuery] string startDate, [FromQuery] string endDate)
    {
        var records = await _db.ExpenseRecords
            .Where(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.ExpenseDate))
            .ToListAsync();

        var total = records
            .Where(x => string.CompareOrdinal(x.ExpenseDate, startDate) >= 0
                && string.CompareOrdinal(x.ExpenseDate, endDate) <= 0)
            .Sum(x => x.Amount);

        return Ok(total);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseRecord>> Create([FromBody] CreateExpenseRequest request)
    {
        var count = await _db.ExpenseRecords.Where(x => !x.IsDeleted).CountAsync() + 1;
        var now = DateTime.UtcNow;
        var record = new ExpenseRecord
        {
            Id = ($"EXP-{Guid.NewGuid():N}")[..12],
            ExpenseNumber = $"EXP-{now.Year}-{count:D3}",
            Category = request.Category,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            PaidBy = request.PaidBy,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "cash" : request.PaymentMethod,
            Remarks = request.Remarks,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "draft" : request.Status,
            SourceModule = request.SourceModule,
            SourceId = request.SourceId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ExpenseRecords.Add(record);
        await SyncExpenseEntryAsync(record);
        await _db.SaveChangesAsync();
        return Ok(record);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExpenseRecord>> Update(string id, [FromBody] UpdateExpenseRequest request)
    {
        var record = await _db.ExpenseRecords.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (record == null)
        {
            return NotFound();
        }

        record.Category = request.Category ?? record.Category;
        record.Amount = request.Amount ?? record.Amount;
        record.ExpenseDate = request.ExpenseDate ?? record.ExpenseDate;
        record.PaidBy = request.PaidBy ?? record.PaidBy;
        record.PaymentMethod = request.PaymentMethod ?? record.PaymentMethod;
        record.Remarks = request.Remarks ?? record.Remarks;
        record.Status = request.Status ?? record.Status;
        record.UpdatedAt = DateTime.UtcNow;

        await SyncExpenseEntryAsync(record);
        await _db.SaveChangesAsync();
        return Ok(record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var record = await _db.ExpenseRecords.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (record == null)
        {
            return NotFound();
        }

        await RemoveExpenseEntryLinkAsync(record.Id);
        record.IsDeleted = true;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("sync-accounting")]
    public async Task<ActionResult<object>> SyncAccountingEntries()
    {
        var records = await _db.ExpenseRecords
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.ExpenseDate)
            .ToListAsync();

        foreach (var record in records)
        {
            await SyncExpenseEntryAsync(record);
        }

        await _db.SaveChangesAsync();

        var linkedEntries = await _db.ExpenseEntries
            .CountAsync(x => x.Reference != null && x.Reference.StartsWith(ExpenseRecordReferencePrefix));
        var activeLinkedEntries = await _db.ExpenseEntries
            .CountAsync(x => x.Reference != null && x.Reference.StartsWith(ExpenseRecordReferencePrefix)
                && x.Status == "active");

        return Ok(new
        {
            syncedRecords = records.Count,
            linkedEntries,
            activeLinkedEntries
        });
    }

    private async Task SyncExpenseEntryAsync(ExpenseRecord record)
    {
        var reference = BuildReference(record.Id);
        var existingEntry = await _db.ExpenseEntries.FirstOrDefaultAsync(x => x.Reference == reference);
        var shouldAffectAccounts = IsAccountingStatus(record.Status);

        if (existingEntry == null)
        {
            if (!shouldAffectAccounts)
            {
                return;
            }

            var newEntry = new ExpenseEntry
            {
                Id = Guid.NewGuid().ToString(),
                Description = string.IsNullOrWhiteSpace(record.Remarks)
                    ? $"Expense {record.ExpenseNumber}"
                    : record.Remarks,
                Amount = record.Amount,
                Type = NormalizeBalanceType(record.PaymentMethod),
                Date = string.IsNullOrWhiteSpace(record.ExpenseDate)
                    ? DateTime.UtcNow.ToString("yyyy-MM-dd")
                    : record.ExpenseDate,
                Category = string.IsNullOrWhiteSpace(record.Category) ? "Other" : record.Category,
                Reference = reference,
                Status = "active"
            };

            _db.ExpenseEntries.Add(newEntry);
            await ApplyBalanceDeltaAsync(newEntry.Type, -newEntry.Amount);
            return;
        }

        if (string.Equals(existingEntry.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyBalanceDeltaAsync(existingEntry.Type, existingEntry.Amount);
        }

        existingEntry.Description = string.IsNullOrWhiteSpace(record.Remarks)
            ? $"Expense {record.ExpenseNumber}"
            : record.Remarks;
        existingEntry.Amount = record.Amount;
        existingEntry.Type = NormalizeBalanceType(record.PaymentMethod);
        existingEntry.Date = string.IsNullOrWhiteSpace(record.ExpenseDate)
            ? existingEntry.Date
            : record.ExpenseDate;
        existingEntry.Category = string.IsNullOrWhiteSpace(record.Category) ? "Other" : record.Category;
        existingEntry.Status = shouldAffectAccounts ? "active" : "cancelled";

        if (string.Equals(existingEntry.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyBalanceDeltaAsync(existingEntry.Type, -existingEntry.Amount);
        }
    }

    private async Task RemoveExpenseEntryLinkAsync(string expenseRecordId)
    {
        var reference = BuildReference(expenseRecordId);
        var existingEntry = await _db.ExpenseEntries.FirstOrDefaultAsync(x => x.Reference == reference);
        if (existingEntry == null)
        {
            return;
        }

        if (string.Equals(existingEntry.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyBalanceDeltaAsync(existingEntry.Type, existingEntry.Amount);
        }

        existingEntry.IsDeleted = true;
        existingEntry.Status = "cancelled";
    }

    private async Task ApplyBalanceDeltaAsync(string balanceType, decimal delta)
    {
        var normalizedType = NormalizeBalanceType(balanceType);
        var balance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == normalizedType);
        if (balance == null)
        {
            balance = new BankCashBalance
            {
                Type = normalizedType,
                Balance = 0,
                Description = $"{char.ToUpper(normalizedType[0])}{normalizedType[1..]} account",
                LastUpdated = DateTime.UtcNow.ToString("O")
            };
            _db.BankCashBalances.Add(balance);
        }

        balance.Balance += delta;
        balance.LastUpdated = DateTime.UtcNow.ToString("O");
    }

    private static string BuildReference(string expenseRecordId) => $"{ExpenseRecordReferencePrefix}{expenseRecordId}";

    private static bool IsAccountingStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized != "draft" && normalized != "cancelled" && normalized != "void";
    }

    private static string NormalizeBalanceType(string? paymentMethod)
    {
        var normalized = (paymentMethod ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Contains("cash"))
        {
            return "cash";
        }

        return "bank";
    }
}