using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesLovSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Seed parent category: Roll Sales Rates ─────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM LovItems WHERE Name = 'Roll Sales Rates' AND Parentvalue IS NULL)
                BEGIN
                    INSERT INTO LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
                    VALUES (NULL, NULL, 'Roll Sales Rates', NULL, 'Configuration rates for roll sales gum and EB calculation', 'CATEGORY', 1, 'Y', 'system', 'system', GETUTCDATE(), GETUTCDATE())
                END
            ");

            // ── Seed child: GumKgPerTon ───────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM LovItems WHERE Name = 'RollSalesGumKgPerTon')
                BEGIN
                    DECLARE @parentId INT = (SELECT TOP 1 Id FROM LovItems WHERE Name = 'Roll Sales Rates' AND Parentvalue IS NULL)
                    INSERT INTO LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
                    VALUES ('Roll Sales Rates', @parentId, 'RollSalesGumKgPerTon', 23, 'Kg of gum used per 1 ton of paper rolled (default 23)', 'RATE', 1, 'Y', 'system', 'system', GETUTCDATE(), GETUTCDATE())
                END
            ");

            // ── Seed child: EbUnitsPerTon ─────────────────────────────────
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM LovItems WHERE Name = 'RollSalesEbUnitsPerTon')
                BEGIN
                    DECLARE @parentId2 INT = (SELECT TOP 1 Id FROM LovItems WHERE Name = 'Roll Sales Rates' AND Parentvalue IS NULL)
                    INSERT INTO LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
                    VALUES ('Roll Sales Rates', @parentId2, 'RollSalesEbUnitsPerTon', 10, 'Electricity units consumed per 1 ton of paper rolled (default 10)', 'RATE', 2, 'Y', 'system', 'system', GETUTCDATE(), GETUTCDATE())
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM LovItems WHERE Name IN ('RollSalesGumKgPerTon', 'RollSalesEbUnitsPerTon');
                DELETE FROM LovItems WHERE Name = 'Roll Sales Rates' AND Parentvalue IS NULL;
            ");
        }
    }
}
