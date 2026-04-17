using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace QuotationAPI.V2.Migrations
{
    public partial class MoveDeliveryDateToInvoice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove DeliveryDate from Quotations
            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "Quotations"
            );

            // Add DeliveryDate to InvoiceCalcRecords
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "InvoiceCalcRecords",
                type: "timestamp with time zone",
                nullable: true
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add DeliveryDate back to Quotations
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "Quotations",
                type: "timestamp with time zone",
                nullable: true
            );

            // Remove DeliveryDate from InvoiceCalcRecords
            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "InvoiceCalcRecords"
            );
        }
    }
}
