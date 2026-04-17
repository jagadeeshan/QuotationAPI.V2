using System.ComponentModel.DataAnnotations;

namespace QuotationAPI.V2.Models.Quotations;

public enum QuotationStatus
{
    Draft,
    Sent,
    Approved,
    Rejected,
    Expired,
    Converted
}

public class Quotation
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, MaxLength(40)]
    public string QuoteNumber { get; set; } = string.Empty;

    [MaxLength(40)]
    public string CustomerId { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 365)]
    public int ValidityDays { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }


    [Required, MaxLength(80)]
    public string CreatedBy { get; set; } = "system";

    [MaxLength(80)]
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; } = false;

    public ICollection<QuotationLineItem> LineItems { get; set; } = new List<QuotationLineItem>();
}

public class QuotationLineItem
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string QuotationId { get; set; } = string.Empty;

    public Quotation? Quotation { get; set; }

    [Required, MaxLength(250)]
    public string ItemDescription { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalPrice { get; set; }

    public bool IsDeleted { get; set; } = false;
}

public class CreateLineItemRequest
{
    [Required, MaxLength(250)]
    public string ItemDescription { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public class CreateQuotationRequest
{
    [Required, MaxLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 365)]
    public int ValidityDays { get; set; }


    public IEnumerable<CreateLineItemRequest>? LineItems { get; set; }
}

public class UpdateQuotationRequest
{
    [Required, MaxLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 365)]
    public int ValidityDays { get; set; }

    public QuotationStatus? Status { get; set; }


    public IEnumerable<CreateLineItemRequest>? LineItems { get; set; }
}

public record QuotationListResponse(
    IEnumerable<Quotation> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
