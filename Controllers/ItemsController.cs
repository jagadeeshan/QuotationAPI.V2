using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly QuotationDbContext _db;

    public ItemsController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await BuildItemsAsync();
        return Ok(items.OrderByDescending(i => i.UpdatedAt).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var items = await BuildItemsAsync();
        var item = items.FirstOrDefault(i => i.Id == id);
        return item is null ? NotFound() : Ok(item);
    }

    private async Task<List<ItemDto>> BuildItemsAsync()
    {
        var quotationRows = await _db.QuotationCalcRecords
            .AsNoTracking()
            .Select(row => new CalcRow("quotation", row.Id, row.DataJson, row.CreatedAt, row.UpdatedAt))
            .ToListAsync();

        var invoiceRows = await _db.InvoiceCalcRecords
            .AsNoTracking()
            .Select(row => new CalcRow("order", row.Id, row.DataJson, row.CreatedAt, row.UpdatedAt))
            .ToListAsync();

        var grouped = new List<ItemAccumulator>();
        var groupAliases = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in quotationRows.Concat(invoiceRows))
        {
            if (string.IsNullOrWhiteSpace(row.DataJson))
            {
                continue;
            }

            JsonObject? root;
            try
            {
                root = JsonNode.Parse(row.DataJson) as JsonObject;
            }
            catch
            {
                continue;
            }

            if (root is null)
            {
                continue;
            }

            var box = root["box"] as JsonObject;
            var sheet = root["sheet"] as JsonObject;
            var expense = root["expense"] as JsonObject;
            var price = root["price"] as JsonObject;
            var itemRef = root["item"] as JsonObject;

            var customerName = ReadString(itemRef, "customerName")
                ?? ReadString(box, "company")
                ?? string.Empty;
            var customerId = ReadString(itemRef, "customerId");
            var itemName = ReadString(itemRef, "itemName");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                continue;
            }

            var nameAlias = BuildNameAlias(customerName, itemName);
            var idAlias = BuildIdAlias(customerId, itemName);
            var explicitId = ReadInt(itemRef, "itemId");
            var generatedId = ComputeStableItemId(nameAlias);
            var itemId = explicitId > 0 ? explicitId : generatedId;

            var groupIndex = -1;
            if (!string.IsNullOrWhiteSpace(idAlias) && groupAliases.TryGetValue(idAlias, out var byIdIndex))
            {
                groupIndex = byIdIndex;
            }
            else if (groupAliases.TryGetValue(nameAlias, out var byNameIndex))
            {
                groupIndex = byNameIndex;
            }

            ItemAccumulator acc;
            if (groupIndex < 0)
            {
                acc = new ItemAccumulator
                {
                    Id = itemId,
                    HasExplicitId = explicitId > 0,
                    Name = itemName,
                    CustomerId = customerId,
                    CustomerName = customerName,
                    Box = box?.DeepClone() as JsonObject ?? new JsonObject(),
                    Sheet = sheet?.DeepClone() as JsonObject ?? new JsonObject(),
                    Expense = SanitizeExpenseSnapshot(expense?.DeepClone() as JsonObject) ?? new JsonObject(),
                    Price = price?.DeepClone() as JsonObject ?? new JsonObject(),
                    CreatedAt = row.CreatedAt,
                    UpdatedAt = row.UpdatedAt,
                };
                grouped.Add(acc);
                groupIndex = grouped.Count - 1;
            }
            else
            {
                acc = grouped[groupIndex];

                if (explicitId > 0)
                {
                    if (!acc.HasExplicitId)
                    {
                        acc.Id = explicitId;
                        acc.HasExplicitId = true;
                    }
                    else if (explicitId < acc.Id)
                    {
                        acc.Id = explicitId;
                    }
                }

                if (row.UpdatedAt >= acc.UpdatedAt)
                {
                    acc.Name = itemName;
                    acc.CustomerId = string.IsNullOrWhiteSpace(customerId) ? acc.CustomerId : customerId;
                    acc.CustomerName = string.IsNullOrWhiteSpace(customerName) ? acc.CustomerName : customerName;
                    acc.Box = box?.DeepClone() as JsonObject ?? acc.Box;
                    acc.Sheet = sheet?.DeepClone() as JsonObject ?? acc.Sheet;
                    acc.Expense = SanitizeExpenseSnapshot(expense?.DeepClone() as JsonObject) ?? acc.Expense;
                    acc.Price = price?.DeepClone() as JsonObject ?? acc.Price;
                    acc.UpdatedAt = row.UpdatedAt;
                }

                if (row.CreatedAt < acc.CreatedAt)
                {
                    acc.CreatedAt = row.CreatedAt;
                }
            }

            groupAliases[nameAlias] = groupIndex;
            if (!string.IsNullOrWhiteSpace(idAlias))
            {
                groupAliases[idAlias] = groupIndex;
            }

            if (row.Source == "quotation")
            {
                acc.QuotationIds.Add(row.RecordId);
            }
            else
            {
                acc.OrderIds.Add(row.RecordId);
            }
        }

        return grouped
            .Select(acc => new ItemDto
            {
                Id = acc.Id,
                Name = acc.Name,
                CustomerId = acc.CustomerId,
                CustomerName = acc.CustomerName,
                Box = acc.Box,
                Sheet = acc.Sheet,
                Expense = acc.Expense,
                Price = acc.Price,
                Links = new ItemLinksDto
                {
                    QuotationIds = acc.QuotationIds.OrderBy(x => x).ToList(),
                    OrderIds = acc.OrderIds.OrderBy(x => x).ToList(),
                },
                CreatedAt = acc.CreatedAt,
                UpdatedAt = acc.UpdatedAt,
            })
            .ToList();
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

    private static string BuildNameAlias(string customerName, string itemName)
    {
        var normalizedCustomerName = NormalizeToken(customerName);
        var normalizedItemName = NormalizeToken(itemName);
        return $"name:{normalizedCustomerName}|item:{normalizedItemName}";
    }

    private static string? BuildIdAlias(string? customerId, string itemName)
    {
        var normalizedCustomerId = NormalizeToken(customerId);
        if (string.IsNullOrWhiteSpace(normalizedCustomerId))
        {
            return null;
        }

        var normalizedItemName = NormalizeToken(itemName);
        return $"id:{normalizedCustomerId}|item:{normalizedItemName}";
    }

    private static int ComputeStableItemId(string canonicalKey)
    {
        var key = canonicalKey.Trim();
        unchecked
        {
            var hash = 23;
            foreach (var ch in key)
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

    private static JsonObject? SanitizeExpenseSnapshot(JsonObject? expense)
    {
        if (expense is null)
        {
            return null;
        }

        expense.Remove("wrkHrs");
        expense.Remove("persons");
        expense.Remove("contractLabour");
        expense.Remove("extraLabour");
        expense.Remove("mTransport");
        expense.Remove("pTransport");
        expense.Remove("dTransport");
        return expense;
    }

    private sealed class ItemAccumulator
    {
        public int Id { get; set; }
        public bool HasExplicitId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public JsonObject Box { get; set; } = new();
        public JsonObject Sheet { get; set; } = new();
        public JsonObject Expense { get; set; } = new();
        public JsonObject Price { get; set; } = new();
        public HashSet<int> QuotationIds { get; } = [];
        public HashSet<int> OrderIds { get; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed record CalcRow(string Source, int RecordId, string DataJson, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public JsonObject Box { get; set; } = new();
        public JsonObject Sheet { get; set; } = new();
        public JsonObject Expense { get; set; } = new();
        public JsonObject Price { get; set; } = new();
        public ItemLinksDto Links { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class ItemLinksDto
    {
        public List<int> QuotationIds { get; set; } = [];
        public List<int> OrderIds { get; set; } = [];
    }
}
