using System.ComponentModel.DataAnnotations;

namespace QuotationAPI.V2.Models.Accounts;

public class BankCashBalance
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, RegularExpression("^(bank|cash)$")]
    public string Type { get; set; } = "cash";

    [Range(0, double.MaxValue)]
    public decimal Balance { get; set; }

    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public string? LastUpdated { get; set; }
}

public class IncomeEntry
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, RegularExpression("^(bank|cash)$")]
    public string Type { get; set; } = "cash";

    [Required, RegularExpression("^(customer_payment|independent)$")]
    public string IncomeType { get; set; } = "independent";

    [Required]
    public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string Category { get; set; } = "Other";

    public string? OutstandingId { get; set; }
    public string? Reference { get; set; }
    public string? CreatedDate { get; set; }

    [Required, RegularExpression("^(active|cancelled)$")]
    public string Status { get; set; } = "active";
    public bool IsDeleted { get; set; } = false;
}

public class CustomerOutstanding
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? OrderId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public string? DueDate { get; set; }
    public string? CreatedDate { get; set; }

    [Required]
    public string Status { get; set; } = "pending";

    public decimal? PaidAmount { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class ExpenseEntry
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, RegularExpression("^(cash|bank)$")]
    public string Type { get; set; } = "cash";

    [Required]
    public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string Category { get; set; } = "Other";

    public string? Reference { get; set; }

    [Required]
    public string Status { get; set; } = "active";
    public bool IsDeleted { get; set; } = false;
}

public class AccountTransaction
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string Type { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, RegularExpression("^(bank|cash)$")]
    public string BalanceType { get; set; } = "cash";

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public string? Category { get; set; }
    public string? Reference { get; set; }
}

public class CashTransfer
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string FromAccount { get; set; } = string.Empty;

    [Required]
    public string ToAccount { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string TransferDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public string? Remarks { get; set; }
    public string? CreatedDate { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class ExpenseLedgerRow
{
    [Key]
    public string VoucherNumber { get; set; } = string.Empty;

    [Required]
    public string Head { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [Required]
    public string ExpenseDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string ApprovedBy { get; set; } = string.Empty;
}

public class IncomeRow
{
    [Key]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Required]
    public string Source { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [Required]
    public string ReceivedDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string PaymentMode { get; set; } = string.Empty;
}

public class PurchaseSalesRow
{
    [Key]
    public string VoucherNumber { get; set; } = string.Empty;

    [Required]
    public string TransactionType { get; set; } = "Purchase";

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    public string PartyName { get; set; } = string.Empty;

    [Required]
    public string PaymentType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal TotalAmountPaid { get; set; }

    public decimal TaxPercent { get; set; }

    public decimal TaxAmount { get; set; }

    [Required]
    public string VoucherDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public bool IsDeleted { get; set; } = false;
}

public class PurchaseCreateRequest
{
    public string? VoucherNumber { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    public string PaymentType { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal TotalAmountPaid { get; set; }

    [Range(0, 100)]
    public decimal TaxPercent { get; set; }

    [Required]
    public string VoucherDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
}

public class PurchaseUpdateRequest
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    public string PaymentType { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal TotalAmountPaid { get; set; }

    [Range(0, 100)]
    public decimal TaxPercent { get; set; }

    [Required]
    public string VoucherDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
}

public class TaxPaymentRow
{
    [Key]
    public string ChallanNo { get; set; } = string.Empty;

    [Required]
    public string TaxType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal OtherInputCredit { get; set; }

    [Required]
    public string PaymentDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string Period { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;
}

public class GstPurchaseSyncItem
{
    public string GstNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceDate { get; set; } = string.Empty;
    public decimal TaxableAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = "bank";
}

public class GstPurchaseComparisonRow
{
    public string RowId { get; set; } = Guid.NewGuid().ToString("N");
    public string MatchStatus { get; set; } = "new";
    public string? MatchReason { get; set; }
    public string? MatchedVoucherNumber { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal AmountDifference { get; set; }
    public GstPurchaseSyncItem GstPurchase { get; set; } = new();
}

public class GstPurchaseSyncResponse
{
    public string Period { get; set; } = string.Empty;
    public decimal GstTotalAmount { get; set; }
    public decimal ExistingTotalAmount { get; set; }
    public List<GstPurchaseComparisonRow> Rows { get; set; } = [];
}

public class GstPurchaseSyncApplyRow
{
    [Required]
    public string Action { get; set; } = "ignore";

    [Required]
    public GstPurchaseSyncItem GstPurchase { get; set; } = new();

    public string? MatchedVoucherNumber { get; set; }
}

public class GstPurchaseSyncApplyRequest
{
    [Required]
    public string Period { get; set; } = string.Empty;

    public List<GstPurchaseSyncApplyRow> Rows { get; set; } = [];
}

public class GstPurchaseSyncApplyResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Ignored { get; set; }
    public List<string> Errors { get; set; } = [];
}

public record ExpenseSummary(
    string Category,
    decimal Amount,
    string Month,
    int Year,
    string Type
);

public record AccountSummary(
    decimal CashInHand,
    decimal BankBalance,
    decimal IncomeMtd,
    decimal ExpenseMtd
);

public record ExpenseDashboard(
    decimal TotalMonthExpense,
    decimal ApprovedAmount,
    decimal PendingAmount,
    IEnumerable<TopCategory> TopCategories
);

public record TopCategory(string Category, decimal MonthlyAmount);

public class SetInitialBalanceRequest
{
    [Required, RegularExpression("^(bank|cash)$")]
    public string Type { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Description { get; set; }
}

public class CashTransferCreateRequest
{
    [Required]
    public string FromAccount { get; set; } = string.Empty;

    [Required]
    public string ToAccount { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string TransferDate { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public string? Description { get; set; }
}

public class CashTransferUpdateRequest
{
    [Required]
    public string FromAccount { get; set; } = string.Empty;

    [Required]
    public string ToAccount { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string TransferDate { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public string? Description { get; set; }
}

public class TaxPaymentCreateRequest
{
    public string? ChallanNo { get; set; }

    [Required]
    public string TaxType { get; set; } = "GST";

    [Required, RegularExpression("^(bank|cash)$")]
    public string Mode { get; set; } = "bank";

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string Period { get; set; } = DateTime.UtcNow.ToString("yyyy-MM");
}

public class TaxPaymentUpdateRequest
{
    [Required]
    public string TaxType { get; set; } = "GST";

    [Required, RegularExpression("^(bank|cash)$")]
    public string Mode { get; set; } = "bank";

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Required]
    public string Period { get; set; } = DateTime.UtcNow.ToString("yyyy-MM");
}

public class CustomerOutstandingSummary
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal AdditionalOutstanding { get; set; }
    public decimal SettledAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
}

public class CustomerSettlementRequest
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, RegularExpression("^(bank|cash)$")]
    public string Type { get; set; } = "bank";

    [Required]
    public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public string? Reference { get; set; }
    public string? Description { get; set; }
}

public class AdditionalOutstandingRequest
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public string Date { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");

    public string? DueDate { get; set; }
    public string? Description { get; set; }
}
