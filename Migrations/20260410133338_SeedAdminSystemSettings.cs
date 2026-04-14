using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Calculation Constants
            migrationBuilder.InsertData(
                table: "AdminSystemSettings",
                columns: new[] { "Id", "Key", "Value", "Description", "Category", "Type", "IsEditable", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    // Calculation Constants
                    { "calc-001", "gsmFactor", "1550", "GSM Factor for paper calculation", "Calculations", "decimal", true, DateTime.UtcNow, "system" },
                    { "calc-002", "unitConversion", "1000", "Unit conversion factor", "Calculations", "decimal", true, DateTime.UtcNow, "system" },
                    { "calc-003", "gsmAdjustment", "10", "GSM Adjustment factor", "Calculations", "decimal", true, DateTime.UtcNow, "system" },
                    { "calc-004", "flutePercentageBase", "100", "Flute percentage base", "Calculations", "decimal", true, DateTime.UtcNow, "system" },
                    { "calc-005", "flapSize", "2", "Flap size", "Calculations", "decimal", true, DateTime.UtcNow, "system" },
                    { "calc-006", "lockSize", "1.25", "Lock size", "Calculations", "decimal", true, DateTime.UtcNow, "system" },
                    { "calc-007", "mmToInch", "0.0393701", "Millimeter to inch conversion", "Calculations", "decimal", true, DateTime.UtcNow, "system" },
                    { "calc-008", "cmToInch", "0.393701", "Centimeter to inch conversion", "Calculations", "decimal", true, DateTime.UtcNow, "system" },
                    { "calc-009", "decimalPrecision", "3", "Decimal precision for calculations", "Calculations", "integer", true, DateTime.UtcNow, "system" },

                    // Rate Defaults
                    { "rate-001", "paperRateDefault", "58", "Default paper rate", "RateDefaults", "decimal", true, DateTime.UtcNow, "system" },
                    { "rate-002", "duplexRateDefault", "72", "Default duplex rate", "RateDefaults", "decimal", true, DateTime.UtcNow, "system" },
                    { "rate-003", "ebRateDefault", "3200", "Default EB rate", "RateDefaults", "decimal", true, DateTime.UtcNow, "system" },
                    { "rate-004", "pinRateDefault", "940", "Default pin rate", "RateDefaults", "decimal", true, DateTime.UtcNow, "system" },
                    { "rate-005", "gumRateDefault", "610", "Default gum rate", "RateDefaults", "decimal", true, DateTime.UtcNow, "system" },
                    { "rate-006", "salaryPerShiftDefault", "850", "Default salary per shift", "RateDefaults", "decimal", true, DateTime.UtcNow, "system" },
                    { "rate-007", "rentMonthlyDefault", "12000", "Default monthly rent", "RateDefaults", "decimal", true, DateTime.UtcNow, "system" },

                    // Joint Multipliers
                    { "joint-001", "joint1Multiplier", "2", "Joint 1 multiplier", "JointMultipliers", "decimal", true, DateTime.UtcNow, "system" },
                    { "joint-002", "joint2Multiplier", "1", "Joint 2 multiplier", "JointMultipliers", "decimal", true, DateTime.UtcNow, "system" },
                    { "joint-003", "joint4Multiplier", "0.5", "Joint 4 multiplier", "JointMultipliers", "decimal", true, DateTime.UtcNow, "system" },

                    // Model Constants
                    { "model-001", "modelBaseAddition", "1", "Model base addition", "ModelConstants", "decimal", true, DateTime.UtcNow, "system" },
                    { "model-002", "model5HeightIncrement", "3", "Model 5 height increment", "ModelConstants", "decimal", true, DateTime.UtcNow, "system" },
                    { "model-003", "model9WidthFactor", "0.5", "Model 9 width factor", "ModelConstants", "decimal", true, DateTime.UtcNow, "system" },

                    // Quotation Settings
                    { "quot-001", "quotationPrefix", "QTN", "Quotation number prefix", "QuotationSettings", "string", true, DateTime.UtcNow, "system" },
                    { "quot-002", "quotationYear", "2026", "Quotation year", "QuotationSettings", "integer", true, DateTime.UtcNow, "system" },
                    { "quot-003", "recordIdPadding", "3", "Record ID padding length", "QuotationSettings", "integer", true, DateTime.UtcNow, "system" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AdminSystemSettings",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    "calc-001", "calc-002", "calc-003", "calc-004", "calc-005", "calc-006", "calc-007", "calc-008", "calc-009",
                    "rate-001", "rate-002", "rate-003", "rate-004", "rate-005", "rate-006", "rate-007",
                    "joint-001", "joint-002", "joint-003",
                    "model-001", "model-002", "model-003",
                    "quot-001", "quot-002", "quot-003"
                });
        }
    }
}
