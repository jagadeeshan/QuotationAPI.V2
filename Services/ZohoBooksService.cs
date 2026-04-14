using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Models.Customer;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Integrations;
using QuotationAPI.V2.Models.Accounts;

namespace QuotationAPI.V2.Services;

public interface IZohoBooksService
{
    Task<ZohoDeltaPullResult> PullDeltaAsync(CancellationToken cancellationToken = default);
}

public class ZohoBooksService : IZohoBooksService
{
    private readonly QuotationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private string? _cachedAccessToken;
    private DateTime _cachedAccessTokenExpiresUtc = DateTime.MinValue;

    public ZohoBooksService(
        QuotationDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<ZohoDeltaPullResult> PullDeltaAsync(CancellationToken cancellationToken = default)
    {
        var orgId = _configuration["ZohoBooks:OrganizationId"];
        var baseUrl = _configuration["ZohoBooks:BaseUrl"] ?? "https://www.zohoapis.in";
        var token = await ResolveAccessTokenAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            return await PullFromLocalDataAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(orgId))
        {
            throw new InvalidOperationException("ZohoBooks configuration missing. Set ZohoBooks:OrganizationId.");
        }

        var state = await _db.ZohoSyncStates.FirstOrDefaultAsync(cancellationToken) ?? new ZohoSyncState();
        if (state.Id != 1)
        {
            state.Id = 1;
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", $"Zoho-oauthtoken {token}");

        var result = new ZohoDeltaPullResult();
        var now = DateTime.UtcNow;

        var customers = await FetchCustomersAsync(client, baseUrl, orgId, state.LastCustomersSyncUtc, cancellationToken);
        foreach (var customer in customers)
        {
            var existing = await _db.ZohoCustomerRecords.FirstOrDefaultAsync(x => x.Id == customer.Id, cancellationToken);
            if (existing == null)
            {
                _db.ZohoCustomerRecords.Add(customer);
            }
            else
            {
                existing.CustomerName = customer.CustomerName;
                existing.Email = customer.Email;
                existing.Phone = customer.Phone;
                existing.OutstandingAmount = customer.OutstandingAmount;
                existing.LastModifiedTimeUtc = customer.LastModifiedTimeUtc;
                existing.PulledAtUtc = now;
            }
            result.CustomersUpserted++;
        }

        var invoices = await FetchInvoicesAsync(client, baseUrl, orgId, state.LastInvoicesSyncUtc, cancellationToken);
        foreach (var invoice in invoices)
        {
            var existing = await _db.ZohoInvoiceRecords.FirstOrDefaultAsync(x => x.Id == invoice.Id, cancellationToken);
            if (existing == null)
            {
                _db.ZohoInvoiceRecords.Add(invoice);
            }
            else
            {
                existing.CustomerId = invoice.CustomerId;
                existing.CustomerName = invoice.CustomerName;
                existing.InvoiceNumber = invoice.InvoiceNumber;
                existing.InvoiceDate = invoice.InvoiceDate;
                existing.DueDate = invoice.DueDate;
                existing.Total = invoice.Total;
                existing.Balance = invoice.Balance;
                existing.Status = invoice.Status;
                existing.LastModifiedTimeUtc = invoice.LastModifiedTimeUtc;
                existing.PulledAtUtc = now;
            }
            result.InvoicesUpserted++;
        }

        var groupedOutstanding = await _db.ZohoInvoiceRecords
            .GroupBy(x => new { x.CustomerId, x.CustomerName })
            .Select(g => new
            {
                CustomerId = g.Key.CustomerId ?? string.Empty,
                g.Key.CustomerName,
                Outstanding = g.Sum(x => x.Balance)
            })
            .Where(x => x.Outstanding > 0)
            .ToListAsync(cancellationToken);

        foreach (var outstanding in groupedOutstanding)
        {
            var key = string.IsNullOrWhiteSpace(outstanding.CustomerId)
                ? $"name:{outstanding.CustomerName}"
                : outstanding.CustomerId;

            var existing = await _db.ZohoOutstandingRecords.FirstOrDefaultAsync(x => x.Id == key, cancellationToken);
            if (existing == null)
            {
                _db.ZohoOutstandingRecords.Add(new ZohoOutstandingRecord
                {
                    Id = key,
                    CustomerName = outstanding.CustomerName,
                    OutstandingAmount = outstanding.Outstanding,
                    PulledAtUtc = now,
                });
            }
            else
            {
                existing.CustomerName = outstanding.CustomerName;
                existing.OutstandingAmount = outstanding.Outstanding;
                existing.PulledAtUtc = now;
            }
            result.OutstandingUpserted++;
        }

        state.LastCustomersSyncUtc = now;
        state.LastInvoicesSyncUtc = now;
        state.LastOutstandingSyncUtc = now;
        state.UpdatedAtUtc = now;

        if (_db.Entry(state).State == EntityState.Detached)
        {
            _db.ZohoSyncStates.Add(state);
        }

        result.PulledAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<string> ResolveAccessTokenAsync(CancellationToken cancellationToken)
    {
        var staticToken = _configuration["ZohoBooks:AccessToken"];
        if (!string.IsNullOrWhiteSpace(staticToken))
        {
            return staticToken;
        }

        if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && _cachedAccessTokenExpiresUtc > DateTime.UtcNow.AddMinutes(1))
        {
            return _cachedAccessToken;
        }

        var refreshToken = _configuration["ZohoBooks:RefreshToken"];
        var clientId = _configuration["ZohoBooks:ClientId"];
        var clientSecret = _configuration["ZohoBooks:ClientSecret"];
        var accountsBaseUrl = _configuration["ZohoBooks:AccountsBaseUrl"] ?? "https://accounts.zoho.in";

        // Keep local fallback behavior when OAuth config is incomplete.
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return string.Empty;
        }

        var client = _httpClientFactory.CreateClient();
        var tokenUrl = $"{accountsBaseUrl.TrimEnd('/')}/oauth/v2/token";
        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
            })
        };

        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Zoho OAuth token refresh failed: {(int)response.StatusCode} {response.ReasonPhrase}. {payload}");
        }

        using var json = JsonDocument.Parse(payload);
        if (!json.RootElement.TryGetProperty("access_token", out var tokenEl))
        {
            throw new InvalidOperationException("Zoho OAuth response did not include access_token.");
        }

        var accessToken = tokenEl.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Zoho OAuth access_token is empty.");
        }

        var expiresIn = json.RootElement.TryGetProperty("expires_in", out var expiresEl)
            ? expiresEl.GetInt32()
            : 3600;

        _cachedAccessToken = accessToken;
        _cachedAccessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 30));

        return accessToken;
    }

    private async Task<ZohoDeltaPullResult> PullFromLocalDataAsync(CancellationToken cancellationToken)
    {
        var result = new ZohoDeltaPullResult();
        var now = DateTime.UtcNow;

        var customers = await _db.CustomerMasters
            .Where(c => !c.IsDeleted && c.Status == "Active")
            .ToListAsync(cancellationToken);

        var outstandingRows = await _db.CustomerOutstandings
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var outstandingByCustomerId = outstandingRows
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerId))
            .GroupBy(x => x.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => Math.Max(0m, x.Amount - (x.PaidAmount ?? 0m))),
                StringComparer.OrdinalIgnoreCase);

        foreach (var customer in customers)
        {
            var existing = await _db.ZohoCustomerRecords.FirstOrDefaultAsync(x => x.Id == customer.Id, cancellationToken);
            var outstandingAmount = outstandingByCustomerId.TryGetValue(customer.Id, out var amt)
                ? amt
                : Math.Max(0m, customer.OpeningBalance);

            if (existing == null)
            {
                _db.ZohoCustomerRecords.Add(new ZohoCustomerRecord
                {
                    Id = customer.Id,
                    CustomerName = customer.Name,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    OutstandingAmount = outstandingAmount,
                    LastModifiedTimeUtc = ParseZohoDateTime(customer.UpdatedDate),
                    PulledAtUtc = now,
                });
            }
            else
            {
                existing.CustomerName = customer.Name;
                existing.Email = customer.Email;
                existing.Phone = customer.Phone;
                existing.OutstandingAmount = outstandingAmount;
                existing.LastModifiedTimeUtc = ParseZohoDateTime(customer.UpdatedDate);
                existing.PulledAtUtc = now;
            }

            result.CustomersUpserted++;
        }

        foreach (var row in outstandingRows)
        {
            var balance = Math.Max(0m, row.Amount - (row.PaidAmount ?? 0m));
            var existingInvoice = await _db.ZohoInvoiceRecords.FirstOrDefaultAsync(x => x.Id == row.Id, cancellationToken);
            if (existingInvoice == null)
            {
                _db.ZohoInvoiceRecords.Add(new ZohoInvoiceRecord
                {
                    Id = row.Id,
                    CustomerId = row.CustomerId,
                    CustomerName = row.CustomerName,
                    InvoiceNumber = string.IsNullOrWhiteSpace(row.OrderId)
                        ? $"LOCAL-{row.Id[..Math.Min(8, row.Id.Length)].ToUpperInvariant()}"
                        : row.OrderId,
                    InvoiceDate = ParseZohoDate(row.Date),
                    DueDate = ParseZohoDate(row.DueDate),
                    Total = row.Amount,
                    Balance = balance,
                    Status = balance <= 0 ? "paid" : "open",
                    LastModifiedTimeUtc = ParseZohoDateTime(row.CreatedDate),
                    PulledAtUtc = now,
                });
            }
            else
            {
                existingInvoice.CustomerId = row.CustomerId;
                existingInvoice.CustomerName = row.CustomerName;
                existingInvoice.InvoiceNumber = string.IsNullOrWhiteSpace(row.OrderId)
                    ? $"LOCAL-{row.Id[..Math.Min(8, row.Id.Length)].ToUpperInvariant()}"
                    : row.OrderId;
                existingInvoice.InvoiceDate = ParseZohoDate(row.Date);
                existingInvoice.DueDate = ParseZohoDate(row.DueDate);
                existingInvoice.Total = row.Amount;
                existingInvoice.Balance = balance;
                existingInvoice.Status = balance <= 0 ? "paid" : "open";
                existingInvoice.LastModifiedTimeUtc = ParseZohoDateTime(row.CreatedDate);
                existingInvoice.PulledAtUtc = now;
            }

            result.InvoicesUpserted++;
        }

        var groupedOutstanding = outstandingRows
            .GroupBy(x => new { x.CustomerId, x.CustomerName })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.CustomerName,
                Outstanding = g.Sum(x => Math.Max(0m, x.Amount - (x.PaidAmount ?? 0m)))
            })
            .Where(x => x.Outstanding > 0)
            .ToList();

        foreach (var outstanding in groupedOutstanding)
        {
            var key = string.IsNullOrWhiteSpace(outstanding.CustomerId)
                ? $"name:{outstanding.CustomerName}"
                : outstanding.CustomerId;

            var existing = await _db.ZohoOutstandingRecords.FirstOrDefaultAsync(x => x.Id == key, cancellationToken);
            if (existing == null)
            {
                _db.ZohoOutstandingRecords.Add(new ZohoOutstandingRecord
                {
                    Id = key,
                    CustomerName = outstanding.CustomerName,
                    OutstandingAmount = outstanding.Outstanding,
                    PulledAtUtc = now,
                });
            }
            else
            {
                existing.CustomerName = outstanding.CustomerName;
                existing.OutstandingAmount = outstanding.Outstanding;
                existing.PulledAtUtc = now;
            }

            result.OutstandingUpserted++;
        }

        var state = await _db.ZohoSyncStates.FirstOrDefaultAsync(cancellationToken) ?? new ZohoSyncState { Id = 1 };
        state.LastCustomersSyncUtc = now;
        state.LastInvoicesSyncUtc = now;
        state.LastOutstandingSyncUtc = now;
        state.UpdatedAtUtc = now;

        if (_db.Entry(state).State == EntityState.Detached)
        {
            _db.ZohoSyncStates.Add(state);
        }

        result.PulledAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task<List<ZohoCustomerRecord>> FetchCustomersAsync(
        HttpClient client,
        string baseUrl,
        string orgId,
        DateTime? lastSyncUtc,
        CancellationToken cancellationToken)
    {
        var url = $"{baseUrl.TrimEnd('/')}/books/v3/contacts?organization_id={Uri.EscapeDataString(orgId)}&filter_by=Status.All";
        if (lastSyncUtc.HasValue)
        {
            var lastSyncIso = lastSyncUtc.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            url += $"&last_modified_time={Uri.EscapeDataString(lastSyncIso)}";
        }

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var list = new List<ZohoCustomerRecord>();
        if (!json.RootElement.TryGetProperty("contacts", out var contacts) || contacts.ValueKind != JsonValueKind.Array)
        {
            return list;
        }

        foreach (var contact in contacts.EnumerateArray())
        {
            var id = contact.TryGetProperty("contact_id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            list.Add(new ZohoCustomerRecord
            {
                Id = id,
                CustomerName = contact.TryGetProperty("contact_name", out var nameEl) ? (nameEl.GetString() ?? string.Empty) : string.Empty,
                Email = contact.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null,
                Phone = contact.TryGetProperty("phone", out var phoneEl) ? phoneEl.GetString() : null,
                OutstandingAmount = contact.TryGetProperty("outstanding_receivable_amount", out var outEl)
                    ? outEl.GetDecimal()
                    : 0,
                LastModifiedTimeUtc = ParseZohoDateTime(contact.TryGetProperty("last_modified_time", out var lmEl) ? lmEl.GetString() : null),
                PulledAtUtc = DateTime.UtcNow,
            });
        }

        return list;
    }

    private static async Task<List<ZohoInvoiceRecord>> FetchInvoicesAsync(
        HttpClient client,
        string baseUrl,
        string orgId,
        DateTime? lastSyncUtc,
        CancellationToken cancellationToken)
    {
        var url = $"{baseUrl.TrimEnd('/')}/books/v3/invoices?organization_id={Uri.EscapeDataString(orgId)}";
        if (lastSyncUtc.HasValue)
        {
            var lastSyncIso = lastSyncUtc.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            url += $"&last_modified_time={Uri.EscapeDataString(lastSyncIso)}";
        }

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var list = new List<ZohoInvoiceRecord>();
        if (!json.RootElement.TryGetProperty("invoices", out var invoices) || invoices.ValueKind != JsonValueKind.Array)
        {
            return list;
        }

        foreach (var invoice in invoices.EnumerateArray())
        {
            var id = invoice.TryGetProperty("invoice_id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            list.Add(new ZohoInvoiceRecord
            {
                Id = id,
                CustomerId = invoice.TryGetProperty("customer_id", out var custIdEl) ? custIdEl.GetString() : null,
                CustomerName = invoice.TryGetProperty("customer_name", out var custNameEl) ? custNameEl.GetString() : null,
                InvoiceNumber = invoice.TryGetProperty("invoice_number", out var invNoEl) ? invNoEl.GetString() : null,
                InvoiceDate = ParseZohoDate(invoice.TryGetProperty("date", out var dateEl) ? dateEl.GetString() : null),
                DueDate = ParseZohoDate(invoice.TryGetProperty("due_date", out var dueEl) ? dueEl.GetString() : null),
                Total = invoice.TryGetProperty("total", out var totalEl) ? totalEl.GetDecimal() : 0,
                Balance = invoice.TryGetProperty("balance", out var balEl) ? balEl.GetDecimal() : 0,
                Status = invoice.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null,
                LastModifiedTimeUtc = ParseZohoDateTime(invoice.TryGetProperty("last_modified_time", out var lmEl) ? lmEl.GetString() : null),
                PulledAtUtc = DateTime.UtcNow,
            });
        }

        return list;
    }

    private static DateTime? ParseZohoDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTime? ParseZohoDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }
}
