using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryTypeForWeeklyAndMonthly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SalaryType",
                table: "SalaryMasters",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "monthly");

            migrationBuilder.AddColumn<string>(
                name: "SalaryType",
                table: "MonthlySalaryCalcs",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "monthly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalaryType",
                table: "SalaryMasters");

            migrationBuilder.DropColumn(
                name: "SalaryType",
                table: "MonthlySalaryCalcs");
        }
    }
}
