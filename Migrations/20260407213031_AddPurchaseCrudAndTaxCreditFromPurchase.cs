using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseCrudAndTaxCreditFromPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "PurchaseSalesRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "PurchaseSalesRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseSalesRows",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxPercent",
                table: "PurchaseSalesRows",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmountPaid",
                table: "PurchaseSalesRows",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE PurchaseSalesRows SET TotalAmountPaid = Amount WHERE TotalAmountPaid = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "PurchaseSalesRows");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "PurchaseSalesRows");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PurchaseSalesRows");

            migrationBuilder.DropColumn(
                name: "TaxPercent",
                table: "PurchaseSalesRows");

            migrationBuilder.DropColumn(
                name: "TotalAmountPaid",
                table: "PurchaseSalesRows");
        }
    }
}
