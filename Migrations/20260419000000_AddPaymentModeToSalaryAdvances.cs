using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentModeToSalaryAdvances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMode",
                table: "SalaryAdvances",
                type: "text",
                nullable: false,
                defaultValue: "cash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "SalaryAdvances");
        }
    }
}
