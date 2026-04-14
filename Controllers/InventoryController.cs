using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Inventory;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly QuotationDbContext _db;

    public InventoryController(QuotationDbContext db)
    {
        _db = db;
    }

    [HttpGet("reel-stocks")]
    public async Task<IActionResult> GetReelStocks([FromQuery] string? stockType = null)
    {
        var query = _db.ReelStocks.Where(r => !r.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(stockType))
        {
            var normalizedType = NormalizeStockType(stockType);
            if (normalizedType == "reel")
            {
                query = query.Where(r => string.IsNullOrWhiteSpace(r.StockType) || r.StockType!.ToLower() == "reel");
            }
            else
            {
                query = query.Where(r => !string.IsNullOrWhiteSpace(r.StockType) && r.StockType!.ToLower() == normalizedType);
            }
        }

        return Ok(await query.OrderByDescending(r => r.LastUpdated).ThenBy(r => r.ReelNumber).ToListAsync());
    }

    [HttpGet("gum-stocks")]
    public Task<IActionResult> GetGumStocks() => GetReelStocks("gum");

    [HttpGet("pin-stocks")]
    public Task<IActionResult> GetPinStocks() => GetReelStocks("pin");

    [HttpGet("rope-stocks")]
    public Task<IActionResult> GetRopeStocks() => GetReelStocks("rope");

    [HttpGet("reel-stocks/{id}")]
    public async Task<IActionResult> GetReelStockById(string id)
    {
        var row = await _db.ReelStocks.FindAsync(id);
        return row == null ? NotFound() : Ok(row);
    }

    [HttpPost("reel-stocks")]
    public async Task<IActionResult> CreateReelStock([FromBody] ReelStock row)
    {
        row.PurchaseVoucherNumber = (row.PurchaseVoucherNumber ?? string.Empty).Trim();
        row.Id = Guid.NewGuid().ToString("N");

        row.StockType = NormalizeStockType(row.StockType);
        row.Material = NormalizeMaterialByStockType(row.StockType, row.Material);

        if (string.IsNullOrWhiteSpace(row.PurchaseVoucherNumber))
        {
            return BadRequest(new { message = "Purchase voucher number is required for all stock entries." });
        }

        var existingReelNumbers = await _db.ReelStocks
            .Select(r => r.ReelNumber)
            .ToListAsync();

        row.ReelNumber = NextReelNumber(existingReelNumbers);

        // Guarantee uniqueness of reel number regardless of stock type/purchase linkage.
        row.ReelNumber = EnsureUniqueReelNumber(row.ReelNumber, existingReelNumbers);

        var purchase = await _db.PurchaseSalesRows.FindAsync(row.PurchaseVoucherNumber);
        if (purchase != null)
        {
            row.DealerName = purchase.PartyName;
        }

        NormalizeStockFinancials(row);

        await UpsertMaterialPriceFromStockAsync(row);

        row.LastUpdated = DateTime.UtcNow.ToString("o");
        if (string.IsNullOrWhiteSpace(row.Status))
        {
            row.Status = "active";
        }

        _db.ReelStocks.Add(row);
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpPost("gum-stocks")]
    public Task<IActionResult> CreateGumStock([FromBody] ReelStock row)
    {
        row.StockType = "gum";
        return CreateReelStock(row);
    }

    [HttpPost("pin-stocks")]
    public Task<IActionResult> CreatePinStock([FromBody] ReelStock row)
    {
        row.StockType = "pin";
        return CreateReelStock(row);
    }

    [HttpPost("rope-stocks")]
    public Task<IActionResult> CreateRopeStock([FromBody] ReelStock row)
    {
        row.StockType = "rope";
        return CreateReelStock(row);
    }

    [HttpPut("reel-stocks/{id}")]
    public async Task<IActionResult> UpdateReelStock(string id, [FromBody] ReelStock updated)
    {
        var row = await _db.ReelStocks.FindAsync(id);
        if (row == null)
        {
            return NotFound();
        }

        row.ReelNumber = updated.ReelNumber;
        row.StockType = NormalizeStockType(updated.StockType);
        row.Material = NormalizeMaterialByStockType(row.StockType, updated.Material);
        row.RollSize = updated.RollSize;
        row.Gsm = updated.Gsm;
        row.Bf = updated.Bf;
        row.Quantity = updated.Quantity;
        row.UnitCost = updated.UnitCost;
        row.Weight = updated.Weight;
        row.Amount = updated.Amount;
        row.PurchaseVoucherNumber = (updated.PurchaseVoucherNumber ?? string.Empty).Trim();
        row.PurchaseInvoiceNumber = updated.PurchaseInvoiceNumber;
        row.ReceivedDate = updated.ReceivedDate;
        row.Remarks = updated.Remarks;
        row.TaxPercent = updated.TaxPercent;
        row.TaxAmount = updated.TaxAmount;
        row.FinalAmount = updated.FinalAmount;
        row.CurrentStock = updated.CurrentStock;
        row.ReorderLevel = updated.ReorderLevel;
        row.Unit = string.IsNullOrWhiteSpace(updated.Unit) ? row.Unit : updated.Unit;
        row.Status = string.IsNullOrWhiteSpace(updated.Status) ? row.Status : updated.Status;

        if (string.IsNullOrWhiteSpace(row.PurchaseVoucherNumber))
        {
            return BadRequest(new { message = "Purchase voucher number is required for all stock entries." });
        }

        var purchase = await _db.PurchaseSalesRows.FindAsync(row.PurchaseVoucherNumber);
        if (purchase != null)
        {
            row.DealerName = purchase.PartyName;
        }

        NormalizeStockFinancials(row);

        await UpsertMaterialPriceFromStockAsync(row);

        row.LastUpdated = DateTime.UtcNow.ToString("o");

        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpPut("gum-stocks/{id}")]
    public Task<IActionResult> UpdateGumStock(string id, [FromBody] ReelStock updated)
    {
        updated.StockType = "gum";
        return UpdateReelStock(id, updated);
    }

    [HttpPut("pin-stocks/{id}")]
    public Task<IActionResult> UpdatePinStock(string id, [FromBody] ReelStock updated)
    {
        updated.StockType = "pin";
        return UpdateReelStock(id, updated);
    }

    [HttpPut("rope-stocks/{id}")]
    public Task<IActionResult> UpdateRopeStock(string id, [FromBody] ReelStock updated)
    {
        updated.StockType = "rope";
        return UpdateReelStock(id, updated);
    }

    [HttpDelete("reel-stocks/{id}")]
    public async Task<IActionResult> DeleteReelStock(string id)
    {
        var row = await _db.ReelStocks.FindAsync(id);
        if (row == null)
        {
            return NotFound();
        }

        row.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("gum-stocks/{id}")]
    public Task<IActionResult> DeleteGumStock(string id) => DeleteReelStock(id);

    [HttpDelete("pin-stocks/{id}")]
    public Task<IActionResult> DeletePinStock(string id) => DeleteReelStock(id);

    [HttpDelete("rope-stocks/{id}")]
    public Task<IActionResult> DeleteRopeStock(string id) => DeleteReelStock(id);

    [HttpGet("material-prices")]
    public async Task<IActionResult> GetMaterialPrices() =>
        Ok(await _db.MaterialPrices.Where(m => !m.IsDeleted).OrderByDescending(m => m.EffectiveDate).ToListAsync());

    [HttpGet("material-prices/lookup")]
    public async Task<IActionResult> LookupMaterialPrice(
        [FromQuery] string? material,
        [FromQuery] int? gsm,
        [FromQuery] decimal? bf)
    {
        var allRows = await _db.MaterialPrices
            .AsNoTracking()
            .Where(m => string.IsNullOrWhiteSpace(m.Status) || !m.Status.ToLower().Contains("inactive"))
            .ToListAsync();

        if (allRows.Count == 0)
        {
            return Ok(null);
        }

        var requestedMaterialKey = NormalizeMaterialKey(material);
        var requestedGsm = gsm.GetValueOrDefault();
        var requestedBf = bf.GetValueOrDefault();
        var hasGsm = requestedGsm > 0;
        var hasBf = requestedBf > 0;

        var bestMatch = allRows
            .Where(row => IsMaterialEquivalent(requestedMaterialKey, NormalizeMaterialKey(row.Material)))
            .Where(row => !hasGsm || row.Gsm == requestedGsm)
            .Where(row =>
            {
                if (!hasBf)
                {
                    return true;
                }

                if (!row.BF.HasValue)
                {
                    return false;
                }

                return row.BF.Value == requestedBf;
            })
            .OrderByDescending(row => row.EffectiveDate)
            .ThenByDescending(row =>
            {
                return int.TryParse(row.Id, out var numericId) ? numericId : 0;
            })
            .FirstOrDefault();

        return Ok(bestMatch);
    }

    /// <summary>
    /// Get latest price for GUM or PIN from MaterialPrice master
    /// Used when quotation/invoice is created or refreshed
    /// Returns null if no price exists in material master (fallback to system settings)
    /// </summary>
    [HttpGet("material-prices/gum-pin-rate/{material}")]
    public async Task<IActionResult> GetGumPinRate(string material)
    {
        try
        {
            var normalizedMaterial = NormalizeMaterialKey(material);
            
            // Fetch all active prices from database first
            var allPrices = await _db.MaterialPrices
                .AsNoTracking()
                .Where(m => string.IsNullOrWhiteSpace(m.Status) || !m.Status.ToLower().Contains("inactive"))
                .ToListAsync(); // Execute query before filtering

            // Apply normalization filter in client-side LINQ
            var latestPrice = allPrices
                .Where(m => NormalizeMaterialKey(m.Material) == normalizedMaterial)
                .OrderByDescending(m => m.EffectiveDate)
                .ThenByDescending(m => m.Id)
                .FirstOrDefault();

            return Ok(latestPrice);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error fetching material price", error = ex.Message });
        }
    }

    [HttpGet("material-prices/{id}")]
    public async Task<IActionResult> GetMaterialPriceById(string id)
    {
        var row = await _db.MaterialPrices.FindAsync(id);
        return row == null ? NotFound() : Ok(row);
    }

    [HttpPost("material-prices")]
    public async Task<IActionResult> CreateMaterialPrice([FromBody] MaterialPrice row)
    {
        row.Id = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(row.Status))
        {
            row.Status = "active";
        }

        _db.MaterialPrices.Add(row);
        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpPut("material-prices/{id}")]
    public async Task<IActionResult> UpdateMaterialPrice(string id, [FromBody] MaterialPrice updated)
    {
        var row = await _db.MaterialPrices.FindAsync(id);
        if (row == null)
        {
            return NotFound();
        }

        row.Material = updated.Material;
        row.Gsm = updated.Gsm;
        row.BF = updated.BF;
        row.Price = updated.Price;
        row.Unit = string.IsNullOrWhiteSpace(updated.Unit) ? row.Unit : updated.Unit;
        row.EffectiveDate = string.IsNullOrWhiteSpace(updated.EffectiveDate) ? row.EffectiveDate : updated.EffectiveDate;
        row.Supplier = updated.Supplier;
        row.Status = string.IsNullOrWhiteSpace(updated.Status) ? row.Status : updated.Status;

        await _db.SaveChangesAsync();
        return Ok(row);
    }

    [HttpDelete("material-prices/{id}")]
    public async Task<IActionResult> DeleteMaterialPrice(string id)
    {
        var row = await _db.MaterialPrices.FindAsync(id);
        if (row == null)
        {
            return NotFound();
        }

        row.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string NextId(IReadOnlyCollection<string> ids)
    {
        var max = ids
            .Select(id => int.TryParse(id, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return (max + 1).ToString();
    }

    private static string NextReelNumber(IReadOnlyCollection<string> reelNumbers)
    {
        var max = reelNumbers
            .Select(code => new string((code ?? string.Empty).Where(char.IsDigit).ToArray()))
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .DefaultIfEmpty(99)
            .Max();
        return $"RL-{max + 1}";
    }

    private static string EnsureUniqueReelNumber(string? requested, IReadOnlyCollection<string> existingReelNumbers)
    {
        var existing = new HashSet<string>(
            existingReelNumbers
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var seed = string.IsNullOrWhiteSpace(requested) ? "RL-100" : requested.Trim();
        if (!existing.Contains(seed))
        {
            return seed;
        }

        var digits = new string(seed.Where(char.IsDigit).ToArray());
        var next = int.TryParse(digits, out var parsed) && parsed > 0 ? parsed + 1 : 100;
        var candidate = $"RL-{next}";
        while (existing.Contains(candidate))
        {
            next++;
            candidate = $"RL-{next}";
        }

        return candidate;
    }

    private static string NormalizeMaterialKey(string? value)
    {
        return string.Concat((value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit));
    }

    private static bool IsMaterialEquivalent(string requestedKey, string rowKey)
    {
        if (string.IsNullOrWhiteSpace(requestedKey) || string.IsNullOrWhiteSpace(rowKey))
        {
            return false;
        }

        if (requestedKey == rowKey)
        {
            return true;
        }

        static bool IsKraftGroup(string key) =>
            key.Contains("kraft") || key.Contains("paper") || key.Contains("top") || key.Contains("base") || key.Contains("flute");

        if (IsKraftGroup(requestedKey) && IsKraftGroup(rowKey))
        {
            return true;
        }

        if (requestedKey.Contains("duplex") && rowKey.Contains("duplex"))
        {
            return true;
        }

        if (requestedKey.Contains("gum") && rowKey.Contains("gum"))
        {
            return true;
        }

        if (requestedKey.Contains("pin") && rowKey.Contains("pin"))
        {
            return true;
        }

        if (requestedKey.Contains("rope") && rowKey.Contains("rope"))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeStockType(string? value)
    {
        var normalized = string.Concat((value ?? "reel")
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit));

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "reel";
        }

        return normalized switch
        {
            "gum" => "gum",
            "pin" => "pin",
            "rope" => "rope",
            _ => "reel",
        };
    }

    private static string NormalizeMaterialByStockType(string stockType, string? material)
    {
        if (stockType == "gum")
        {
            return "Gum";
        }

        if (stockType == "pin")
        {
            return "Pin";
        }

        if (stockType == "rope")
        {
            return "Rope";
        }

        var requested = (material ?? string.Empty).Trim();
        if (requested.Equals("duplex", StringComparison.OrdinalIgnoreCase))
        {
            return "Duplex";
        }

        return "Kraft";
    }

    private static void NormalizeStockFinancials(ReelStock row)
    {
        row.Weight = row.Weight < 0 ? 0 : row.Weight;
        row.Quantity = row.Quantity < 0 ? 0 : row.Quantity;
        row.TaxPercent = row.TaxPercent < 0 ? 0 : row.TaxPercent;

        // Keep compatibility with existing payloads: if amount is missing, derive from weight x price/kg.
        if (row.Amount <= 0 && row.UnitCost > 0 && row.Weight > 0)
        {
            row.Amount = Math.Round(row.UnitCost * row.Weight, 4);
        }

        if (row.TaxAmount <= 0 && row.Amount > 0 && row.TaxPercent > 0)
        {
            row.TaxAmount = Math.Round(row.Amount * row.TaxPercent / 100m, 4);
        }

        if (row.FinalAmount <= 0)
        {
            row.FinalAmount = Math.Round(row.Amount + row.TaxAmount, 4);
        }

        if (row.CurrentStock <= 0 && row.Weight > 0)
        {
            row.CurrentStock = row.Weight;
        }
    }

    private async Task UpsertMaterialPriceFromStockAsync(ReelStock stock)
    {
        if (stock.UnitCost <= 0)
        {
            return;
        }

        var material = NormalizeMaterialByStockType(NormalizeStockType(stock.StockType), stock.Material);
        if (string.IsNullOrWhiteSpace(material))
        {
            return;
        }

        var normalizedMaterial = NormalizeMaterialKey(material);
        var stockGsm = stock.Gsm;
        var stockBf = stock.Bf ?? 0m;
        var requiresBf = stock.StockType == "reel" && stockBf > 0;

        var activePrices = await _db.MaterialPrices
            .Where(m => string.IsNullOrWhiteSpace(m.Status) || !m.Status.ToLower().Contains("inactive"))
            .ToListAsync();

        var existing = activePrices
            .Where(m => NormalizeMaterialKey(m.Material) == normalizedMaterial)
            .Where(m => stockGsm <= 0 || m.Gsm == stockGsm)
            .Where(m => !requiresBf || (m.BF.HasValue && m.BF.Value == stockBf))
            .OrderByDescending(m => m.EffectiveDate)
            .ThenByDescending(m => m.Id)
            .FirstOrDefault();

        var effectiveDate = string.IsNullOrWhiteSpace(stock.ReceivedDate)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : stock.ReceivedDate!;

        if (existing == null)
        {
            _db.MaterialPrices.Add(new MaterialPrice
            {
                Id = Guid.NewGuid().ToString("N"),
                Material = material,
                Gsm = stockGsm,
                BF = requiresBf ? stockBf : null,
                Price = stock.UnitCost,
                Unit = string.IsNullOrWhiteSpace(stock.Unit) ? "kg" : stock.Unit,
                EffectiveDate = effectiveDate,
                Supplier = string.IsNullOrWhiteSpace(stock.DealerName) ? "" : stock.DealerName!,
                Status = "active"
            });
            return;
        }

        var hasChange = existing.Price != stock.UnitCost
            || existing.Gsm != stockGsm
            || (requiresBf && existing.BF != stockBf)
            || (!requiresBf && existing.BF.HasValue)
            || existing.Material != material
            || !string.Equals(existing.Status, "active", StringComparison.OrdinalIgnoreCase)
            || existing.EffectiveDate != effectiveDate;

        if (!hasChange)
        {
            return;
        }

        existing.Material = material;
        existing.Gsm = stockGsm;
        existing.BF = requiresBf ? stockBf : null;
        existing.Price = stock.UnitCost;
        existing.Unit = string.IsNullOrWhiteSpace(stock.Unit) ? existing.Unit : stock.Unit;
        existing.EffectiveDate = effectiveDate;
        existing.Supplier = string.IsNullOrWhiteSpace(stock.DealerName) ? existing.Supplier : stock.DealerName!;
        existing.Status = "active";
    }
}
