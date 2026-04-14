using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Accounts;
using QuotationAPI.V2.Models.Integrations;
using QuotationAPI.V2.Services;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/zoho-books")]
public class ZohoBooksController : ControllerBase
{
    private readonly QuotationDbContext _db;
    private readonly IZohoBooksService _zohoBooksService;
    private readonly IConfiguration _configuration;

    public ZohoBooksController(QuotationDbContext db, IZohoBooksService zohoBooksService, IConfiguration configuration)
    {
        _db = db;
        _zohoBooksService = zohoBooksService;
        _configuration = configuration;
    }

    [HttpPost("pull-delta")]
    public async Task<IActionResult> PullDelta(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _zohoBooksService.PullDeltaAsync(cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var state = await _db.ZohoSyncStates.FirstOrDefaultAsync(cancellationToken);
        var zohoCustomers = await _db.ZohoCustomerRecords.CountAsync(cancellationToken);
        var zohoInvoices = await _db.ZohoInvoiceRecords.CountAsync(cancellationToken);
        var zohoOutstanding = await _db.ZohoOutstandingRecords.CountAsync(cancellationToken);

        var fallbackCustomers = 0;
        var fallbackInvoices = 0;
        var fallbackOutstanding = 0;
        var isFallback = zohoCustomers == 0 && zohoInvoices == 0 && zohoOutstanding == 0;

        if (isFallback)
        {
            fallbackCustomers = await _db.CustomerMasters.CountAsync(c => !c.IsDeleted && c.Status == "Active", cancellationToken);
            fallbackInvoices = await _db.CustomerOutstandings.CountAsync(x => !x.IsDeleted, cancellationToken);

            var fallbackRows = await BuildFallbackOutstandingAsync(cancellationToken);
            fallbackOutstanding = fallbackRows.Count;
        }

        var hasStaticToken = !string.IsNullOrWhiteSpace(_configuration["ZohoBooks:AccessToken"]);
        var hasOauthFlow =
            !string.IsNullOrWhiteSpace(_configuration["ZohoBooks:RefreshToken"]) &&
            !string.IsNullOrWhiteSpace(_configuration["ZohoBooks:ClientId"]) &&
            !string.IsNullOrWhiteSpace(_configuration["ZohoBooks:ClientSecret"]);
        var isZohoConfigured =
            !string.IsNullOrWhiteSpace(_configuration["ZohoBooks:OrganizationId"]) &&
            (hasStaticToken || hasOauthFlow);

        return Ok(new
        {
            customers = isFallback ? fallbackCustomers : zohoCustomers,
            invoices = isFallback ? fallbackInvoices : zohoInvoices,
            outstanding = isFallback ? fallbackOutstanding : zohoOutstanding,
            lastCustomersSyncUtc = state?.LastCustomersSyncUtc,
            lastInvoicesSyncUtc = state?.LastInvoicesSyncUtc,
            lastOutstandingSyncUtc = state?.LastOutstandingSyncUtc,
            updatedAtUtc = state?.UpdatedAtUtc,
            source = isFallback ? "local-db-fallback" : "zoho-sync",
            isZohoConfigured,
        });
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] int pageSize = 200, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 1000);
        var zohoRows = await _db.ZohoCustomerRecords
            .OrderByDescending(x => x.PulledAtUtc)
            .ThenBy(x => x.CustomerName)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (zohoRows.Count > 0)
        {
            return Ok(zohoRows);
        }

        var fallbackRows = await BuildFallbackCustomersAsync(pageSize, cancellationToken);
        return Ok(fallbackRows);
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] int pageSize = 200, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 1000);
        var zohoRows = await _db.ZohoInvoiceRecords
            .OrderByDescending(x => x.PulledAtUtc)
            .ThenByDescending(x => x.InvoiceDate)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (zohoRows.Count > 0)
        {
            return Ok(zohoRows);
        }

        var fallbackRows = await BuildFallbackInvoicesAsync(pageSize, cancellationToken);
        return Ok(fallbackRows);
    }

    [HttpGet("outstanding")]
    public async Task<IActionResult> GetOutstanding([FromQuery] int pageSize = 200, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 1000);
        var zohoRows = await _db.ZohoOutstandingRecords
            .OrderByDescending(x => x.OutstandingAmount)
            .ThenBy(x => x.CustomerName)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (zohoRows.Count > 0)
        {
            return Ok(zohoRows);
        }

        var fallbackRows = await BuildFallbackOutstandingAsync(cancellationToken);
        return Ok(fallbackRows.Take(pageSize));
    }

    private async Task<List<ZohoCustomerRecord>> BuildFallbackCustomersAsync(int pageSize, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var outstandingByCustomerId = await _db.CustomerOutstandings
            .Where(x => !x.IsDeleted)
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Outstanding = g.Sum(x => (x.Amount - (x.PaidAmount ?? 0m)) > 0m ? (x.Amount - (x.PaidAmount ?? 0m)) : 0m)
            })
            .ToListAsync(cancellationToken);

        var outstandingMap = outstandingByCustomerId
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerId))
            .ToDictionary(x => x.CustomerId!, x => x.Outstanding, StringComparer.OrdinalIgnoreCase);

        var customers = await _db.CustomerMasters
            .Where(c => !c.IsDeleted && c.Status == "Active")
            .OrderBy(c => c.Code)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return customers.Select(c => new ZohoCustomerRecord
        {
            Id = c.Id,
            CustomerName = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            OutstandingAmount = outstandingMap.TryGetValue(c.Id, out var amount) ? amount : c.OpeningBalance,
            LastModifiedTimeUtc = ParseDateTime(c.UpdatedDate) ?? ParseDateTime(c.CreatedDate),
            PulledAtUtc = now,
        }).ToList();
    }

    private async Task<List<ZohoInvoiceRecord>> BuildFallbackInvoicesAsync(int pageSize, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await _db.CustomerOutstandings
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.Date)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return rows.Select(x =>
        {
            var paid = x.PaidAmount ?? 0m;
            var balance = Math.Max(0m, x.Amount - paid);
            return new ZohoInvoiceRecord
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.CustomerName,
                InvoiceNumber = string.IsNullOrWhiteSpace(x.OrderId) ? $"LOCAL-{x.Id[..Math.Min(8, x.Id.Length)].ToUpperInvariant()}" : x.OrderId,
                InvoiceDate = ParseDateTime(x.Date),
                DueDate = ParseDateTime(x.DueDate),
                Total = x.Amount,
                Balance = balance,
                Status = balance <= 0 ? "paid" : "open",
                LastModifiedTimeUtc = ParseDateTime(x.CreatedDate),
                PulledAtUtc = now,
            };
        }).ToList();
    }

    private async Task<List<ZohoOutstandingRecord>> BuildFallbackOutstandingAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var customers = await _db.CustomerMasters
            .Where(c => !c.IsDeleted && c.Status == "Active")
            .Select(c => new { c.Id, c.Name, c.OpeningBalance })
            .ToListAsync(cancellationToken);

        var grouped = await _db.CustomerOutstandings
            .Where(x => !x.IsDeleted)
            .GroupBy(x => new { x.CustomerId, x.CustomerName })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.CustomerName,
                Outstanding = g.Sum(x => (x.Amount - (x.PaidAmount ?? 0m)) > 0m ? (x.Amount - (x.PaidAmount ?? 0m)) : 0m)
            })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<string, ZohoOutstandingRecord>(StringComparer.OrdinalIgnoreCase);

        foreach (var customer in customers)
        {
            if (string.IsNullOrWhiteSpace(customer.Id))
            {
                continue;
            }

            map[customer.Id] = new ZohoOutstandingRecord
            {
                Id = customer.Id,
                CustomerName = customer.Name,
                OutstandingAmount = Math.Max(0m, customer.OpeningBalance),
                PulledAtUtc = now,
            };
        }

        foreach (var row in grouped)
        {
            var key = !string.IsNullOrWhiteSpace(row.CustomerId)
                ? row.CustomerId!
                : $"name:{(row.CustomerName ?? string.Empty).Trim().ToLowerInvariant()}";

            if (!map.TryGetValue(key, out var existing))
            {
                map[key] = new ZohoOutstandingRecord
                {
                    Id = key,
                    CustomerName = row.CustomerName,
                    OutstandingAmount = Math.Max(0m, row.Outstanding),
                    PulledAtUtc = now,
                };
                continue;
            }

            existing.OutstandingAmount += Math.Max(0m, row.Outstanding);
        }

        return map.Values
            .Where(x => x.OutstandingAmount > 0)
            .OrderByDescending(x => x.OutstandingAmount)
            .ThenBy(x => x.CustomerName)
            .ToList();
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
