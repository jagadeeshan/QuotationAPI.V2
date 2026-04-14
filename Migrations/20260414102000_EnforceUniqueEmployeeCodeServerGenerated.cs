using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueEmployeeCodeServerGenerated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Employees]', N'U') IS NOT NULL
BEGIN
    ;WITH blanks AS
    (
        SELECT Id,
               ROW_NUMBER() OVER (ORDER BY Id) AS RN
        FROM [dbo].[Employees]
        WHERE [EmployeeCode] IS NULL OR LTRIM(RTRIM([EmployeeCode])) = ''
    ),
    mx AS
    (
        SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING([EmployeeCode], 4, 20))), 0) AS MaxSerial
        FROM [dbo].[Employees]
        WHERE [EmployeeCode] LIKE 'EMP%'
    )
    UPDATE e
    SET [EmployeeCode] = 'EMP' + RIGHT('000000' + CAST(mx.MaxSerial + blanks.RN AS varchar(6)), 6)
    FROM [dbo].[Employees] e
    INNER JOIN blanks ON blanks.Id = e.Id
    CROSS JOIN mx;

    ;WITH dup AS
    (
        SELECT Id,
               [EmployeeCode],
               ROW_NUMBER() OVER (PARTITION BY [EmployeeCode] ORDER BY Id) AS DuplicateRank
        FROM [dbo].[Employees]
        WHERE [EmployeeCode] IS NOT NULL AND LTRIM(RTRIM([EmployeeCode])) <> ''
    ),
    dupRows AS
    (
        SELECT Id,
               ROW_NUMBER() OVER (ORDER BY Id) AS RN
        FROM dup
        WHERE DuplicateRank > 1
    ),
    mx2 AS
    (
        SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING([EmployeeCode], 4, 20))), 0) AS MaxSerial
        FROM [dbo].[Employees]
        WHERE [EmployeeCode] LIKE 'EMP%'
    )
    UPDATE e
    SET [EmployeeCode] = 'EMP' + RIGHT('000000' + CAST(mx2.MaxSerial + dupRows.RN AS varchar(6)), 6)
    FROM [dbo].[Employees] e
    INNER JOIN dupRows ON dupRows.Id = e.Id
    CROSS JOIN mx2;
END
");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeCode",
                table: "Employees",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeCode",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);
        }
    }
}
