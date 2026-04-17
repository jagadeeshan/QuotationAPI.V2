using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryDateToInvoiceCalcRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "Quotations");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "InvoiceCalcRecords",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "InvoiceCalcRecords");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "Quotations",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
