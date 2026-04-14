using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    public partial class AddZohoBooksSyncTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ZohoCustomerRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [ZohoCustomerRecords] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [OutstandingAmount] decimal(18,4) NOT NULL,
        [LastModifiedTimeUtc] datetime2 NULL,
        [PulledAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoCustomerRecords] PRIMARY KEY ([Id])
    );
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ZohoInvoiceRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [ZohoInvoiceRecords] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerId] nvarchar(max) NULL,
        [CustomerName] nvarchar(max) NULL,
        [InvoiceNumber] nvarchar(max) NULL,
        [InvoiceDate] datetime2 NULL,
        [DueDate] datetime2 NULL,
        [Total] decimal(18,4) NOT NULL,
        [Balance] decimal(18,4) NOT NULL,
        [Status] nvarchar(max) NULL,
        [LastModifiedTimeUtc] datetime2 NULL,
        [PulledAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoInvoiceRecords] PRIMARY KEY ([Id])
    );
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ZohoOutstandingRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [ZohoOutstandingRecords] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerName] nvarchar(max) NULL,
        [OutstandingAmount] decimal(18,4) NOT NULL,
        [PulledAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoOutstandingRecords] PRIMARY KEY ([Id])
    );
END");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ZohoSyncStates]', N'U') IS NULL
BEGIN
    CREATE TABLE [ZohoSyncStates] (
        [Id] int NOT NULL,
        [LastCustomersSyncUtc] datetime2 NULL,
        [LastInvoicesSyncUtc] datetime2 NULL,
        [LastOutstandingSyncUtc] datetime2 NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoSyncStates] PRIMARY KEY ([Id])
    );
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[ZohoCustomerRecords]', N'U') IS NOT NULL DROP TABLE [ZohoCustomerRecords];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[ZohoInvoiceRecords]', N'U') IS NOT NULL DROP TABLE [ZohoInvoiceRecords];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[ZohoOutstandingRecords]', N'U') IS NOT NULL DROP TABLE [ZohoOutstandingRecords];");
            migrationBuilder.Sql("IF OBJECT_ID(N'[ZohoSyncStates]', N'U') IS NOT NULL DROP TABLE [ZohoSyncStates];");
        }
    }
}
