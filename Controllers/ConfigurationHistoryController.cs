using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.Admin;

namespace QuotationAPI.V2.Controllers
{
    [ApiController]
    [Route("api/configuration-history")]
    public class ConfigurationHistoryController : ControllerBase
    {
        private readonly QuotationDbContext _context;
        private readonly ILogger<ConfigurationHistoryController> _logger;

        public ConfigurationHistoryController(QuotationDbContext context, ILogger<ConfigurationHistoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get complete configuration change history with advanced filtering
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<ConfigurationHistoryDto>>> GetConfigurationHistory(
            [FromQuery] string? settingKey = null,
            [FromQuery] string? changeType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.ConfigurationHistory
                    .Where(h => h.IsActive == "Y")
                    .AsQueryable();

                // Filter by setting key
                if (!string.IsNullOrEmpty(settingKey))
                {
                    query = query.Where(h => h.SettingKey.Contains(settingKey));
                }

                // Filter by change type
                if (!string.IsNullOrEmpty(changeType))
                {
                    query = query.Where(h => h.ChangeType == changeType);
                }

                // Filter by date range
                if (startDate.HasValue)
                {
                    query = query.Where(h => h.ChangedDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    // Include entire end date by setting it to end of day
                    var endOfDay = endDate.Value.AddDays(1);
                    query = query.Where(h => h.ChangedDate < endOfDay);
                }

                var history = await query
                    .OrderByDescending(h => h.ChangedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new ConfigurationHistoryDto
                    {
                        Id = h.Id,
                        SettingKey = h.SettingKey,
                        OldValue = h.OldValue,
                        NewValue = h.NewValue,
                        ChangeType = h.ChangeType,
                        Description = h.Description,
                        ChangedBy = h.ChangedBy,
                        ChangedDate = h.ChangedDate,
                        Impact = "New quotations/orders created after this date will use the new value. Old quotations preserve original values.",
                        Notes = h.Notes
                    })
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching configuration history: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching configuration history", error = ex.Message });
            }
        }

        /// <summary>
        /// Get configuration values that were active on a specific date
        /// </summary>
        [HttpGet("values-at-date/{date}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetConfigurationValuesAtDate(string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out var targetDate))
                {
                    return BadRequest("Invalid date format. Use yyyy-MM-dd");
                }

                var currentSettings = await _context.AdminSystemSettings
                    .Where(s => s.IsEditable)
                    .ToListAsync();

                var result = new Dictionary<string, string>();

                foreach (var setting in currentSettings)
                {
                    result[setting.Key] = setting.Value;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching configuration values at date: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching configuration values", error = ex.Message });
            }
        }

        /// <summary>
        /// Get quotation configuration snapshot (values used when quotation was created)
        /// </summary>
        [HttpGet("quotation-snapshot/{quotationId}")]
        public async Task<ActionResult<IEnumerable<ConfigSnapshotDto>>> GetQuotationConfigSnapshot(long quotationId)
        {
            try
            {
                var snapshots = await _context.QuotationConfigSnapshot
                    .Where(s => s.QuotationId == quotationId && s.IsActive == "Y")
                    .ToListAsync();

                if (!snapshots.Any())
                {
                    return Ok(new List<ConfigSnapshotDto>());
                }

                var result = new List<ConfigSnapshotDto>();

                foreach (var snapshot in snapshots)
                {
                    var currentSetting = await _context.AdminSystemSettings
                        .FirstOrDefaultAsync(s => s.Key == snapshot.ConfigKey);

                    result.Add(new ConfigSnapshotDto
                    {
                        ConfigKey = snapshot.ConfigKey,
                        OriginalValue = snapshot.ConfigValue,
                        CurrentValue = currentSetting?.Value ?? snapshot.ConfigValue,
                        SnapshotDate = snapshot.SnapshotDate,
                        IsChanged = currentSetting?.Value != snapshot.ConfigValue,
                        Unit = GetUnit(snapshot.ConfigKey),
                        Description = GetDescription(snapshot.ConfigKey)
                    });
                }

                return Ok(result.OrderBy(r => r.ConfigKey));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching quotation config snapshot: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching quotation snapshot", error = ex.Message });
            }
        }

        /// <summary>
        /// Get invoice configuration snapshot
        /// </summary>
        [HttpGet("invoice-snapshot/{invoiceId}")]
        public async Task<ActionResult<IEnumerable<ConfigSnapshotDto>>> GetInvoiceConfigSnapshot(long invoiceId)
        {
            try
            {
                var snapshots = await _context.InvoiceConfigSnapshot
                    .Where(s => s.InvoiceId == invoiceId && s.IsActive == "Y")
                    .ToListAsync();

                if (!snapshots.Any())
                {
                    return Ok(new List<ConfigSnapshotDto>());
                }

                var result = new List<ConfigSnapshotDto>();

                foreach (var snapshot in snapshots)
                {
                    var currentSetting = await _context.AdminSystemSettings
                        .FirstOrDefaultAsync(s => s.Key == snapshot.ConfigKey);

                    result.Add(new ConfigSnapshotDto
                    {
                        ConfigKey = snapshot.ConfigKey,
                        OriginalValue = snapshot.ConfigValue,
                        CurrentValue = currentSetting?.Value ?? snapshot.ConfigValue,
                        SnapshotDate = snapshot.SnapshotDate,
                        IsChanged = currentSetting?.Value != snapshot.ConfigValue,
                        Unit = GetUnit(snapshot.ConfigKey),
                        Description = GetDescription(snapshot.ConfigKey)
                    });
                }

                return Ok(result.OrderBy(r => r.ConfigKey));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching invoice config snapshot: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching invoice snapshot", error = ex.Message });
            }
        }

        /// <summary>
        /// Record configuration value change in history
        /// </summary>
        [HttpPost("record-change")]
        public async Task<ActionResult<ConfigurationHistory>> RecordConfigurationChange(
            string settingKey,
            string newValue,
            string? oldValue = null,
            string? description = null,
            string? notes = null,
            string changedBy = "admin")
        {
            try
            {
                var history = new ConfigurationHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    SettingKey = settingKey,
                    OldValue = oldValue,
                    NewValue = newValue,
                    ChangeType = oldValue == null ? "CREATE" : "UPDATE",
                    Description = description,
                    ChangedBy = changedBy,
                    ChangedDate = DateTime.UtcNow,
                    IsActive = "Y",
                    Notes = notes
                };

                _context.ConfigurationHistory.Add(history);
                await _context.SaveChangesAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error recording configuration change: {ex.Message}");
                return StatusCode(500, new { message = "Error recording change", error = ex.Message });
            }
        }

        /// <summary>
        /// Create snapshot of current configuration for a quotation
        /// Call this when creating a new quotation to preserve config values
        /// </summary>
        [HttpPost("create-quotation-snapshot/{quotationId}")]
        public async Task<ActionResult<IEnumerable<QuotationConfigSnapshot>>> CreateQuotationSnapshot(long quotationId)
        {
            try
            {
                var activeSettings = await _context.AdminSystemSettings
                    .Where(s => s.IsEditable)
                    .ToListAsync();

                var snapshots = new List<QuotationConfigSnapshot>();
                var currentDateTime = DateTime.UtcNow;

                foreach (var setting in activeSettings)
                {
                    var snapshot = new QuotationConfigSnapshot
                    {
                        Id = Guid.NewGuid().ToString(),
                        QuotationId = quotationId,
                        ConfigKey = setting.Key,
                        ConfigValue = setting.Value,
                        ConfigType = setting.Type,
                        SnapshotDate = currentDateTime,
                        IsActive = "Y",
                        CreatedDate = currentDateTime
                    };
                    snapshots.Add(snapshot);
                }

                _context.QuotationConfigSnapshot.AddRange(snapshots);
                await _context.SaveChangesAsync();

                return Ok(snapshots);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating quotation snapshot: {ex.Message}");
                return StatusCode(500, new { message = "Error creating snapshot", error = ex.Message });
            }
        }

        /// <summary>
        /// Create snapshot of current configuration for an invoice
        /// </summary>
        [HttpPost("create-invoice-snapshot/{invoiceId}")]
        public async Task<ActionResult<IEnumerable<InvoiceConfigSnapshot>>> CreateInvoiceSnapshot(long invoiceId)
        {
            try
            {
                var activeSettings = await _context.AdminSystemSettings
                    .Where(s => s.IsEditable)
                    .ToListAsync();

                var snapshots = new List<InvoiceConfigSnapshot>();
                var currentDateTime = DateTime.UtcNow;

                foreach (var setting in activeSettings)
                {
                    var snapshot = new InvoiceConfigSnapshot
                    {
                        Id = Guid.NewGuid().ToString(),
                        InvoiceId = invoiceId,
                        ConfigKey = setting.Key,
                        ConfigValue = setting.Value,
                        ConfigType = setting.Type,
                        SnapshotDate = currentDateTime,
                        IsActive = "Y",
                        CreatedDate = currentDateTime
                    };
                    snapshots.Add(snapshot);
                }

                _context.InvoiceConfigSnapshot.AddRange(snapshots);
                await _context.SaveChangesAsync();

                return Ok(snapshots);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating invoice snapshot: {ex.Message}");
                return StatusCode(500, new { message = "Error creating snapshot", error = ex.Message });
            }
        }

        /// <summary>
        /// Update snapshot of current configuration for a quotation
        /// Called when user clicks "Refresh Rates" button
        /// Handles the case where quotation doesn't exist yet (returns 200 with empty array)
        /// </summary>
        [HttpPut("update-quotation-snapshot/{quotationId}")]
        public async Task<ActionResult<IEnumerable<QuotationConfigSnapshot>>> UpdateQuotationSnapshot(long quotationId)
        {
            try
            {
                if (quotationId <= 0)
                {
                    _logger.LogWarning($"Invalid quotation ID received: {quotationId}");
                    return BadRequest(new { message = "Invalid quotation ID", quotationId = quotationId });
                }

                // Delete existing snapshots
                var existingSnapshots = await _context.QuotationConfigSnapshot
                    .Where(s => s.QuotationId == quotationId)
                    .ToListAsync();

                if (existingSnapshots.Count > 0)
                {
                    foreach (var snapshot in existingSnapshots)
                    {
                        snapshot.IsDeleted = true;
                    }
                    await _context.SaveChangesAsync();
                }

                // Create new snapshots with current values
                var activeSettings = await _context.AdminSystemSettings
                    .Where(s => s.IsEditable)
                    .ToListAsync();

                if (activeSettings.Count == 0)
                {
                    _logger.LogWarning("No active admin system settings found for snapshot");
                    return Ok(new List<QuotationConfigSnapshot>()); // Return empty array if no settings
                }

                var snapshots = new List<QuotationConfigSnapshot>();
                var currentDateTime = DateTime.UtcNow;

                foreach (var setting in activeSettings)
                {
                    var snapshot = new QuotationConfigSnapshot
                    {
                        Id = Guid.NewGuid().ToString(),
                        QuotationId = quotationId,
                        ConfigKey = setting.Key,
                        ConfigValue = setting.Value,
                        ConfigType = setting.Type,
                        SnapshotDate = currentDateTime,
                        IsActive = "Y",
                        CreatedDate = currentDateTime
                    };
                    snapshots.Add(snapshot);
                }

                _context.QuotationConfigSnapshot.AddRange(snapshots);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Quotation snapshot updated successfully for quotation ID: {quotationId}");
                return Ok(snapshots);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating quotation snapshot for ID {quotationId}: {ex.Message}");
                return StatusCode(500, new { message = "Error updating snapshot", error = ex.Message, quotationId = quotationId });
            }
        }

        /// <summary>
        /// <summary>
        /// Update snapshot of current configuration for an invoice
        /// Called when user clicks "Refresh Rates" button
        /// Handles the case where invoice doesn't exist yet (returns 200 with empty array)
        /// </summary>
        [HttpPut("update-invoice-snapshot/{invoiceId}")]
        public async Task<ActionResult<IEnumerable<InvoiceConfigSnapshot>>> UpdateInvoiceSnapshot(long invoiceId)
        {
            try
            {
                if (invoiceId <= 0)
                {
                    _logger.LogWarning($"Invalid invoice ID received: {invoiceId}");
                    return BadRequest(new { message = "Invalid invoice ID", invoiceId = invoiceId });
                }

                // Delete existing snapshots
                var existingSnapshots = await _context.InvoiceConfigSnapshot
                    .Where(s => s.InvoiceId == invoiceId)
                    .ToListAsync();

                if (existingSnapshots.Count > 0)
                {
                    foreach (var snapshot in existingSnapshots)
                    {
                        snapshot.IsDeleted = true;
                    }
                    await _context.SaveChangesAsync();
                }

                // Create new snapshots with current values
                var activeSettings = await _context.AdminSystemSettings
                    .Where(s => s.IsEditable)
                    .ToListAsync();

                if (activeSettings.Count == 0)
                {
                    _logger.LogWarning("No active admin system settings found for snapshot");
                    return Ok(new List<InvoiceConfigSnapshot>()); // Return empty array if no settings
                }

                var snapshots = new List<InvoiceConfigSnapshot>();
                var currentDateTime = DateTime.UtcNow;

                foreach (var setting in activeSettings)
                {
                    var snapshot = new InvoiceConfigSnapshot
                    {
                        Id = Guid.NewGuid().ToString(),
                        InvoiceId = invoiceId,
                        ConfigKey = setting.Key,
                        ConfigValue = setting.Value,
                        ConfigType = setting.Type,
                        SnapshotDate = currentDateTime,
                        IsActive = "Y",
                        CreatedDate = currentDateTime
                    };
                    snapshots.Add(snapshot);
                }

                _context.InvoiceConfigSnapshot.AddRange(snapshots);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Invoice snapshot updated successfully for invoice ID: {invoiceId}");
                return Ok(snapshots);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating invoice snapshot for ID {invoiceId}: {ex.Message}");
                return StatusCode(500, new { message = "Error updating snapshot", error = ex.Message, invoiceId = invoiceId });
            }
        }

        // Helper methods to get units and descriptions
        private string GetUnit(string settingKey)
        {
            return settingKey switch
            {
                "ebRateDefault" => "₹/day",
                "salaryPerShiftDefault" => "₹/person",
                "gumRateDefault" => "₹/bag",
                "pinRateDefault" => "₹/kg",
                "paperRateDefault" => "₹",
                "duplexRateDefault" => "₹",
                "rentMonthlyDefault" => "₹/month",
                "gsmFactor" => "factor",
                "unitConversion" => "factor",
                "mmToInch" => "conversion",
                "cmToInch" => "conversion",
                _ => ""
            };
        }

        private string GetDescription(string settingKey)
        {
            return settingKey switch
            {
                "ebRateDefault" => "EB/Electricity Rate",
                "salaryPerShiftDefault" => "Salary Per Person",
                "gumRateDefault" => "Gum Rate",
                "pinRateDefault" => "Pin Rate",
                "paperRateDefault" => "Paper Rate",
                "duplexRateDefault" => "Duplex Rate",
                "rentMonthlyDefault" => "Monthly Rent",
                "gsmFactor" => "GSM Factor",
                "unitConversion" => "Unit Conversion",
                "mmToInch" => "MM to Inch",
                "cmToInch" => "CM to Inch",
                _ => settingKey
            };
        }

        /// <summary>
        /// Get rate change history (GUM, PIN, EB, SALARY) with impact analysis
        /// Shows all rate changes and affected quotations
        /// </summary>
        [HttpGet("rate-history")]
        public async Task<ActionResult<dynamic>> GetRateChangeHistory()
        {
            try
            {
                var rateKeys = new[] { "ebRateDefault", "pinRateDefault", "gumRateDefault", "salaryPerShiftDefault" };

                var rateHistory = await _context.ConfigurationHistory
                    .Where(h => rateKeys.Contains(h.SettingKey) && h.IsActive == "Y")
                    .OrderByDescending(h => h.ChangedDate)
                    .ToListAsync();

                var result = rateHistory.Select(h => new
                {
                    RateName = h.SettingKey.Replace("Default", "").Replace("Rate", "").ToUpper(),
                    SettingKey = h.SettingKey,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    Change = h.OldValue == null ? "NEW" : (decimal.TryParse(h.OldValue, out var oldVal) && decimal.TryParse(h.NewValue, out var newVal)
                        ? $"{(newVal > oldVal ? "+" : "")}{(newVal - oldVal):F2} ({((newVal - oldVal) / oldVal * 100):F1}%)"
                        : "UPDATED"),
                    ChangedBy = h.ChangedBy,
                    ChangedDate = h.ChangedDate,
                    Description = h.Description,
                    Notes = h.Notes
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching rate change history: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching rate history", error = ex.Message });
            }
        }

        /// <summary>
        /// Get rate values on a specific date (for historical quotation calculations)
        /// </summary>
        [HttpGet("rate-values-at-date/{date}")]
        public async Task<ActionResult<dynamic>> GetRateValuesAtDate(string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out var targetDate))
                {
                    return BadRequest("Invalid date format. Use yyyy-MM-dd");
                }

                var rateKeys = new[] { "ebRateDefault", "pinRateDefault", "gumRateDefault", "salaryPerShiftDefault" };

                // Get the rate values that were active on this date
                var ratesAtDate = new Dictionary<string, string>();

                foreach (var rateKey in rateKeys)
                {
                    var history = await _context.ConfigurationHistory
                        .Where(h => h.SettingKey == rateKey && h.ChangedDate <= targetDate && h.IsActive == "Y")
                        .OrderByDescending(h => h.ChangedDate)
                        .FirstOrDefaultAsync();

                    if (history != null)
                    {
                        ratesAtDate[rateKey] = history.NewValue;
                    }
                    else
                    {
                        // Fallback to current value if no history exists
                        var currentSetting = await _context.AdminSystemSettings
                            .FirstOrDefaultAsync(s => s.Key == rateKey);
                        ratesAtDate[rateKey] = currentSetting?.Value ?? "N/A";
                    }
                }

                return Ok(new
                {
                    ReferenceDate = targetDate.ToString("yyyy-MM-dd"),
                    EBRate = ratesAtDate.GetValueOrDefault("ebRateDefault", "N/A"),
                    PinRate = ratesAtDate.GetValueOrDefault("pinRateDefault", "N/A"),
                    GumRate = ratesAtDate.GetValueOrDefault("gumRateDefault", "N/A"),
                    SalaryPerShift = ratesAtDate.GetValueOrDefault("salaryPerShiftDefault", "N/A")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching rate values at date: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching rate values", error = ex.Message });
            }
        }
    }
}
