namespace QuotationAPI.V2.Models.Calculations;

public class QuotationCalcRecord
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CompanyName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string DataJson { get; set; } = "{}";
    public bool IsDeleted { get; set; } = false;
}

public class InvoiceCalcRecord
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CompanyName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string DataJson { get; set; } = "{}";
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeliveryDate { get; set; }
}
public class CalcRecordSaveRequest
{
    public string CompanyName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string DataJson { get; set; } = "{}";
    public DateTime? DeliveryDate { get; set; }
    public bool ForceNewItemIdentity { get; set; }
}
