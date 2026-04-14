using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Accounts;
using QuotationAPI.V2.Models.Sales;

namespace QuotationAPI.V2.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly QuotationDbContext _db;

    public SalesController(QuotationDbContext db)
    {
        _db = db;
    }

    // ────────────────────────────────────────────────────────────────
    //  WASTE SALES
    // ────────────────────────────────────────────────────────────────

    [HttpGet("waste")]
    public async Task<IActionResult> GetWasteSales() =>
        Ok(await _db.WasteSales.Where(x => !x.IsDeleted).OrderByDescending(x => x.SaleDate).ToListAsync());

    [HttpGet("waste/{id}")]
    public async Task<IActionResult> GetWasteSaleById(string id)
    {
        var row = await _db.WasteSales.FindAsync(id);
        return row == null ? NotFound() : Ok(row);
    }

    [HttpPost("waste")]
    public async Task<IActionResult> CreateWasteSale([FromBody] SaveWasteSaleRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var totalAmount = Math.Round(request.WeightKg * request.UnitPrice, 4);

        var sale = new WasteSale
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            WeightKg = request.WeightKg,
            UnitPrice = request.UnitPrice,
            TotalAmount = totalAmount,
            Description = request.Description,
            SaleDate = string.IsNullOrWhiteSpace(request.SaleDate)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd")
                : request.SaleDate,
            Status = "active",
            CreatedDate = DateTime.UtcNow.ToString("o"),
        };

        // Auto-create customer outstanding for the sale amount
        var outstanding = new CustomerOutstanding
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            Amount = totalAmount,
            Description = $"Waste Sale – {(string.IsNullOrWhiteSpace(request.Description) ? "Waste material" : request.Description)}",
            Date = sale.SaleDate,
            Status = "pending",
            CreatedDate = DateTime.UtcNow.ToString("O"),
        };
        _db.CustomerOutstandings.Add(outstanding);
        sale.OutstandingId = outstanding.Id;

        _db.WasteSales.Add(sale);
        await _db.SaveChangesAsync();
        return Ok(sale);
    }

    [HttpPut("waste/{id}")]
    public async Task<IActionResult> UpdateWasteSale(string id, [FromBody] SaveWasteSaleRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var sale = await _db.WasteSales.FindAsync(id);
        if (sale == null) return NotFound();

        var totalAmount = Math.Round(request.WeightKg * request.UnitPrice, 4);
        var oldAmount = sale.TotalAmount;

        sale.CustomerId = request.CustomerId;
        sale.CustomerName = request.CustomerName;
        sale.WeightKg = request.WeightKg;
        sale.UnitPrice = request.UnitPrice;
        sale.TotalAmount = totalAmount;
        sale.Description = request.Description;
        sale.SaleDate = string.IsNullOrWhiteSpace(request.SaleDate) ? sale.SaleDate : request.SaleDate;
        sale.UpdatedDate = DateTime.UtcNow.ToString("o");

        // Update the linked outstanding if amount changed
        if (sale.OutstandingId != null && oldAmount != totalAmount)
        {
            var outstanding = await _db.CustomerOutstandings.FindAsync(sale.OutstandingId);
            if (outstanding != null)
            {
                outstanding.Amount = totalAmount;
                outstanding.CustomerName = request.CustomerName;
                outstanding.CustomerId = request.CustomerId;
                outstanding.Description = $"Waste Sale – {(string.IsNullOrWhiteSpace(request.Description) ? "Waste material" : request.Description)}";
            }
        }

        await _db.SaveChangesAsync();
        return Ok(sale);
    }

    [HttpDelete("waste/{id}")]
    public async Task<IActionResult> DeleteWasteSale(string id)
    {
        var sale = await _db.WasteSales.FindAsync(id);
        if (sale == null) return NotFound();

        // Soft-delete: mark linked outstanding
        if (sale.OutstandingId != null)
        {
            var outstanding = await _db.CustomerOutstandings.FindAsync(sale.OutstandingId);
            if (outstanding != null)
            {
                outstanding.IsDeleted = true;
            }
        }

        sale.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ────────────────────────────────────────────────────────────────
    //  ROLL SALES
    // ────────────────────────────────────────────────────────────────

    [HttpGet("roll")]
    public async Task<IActionResult> GetRollSales() =>
        Ok(await _db.RollSales.Where(x => !x.IsDeleted).OrderByDescending(x => x.SaleDate).ToListAsync());

    [HttpGet("roll/{id}")]
    public async Task<IActionResult> GetRollSaleById(string id)
    {
        var row = await _db.RollSales.FindAsync(id);
        return row == null ? NotFound() : Ok(row);
    }

    [HttpPost("roll")]
    public async Task<IActionResult> CreateRollSale([FromBody] SaveRollSaleRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var sale = await BuildRollSale(request);
        sale.Id = Guid.NewGuid().ToString();
        sale.CreatedDate = DateTime.UtcNow.ToString("o");

        // Auto-create customer outstanding for the income amount
        var outstanding = new CustomerOutstanding
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            Amount = sale.TotalIncome,
            Description = $"Roll Sale – {(string.IsNullOrWhiteSpace(request.Description) ? "Paper rolls" : request.Description)}",
            Date = sale.SaleDate,
            Status = "pending",
            CreatedDate = DateTime.UtcNow.ToString("O"),
        };
        _db.CustomerOutstandings.Add(outstanding);
        sale.OutstandingId = outstanding.Id;

        _db.RollSales.Add(sale);
        await _db.SaveChangesAsync();
        return Ok(sale);
    }

    [HttpPut("roll/{id}")]
    public async Task<IActionResult> UpdateRollSale(string id, [FromBody] SaveRollSaleRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var existing = await _db.RollSales.FindAsync(id);
        if (existing == null) return NotFound();

        var updated = await BuildRollSale(request);
        var oldIncome = existing.TotalIncome;

        existing.CustomerId = updated.CustomerId;
        existing.CustomerName = updated.CustomerName;
        existing.WeightKg = updated.WeightKg;
        existing.UnitPrice = updated.UnitPrice;
        existing.PaperPricePerKg = updated.PaperPricePerKg;
        existing.RollSize = updated.RollSize;
        existing.Description = updated.Description;
        existing.SaleDate = updated.SaleDate;
        existing.TotalIncome = updated.TotalIncome;
        existing.PaperCost = updated.PaperCost;
        existing.GumUsedKg = updated.GumUsedKg;
        existing.GumCost = updated.GumCost;
        existing.EbUsedUnits = updated.EbUsedUnits;
        existing.EbCost = updated.EbCost;
        existing.Profit = updated.Profit;
        existing.UpdatedDate = DateTime.UtcNow.ToString("o");

        // Update linked outstanding
        if (existing.OutstandingId != null && oldIncome != updated.TotalIncome)
        {
            var outstanding = await _db.CustomerOutstandings.FindAsync(existing.OutstandingId);
            if (outstanding != null)
            {
                outstanding.Amount = updated.TotalIncome;
                outstanding.CustomerName = updated.CustomerName;
                outstanding.CustomerId = updated.CustomerId;
                outstanding.Description = $"Roll Sale – {(string.IsNullOrWhiteSpace(request.Description) ? "Paper rolls" : request.Description)}";
            }
        }

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("roll/{id}")]
    public async Task<IActionResult> DeleteRollSale(string id)
    {
        var sale = await _db.RollSales.FindAsync(id);
        if (sale == null) return NotFound();

        // Soft-delete: mark linked outstanding
        if (sale.OutstandingId != null)
        {
            var outstanding = await _db.CustomerOutstandings.FindAsync(sale.OutstandingId);
            if (outstanding != null)
            {
                outstanding.IsDeleted = true;
            }
        }

        sale.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ────────────────────────────────────────────────────────────────
    //  PRIVATE HELPERS
    // ────────────────────────────────────────────────────────────────

    private async Task<RollSale> BuildRollSale(SaveRollSaleRequest request)
    {
        // Read LOV rates
        var gumRateLov = await GetLovNumericValue("gumrate", "gum", 610m);
        var ebRateLov = await GetLovNumericValue("ebrate", "eb", 3200m);
        var gumKgPerTon = await GetLovNumericValue("rollsalesgumkgperton", "roll sales gum kg per ton", 23m);
        var ebUnitsPerTon = await GetLovNumericValue("rollsalesebunitsperton", "roll sales eb units per ton", 10m);

        var weightTons = request.WeightKg / 1000m;

        var totalIncome = Math.Round(request.WeightKg * request.UnitPrice, 4);
        var paperCost = Math.Round(request.WeightKg * request.PaperPricePerKg, 4);

        var gumUsedKg = Math.Round(weightTons * gumKgPerTon, 4);
        var gumCost = Math.Round(gumUsedKg * (gumRateLov / 170m), 4);

        var ebUsedUnits = Math.Round(weightTons * ebUnitsPerTon, 4);
        var ebCost = Math.Round(ebUsedUnits * (ebRateLov / 8m), 4);

        var profit = Math.Round(totalIncome - paperCost - gumCost - ebCost, 4);

        return new RollSale
        {
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            WeightKg = request.WeightKg,
            UnitPrice = request.UnitPrice,
            PaperPricePerKg = request.PaperPricePerKg,
            RollSize = request.RollSize,
            Description = request.Description,
            SaleDate = string.IsNullOrWhiteSpace(request.SaleDate)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd")
                : request.SaleDate,
            TotalIncome = totalIncome,
            PaperCost = paperCost,
            GumUsedKg = gumUsedKg,
            GumCost = gumCost,
            EbUsedUnits = ebUsedUnits,
            EbCost = ebCost,
            Profit = profit,
            Status = "active",
        };
    }

    private async Task<decimal> GetLovNumericValue(string nameKeyLower, string fallbackKeyword, decimal defaultValue)
    {
        var items = await _db.LovItems
            .Where(x => x.Isactive == "Y")
            .ToListAsync();

        var match = items.FirstOrDefault(x =>
            x.Name.ToLower().Replace(" ", "").Contains(nameKeyLower.Replace(" ", "")) ||
            x.Description != null && x.Description.ToLower().Contains(fallbackKeyword));

        if (match?.Value != null) return match.Value.Value;

        // Try description as numeric if no Value field set
        if (match?.Description != null && decimal.TryParse(match.Description, out var dval))
            return dval;

        return defaultValue;
    }
}
