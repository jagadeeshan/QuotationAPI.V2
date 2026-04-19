using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentDate",
                table: "TaxPaymentRows",
                newName: "Date");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMode",
                table: "SalaryAdvances",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "cash");

            migrationBuilder.AddColumn<string>(
                name: "ExpenseId",
                table: "SalaryAdvances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "ExpenseRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceModule",
                table: "ExpenseRecords",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpenseId",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "ExpenseRecords");

            migrationBuilder.DropColumn(
                name: "SourceModule",
                table: "ExpenseRecords");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "TaxPaymentRows",
                newName: "PaymentDate");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMode",
                table: "SalaryAdvances",
                type: "text",
                nullable: false,
                defaultValue: "cash",
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
