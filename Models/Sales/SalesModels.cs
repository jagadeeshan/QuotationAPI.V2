using System.ComponentModel.DataAnnotations;

namespace QuotationAPI.V2.Models.Sales;

public class WasteSale
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? CustomerId { get; set; }

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Weight of waste in kilograms.</summary>
    [Range(0, double.MaxValue)]
    public decimal WeightKg { get; set; }

    /// <summary>Selling price per kg of waste.</summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>Total sale value = WeightKg × UnitPrice.</summary>
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    public string? Description { get; set; }

    [Required]
    public string SaleDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string Status { get; set; } = "active";

    public string CreatedDate { get; set; } = DateTime.UtcNow.ToString("o");
    public string? UpdatedDate { get; set; }

    /// <summary>Reference to the auto-created CustomerOutstanding Id.</summary>
    public string? OutstandingId { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class RollSale
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? CustomerId { get; set; }

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Weight of rolls sold in kilograms.</summary>
    [Range(0, double.MaxValue)]
    public decimal WeightKg { get; set; }

    /// <summary>Selling price per kg of rolls.</summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>Raw material (paper) cost per kg.</summary>
    [Range(0, double.MaxValue)]
    public decimal PaperPricePerKg { get; set; }

    public string? RollSize { get; set; }

    public string? Description { get; set; }

    // --- Computed and stored at save time ---

    /// <summary>Income = WeightKg × UnitPrice.</summary>
    [Range(0, double.MaxValue)]
    public decimal TotalIncome { get; set; }

    /// <summary>Paper raw material cost = WeightKg × PaperPricePerKg.</summary>
    [Range(0, double.MaxValue)]
    public decimal PaperCost { get; set; }

    /// <summary>Gum used in kg = (WeightKg/1000) × GumKgPerTon (from LOV).</summary>
    [Range(0, double.MaxValue)]
    public decimal GumUsedKg { get; set; }

    /// <summary>Monetary cost of gum = GumUsedKg × (gumRate/170).</summary>
    [Range(0, double.MaxValue)]
    public decimal GumCost { get; set; }

    /// <summary>EB units consumed = (WeightKg/1000) × EbUnitsPerTon (from LOV).</summary>
    [Range(0, double.MaxValue)]
    public decimal EbUsedUnits { get; set; }

    /// <summary>Monetary EB cost = EbUsedUnits × (ebRate/8).</summary>
    [Range(0, double.MaxValue)]
    public decimal EbCost { get; set; }

    /// <summary>Profit = TotalIncome − PaperCost − GumCost − EbCost.</summary>
    public decimal Profit { get; set; }

    [Required]
    public string SaleDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string Status { get; set; } = "active";

    public string CreatedDate { get; set; } = DateTime.UtcNow.ToString("o");
    public string? UpdatedDate { get; set; }

    /// <summary>Reference to the auto-created CustomerOutstanding Id.</summary>
    public string? OutstandingId { get; set; }
    public bool IsDeleted { get; set; } = false;
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public class SaveWasteSaleRequest
{
    public string? CustomerId { get; set; }
    [Required] public string CustomerName { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Description { get; set; }
    public string? SaleDate { get; set; }
}

public class SaveRollSaleRequest
{
    public string? CustomerId { get; set; }
    [Required] public string CustomerName { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal PaperPricePerKg { get; set; }
    public string? RollSize { get; set; }
    public string? Description { get; set; }
    public string? SaleDate { get; set; }
}
