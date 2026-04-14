namespace QuotationAPI.V2.Models.Inventory;

using System.ComponentModel.DataAnnotations.Schema;

public class ReelStock
{
    public string Id { get; set; } = "";
    public string ReelNumber { get; set; } = "";
    public string StockType { get; set; } = "reel";
    public string Material { get; set; } = "";
    public string? RollSize { get; set; }
    public int Gsm { get; set; }
    public decimal? Bf { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Weight { get; set; }
    public decimal Amount { get; set; }
    public string? PurchaseVoucherNumber { get; set; }
    public string? DealerName { get; set; }
    public string? PurchaseInvoiceNumber { get; set; }
    public string? ReceivedDate { get; set; }
    public string? Remarks { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public string Unit { get; set; } = "kg";
    public string LastUpdated { get; set; } = "";
    public string Status { get; set; } = "active";
    public bool IsDeleted { get; set; } = false;
}

public class MaterialPrice
{
    public string Id { get; set; } = "";
    public string Material { get; set; } = "";
    public int Gsm { get; set; }
    public decimal? BF { get; set; }
    public decimal Price { get; set; }
    [NotMapped]
    public decimal PricePerKg => Price;
    public string Unit { get; set; } = "kg";
    public string EffectiveDate { get; set; } = "";
    public string Supplier { get; set; } = "";
    public string Status { get; set; } = "active";
    public bool IsDeleted { get; set; } = false;
}
