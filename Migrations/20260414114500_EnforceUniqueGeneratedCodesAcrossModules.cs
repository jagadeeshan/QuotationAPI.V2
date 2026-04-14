using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueGeneratedCodesAcrossModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[CustomerMasters]', N'U') IS NOT NULL
BEGIN
    ;WITH blanks AS
    (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS RN
        FROM [dbo].[CustomerMasters]
        WHERE [Code] IS NULL OR LTRIM(RTRIM([Code])) = ''
    ),
    mx AS
    (
        SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING([Code], 5, 20))), 0) AS MaxSerial
        FROM [dbo].[CustomerMasters]
        WHERE [Code] LIKE 'CUST%'
    )
    UPDATE cm
    SET [Code] = 'CUST' + RIGHT('000' + CAST(mx.MaxSerial + blanks.RN AS varchar(10)), 3)
    FROM [dbo].[CustomerMasters] cm
    INNER JOIN blanks ON blanks.Id = cm.Id
    CROSS JOIN mx;

    ;WITH dup AS
    (
        SELECT Id, [Code], ROW_NUMBER() OVER (PARTITION BY [Code] ORDER BY Id) AS DuplicateRank
        FROM [dbo].[CustomerMasters]
        WHERE [Code] IS NOT NULL AND LTRIM(RTRIM([Code])) <> ''
    ),
    dupRows AS
    (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS RN
        FROM dup
        WHERE DuplicateRank > 1
    ),
    mx2 AS
    (
        SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING([Code], 5, 20))), 0) AS MaxSerial
        FROM [dbo].[CustomerMasters]
        WHERE [Code] LIKE 'CUST%'
    )
    UPDATE cm
    SET [Code] = 'CUST' + RIGHT('000' + CAST(mx2.MaxSerial + dupRows.RN AS varchar(10)), 3)
    FROM [dbo].[CustomerMasters] cm
    INNER JOIN dupRows ON dupRows.Id = cm.Id
    CROSS JOIN mx2;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ReelStocks]', N'U') IS NOT NULL
BEGIN
    ;WITH blanks AS
    (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS RN
        FROM [dbo].[ReelStocks]
        WHERE [ReelNumber] IS NULL OR LTRIM(RTRIM([ReelNumber])) = ''
    ),
    mx AS
    (
        SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING([ReelNumber], 3, 20))), 0) AS MaxSerial
        FROM [dbo].[ReelStocks]
        WHERE [ReelNumber] LIKE 'RL%'
    )
    UPDATE rs
    SET [ReelNumber] = 'RL' + RIGHT('000000' + CAST(mx.MaxSerial + blanks.RN AS varchar(10)), 6)
    FROM [dbo].[ReelStocks] rs
    INNER JOIN blanks ON blanks.Id = rs.Id
    CROSS JOIN mx;

    ;WITH dup AS
    (
        SELECT Id, [ReelNumber], ROW_NUMBER() OVER (PARTITION BY [ReelNumber] ORDER BY Id) AS DuplicateRank
        FROM [dbo].[ReelStocks]
        WHERE [ReelNumber] IS NOT NULL AND LTRIM(RTRIM([ReelNumber])) <> ''
    ),
    dupRows AS
    (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS RN
        FROM dup
        WHERE DuplicateRank > 1
    ),
    mx2 AS
    (
        SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING([ReelNumber], 3, 20))), 0) AS MaxSerial
        FROM [dbo].[ReelStocks]
        WHERE [ReelNumber] LIKE 'RL%'
    )
    UPDATE rs
    SET [ReelNumber] = 'RL' + RIGHT('000000' + CAST(mx2.MaxSerial + dupRows.RN AS varchar(10)), 6)
    FROM [dbo].[ReelStocks] rs
    INNER JOIN dupRows ON dupRows.Id = rs.Id
    CROSS JOIN mx2;
END
");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "CustomerMasters",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ReelNumber",
                table: "ReelStocks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMasters_Code",
                table: "CustomerMasters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReelStocks_ReelNumber",
                table: "ReelStocks",
                column: "ReelNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerMasters_Code",
                table: "CustomerMasters");

            migrationBuilder.DropIndex(
                name: "IX_ReelStocks_ReelNumber",
                table: "ReelStocks");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "CustomerMasters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "ReelNumber",
                table: "ReelStocks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);
        }
    }
}
