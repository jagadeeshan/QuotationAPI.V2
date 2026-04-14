using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Calculations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/quotation-calc-records")]
public class QuotationCalcController : ControllerBase
{
    private readonly QuotationDbContext _db;
    public QuotationCalcController(QuotationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var records = await _db.QuotationCalcRecords
            .OrderByDescending(r => r.Id)
            .Select(r => new { r.Id, r.CreatedAt, r.UpdatedAt, r.CompanyName, r.Description, r.Amount, r.DataJson })
            .ToListAsync();

        var normalized = records.Select(r => new
        {
            r.Id,
            r.CreatedAt,
            r.UpdatedAt,
            r.CompanyName,
            r.Description,
            r.Amount,
            DataJson = NormalizeCalculationDataJson(r.DataJson)
        });

        return Ok(normalized);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var record = await _db.QuotationCalcRecords.FindAsync(id);
        if (record != null)
        {
            record.DataJson = NormalizeCalculationDataJson(record.DataJson);
        }
        return record == null ? NotFound() : Ok(record);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CalcRecordSaveRequest req)
    {
        var record = new QuotationCalcRecord
        {
            CompanyName = req.CompanyName,
            Description = req.Description,
            Amount = req.Amount,
            DataJson = NormalizeCalculationDataJson(req.DataJson),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.QuotationCalcRecords.Add(record);
        await _db.SaveChangesAsync();
        return Ok(record);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CalcRecordSaveRequest req)
    {
        var record = await _db.QuotationCalcRecords.FindAsync(id);
        if (record == null) return NotFound();
        record.CompanyName = req.CompanyName;
        record.Description = req.Description;
        record.Amount = req.Amount;
        record.DataJson = NormalizeCalculationDataJson(req.DataJson);
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.QuotationCalcRecords.FindAsync(id);
        if (record == null) return NotFound();
        record.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> Duplicate(int id)
    {
        var source = await _db.QuotationCalcRecords.FindAsync(id);
        if (source == null) return NotFound();
        var clone = new QuotationCalcRecord
        {
            CompanyName = source.CompanyName,
            Description = source.Description,
            Amount = source.Amount,
            DataJson = NormalizeCalculationDataJson(source.DataJson),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.QuotationCalcRecords.Add(clone);
        await _db.SaveChangesAsync();
        return Ok(clone);
    }

    private static string NormalizeCalculationDataJson(string? dataJson)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
        {
            return "{\"expense\":{\"includeRent\":true,\"contractLabour\":0}}";
        }

        try
        {
            var rootNode = JsonNode.Parse(dataJson) as JsonObject;
            if (rootNode == null)
            {
                return dataJson;
            }

            if (rootNode["expense"] is not JsonObject expenseNode)
            {
                expenseNode = new JsonObject();
                rootNode["expense"] = expenseNode;
            }

            if (expenseNode["includeRent"] == null)
            {
                expenseNode["includeRent"] = true;
            }

            if (expenseNode["contractLabour"] == null)
            {
                expenseNode["contractLabour"] = 0;
            }

            if (rootNode["item"] is not JsonObject itemNode)
            {
                itemNode = new JsonObject();
                rootNode["item"] = itemNode;
            }

            var boxNode = rootNode["box"] as JsonObject;
            var customerName = ReadString(itemNode, "customerName")
                ?? ReadString(boxNode, "company")
                ?? string.Empty;
            var customerId = ReadString(itemNode, "customerId");
            var itemName = ReadString(itemNode, "itemName") ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(itemName))
            {
                var canonicalKey = BuildCanonicalItemKey(customerId, customerName, itemName);
                var existingId = ReadInt(itemNode, "itemId");
                var stableId = existingId > 0 ? existingId : ComputeStableItemId(canonicalKey);

                itemNode["itemId"] = stableId;
                itemNode["itemKey"] = canonicalKey;
                itemNode["itemName"] = itemName.Trim();
                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    itemNode["customerName"] = customerName.Trim();
                }
                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    itemNode["customerId"] = customerId.Trim();
                }
            }

            return rootNode.ToJsonString(new JsonSerializerOptions());
        }
        catch
        {
            return dataJson;
        }
    }

    private static string? ReadString(JsonObject? node, string key)
    {
        if (node is null || node[key] is null)
        {
            return null;
        }

        try
        {
            return node[key]?.GetValue<string>()?.Trim();
        }
        catch
        {
            return node[key]?.ToJsonString().Trim('"').Trim();
        }
    }

    private static int ReadInt(JsonObject? node, string key)
    {
        if (node is null || node[key] is null)
        {
            return 0;
        }

        try
        {
            return node[key]?.GetValue<int>() ?? 0;
        }
        catch
        {
            if (int.TryParse(node[key]?.ToJsonString().Trim('"'), out var parsed))
            {
                return parsed;
            }

            return 0;
        }
    }

    private static string BuildCanonicalItemKey(string? customerId, string customerName, string itemName)
    {
        var normalizedCustomerName = NormalizeToken(customerName);
        var normalizedItemName = NormalizeToken(itemName);

        var identity = !string.IsNullOrWhiteSpace(normalizedCustomerName)
            ? $"name:{normalizedCustomerName}"
            : $"id:{NormalizeToken(customerId)}";

        return $"{identity}|item:{normalizedItemName}";
    }

    private static int ComputeStableItemId(string canonicalKey)
    {
        unchecked
        {
            var hash = 23;
            foreach (var ch in canonicalKey)
            {
                hash = (hash * 31) + ch;
            }

            if (hash == int.MinValue)
            {
                return int.MaxValue;
            }

            return Math.Abs(hash);
        }
    }

    private static string NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
