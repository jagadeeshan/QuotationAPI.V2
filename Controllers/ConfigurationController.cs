#nullable disable
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models;
using QuotationAPI.V2.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace QuotationAPI.V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly QuotationDbContext _context;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(QuotationDbContext context, ILogger<ConfigurationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all system settings (calculation constants, rate defaults, etc.)
        /// </summary>
        [HttpGet("system-settings")]
        public async Task<ActionResult<IEnumerable<AdminSystemSetting>>> GetSystemSettings()
        {
            try
            {
                var settings = await _context.AdminSystemSettings
                    .Where(s => s.IsEditable)
                    .ToListAsync();

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching system settings: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching system settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Get system setting by key
        /// </summary>
        [HttpGet("system-settings/{key}")]
        public async Task<ActionResult<AdminSystemSetting>> GetSystemSettingByKey(string key)
        {
            try
            {
                var setting = await _context.AdminSystemSettings
                    .FirstOrDefaultAsync(s => s.Key == key && s.IsEditable);

                if (setting == null)
                {
                    return NotFound(new { message = $"Setting '{key}' not found" });
                }

                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching system setting {key}: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching system setting", error = ex.Message });
            }
        }

        /// <summary>
        /// Get calculation constants (GSM factor, unit conversion, adjustments, etc.)
        /// </summary>
        [HttpGet("calculation-constants")]
        public async Task<ActionResult<dynamic>> GetCalculationConstants()
        {
            try
            {
                var constants = await _context.AdminSystemSettings
                    .Where(s => s.Key.StartsWith("SET_") && s.IsEditable)
                    .ToListAsync();

                var dto = new
                {
                    GsmFactor = GetSettingValue(constants, "gsmFactor", "1550"),
                    UnitConversion = GetSettingValue(constants, "unitConversion", "1000"),
                    GsmAdjustment = GetSettingValue(constants, "gsmAdjustment", "10"),
                    FlutePercentageBase = GetSettingValue(constants, "flutePercentageBase", "100"),
                    FlapSize = GetSettingValue(constants, "flapSize", "2"),
                    LockSize = GetSettingValue(constants, "lockSize", "1.25"),
                    MmToInch = GetSettingValue(constants, "mmToInch", "0.0393701"),
                    CmToInch = GetSettingValue(constants, "cmToInch", "0.393701"),
                    DecimalPrecision = GetSettingIntValue(constants, "decimalPrecision", 3)
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching calculation constants: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching calculation constants", error = ex.Message });
            }
        }

        /// <summary>
        /// Get rate defaults (paper, duplex, EB, pin, gum, salary, rent)
        /// </summary>
        [HttpGet("rate-defaults")]
        public async Task<ActionResult<dynamic>> GetRateDefaults()
        {
            try
            {
                var settings = await _context.AdminSystemSettings
                    .Where(s => s.Key.Contains("Default") && s.IsEditable)
                    .ToListAsync();

                var dto = new
                {
                    PaperRate = GetSettingValue(settings, "paperRateDefault", "58"),
                    DuplexRate = GetSettingValue(settings, "duplexRateDefault", "72"),
                    EbRate = GetSettingValue(settings, "ebRateDefault", "3200"),
                    PinRate = GetSettingValue(settings, "pinRateDefault", "940"),
                    GumRate = GetSettingValue(settings, "gumRateDefault", "610"),
                    SalaryPerShift = GetSettingValue(settings, "salaryPerShiftDefault", "850"),
                    RentMonthly = GetSettingValue(settings, "rentMonthlyDefault", "12000")
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching rate defaults: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching rate defaults", error = ex.Message });
            }
        }

        /// <summary>
        /// Get joint multipliers for cutting size calculation
        /// </summary>
        [HttpGet("joint-multipliers")]
        public async Task<ActionResult<dynamic>> GetJointMultipliers()
        {
            try
            {
                var settings = await _context.AdminSystemSettings
                    .Where(s => s.Key.Contains("Multiplier") && s.IsEditable)
                    .ToListAsync();

                var dto = new
                {
                    Joint1 = GetSettingValue(settings, "joint1Multiplier", "2"),
                    Joint2 = GetSettingValue(settings, "joint2Multiplier", "1"),
                    Joint4 = GetSettingValue(settings, "joint4Multiplier", "0.5")
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching joint multipliers: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching joint multipliers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get model-specific constants
        /// </summary>
        [HttpGet("model-constants")]
        public async Task<ActionResult<dynamic>> GetModelConstants()
        {
            try
            {
                var settings = await _context.AdminSystemSettings
                    .Where(s => s.Key.StartsWith("SET_MODEL_") && s.IsEditable)
                    .ToListAsync();

                var dto = new
                {
                    BaseAddition = GetSettingValue(settings, "modelBaseAddition", "1"),
                    Model5HeightIncrement = GetSettingValue(settings, "model5HeightIncrement", "3"),
                    Model9WidthFactor = GetSettingValue(settings, "model9WidthFactor", "0.5")
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching model constants: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching model constants", error = ex.Message });
            }
        }

        /// <summary>
        /// Get quotation numbering settings
        /// </summary>
        [HttpGet("quotation-settings")]
        public async Task<ActionResult<dynamic>> GetQuotationSettings()
        {
            try
            {
                var settings = await _context.AdminSystemSettings
                    .Where(s => s.Key.StartsWith("SET_QUOTATION_") && s.IsEditable)
                    .ToListAsync();

                var dto = new
                {
                    Prefix = GetSettingValue(settings, "quotationPrefix", "QTN"),
                    Year = GetSettingIntValue(settings, "quotationYear", 2026),
                    RecordIdPadding = GetSettingIntValue(settings, "recordIdPadding", 3)
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching quotation settings: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching quotation settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Update system setting (admin only)
        /// </summary>
        [HttpPut("system-settings/{key}")]
        public async Task<ActionResult<AdminSystemSetting>> UpdateSystemSetting(string key, [FromBody] UpdateSettingRequest request)
        {
            try
            {
                var setting = await _context.AdminSystemSettings
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (setting == null)
                {
                    return NotFound(new { message = $"Setting '{key}' not found" });
                }

                if (!setting.IsEditable)
                {
                    return BadRequest(new { message = $"Setting '{key}' is not editable" });
                }

                // Store old value for audit trail
                var oldValue = setting.Value;

                setting.Value = request.SettingValue;
                setting.UpdatedBy = request.UpdatedBy ?? "API";
                setting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the change to ConfigurationHistory
                await LogConfigurationChange(key, oldValue, request.SettingValue, request.UpdatedBy ?? "API");

                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating system setting {key}: {ex.Message}");
                return StatusCode(500, new { message = "Error updating system setting", error = ex.Message });
            }
        }

        /// <summary>
        /// Helper method to log configuration changes to ConfigurationHistory
        /// </summary>
        private async Task LogConfigurationChange(string settingKey, string oldValue, string newValue, string changedBy)
        {
            try
            {
                var historyEntry = new ConfigurationHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    SettingKey = settingKey,
                    OldValue = oldValue,
                    NewValue = newValue,
                    ChangeType = string.IsNullOrEmpty(oldValue) ? "CREATE" : "UPDATE",
                    Description = $"System setting updated via Configuration API",
                    ChangedBy = changedBy,
                    ChangedDate = DateTime.UtcNow,
                    IsActive = "Y",
                    Notes = $"Key: {settingKey} | Previous: {oldValue} | Current: {newValue}"
                };

                _context.ConfigurationHistory.Add(historyEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Configuration change logged: {settingKey} changed from '{oldValue}' to '{newValue}' by {changedBy}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to log configuration change: {ex.Message}");
                // Don't throw - log failures shouldn't block the update
            }
        }

        // Helper methods
        private string GetSettingValue(List<AdminSystemSetting> settings, string key, string defaultValue)
        {
            var setting = settings.FirstOrDefault(s => s.Value.Contains(key) || s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        private int GetSettingIntValue(List<AdminSystemSetting> settings, string key, int defaultValue)
        {
            var setting = settings.FirstOrDefault(s => s.Value.Contains(key) || s.Key == key);
            if (setting != null && int.TryParse(setting.Value, out int value))
            {
                return value;
            }
            return defaultValue;
        }

        public class UpdateSettingRequest
        {
            public string SettingValue { get; set; }
            public string UpdatedBy { get; set; }
        }
    }
}
