using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class SeedConfigurationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow;
            
            migrationBuilder.InsertData(
                table: "ConfigurationHistory",
                columns: new[] { "Id", "SettingKey", "OldValue", "NewValue", "ChangeType", "Description", "ChangedBy", "ChangedDate", "IsActive", "Notes" },
                values: new object[,]
                {
                    { "ch-gstrate-001", "GstRate", null, "18", "CREATE", "Initial GST rate setting", "system", now, "Y", "Standard GST rate for quotations" },
                    { "ch-currency-001", "DefaultCurrency", null, "INR", "CREATE", "Default currency setting", "system", now.AddDays(-5), "Y", "All quotations default to INR" },
                    { "ch-gstrate-upd", "GstRate", "18", "18", "UPDATE", "GST rate verification", "admin", now.AddDays(-3), "Y", "Verified GST rate is correct" },
                    { "ch-invprefix-001", "InvoicePrefix", null, "INV-", "CREATE", "Invoice number prefix", "system", now.AddDays(-10), "Y", "All invoices prefixed with INV-" },
                    { "ch-quotvalid-001", "QuotationValidityDays", null, "30", "CREATE", "Quotation validity period", "system", now.AddDays(-7), "Y", "Quotations valid for 30 days" },
                    { "ch-discount-001", "DiscountAllowedPercentage", null, "10", "CREATE", "Maximum discount percentage", "system", now.AddDays(-15), "Y", "Maximum 10% discount allowed on quotations" },
                    { "ch-minorder-001", "MinimumOrderValue", null, "10000", "CREATE", "Minimum order value", "admin", now.AddDays(-12), "Y", "Minimum order value set to 10,000" },
                    { "ch-company-001", "CompanyName", null, "Quotation Management System", "CREATE", "Company name setting", "system", now.AddDays(-20), "Y", "Default company name" },
                    { "ch-taxded-001", "TaxDeductionRate", null, "10", "CREATE", "Tax deduction rate", "system", now.AddDays(-8), "Y", "Standard tax deduction rate" },
                    { "ch-payment-001", "PaymentTermsDays", null, "7", "CREATE", "Payment terms in days", "admin", now.AddDays(-6), "Y", "Payment due within 7 days of invoice" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ConfigurationHistory",
                keyColumn: "Id",
                keyValues: new object[] { "ch-gstrate-001", "ch-currency-001", "ch-gstrate-upd", "ch-invprefix-001", "ch-quotvalid-001", "ch-discount-001", "ch-minorder-001", "ch-company-001", "ch-taxded-001", "ch-payment-001" });
        }
    }
}
