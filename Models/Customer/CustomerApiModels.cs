namespace QuotationAPI.V2.Models.Customer;

public class CustomerMaster
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string Address { get; set; } = "";
    public string? GstNumber { get; set; }
    public string CustomerType { get; set; } = "Retail";
    public string Status { get; set; } = "Active";
    public decimal OpeningBalance { get; set; }
    public string CreatedDate { get; set; } = "";
    public string? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class SaveCustomerRequest
{
    public string? Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string Address { get; set; } = "";
    public string? GstNumber { get; set; }
    public string CustomerType { get; set; } = "Retail";
    public string Status { get; set; } = "Active";
    public decimal OpeningBalance { get; set; }
}

public class GstCustomerLookupResponse
{
    public string GstNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? StateCode { get; set; }
    public string? Pincode { get; set; }
    public string Source { get; set; } = "GST_SIMULATED";
    public string LastUpdatedUtc { get; set; } = DateTime.UtcNow.ToString("O");
}
