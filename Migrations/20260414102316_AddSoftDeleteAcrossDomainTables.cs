using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAcrossDomainTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TaxPaymentRows",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SalaryMasters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SalaryAdvances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Quotations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "QuotationLineItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "QuotationConfigSnapshot",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "QuotationCalcRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PurchaseSalesRows",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MonthlySalaryCalcs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "LovItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InvoiceConfigSnapshot",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InvoiceCalcRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Holidays",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AdminUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AdminUserGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AdminSystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AdminPermissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AdminFeatures",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AdminCompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

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
        [Id] int IDENTITY(1,1) NOT NULL,
        [LastCustomersSyncUtc] datetime2 NULL,
        [LastInvoicesSyncUtc] datetime2 NULL,
        [LastOutstandingSyncUtc] datetime2 NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoSyncStates] PRIMARY KEY ([Id])
    );
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'[ZohoCustomerRecords]', N'U') IS NOT NULL DROP TABLE [ZohoCustomerRecords];");

            migrationBuilder.Sql("IF OBJECT_ID(N'[ZohoInvoiceRecords]', N'U') IS NOT NULL DROP TABLE [ZohoInvoiceRecords];");

            migrationBuilder.Sql("IF OBJECT_ID(N'[ZohoOutstandingRecords]', N'U') IS NOT NULL DROP TABLE [ZohoOutstandingRecords];");

            migrationBuilder.Sql("IF OBJECT_ID(N'[ZohoSyncStates]', N'U') IS NOT NULL DROP TABLE [ZohoSyncStates];");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TaxPaymentRows");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SalaryMasters");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "QuotationLineItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "QuotationConfigSnapshot");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "QuotationCalcRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PurchaseSalesRows");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MonthlySalaryCalcs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "LovItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InvoiceConfigSnapshot");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InvoiceCalcRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AdminUserGroups");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AdminSystemSettings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AdminPermissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AdminFeatures");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AdminCompanyProfiles");
        }
    }
}
