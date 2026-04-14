using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Customer;
using System.Text.RegularExpressions;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/customer-masters")]
public class CustomerMastersController : ControllerBase
{
    private readonly QuotationDbContext _db;
    private static readonly Regex GstRegex = new("^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$", RegexOptions.Compiled);

    public CustomerMastersController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rows = await _db.CustomerMasters.Where(c => !c.IsDeleted).OrderBy(c => c.Code).ToListAsync();
        return Ok(rows);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var rows = await _db.CustomerMasters
            .Where(c => c.Status == "Active" && !c.IsDeleted)
            .OrderBy(c => c.Code)
            .ToListAsync();
        return Ok(rows);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var row = await _db.CustomerMasters.FindAsync(id);
        return row == null ? NotFound() : Ok(row);
    }

    [HttpGet("gst/lookup")]
    public IActionResult LookupByGst([FromQuery] string gstNumber)
    {
        var normalized = NormalizeGstNumber(gstNumber);
        if (!IsValidGstNumber(normalized))
        {
            return BadRequest(new { message = "Please enter a valid GST number." });
        }

        return Ok(BuildGstLookupResponse(normalized));
    }

    [HttpPut("{id}/refresh-from-gst")]
    public async Task<IActionResult> RefreshCustomerFromGst(string id, [FromQuery] string? gstNumber = null)
    {
        var customer = await _db.CustomerMasters.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var normalized = NormalizeGstNumber(string.IsNullOrWhiteSpace(gstNumber) ? customer.GstNumber : gstNumber);
        if (!IsValidGstNumber(normalized))
        {
            return BadRequest(new { message = "Customer GST number is missing or invalid." });
        }

        var gstData = BuildGstLookupResponse(normalized);
        customer.GstNumber = gstData.GstNumber;
        customer.Name = string.IsNullOrWhiteSpace(gstData.TradeName) ? gstData.LegalName : gstData.TradeName;
        customer.Address = gstData.Address;
        if (!string.IsNullOrWhiteSpace(gstData.Phone))
        {
            customer.Phone = gstData.Phone;
        }
        if (!string.IsNullOrWhiteSpace(gstData.Email))
        {
            customer.Email = gstData.Email;
        }

        customer.UpdatedDate = DateTime.UtcNow.ToString("o");
        await _db.SaveChangesAsync();
        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCustomerRequest req)
    {
        var existingCodes = await _db.CustomerMasters.Select(c => c.Code).ToListAsync();
        var code = NextCode(existingCodes);

        var customer = new CustomerMaster
        {
            Id = NextId(await _db.CustomerMasters.Select(c => c.Id).ToListAsync()),
            Code = code,
            Name = req.Name,
            Phone = req.Phone,
            Email = req.Email,
            Address = req.Address,
            GstNumber = req.GstNumber,
            CustomerType = string.IsNullOrWhiteSpace(req.CustomerType) ? "Retail" : req.CustomerType,
            Status = string.IsNullOrWhiteSpace(req.Status) ? "Active" : req.Status,
            OpeningBalance = req.OpeningBalance,
            CreatedDate = DateTime.UtcNow.ToString("o"),
            UpdatedDate = DateTime.UtcNow.ToString("o")
        };

        _db.CustomerMasters.Add(customer);
        await _db.SaveChangesAsync();
        return Ok(customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] SaveCustomerRequest req)
    {
        var customer = await _db.CustomerMasters.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        customer.Name = req.Name;
        customer.Phone = req.Phone;
        customer.Email = req.Email;
        customer.Address = req.Address;
        customer.GstNumber = req.GstNumber;
        customer.CustomerType = string.IsNullOrWhiteSpace(req.CustomerType) ? customer.CustomerType : req.CustomerType;
        customer.Status = string.IsNullOrWhiteSpace(req.Status) ? customer.Status : req.Status;
        customer.OpeningBalance = req.OpeningBalance;
        customer.UpdatedDate = DateTime.UtcNow.ToString("o");

        await _db.SaveChangesAsync();
        return Ok(customer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var customer = await _db.CustomerMasters.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        customer.IsDeleted = true;
        customer.UpdatedDate = DateTime.UtcNow.ToString("o");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string NextId(IReadOnlyCollection<string> ids)
    {
        var max = ids
            .Select(id => id?.Replace("cust-", "", StringComparison.OrdinalIgnoreCase))
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"cust-{max + 1}";
    }

    private static string NextCode(IReadOnlyCollection<string> codes)
    {
        var max = codes
            .Select(code => new string((code ?? string.Empty).Where(char.IsDigit).ToArray()))
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"CUST{(max + 1):D3}";
    }

    private static string NormalizeGstNumber(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static bool IsValidGstNumber(string gstNumber)
    {
        return GstRegex.IsMatch(gstNumber);
    }

    private static GstCustomerLookupResponse BuildGstLookupResponse(string gstNumber)
    {
        var token = gstNumber.Substring(2, 5);
        var panToken = gstNumber.Substring(7, 4);
        var stateCode = gstNumber.Substring(0, 2);

        return new GstCustomerLookupResponse
        {
            GstNumber = gstNumber,
            LegalName = $"{token} Industries Private Limited",
            TradeName = $"{token} Traders",
            Address = $"{panToken} GST Plaza, Sector 1, State-{stateCode}, India",
            Phone = "9000000000",
            Email = $"accounts.{token.ToLowerInvariant()}@gstmail.in",
            StateCode = stateCode,
            Pincode = "600001",
            Source = "GST_SIMULATED",
            LastUpdatedUtc = DateTime.UtcNow.ToString("O")
        };
    }
}
