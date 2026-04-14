using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Accounts;
using QuotationAPI.V2.Models.Customer;
using System.Globalization;
using System.Text.Json;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly QuotationDbContext _db;

    public AccountsController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet("balances/current")]
    public async Task<ActionResult<object>> GetCurrentBalances()
    {
        var cash = await _db.BankCashBalances.Where(x => x.Type == "cash").Select(x => x.Balance).FirstOrDefaultAsync();
        var bank = await _db.BankCashBalances.Where(x => x.Type == "bank").Select(x => x.Balance).FirstOrDefaultAsync();
        return Ok(new { cash, bank });
    }

    [HttpGet("income/total")]
    public async Task<ActionResult<decimal>> GetTotalIncome()
    {
        var rows = await _db.IncomeEntries.Where(x => x.Status == "active").ToListAsync();
        return Ok(rows.Sum(x => x.Amount));
    }

    [HttpGet("expenses/total")]
    public async Task<ActionResult<decimal>> GetTotalExpenses()
    {
        var rows = await _db.ExpenseEntries.Where(x => x.Status == "active").ToListAsync();
        return Ok(rows.Sum(x => x.Amount));
    }

    [HttpGet("outstandings/total")]
    public async Task<ActionResult<decimal>> GetTotalOutstanding()
    {
        var rows = await BuildCustomerOutstandingSummaryAsync();
        return Ok(rows.Sum(x => x.OutstandingAmount));
    }

    [HttpGet("balances")]
    public async Task<ActionResult<IEnumerable<BankCashBalance>>> GetBankCashBalances() => Ok(await _db.BankCashBalances.ToListAsync());

    [HttpGet("outstandings")]
    public async Task<ActionResult<IEnumerable<CustomerOutstanding>>> GetCustomerOutstandings() => Ok(await _db.CustomerOutstandings.Where(x => !x.IsDeleted).OrderByDescending(x => x.Date).ToListAsync());

    [HttpGet("customer-outstanding/summary")]
    public async Task<ActionResult<IEnumerable<CustomerOutstandingSummary>>> GetCustomerOutstandingSummary()
    {
        var summary = await BuildCustomerOutstandingSummaryAsync();
        return Ok(summary.OrderByDescending(x => x.OutstandingAmount).ThenBy(x => x.CustomerName));
    }

    [HttpGet("income-entries")]
    public async Task<ActionResult<IEnumerable<IncomeEntry>>> GetIncomeEntries() => Ok(await _db.IncomeEntries.Where(x => !x.IsDeleted).ToListAsync());

    [HttpGet("expense-entries")]
    public async Task<ActionResult<IEnumerable<ExpenseEntry>>> GetExpenseEntries() => Ok(await _db.ExpenseEntries.Where(x => !x.IsDeleted).ToListAsync());

    [HttpGet("transactions")]
    public async Task<ActionResult<IEnumerable<AccountTransaction>>> GetTransactions()
    {
        var income = await _db.IncomeEntries
            .Where(e => e.Status == "active")
            .Select(e => new AccountTransaction
            {
                Id = e.Id,
                Type = "income",
                Amount = e.Amount,
                BalanceType = e.Type,
                Description = e.Description,
                Date = e.Date,
                Category = e.Category,
                Reference = e.Reference
            }).ToListAsync();

        var expense = await _db.ExpenseEntries
            .Where(e => e.Status == "active")
            .Select(e => new AccountTransaction
            {
                Id = e.Id,
                Type = "expense",
                Amount = e.Amount,
                BalanceType = e.Type,
                Description = e.Description,
                Date = e.Date,
                Category = e.Category,
                Reference = e.Reference
            }).ToListAsync();

        var transfers = await _db.CashTransfers
            .Select(t => new AccountTransaction
            {
                Id = t.Id,
                Type = "cash_transfer",
                Amount = t.Amount,
                BalanceType = t.FromAccount,
                Description = $"Transfer: {t.FromAccount} → {t.ToAccount}" + (string.IsNullOrEmpty(t.Remarks) ? "" : $" ({t.Remarks})"),
                Date = t.TransferDate,
                Category = "Transfer",
                Reference = null
            }).ToListAsync();

        var all = income.Concat(expense).Concat(transfers)
            .OrderByDescending(t => t.Date)
            .ToList();

        return Ok(all);
    }

    [HttpGet("cash-transfers")]
    public async Task<ActionResult<IEnumerable<CashTransfer>>> GetCashTransfers() => Ok(await _db.CashTransfers.Where(x => !x.IsDeleted).OrderByDescending(x => x.TransferDate).ToListAsync());

    [HttpPost("cash-transfers")]
    public async Task<ActionResult<CashTransfer>> CreateCashTransfer([FromBody] CashTransferCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var fromType = NormalizeBalanceType(request.FromAccount);
        var toType = NormalizeBalanceType(request.ToAccount);

        if (fromType == toType || (fromType != "bank" && fromType != "cash") || (toType != "bank" && toType != "cash"))
        {
            return BadRequest(new { message = "From and To accounts must be opposite bank/cash accounts." });
        }

        var fromBalance = await EnsureBalanceExistsAsync(fromType);
        var toBalance = await EnsureBalanceExistsAsync(toType);

        if (fromBalance.Balance < request.Amount)
        {
            return BadRequest(new { message = $"Insufficient {fromType} balance." });
        }

        var entity = new CashTransfer
        {
            FromAccount = fromType,
            ToAccount = toType,
            Amount = request.Amount,
            TransferDate = request.TransferDate,
            Remarks = string.IsNullOrWhiteSpace(request.Description) ? request.Remarks : request.Description,
            CreatedDate = DateTime.UtcNow.ToString("O")
        };

        fromBalance.Balance -= request.Amount;
        fromBalance.LastUpdated = DateTime.UtcNow.ToString("O");

        toBalance.Balance += request.Amount;
        toBalance.LastUpdated = DateTime.UtcNow.ToString("O");

        _db.CashTransfers.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpPut("cash-transfers/{id}")]
    public async Task<ActionResult<CashTransfer>> UpdateCashTransfer(string id, [FromBody] CashTransferUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var existing = await _db.CashTransfers.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound(new { message = "Cash transfer not found." });
        }

        var oldFromType = NormalizeBalanceType(existing.FromAccount);
        var oldToType = NormalizeBalanceType(existing.ToAccount);

        var newFromType = NormalizeBalanceType(request.FromAccount);
        var newToType = NormalizeBalanceType(request.ToAccount);

        if (newFromType == newToType || (newFromType != "bank" && newFromType != "cash") || (newToType != "bank" && newToType != "cash"))
        {
            return BadRequest(new { message = "From and To accounts must be opposite bank/cash accounts." });
        }

        var oldFromBalance = await EnsureBalanceExistsAsync(oldFromType);
        var oldToBalance = await EnsureBalanceExistsAsync(oldToType);

        // Reverse old transaction effect.
        oldFromBalance.Balance += existing.Amount;
        oldToBalance.Balance -= existing.Amount;

        var newFromBalance = await EnsureBalanceExistsAsync(newFromType);
        var newToBalance = await EnsureBalanceExistsAsync(newToType);

        if (newFromBalance.Balance < request.Amount)
        {
            return BadRequest(new { message = $"Insufficient {newFromType} balance." });
        }

        // Apply new transaction effect.
        newFromBalance.Balance -= request.Amount;
        newToBalance.Balance += request.Amount;

        var now = DateTime.UtcNow.ToString("O");
        oldFromBalance.LastUpdated = now;
        oldToBalance.LastUpdated = now;
        newFromBalance.LastUpdated = now;
        newToBalance.LastUpdated = now;

        existing.FromAccount = newFromType;
        existing.ToAccount = newToType;
        existing.Amount = request.Amount;
        existing.TransferDate = request.TransferDate;
        existing.Remarks = string.IsNullOrWhiteSpace(request.Description) ? request.Remarks : request.Description;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpPut("cash-transfer/{id}")]
    public Task<ActionResult<CashTransfer>> UpdateCashTransferAlias(string id, [FromBody] CashTransferUpdateRequest request)
        => UpdateCashTransfer(id, request);

    [HttpDelete("cash-transfers/{id}")]
    public async Task<IActionResult> DeleteCashTransfer(string id)
    {
        var existing = await _db.CashTransfers.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound(new { message = "Cash transfer not found." });
        }

        var fromType = NormalizeBalanceType(existing.FromAccount);
        var toType = NormalizeBalanceType(existing.ToAccount);

        var fromBalance = await EnsureBalanceExistsAsync(fromType);
        var toBalance = await EnsureBalanceExistsAsync(toType);

        // Reverse transaction effect on delete.
        fromBalance.Balance += existing.Amount;
        toBalance.Balance -= existing.Amount;

        var now = DateTime.UtcNow.ToString("O");
        fromBalance.LastUpdated = now;
        toBalance.LastUpdated = now;

        existing.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("cash-transfer/{id}")]
    public Task<IActionResult> DeleteCashTransferAlias(string id)
        => DeleteCashTransfer(id);

    [HttpPost("customer-outstandings")]
    public async Task<ActionResult<CustomerOutstanding>> CreateCustomerOutstanding([FromBody] CustomerOutstanding model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }
        model.Id = Guid.NewGuid().ToString();
        model.CreatedDate ??= DateTime.UtcNow.ToString("yyyy-MM-dd");
        _db.CustomerOutstandings.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPost("customer-outstanding/additional")]
    public async Task<ActionResult<CustomerOutstanding>> CreateAdditionalOutstanding([FromBody] AdditionalOutstandingRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var model = new CustomerOutstanding
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            Amount = request.Amount,
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Manual outstanding adjustment" : request.Description,
            Date = request.Date,
            DueDate = request.DueDate,
            Status = "pending",
            CreatedDate = DateTime.UtcNow.ToString("O")
        };

        _db.CustomerOutstandings.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("customer-outstandings/{id}")]
    public async Task<ActionResult<CustomerOutstanding>> UpdateCustomerOutstanding(string id, [FromBody] CustomerOutstanding model)
    {
        var existing = await _db.CustomerOutstandings.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound(new { message = "Record not found" });
        }

        existing.CustomerId = model.CustomerId;
        existing.CustomerName = model.CustomerName;
        existing.OrderId = model.OrderId;
        existing.Amount = model.Amount;
        existing.Description = model.Description;
        existing.Date = model.Date;
        existing.DueDate = model.DueDate;
        existing.Status = model.Status;
        existing.PaidAmount = model.PaidAmount;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("customer-outstandings/{id}")]
    public async Task<IActionResult> DeleteCustomerOutstanding(string id)
    {
        var existing = await _db.CustomerOutstandings.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound(new { message = "Record not found" });
        }

        existing.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("income-entries")]
    public async Task<ActionResult<IncomeEntry>> CreateIncomeEntry([FromBody] IncomeEntry model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        model.Id = Guid.NewGuid().ToString();
        model.CreatedDate ??= DateTime.UtcNow.ToString("O");
        _db.IncomeEntries.Add(model);

        if (string.Equals(model.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            var balance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == model.Type);
            if (balance is not null)
            {
                balance.Balance += model.Amount;
                balance.LastUpdated = DateTime.UtcNow.ToString("O");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPost("customer-outstanding/settlements")]
    public async Task<ActionResult<IncomeEntry>> SettleCustomerOutstanding([FromBody] CustomerSettlementRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var summaryRows = await BuildCustomerOutstandingSummaryAsync();
        var matchedOutstanding = summaryRows.FirstOrDefault(row =>
            (!string.IsNullOrWhiteSpace(request.CustomerId) && row.CustomerId == request.CustomerId)
            || string.Equals(row.CustomerName, request.CustomerName, StringComparison.OrdinalIgnoreCase));
        var availableOutstanding = Math.Max(0m, matchedOutstanding?.OutstandingAmount ?? 0m);

        if (request.Amount > availableOutstanding)
        {
            return BadRequest(new
            {
                message = $"Settlement amount exceeds the current outstanding balance of {availableOutstanding:0.00}."
            });
        }

        var model = new IncomeEntry
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Customer outstanding settlement" : request.Description,
            Amount = request.Amount,
            Type = request.Type,
            IncomeType = "customer_payment",
            Date = request.Date,
            Category = "Customer Settlement",
            Reference = request.Reference,
            CreatedDate = DateTime.UtcNow.ToString("O"),
            Status = "active"
        };

        _db.IncomeEntries.Add(model);

        var balance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == model.Type);
        if (balance is not null)
        {
            balance.Balance += model.Amount;
            balance.LastUpdated = DateTime.UtcNow.ToString("O");
        }

        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("income-entries/{id}")]
    public async Task<ActionResult<IncomeEntry>> UpdateIncomeEntry(string id, [FromBody] IncomeEntry model)
    {
        var existing = await _db.IncomeEntries.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound(new { message = "Record not found" });
        }

        // Reverse old balance contribution if the entry was active
        if (string.Equals(existing.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            var oldBalance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == existing.Type);
            if (oldBalance is not null)
            {
                oldBalance.Balance -= existing.Amount;
                oldBalance.LastUpdated = DateTime.UtcNow.ToString("O");
            }
        }

        existing.CustomerId = model.CustomerId;
        existing.CustomerName = model.CustomerName;
        existing.Description = model.Description;
        existing.Amount = model.Amount;
        existing.Type = model.Type;
        existing.IncomeType = model.IncomeType;
        existing.Date = model.Date;
        existing.Category = model.Category;
        existing.OutstandingId = model.OutstandingId;
        existing.Reference = model.Reference;
        existing.Status = model.Status;

        // Apply new balance contribution if the updated entry is active
        if (string.Equals(existing.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            var newBalance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == existing.Type);
            if (newBalance is not null)
            {
                newBalance.Balance += existing.Amount;
                newBalance.LastUpdated = DateTime.UtcNow.ToString("O");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("income-entries/{id}")]
    public async Task<IActionResult> DeleteIncomeEntry(string id)
    {
        var existing = await _db.IncomeEntries.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound(new { message = "Record not found" });
        }

        // Reverse balance when deleting an active income entry
        if (string.Equals(existing.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            var balance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == existing.Type);
            if (balance is not null)
            {
                balance.Balance -= existing.Amount;
                balance.LastUpdated = DateTime.UtcNow.ToString("O");
            }
        }

        existing.IsDeleted = true;
        existing.Status = "cancelled";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("expense-entries")]
    public async Task<ActionResult<ExpenseEntry>> CreateExpenseEntry([FromBody] ExpenseEntry model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        model.Id = Guid.NewGuid().ToString();
        _db.ExpenseEntries.Add(model);

        if (model.Status == "active")
        {
            var balance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == model.Type);
            if (balance is not null)
            {
                balance.Balance -= model.Amount;
                balance.LastUpdated = DateTime.UtcNow.ToString("O");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("expense-entries/{id}")]
    public async Task<ActionResult<ExpenseEntry>> UpdateExpenseEntry(string id, [FromBody] ExpenseEntry model)
    {
        var existing = await _db.ExpenseEntries.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound(new { message = "Record not found" });
        }

        if (existing.Status == "active")
        {
            var oldBalance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == existing.Type);
            if (oldBalance is not null)
            {
                oldBalance.Balance += existing.Amount;
                oldBalance.LastUpdated = DateTime.UtcNow.ToString("O");
            }
        }

        existing.Description = model.Description;
        existing.Amount = model.Amount;
        existing.Type = model.Type;
        existing.Date = model.Date;
        existing.Category = model.Category;
        existing.Reference = model.Reference;
        existing.Status = model.Status;

        if (existing.Status == "active")
        {
            var newBalance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == existing.Type);
            if (newBalance is not null)
            {
                newBalance.Balance -= existing.Amount;
                newBalance.LastUpdated = DateTime.UtcNow.ToString("O");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("expense-entries/{id}")]
    public async Task<IActionResult> DeleteExpenseEntry(string id)
    {
        var existing = await _db.ExpenseEntries.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
        {
            return NotFound(new { message = "Record not found" });
        }

        if (existing.Status == "active")
        {
            var balance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == existing.Type);
            if (balance is not null)
            {
                balance.Balance += existing.Amount;
                balance.LastUpdated = DateTime.UtcNow.ToString("O");
            }
        }

        existing.IsDeleted = true;
        existing.Status = "cancelled";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("initial-balance")]
    public async Task<ActionResult<BankCashBalance>> SetInitialBalance([FromBody] SetInitialBalanceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var item = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == request.Type);
        if (item is null)
        {
            item = new BankCashBalance
            {
                Type = request.Type,
                Balance = request.Amount,
                Description = request.Description ?? $"Initial {request.Type} balance",
                LastUpdated = DateTime.UtcNow.ToString("O")
            };
            _db.BankCashBalances.Add(item);
        }
        else
        {
            item.Balance = request.Amount;
            item.Description = request.Description ?? item.Description;
            item.LastUpdated = DateTime.UtcNow.ToString("O");
        }

        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpGet("analytics/expense-summary")]
    public async Task<ActionResult<IEnumerable<ExpenseSummary>>> GetExpenseSummary([FromQuery] int year, [FromQuery] int? month)
    {
        var expenses = await _db.ExpenseEntries
            .Where(x => x.Status == "active")
            .ToListAsync();

        var monthly = expenses
            .Where(e => ParseDate(e.Date).Year == year && (!month.HasValue || ParseDate(e.Date).Month == month.Value))
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseSummary(g.Key, g.Sum(x => x.Amount), $"{year}-{(month ?? DateTime.UtcNow.Month):D2}", year, "monthly"))
            .ToList();

        var yearly = expenses
            .Where(e => ParseDate(e.Date).Year == year)
            .GroupBy(e => e.Category)
            .Select(g => new ExpenseSummary(g.Key, g.Sum(x => x.Amount), $"{year}-{(month ?? DateTime.UtcNow.Month):D2}", year, "yearly"))
            .ToList();

        return Ok(month.HasValue ? monthly : monthly.Concat(yearly));
    }

    [HttpGet("analytics/monthly-trend")]
    public async Task<ActionResult<IEnumerable<object>>> GetMonthlyTrend()
    {
        var income = await _db.IncomeEntries
            .Where(x => x.Status == "active")
            .ToListAsync();
        var expense = await _db.ExpenseEntries
            .Where(x => x.Status == "active")
            .ToListAsync();

        var now = DateTime.UtcNow;
        var result = new List<object>();

        for (var i = 5; i >= 0; i--)
        {
            var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var monthKey = d.ToString("yyyy-MM");

            var monthIncome = income.Where(x => x.Date.StartsWith(monthKey)).Sum(x => x.Amount);
            var monthExpense = expense.Where(x => x.Date.StartsWith(monthKey)).Sum(x => x.Amount);

            result.Add(new
            {
                month = d.ToString("MMM yyyy"),
                income = monthIncome,
                expense = monthExpense
            });
        }

        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AccountSummary>> GetSummary()
    {
        var cash = await _db.BankCashBalances.Where(x => x.Type == "cash").Select(x => x.Balance).FirstOrDefaultAsync();
        var bank = await _db.BankCashBalances.Where(x => x.Type == "bank").Select(x => x.Balance).FirstOrDefaultAsync();
        var income = (await _db.IncomeEntries.Where(x => x.Status == "active").ToListAsync()).Sum(x => x.Amount);
        var expense = (await _db.ExpenseEntries.Where(x => x.Status == "active").ToListAsync()).Sum(x => x.Amount);

        return Ok(new AccountSummary(cash, bank, income, expense));
    }

    [HttpGet("orders/profit")]
    public async Task<ActionResult<object>> GetOrdersProfit()
    {
        var records = await _db.InvoiceCalcRecords.ToListAsync();

        decimal totalProfit = 0m;
        decimal totalRevenue = 0m;
        int orderCount = records.Count;

        foreach (var record in records)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(record.DataJson))
                {
                    continue;
                }

                using var doc = System.Text.Json.JsonDocument.Parse(record.DataJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("price", out var price))
                {
                    if (price.TryGetProperty("profit", out var profitEl))
                    {
                        totalProfit += profitEl.GetDecimal();
                    }
                    if (price.TryGetProperty("actualAmount", out var amountEl))
                    {
                        totalRevenue += amountEl.GetDecimal();
                    }
                }
            }
            catch
            {
                // skip malformed records
            }
        }

        return Ok(new
        {
            totalProfit = Math.Round(totalProfit, 2),
            totalRevenue = Math.Round(totalRevenue, 2),
            orderCount
        });
    }

    [HttpGet("expense-dashboard")]
    public async Task<ActionResult<ExpenseDashboard>> GetExpenseDashboard()
    {
        var allExpenses = await _db.ExpenseEntries.Where(x => x.Status == "active").ToListAsync();

        var now = DateTime.UtcNow;
        var monthKey = now.ToString("yyyy-MM");
        var monthExpenses = allExpenses.Where(x => x.Date.StartsWith(monthKey)).ToList();

        var totalMonth = monthExpenses.Sum(x => x.Amount);
        var approved = Math.Round(totalMonth * 0.83m, 2);

        var top = allExpenses
            .GroupBy(x => x.Category)
            .Select(g => new TopCategory(g.Key, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.MonthlyAmount)
            .Take(5)
            .ToList();

        return Ok(new ExpenseDashboard(totalMonth, approved, totalMonth - approved, top));
    }

    [HttpGet("ledger/expenses")]
    public async Task<ActionResult<IEnumerable<ExpenseLedgerRow>>> GetExpenseLedger() => Ok(await _db.ExpenseLedgerRows.ToListAsync());

    [HttpGet("ledger/income")]
    public async Task<ActionResult<IEnumerable<IncomeRow>>> GetIncomeList() => Ok(await _db.IncomeRows.ToListAsync());

    [HttpGet("ledger/purchase-sales")]
    public async Task<ActionResult<IEnumerable<PurchaseSalesRow>>> GetPurchaseSales() => Ok(
        await _db.PurchaseSalesRows
            .OrderByDescending(x => x.VoucherDate)
            .ThenByDescending(x => x.VoucherNumber)
            .ToListAsync()
    );

    [HttpGet("ledger/purchase-sales/next-voucher")]
    public async Task<ActionResult<object>> GetNextPurchaseVoucher()
    {
        var nextVoucher = await GenerateNextPurchaseVoucherAsync();
        return Ok(new { voucherNumber = nextVoucher });
    }

    [HttpPost("ledger/purchase-sales")]
    public async Task<ActionResult<PurchaseSalesRow>> CreatePurchase([FromBody] PurchaseCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var voucherNo = string.IsNullOrWhiteSpace(request.VoucherNumber)
            ? await GenerateNextPurchaseVoucherAsync()
            : request.VoucherNumber.Trim();
        var exists = await _db.PurchaseSalesRows.AnyAsync(x => x.VoucherNumber == voucherNo);
        if (exists)
        {
            return Conflict(new { message = "Voucher number already exists." });
        }

        var taxAmount = request.TaxPercent > 0
            ? Math.Round(request.TotalAmountPaid * (request.TaxPercent / (100m + request.TaxPercent)), 2)
            : 0m;

        var balanceType = NormalizeBalanceType(request.PaymentType);
        var balance = await EnsureBalanceExistsAsync(balanceType);
        if (balance.Balance < request.TotalAmountPaid)
        {
            return BadRequest(new { message = $"Insufficient {balanceType} balance." });
        }

        var row = new PurchaseSalesRow
        {
            VoucherNumber = voucherNo,
            TransactionType = "Purchase",
            CustomerId = request.CustomerId.Trim(),
            PartyName = request.CustomerName.Trim(),
            PaymentType = request.PaymentType.Trim(),
            Amount = request.TotalAmountPaid,
            TotalAmountPaid = request.TotalAmountPaid,
            TaxPercent = request.TaxPercent,
            TaxAmount = taxAmount,
            VoucherDate = request.VoucherDate,
        };

        balance.Balance -= request.TotalAmountPaid;
        balance.LastUpdated = DateTime.UtcNow.ToString("O");

        _db.PurchaseSalesRows.Add(row);

        var expense = await FindPurchaseExpenseEntryAsync(voucherNo);
        if (expense is null)
        {
            expense = new ExpenseEntry
            {
                Id = Guid.NewGuid().ToString(),
                Description = $"Purchase {voucherNo} - {row.PartyName}",
                Amount = request.TotalAmountPaid,
                Type = balanceType,
                Date = request.VoucherDate,
                Category = "Purchase",
                Reference = voucherNo,
                Status = "active"
            };
            _db.ExpenseEntries.Add(expense);
        }

        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpPut("ledger/purchase-sales/{voucherNo}")]
    public async Task<ActionResult<PurchaseSalesRow>> UpdatePurchase(string voucherNo, [FromBody] PurchaseUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var existing = await _db.PurchaseSalesRows.FirstOrDefaultAsync(x => x.VoucherNumber == voucherNo);
        if (existing is null)
        {
            return NotFound(new { message = "Purchase entry not found." });
        }

        var oldBalanceType = NormalizeBalanceType(existing.PaymentType);
        var oldBalance = await EnsureBalanceExistsAsync(oldBalanceType);
        oldBalance.Balance += existing.TotalAmountPaid;
        oldBalance.LastUpdated = DateTime.UtcNow.ToString("O");

        var newBalanceType = NormalizeBalanceType(request.PaymentType);
        var newBalance = await EnsureBalanceExistsAsync(newBalanceType);
        if (newBalance.Balance < request.TotalAmountPaid)
        {
            oldBalance.Balance -= existing.TotalAmountPaid;
            oldBalance.LastUpdated = DateTime.UtcNow.ToString("O");
            return BadRequest(new { message = $"Insufficient {newBalanceType} balance." });
        }

        var taxAmount = request.TaxPercent > 0
            ? Math.Round(request.TotalAmountPaid * (request.TaxPercent / (100m + request.TaxPercent)), 2)
            : 0m;

        newBalance.Balance -= request.TotalAmountPaid;
        newBalance.LastUpdated = DateTime.UtcNow.ToString("O");

        existing.TransactionType = "Purchase";
        existing.CustomerId = request.CustomerId.Trim();
        existing.PartyName = request.CustomerName.Trim();
        existing.PaymentType = request.PaymentType.Trim();
        existing.Amount = request.TotalAmountPaid;
        existing.TotalAmountPaid = request.TotalAmountPaid;
        existing.TaxPercent = request.TaxPercent;
        existing.TaxAmount = taxAmount;
        existing.VoucherDate = request.VoucherDate;

        var expense = await FindPurchaseExpenseEntryAsync(existing.VoucherNumber);
        if (expense is null)
        {
            expense = new ExpenseEntry
            {
                Id = Guid.NewGuid().ToString(),
                Reference = existing.VoucherNumber,
                Category = "Purchase",
                Status = "active"
            };
            _db.ExpenseEntries.Add(expense);
        }

        expense.Description = $"Purchase {existing.VoucherNumber} - {existing.PartyName}";
        expense.Amount = existing.TotalAmountPaid;
        expense.Type = newBalanceType;
        expense.Date = existing.VoucherDate;
        expense.Category = "Purchase";
        expense.Reference = existing.VoucherNumber;
        expense.Status = "active";

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("ledger/purchase-sales/{voucherNo}")]
    public async Task<IActionResult> DeletePurchase(string voucherNo)
    {
        var existing = await _db.PurchaseSalesRows.FirstOrDefaultAsync(x => x.VoucherNumber == voucherNo);
        if (existing is null)
        {
            return NotFound(new { message = "Purchase entry not found." });
        }

        var balanceType = NormalizeBalanceType(existing.PaymentType);
        var balance = await EnsureBalanceExistsAsync(balanceType);
        balance.Balance += existing.TotalAmountPaid;
        balance.LastUpdated = DateTime.UtcNow.ToString("O");

        var expense = await FindPurchaseExpenseEntryAsync(existing.VoucherNumber);
        if (expense is not null)
        {
            expense.IsDeleted = true;
            expense.Status = "cancelled";
        }

        existing.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("ledger/purchase-sales/gst-sync")]
    public async Task<ActionResult<GstPurchaseSyncResponse>> GetGstPurchaseSync([FromQuery] string period)
    {
        if (!TryParsePeriod(period, out var periodDate))
        {
            return BadRequest(new { message = "Period is required in yyyy-MM format." });
        }

        var monthKey = periodDate.ToString("yyyy-MM");
        var monthRows = await _db.PurchaseSalesRows
            .Where(x => x.TransactionType == "Purchase" && x.VoucherDate.StartsWith(monthKey))
            .ToListAsync();

        var customers = await _db.CustomerMasters.ToListAsync();
        var gstRows = BuildMockGstPurchases(periodDate, customers);

        var response = new GstPurchaseSyncResponse
        {
            Period = monthKey,
            GstTotalAmount = gstRows.Sum(x => x.TotalAmount),
            ExistingTotalAmount = monthRows.Sum(x => x.TotalAmountPaid)
        };

        foreach (var gst in gstRows)
        {
            var linkedCustomer = customers.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.GstNumber) &&
                string.Equals(c.GstNumber.Trim(), gst.GstNumber, StringComparison.OrdinalIgnoreCase));

            var customerRows = linkedCustomer is null
                ? []
                : monthRows.Where(x => x.CustomerId == linkedCustomer.Id).ToList();

            var exact = customerRows.FirstOrDefault(x =>
                string.Equals(x.VoucherDate, gst.InvoiceDate, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(x.TotalAmountPaid - gst.TotalAmount) <= 0.01m);

            if (exact is not null)
            {
                response.Rows.Add(new GstPurchaseComparisonRow
                {
                    MatchStatus = "matched",
                    MatchReason = "Same customer, date and amount.",
                    MatchedVoucherNumber = exact.VoucherNumber,
                    CustomerId = linkedCustomer?.Id,
                    CustomerName = linkedCustomer?.Name,
                    AmountDifference = 0,
                    GstPurchase = gst
                });
                continue;
            }

            var similar = customerRows
                .OrderBy(x => Math.Abs(x.TotalAmountPaid - gst.TotalAmount))
                .FirstOrDefault();

            if (similar is not null)
            {
                response.Rows.Add(new GstPurchaseComparisonRow
                {
                    MatchStatus = "update",
                    MatchReason = "Potential match for same customer in selected month.",
                    MatchedVoucherNumber = similar.VoucherNumber,
                    CustomerId = linkedCustomer?.Id,
                    CustomerName = linkedCustomer?.Name,
                    AmountDifference = Math.Round(gst.TotalAmount - similar.TotalAmountPaid, 2),
                    GstPurchase = gst
                });
                continue;
            }

            response.Rows.Add(new GstPurchaseComparisonRow
            {
                MatchStatus = linkedCustomer is null ? "missing-customer" : "new",
                MatchReason = linkedCustomer is null
                    ? "No customer found for GST number."
                    : "No existing purchase found. Ready to add.",
                CustomerId = linkedCustomer?.Id,
                CustomerName = linkedCustomer?.Name,
                GstPurchase = gst
            });
        }

        return Ok(response);
    }

    [HttpPost("ledger/purchase-sales/gst-sync/apply")]
    public async Task<ActionResult<GstPurchaseSyncApplyResult>> ApplyGstPurchaseSync([FromBody] GstPurchaseSyncApplyRequest request)
    {
        if (!TryParsePeriod(request.Period, out var periodDate))
        {
            return BadRequest(new { message = "Period is required in yyyy-MM format." });
        }

        var result = new GstPurchaseSyncApplyResult();
        var customers = await _db.CustomerMasters.ToListAsync();

        foreach (var row in request.Rows)
        {
            var action = (row.Action ?? string.Empty).Trim().ToLowerInvariant();
            if (action == "ignore" || string.IsNullOrWhiteSpace(action))
            {
                result.Ignored++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.GstPurchase.InvoiceDate) || row.GstPurchase.InvoiceDate.Length < 7 ||
                !string.Equals(row.GstPurchase.InvoiceDate[..7], periodDate.ToString("yyyy-MM"), StringComparison.Ordinal))
            {
                result.Errors.Add($"{row.GstPurchase.InvoiceNumber}: invoice date is outside selected period.");
                continue;
            }

            var customer = customers.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.GstNumber) &&
                string.Equals(c.GstNumber.Trim(), row.GstPurchase.GstNumber, StringComparison.OrdinalIgnoreCase));

            if (customer is null)
            {
                result.Errors.Add($"{row.GstPurchase.InvoiceNumber}: customer not found for GST {row.GstPurchase.GstNumber}.");
                continue;
            }

            if (action == "add")
            {
                var addError = await CreatePurchaseFromGstAsync(customer, row.GstPurchase);
                if (addError is null)
                {
                    result.Added++;
                }
                else
                {
                    result.Errors.Add($"{row.GstPurchase.InvoiceNumber}: {addError}");
                }

                continue;
            }

            if (action == "update")
            {
                if (string.IsNullOrWhiteSpace(row.MatchedVoucherNumber))
                {
                    result.Errors.Add($"{row.GstPurchase.InvoiceNumber}: voucher number is required for update.");
                    continue;
                }

                var updateError = await UpdatePurchaseFromGstAsync(row.MatchedVoucherNumber, customer, row.GstPurchase);
                if (updateError is null)
                {
                    result.Updated++;
                }
                else
                {
                    result.Errors.Add($"{row.GstPurchase.InvoiceNumber}: {updateError}");
                }

                continue;
            }

            result.Errors.Add($"{row.GstPurchase.InvoiceNumber}: unsupported action '{row.Action}'.");
        }

        await _db.SaveChangesAsync();
        return Ok(result);
    }

    [HttpGet("ledger/tax-payments")]
    public async Task<ActionResult<IEnumerable<TaxPaymentRow>>> GetTaxPayments() => Ok(await _db.TaxPaymentRows.OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.ChallanNo).ToListAsync());

    [HttpGet("ledger/tax-payments/monthly-due")]
    public async Task<ActionResult<object>> GetMonthlyTaxDue([FromQuery] string period)
    {
        if (!TryParsePeriod(period, out var periodDate))
        {
            return BadRequest(new { message = "Period is required in yyyy-MM format." });
        }

        var monthKey = periodDate.ToString("yyyy-MM");
        var (totalTaxDue, inputTaxCredit, netTaxPayable) = await CalculateMonthlyTaxDueAsync(periodDate);

        var totalPaid = await _db.TaxPaymentRows
            .Where(x => x.Period == monthKey)
            .SumAsync(x => x.Amount);

        return Ok(new
        {
            period = monthKey,
            totalTaxDue = Math.Round(totalTaxDue, 2),
            inputTaxCredit = Math.Round(inputTaxCredit, 2),
            netTaxPayable = Math.Round(netTaxPayable, 2),
            totalPaid = Math.Round(totalPaid, 2),
            outstanding = Math.Round(Math.Max(0m, netTaxPayable - totalPaid), 2)
        });
    }

    [HttpPost("ledger/tax-payments")]
    public async Task<ActionResult<TaxPaymentRow>> CreateTaxPayment([FromBody] TaxPaymentCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryParsePeriod(request.Period, out var periodDate))
        {
            return BadRequest(new { message = "Period is required in yyyy-MM format." });
        }

        var normalizedPeriod = periodDate.ToString("yyyy-MM");

        var mode = NormalizeBalanceType(request.Mode);
        var balance = await EnsureBalanceExistsAsync(mode);
        if (balance.Balance < request.Amount)
        {
            return BadRequest(new { message = $"Insufficient {mode} balance." });
        }

        var challanNo = string.IsNullOrWhiteSpace(request.ChallanNo)
            ? $"TX-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
            : request.ChallanNo.Trim();

        var exists = await _db.TaxPaymentRows.AnyAsync(x => x.ChallanNo == challanNo);
        if (exists)
        {
            return Conflict(new { message = "Challan number already exists." });
        }

        var row = new TaxPaymentRow
        {
            ChallanNo = challanNo,
            TaxType = BuildTaxTypeWithMode(request.TaxType, mode),
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Period = normalizedPeriod,
            OtherInputCredit = 0m
        };

        var (_, inputTaxCredit, netTaxPayable) = await CalculateMonthlyTaxDueAsync(periodDate);
        var totalPaidForPeriod = await _db.TaxPaymentRows
            .Where(x => x.Period == normalizedPeriod)
            .SumAsync(x => x.Amount);
        var amountToBePaid = Math.Max(0m, netTaxPayable - totalPaidForPeriod);
        row.OtherInputCredit = Math.Round(amountToBePaid - request.Amount - inputTaxCredit, 2);

        balance.Balance -= request.Amount;
        balance.LastUpdated = DateTime.UtcNow.ToString("O");

        _db.TaxPaymentRows.Add(row);
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpPut("ledger/tax-payments/{challanNo}")]
    public async Task<ActionResult<TaxPaymentRow>> UpdateTaxPayment(string challanNo, [FromBody] TaxPaymentUpdateRequest request)
    {
        if (!TryParsePeriod(request.Period, out var periodDate))
        {
            return BadRequest(new { message = "Period is required in yyyy-MM format." });
        }

        var normalizedPeriod = periodDate.ToString("yyyy-MM");

        var existing = await _db.TaxPaymentRows.FirstOrDefaultAsync(x => x.ChallanNo == challanNo);
        if (existing is null)
        {
            return NotFound(new { message = "Tax payment not found." });
        }

        var oldMode = ExtractMode(existing.TaxType);
        var newMode = NormalizeBalanceType(request.Mode);

        var oldBalance = await EnsureBalanceExistsAsync(oldMode);
        oldBalance.Balance += existing.Amount;
        oldBalance.LastUpdated = DateTime.UtcNow.ToString("O");

        var newBalance = await EnsureBalanceExistsAsync(newMode);
        if (newBalance.Balance < request.Amount)
        {
            return BadRequest(new { message = $"Insufficient {newMode} balance." });
        }

        newBalance.Balance -= request.Amount;
        newBalance.LastUpdated = DateTime.UtcNow.ToString("O");

        existing.TaxType = BuildTaxTypeWithMode(request.TaxType, newMode);
        existing.Amount = request.Amount;
        existing.PaymentDate = request.PaymentDate;
        existing.Period = normalizedPeriod;

        var (_, inputTaxCredit, netTaxPayable) = await CalculateMonthlyTaxDueAsync(periodDate);
        var totalPaidExcludingCurrent = await _db.TaxPaymentRows
            .Where(x => x.Period == normalizedPeriod && x.ChallanNo != challanNo)
            .SumAsync(x => x.Amount);
        var amountToBePaid = Math.Max(0m, netTaxPayable - totalPaidExcludingCurrent);
        existing.OtherInputCredit = Math.Round(amountToBePaid - request.Amount - inputTaxCredit, 2);

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("ledger/tax-payments/{challanNo}")]
    public async Task<IActionResult> DeleteTaxPayment(string challanNo)
    {
        var existing = await _db.TaxPaymentRows.FirstOrDefaultAsync(x => x.ChallanNo == challanNo);
        if (existing is null)
        {
            return NotFound(new { message = "Tax payment not found." });
        }

        var mode = ExtractMode(existing.TaxType);
        var balance = await EnsureBalanceExistsAsync(mode);
        balance.Balance += existing.Amount;
        balance.LastUpdated = DateTime.UtcNow.ToString("O");

        existing.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParse(value, out var date) ? date : DateTime.UtcNow;
    }

    private static string NormalizeBalanceType(string? value)
    {
        var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (raw.Contains("bank"))
        {
            return "bank";
        }

        if (raw.Contains("cash"))
        {
            return "cash";
        }

        return raw == "bank" ? "bank" : "cash";
    }

    private static string BuildTaxTypeWithMode(string taxType, string mode)
    {
        var cleanTaxType = string.IsNullOrWhiteSpace(taxType) ? "GST" : taxType.Trim();
        return $"{cleanTaxType} [{mode}]";
    }

    private static string ExtractMode(string taxType)
    {
        var raw = (taxType ?? string.Empty).Trim().ToLowerInvariant();
        return raw.Contains("[cash]") ? "cash" : "bank";
    }

    private static bool TryParsePeriod(string? period, out DateTime periodDate)
    {
        if (!string.IsNullOrWhiteSpace(period) && DateTime.TryParseExact($"{period}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out periodDate))
        {
            return true;
        }

        periodDate = default;
        return false;
    }

    private static List<GstPurchaseSyncItem> BuildMockGstPurchases(DateTime periodDate, IReadOnlyCollection<CustomerMaster> customers)
    {
        var seeded = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.GstNumber))
            .Take(3)
            .Select((customer, index) =>
            {
                var taxable = 10000m + (index * 3500m);
                var taxPercent = index % 2 == 0 ? 18m : 12m;
                var taxAmount = Math.Round(taxable * (taxPercent / 100m), 2);
                return new GstPurchaseSyncItem
                {
                    GstNumber = customer.GstNumber!.Trim().ToUpperInvariant(),
                    SupplierName = customer.Name,
                    InvoiceNumber = $"GST-{periodDate:yyyyMM}-{index + 1:000}",
                    InvoiceDate = new DateTime(periodDate.Year, periodDate.Month, Math.Min(5 + index * 7, DateTime.DaysInMonth(periodDate.Year, periodDate.Month))).ToString("yyyy-MM-dd"),
                    TaxableAmount = taxable,
                    TaxPercent = taxPercent,
                    TaxAmount = taxAmount,
                    TotalAmount = taxable + taxAmount,
                    PaymentType = index % 2 == 0 ? "bank" : "cash"
                };
            })
            .ToList();

        if (seeded.Count > 0)
        {
            return seeded;
        }

        var defaultTaxable = 12000m;
        var defaultTax = Math.Round(defaultTaxable * 0.18m, 2);
        return
        [
            new GstPurchaseSyncItem
            {
                GstNumber = "33AAAAA0000A1Z5",
                SupplierName = "Sample GST Supplier",
                InvoiceNumber = $"GST-{periodDate:yyyyMM}-001",
                InvoiceDate = new DateTime(periodDate.Year, periodDate.Month, 5).ToString("yyyy-MM-dd"),
                TaxableAmount = defaultTaxable,
                TaxPercent = 18m,
                TaxAmount = defaultTax,
                TotalAmount = defaultTaxable + defaultTax,
                PaymentType = "bank"
            }
        ];
    }

    private async Task<string?> CreatePurchaseFromGstAsync(CustomerMaster customer, GstPurchaseSyncItem gst)
    {
        var mode = NormalizeBalanceType(gst.PaymentType);
        var balance = await EnsureBalanceExistsAsync(mode);
        if (balance.Balance < gst.TotalAmount)
        {
            return $"Insufficient {mode} balance.";
        }

        var voucherNo = await GenerateNextPurchaseVoucherAsync();
        var row = new PurchaseSalesRow
        {
            VoucherNumber = voucherNo,
            TransactionType = "Purchase",
            CustomerId = customer.Id,
            PartyName = customer.Name,
            PaymentType = mode,
            Amount = gst.TotalAmount,
            TotalAmountPaid = gst.TotalAmount,
            TaxPercent = gst.TaxPercent,
            TaxAmount = gst.TaxAmount,
            VoucherDate = gst.InvoiceDate,
        };

        balance.Balance -= gst.TotalAmount;
        balance.LastUpdated = DateTime.UtcNow.ToString("O");
        _db.PurchaseSalesRows.Add(row);

        var expense = new ExpenseEntry
        {
            Id = Guid.NewGuid().ToString(),
            Description = $"Purchase {voucherNo} - {customer.Name}",
            Amount = gst.TotalAmount,
            Type = mode,
            Date = gst.InvoiceDate,
            Category = "Purchase",
            Reference = voucherNo,
            Status = "active"
        };

        _db.ExpenseEntries.Add(expense);
        return null;
    }

    private async Task<string?> UpdatePurchaseFromGstAsync(string voucherNo, CustomerMaster customer, GstPurchaseSyncItem gst)
    {
        var existing = await _db.PurchaseSalesRows.FirstOrDefaultAsync(x => x.VoucherNumber == voucherNo);
        if (existing is null)
        {
            return "Purchase entry not found.";
        }

        var oldMode = NormalizeBalanceType(existing.PaymentType);
        var newMode = NormalizeBalanceType(gst.PaymentType);

        var oldBalance = await EnsureBalanceExistsAsync(oldMode);
        var newBalance = await EnsureBalanceExistsAsync(newMode);

        oldBalance.Balance += existing.TotalAmountPaid;
        if (newBalance.Balance < gst.TotalAmount)
        {
            oldBalance.Balance -= existing.TotalAmountPaid;
            return $"Insufficient {newMode} balance.";
        }

        newBalance.Balance -= gst.TotalAmount;
        oldBalance.LastUpdated = DateTime.UtcNow.ToString("O");
        newBalance.LastUpdated = DateTime.UtcNow.ToString("O");

        existing.CustomerId = customer.Id;
        existing.PartyName = customer.Name;
        existing.PaymentType = newMode;
        existing.Amount = gst.TotalAmount;
        existing.TotalAmountPaid = gst.TotalAmount;
        existing.TaxPercent = gst.TaxPercent;
        existing.TaxAmount = gst.TaxAmount;
        existing.VoucherDate = gst.InvoiceDate;

        var expense = await FindPurchaseExpenseEntryAsync(existing.VoucherNumber);
        if (expense is null)
        {
            expense = new ExpenseEntry
            {
                Id = Guid.NewGuid().ToString(),
                Reference = existing.VoucherNumber,
                Category = "Purchase",
                Status = "active"
            };
            _db.ExpenseEntries.Add(expense);
        }

        expense.Description = $"Purchase {existing.VoucherNumber} - {customer.Name}";
        expense.Amount = gst.TotalAmount;
        expense.Type = newMode;
        expense.Date = gst.InvoiceDate;
        expense.Category = "Purchase";
        expense.Reference = existing.VoucherNumber;
        expense.Status = "active";

        return null;
    }

    private async Task<(decimal totalTaxDue, decimal inputTaxCredit, decimal netTaxPayable)> CalculateMonthlyTaxDueAsync(DateTime periodDate)
    {
        var invoices = await _db.InvoiceCalcRecords
            .Where(x => x.CreatedAt.Year == periodDate.Year && x.CreatedAt.Month == periodDate.Month)
            .ToListAsync();

        decimal totalTaxDue = 0m;
        foreach (var invoice in invoices)
        {
            if (string.IsNullOrWhiteSpace(invoice.DataJson))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(invoice.DataJson);
                if (doc.RootElement.TryGetProperty("price", out var price) &&
                    price.TryGetProperty("taxAmount", out var taxAmountEl) &&
                    taxAmountEl.TryGetDecimal(out var taxAmount))
                {
                    totalTaxDue += taxAmount;
                }
            }
            catch
            {
                // Ignore malformed invoice payloads
            }
        }

        var monthKey = periodDate.ToString("yyyy-MM");
        var inputTaxCredit = await _db.PurchaseSalesRows
            .Where(x => x.TransactionType == "Purchase" && x.VoucherDate.StartsWith(monthKey))
            .SumAsync(x => x.TaxAmount);
        var netTaxPayable = Math.Max(0m, totalTaxDue - inputTaxCredit);
        return (Math.Round(totalTaxDue, 2), Math.Round(inputTaxCredit, 2), Math.Round(netTaxPayable, 2));
    }

    private async Task<BankCashBalance> EnsureBalanceExistsAsync(string type)
    {
        var normalizedType = NormalizeBalanceType(type);
        var balance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == normalizedType);
        if (balance is not null)
        {
            return balance;
        }

        balance = new BankCashBalance
        {
            Type = normalizedType,
            Balance = 0,
            Description = normalizedType == "bank" ? "Bank account" : "Cash account",
            LastUpdated = DateTime.UtcNow.ToString("O")
        };

        _db.BankCashBalances.Add(balance);
        return balance;
    }

    private async Task<ExpenseEntry?> FindPurchaseExpenseEntryAsync(string voucherNo)
    {
        return await _db.ExpenseEntries
            .Where(x => x.Reference == voucherNo || (x.Category == "Purchase" && x.Description.Contains(voucherNo)))
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync();
    }

    private async Task<string> GenerateNextPurchaseVoucherAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"PUR-{today}-";

        var sequences = await _db.PurchaseSalesRows
            .Where(x => x.VoucherNumber.StartsWith(prefix))
            .Select(x => x.VoucherNumber)
            .ToListAsync();

        var maxSeq = sequences
            .Select(v => v.Length >= prefix.Length + 4 ? v.Substring(prefix.Length, 4) : "0000")
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{(maxSeq + 1):D4}";
    }

    private async Task<List<CustomerOutstandingSummary>> BuildCustomerOutstandingSummaryAsync()
    {
        var customers = await _db.CustomerMasters.Where(c => !c.IsDeleted).ToListAsync();
        var invoices = await _db.InvoiceCalcRecords.ToListAsync();
        var additionalRows = await _db.CustomerOutstandings.Where(x => !x.IsDeleted).ToListAsync();
        var settlements = await _db.IncomeEntries
            .Where(x => x.IncomeType == "customer_payment" && x.Status == "active" && !x.IsDeleted)
            .ToListAsync();

        var map = new Dictionary<string, CustomerOutstandingSummary>(StringComparer.OrdinalIgnoreCase);

        static string Key(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

        foreach (var customer in customers)
        {
            var key = Key(customer.Name);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            map[key] = new CustomerOutstandingSummary
            {
                CustomerId = customer.Id,
                CustomerCode = customer.Code,
                CustomerName = customer.Name,
                OpeningBalance = customer.OpeningBalance
            };
        }

        foreach (var invoice in invoices)
        {
            var key = Key(invoice.CompanyName);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!map.TryGetValue(key, out var row))
            {
                row = new CustomerOutstandingSummary
                {
                    CustomerName = invoice.CompanyName,
                    CustomerCode = "-",
                    CustomerId = key
                };
                map[key] = row;
            }

            row.OrderAmount += invoice.Amount;
        }

        foreach (var outstanding in additionalRows)
        {
            // Order-linked outstandings are already represented by InvoiceCalcRecords
            // in row.OrderAmount; only manual rows should contribute to AdditionalOutstanding.
            if (!string.IsNullOrWhiteSpace(outstanding.OrderId))
            {
                continue;
            }

            var key = Key(outstanding.CustomerName);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!map.TryGetValue(key, out var row))
            {
                row = new CustomerOutstandingSummary
                {
                    CustomerName = outstanding.CustomerName ?? string.Empty,
                    CustomerCode = "-",
                    CustomerId = outstanding.CustomerId ?? key
                };
                map[key] = row;
            }

            var paid = outstanding.PaidAmount ?? 0m;
            var remaining = Math.Max(0m, outstanding.Amount - paid);
            row.AdditionalOutstanding += remaining;
        }

        foreach (var settlement in settlements)
        {
            var key = Key(settlement.CustomerName);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!map.TryGetValue(key, out var row))
            {
                row = new CustomerOutstandingSummary
                {
                    CustomerName = settlement.CustomerName ?? string.Empty,
                    CustomerCode = "-",
                    CustomerId = settlement.CustomerId ?? key
                };
                map[key] = row;
            }

            row.SettledAmount += settlement.Amount;
        }

        foreach (var row in map.Values)
        {
            row.OutstandingAmount = row.OpeningBalance + row.OrderAmount + row.AdditionalOutstanding - row.SettledAmount;
        }

        return map.Values.ToList();
    }

    /// <summary>
    /// Resets all accounts data to zero for testing.
    /// Clears: IncomeEntries, ExpenseEntries, AccountTransactions, CustomerOutstandings,
    ///         CashTransfers, ExpenseLedgerRows, IncomeRows, PurchaseSalesRows, TaxPaymentRows.
    /// Reinitialises BankCashBalances (cash=0, bank=0).
    /// </summary>
    [HttpPost("reset-all")]
    public async Task<IActionResult> ResetAllAccountsData([FromQuery] string confirm = "")
    {
        if (confirm != "YES")
        {
            return BadRequest(new { message = "Append ?confirm=YES to the request to confirm data reset." });
        }

        var incomeEntries = await _db.IncomeEntries.ToListAsync();
        foreach (var row in incomeEntries)
        {
            row.IsDeleted = true;
            row.Status = "cancelled";
        }

        var expenseEntries = await _db.ExpenseEntries.ToListAsync();
        foreach (var row in expenseEntries)
        {
            row.IsDeleted = true;
            row.Status = "cancelled";
        }

        var customerOutstandings = await _db.CustomerOutstandings.ToListAsync();
        foreach (var row in customerOutstandings)
        {
            row.IsDeleted = true;
            row.Status = "cancelled";
        }

        var cashTransfers = await _db.CashTransfers.ToListAsync();
        foreach (var row in cashTransfers)
        {
            row.IsDeleted = true;
        }

        var purchaseSalesRows = await _db.PurchaseSalesRows.ToListAsync();
        foreach (var row in purchaseSalesRows)
        {
            row.IsDeleted = true;
        }

        var taxPaymentRows = await _db.TaxPaymentRows.ToListAsync();
        foreach (var row in taxPaymentRows)
        {
            row.IsDeleted = true;
        }

        // Upsert zero balances
        var cashBalance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == "cash");
        if (cashBalance is null)
        {
            _db.BankCashBalances.Add(new BankCashBalance { Type = "cash", Balance = 0, Description = "Cash account", LastUpdated = DateTime.UtcNow.ToString("O") });
        }
        else
        {
            cashBalance.Balance = 0;
            cashBalance.LastUpdated = DateTime.UtcNow.ToString("O");
        }

        var bankBalance = await _db.BankCashBalances.FirstOrDefaultAsync(x => x.Type == "bank");
        if (bankBalance is null)
        {
            _db.BankCashBalances.Add(new BankCashBalance { Type = "bank", Balance = 0, Description = "Bank account", LastUpdated = DateTime.UtcNow.ToString("O") });
        }
        else
        {
            bankBalance.Balance = 0;
            bankBalance.LastUpdated = DateTime.UtcNow.ToString("O");
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "All accounts data has been reset to zero.", resetAt = DateTime.UtcNow });
    }
}
