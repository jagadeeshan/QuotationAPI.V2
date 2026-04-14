namespace QuotationAPI.V2.Models.Integrations;

public class ZohoSyncState
{
    public int Id { get; set; } = 1;
    public DateTime? LastCustomersSyncUtc { get; set; }
    public DateTime? LastInvoicesSyncUtc { get; set; }
    public DateTime? LastOutstandingSyncUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ZohoCustomerRecord
{
    public string Id { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateTime? LastModifiedTimeUtc { get; set; }
    public DateTime PulledAtUtc { get; set; } = DateTime.UtcNow;
}

public class ZohoInvoiceRecord
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public string? Status { get; set; }
    public DateTime? LastModifiedTimeUtc { get; set; }
    public DateTime PulledAtUtc { get; set; } = DateTime.UtcNow;
}

public class ZohoOutstandingRecord
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateTime PulledAtUtc { get; set; } = DateTime.UtcNow;
}

public class ZohoDeltaPullResult
{
    public int CustomersUpserted { get; set; }
    public int InvoicesUpserted { get; set; }
    public int OutstandingUpserted { get; set; }
    public DateTime PulledAtUtc { get; set; } = DateTime.UtcNow;
}
