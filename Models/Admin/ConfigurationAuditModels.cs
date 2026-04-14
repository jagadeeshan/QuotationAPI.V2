namespace QuotationAPI.V2.Models.Admin;

/// <summary>
/// Audits all configuration value changes for history tracking
/// </summary>
public class ConfigurationHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SettingKey { get; set; } = "";
    public string? OldValue { get; set; }
    public string NewValue { get; set; } = "";
    public string ChangeType { get; set; } = "UPDATE"; // CREATE, UPDATE, DELETE
    public string? Description { get; set; }
    public string ChangedBy { get; set; } = "system";
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    public string IsActive { get; set; } = "Y";
    public string? Notes { get; set; }
}

/// <summary>
/// Stores configuration values as a snapshot when a quotation is created
/// Allows old quotations to use values current at their creation time
/// </summary>
public class QuotationConfigSnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long QuotationId { get; set; }
    public string ConfigKey { get; set; } = "";
    public string ConfigValue { get; set; } = "";
    public string ConfigType { get; set; } = "string"; // decimal, int, string
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public string IsActive { get; set; } = "Y";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}

/// <summary>
/// Stores configuration values as a snapshot when an invoice is created
/// Identical to QuotationConfigSnapshot but for invoice records
/// </summary>
public class InvoiceConfigSnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long InvoiceId { get; set; }
    public string ConfigKey { get; set; } = "";
    public string ConfigValue { get; set; } = "";
    public string ConfigType { get; set; } = "string";
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public string IsActive { get; set; } = "Y";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}

/// <summary>
/// DTO for configuration history response
/// </summary>
public class ConfigurationHistoryDto
{
    public string Id { get; set; } = "";
    public string SettingKey { get; set; } = "";
    public string? OldValue { get; set; }
    public string NewValue { get; set; } = "";
    public string ChangeType { get; set; } = "";
    public string? Description { get; set; }
    public string ChangedBy { get; set; } = "";
    public DateTime ChangedDate { get; set; }
    public string Impact { get; set; } = ""; // "New quotations only", "All calculations", etc.
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for configuration snapshot display
/// </summary>
public class ConfigSnapshotDto
{
    public string ConfigKey { get; set; } = "";
    public string CurrentValue { get; set; } = "";
    public string OriginalValue { get; set; } = "";
    public DateTime SnapshotDate { get; set; }
    public bool IsChanged { get; set; }
    public string Unit { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>
/// Request DTO to recalculate quotation with specific configuration values
/// </summary>
public class RecalculateQuotationRequest
{
    public long QuotationId { get; set; }
    public string UseConfigurationDate { get; set; } = "current"; // "original" or "current" or "yyyy-MM-dd"
    public bool UpdateRecords { get; set; } = false; // If true, save the recalculated values
    public string RecalculatedBy { get; set; } = "system";
}
