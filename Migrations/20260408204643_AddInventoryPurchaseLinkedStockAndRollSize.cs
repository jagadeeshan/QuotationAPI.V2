using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryPurchaseLinkedStockAndRollSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RollSize",
                table: "RollSales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "ReelStocks",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DealerName",
                table: "ReelStocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseInvoiceNumber",
                table: "ReelStocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseVoucherNumber",
                table: "ReelStocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedDate",
                table: "ReelStocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "ReelStocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RollSize",
                table: "ReelStocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockType",
                table: "ReelStocks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RollSize",
                table: "RollSales");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "ReelStocks");

            migrationBuilder.DropColumn(
                name: "DealerName",
                table: "ReelStocks");

            migrationBuilder.DropColumn(
                name: "PurchaseInvoiceNumber",
                table: "ReelStocks");

            migrationBuilder.DropColumn(
                name: "PurchaseVoucherNumber",
                table: "ReelStocks");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "ReelStocks");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "ReelStocks");

            migrationBuilder.DropColumn(
                name: "RollSize",
                table: "ReelStocks");

            migrationBuilder.DropColumn(
                name: "StockType",
                table: "ReelStocks");
        }
    }
}
