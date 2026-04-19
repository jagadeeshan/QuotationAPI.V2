namespace QuotationAPI.V2.Models.Expense;

public class ExpenseRecord
{
    public string Id { get; set; } = "";
    public string ExpenseNumber { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string ExpenseDate { get; set; } = "";
    public string PaidBy { get; set; } = "";
    public string PaymentMethod { get; set; } = "cash";
    public string Remarks { get; set; } = "";
    public string Status { get; set; } = "draft";
    public string? SourceModule { get; set; }  // e.g. "salary-advance", "salary-settlement"
    public string? SourceId { get; set; }       // ID of the source record
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}

public class CreateExpenseRequest
{
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string ExpenseDate { get; set; } = "";
    public string PaidBy { get; set; } = "";
    public string PaymentMethod { get; set; } = "cash";
    public string Remarks { get; set; } = "";
    public string Status { get; set; } = "draft";
    public string? SourceModule { get; set; }
    public string? SourceId { get; set; }
}

public class UpdateExpenseRequest
{
    public string? Category { get; set; }
    public decimal? Amount { get; set; }
    public string? ExpenseDate { get; set; }
    public string? PaidBy { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Remarks { get; set; }
    public string? Status { get; set; }
}
