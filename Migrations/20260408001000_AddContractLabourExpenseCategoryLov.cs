using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    public partial class AddContractLabourExpenseCategoryLov : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[LovItems]', N'U') IS NOT NULL
BEGIN
    DECLARE @ExpenseCategoryId INT;
    SELECT TOP(1) @ExpenseCategoryId = [Id]
    FROM [dbo].[LovItems]
    WHERE [Parentvalue] IS NULL AND [Name] = N'Expense Category'
    ORDER BY [Id];

    IF @ExpenseCategoryId IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM [dbo].[LovItems]
            WHERE [Parentvalue] = @ExpenseCategoryId
              AND LOWER(LTRIM(RTRIM([Name]))) = N'contract labour'
       )
    BEGIN
        DECLARE @NextValue INT;
        DECLARE @NextDisplayOrder INT;

        SELECT @NextValue = ISNULL(MAX([Value]), 0) + 1,
               @NextDisplayOrder = ISNULL(MAX([Displayorder]), 0) + 1
        FROM [dbo].[LovItems]
        WHERE [Parentvalue] = @ExpenseCategoryId;

        INSERT INTO [dbo].[LovItems]
            ([Parentname], [Parentvalue], [Name], [Value], [Description], [Itemtype], [Displayorder], [Isactive], [Createdby], [Updatedby], [Createddt], [Updateddt])
        VALUES
            (N'Expense Category', @ExpenseCategoryId, N'Contract Labour', @NextValue, N'Contract labour wages', N'CATEGORY_VALUE', @NextDisplayOrder, N'Y', N'system', N'system', CONVERT(varchar(33), SYSUTCDATETIME(), 127), CONVERT(varchar(33), SYSUTCDATETIME(), 127));
    END
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[LovItems]', N'U') IS NOT NULL
BEGIN
    DECLARE @ExpenseCategoryId INT;
    SELECT TOP(1) @ExpenseCategoryId = [Id]
    FROM [dbo].[LovItems]
    WHERE [Parentvalue] IS NULL AND [Name] = N'Expense Category'
    ORDER BY [Id];

    IF @ExpenseCategoryId IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[LovItems]
        WHERE [Parentvalue] = @ExpenseCategoryId
          AND LOWER(LTRIM(RTRIM([Name]))) = N'contract labour';
    END
END
");
        }
    }
}
