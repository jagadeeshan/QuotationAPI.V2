CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AccountTransactions" (
        "Id" text NOT NULL,
        "Type" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "BalanceType" text NOT NULL,
        "Description" text NOT NULL,
        "Date" text NOT NULL,
        "Category" text,
        "Reference" text,
        CONSTRAINT "PK_AccountTransactions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AdminAuditLogs" (
        "Id" text NOT NULL,
        "UserId" text NOT NULL,
        "Action" text NOT NULL,
        "Resource" text NOT NULL,
        "ResourceId" text,
        "Details" text NOT NULL,
        "IpAddress" text NOT NULL,
        "UserAgent" text NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "Severity" text NOT NULL,
        CONSTRAINT "PK_AdminAuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AdminCompanyProfiles" (
        "Id" text NOT NULL,
        "CompanyName" character varying(200) NOT NULL,
        "Address" text NOT NULL,
        "GstNo" character varying(30) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_AdminCompanyProfiles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AdminFeatures" (
        "Id" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text NOT NULL,
        "Key" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "EnabledRolesJson" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_AdminFeatures" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AdminPermissions" (
        "Id" text NOT NULL,
        "GroupId" text NOT NULL,
        "FeatureId" text NOT NULL,
        "PermissionsJson" text NOT NULL,
        "GrantedAt" timestamp with time zone NOT NULL,
        "GrantedBy" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_AdminPermissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AdminSystemSettings" (
        "Id" text NOT NULL,
        "Key" text NOT NULL,
        "Value" text NOT NULL,
        "Description" text NOT NULL,
        "Category" text NOT NULL,
        "Type" text NOT NULL,
        "IsEditable" boolean NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "UpdatedBy" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_AdminSystemSettings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AdminUserGroups" (
        "Id" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text NOT NULL,
        "PermissionsJson" text NOT NULL,
        "ParentGroup" text,
        "MembersJson" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_AdminUserGroups" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AdminUsers" (
        "Id" text NOT NULL,
        "Username" text NOT NULL,
        "Email" text NOT NULL,
        "FirstName" text NOT NULL,
        "LastName" text NOT NULL,
        "Role" text NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "LastLoginAt" timestamp with time zone,
        "GroupsJson" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_AdminUsers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "AttendanceRecords" (
        "Id" text NOT NULL,
        "EmployeeId" text NOT NULL,
        "Date" text NOT NULL,
        "Status" text NOT NULL,
        "AttendanceHours" numeric(18,4) NOT NULL,
        "OtHours" numeric(18,4) NOT NULL,
        "HourlyRate" numeric(18,4) NOT NULL,
        "OtRate" numeric(18,4) NOT NULL,
        "RegularPay" numeric(18,4) NOT NULL,
        "OtPay" numeric(18,4) NOT NULL,
        "TotalPay" numeric(18,4) NOT NULL,
        "Notes" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_AttendanceRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "BankCashBalances" (
        "Id" text NOT NULL,
        "Type" text NOT NULL,
        "Balance" numeric(18,4) NOT NULL,
        "Description" character varying(200) NOT NULL,
        "LastUpdated" text,
        CONSTRAINT "PK_BankCashBalances" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "CashTransfers" (
        "Id" text NOT NULL,
        "FromAccount" text NOT NULL,
        "ToAccount" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "TransferDate" text NOT NULL,
        "Remarks" text,
        "CreatedDate" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_CashTransfers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ConfigurationHistory" (
        "Id" text NOT NULL,
        "SettingKey" text NOT NULL,
        "OldValue" text,
        "NewValue" text NOT NULL,
        "ChangeType" text NOT NULL,
        "Description" text,
        "ChangedBy" text NOT NULL,
        "ChangedDate" timestamp with time zone NOT NULL,
        "IsActive" text NOT NULL,
        "Notes" text,
        CONSTRAINT "PK_ConfigurationHistory" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "CustomerMasters" (
        "Id" text NOT NULL,
        "Code" character varying(32) NOT NULL,
        "Name" text NOT NULL,
        "Phone" text NOT NULL,
        "Email" text,
        "Address" text NOT NULL,
        "GstNumber" text,
        "CustomerType" text NOT NULL,
        "Status" text NOT NULL,
        "OpeningBalance" numeric(18,4) NOT NULL,
        "CreatedDate" text NOT NULL,
        "UpdatedDate" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_CustomerMasters" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "CustomerOutstandings" (
        "Id" text NOT NULL,
        "CustomerId" text,
        "CustomerName" text,
        "OrderId" text,
        "Amount" numeric(18,4) NOT NULL,
        "Description" text NOT NULL,
        "Date" text NOT NULL,
        "DueDate" text,
        "CreatedDate" text,
        "Status" text NOT NULL,
        "PaidAmount" numeric(18,4),
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_CustomerOutstandings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "Employees" (
        "Id" text NOT NULL,
        "EmployeeCode" character varying(32) NOT NULL,
        "FullName" text NOT NULL,
        "Phone" text NOT NULL,
        "Designation" text NOT NULL,
        "JoiningDate" text NOT NULL,
        "MonthlySalary" numeric(18,4) NOT NULL,
        "Status" text NOT NULL,
        "Department" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Employees" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ExpenseEntries" (
        "Id" text NOT NULL,
        "Description" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "Type" text NOT NULL,
        "Date" text NOT NULL,
        "Category" text NOT NULL,
        "Reference" text,
        "Status" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_ExpenseEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ExpenseLedgerRows" (
        "VoucherNumber" text NOT NULL,
        "Head" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "ExpenseDate" text NOT NULL,
        "ApprovedBy" text NOT NULL,
        CONSTRAINT "PK_ExpenseLedgerRows" PRIMARY KEY ("VoucherNumber")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ExpenseRecords" (
        "Id" text NOT NULL,
        "ExpenseNumber" text NOT NULL,
        "Category" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "ExpenseDate" text NOT NULL,
        "PaidBy" text NOT NULL,
        "PaymentMethod" text NOT NULL,
        "Remarks" text NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_ExpenseRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "Holidays" (
        "Id" text NOT NULL,
        "Year" integer NOT NULL,
        "Date" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Holidays" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "IncomeEntries" (
        "Id" text NOT NULL,
        "CustomerId" text,
        "CustomerName" text,
        "Description" character varying(300) NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "Type" text NOT NULL,
        "IncomeType" text NOT NULL,
        "Date" text NOT NULL,
        "Category" text NOT NULL,
        "OutstandingId" text,
        "Reference" text,
        "CreatedDate" text,
        "Status" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_IncomeEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "IncomeRows" (
        "ReceiptNumber" text NOT NULL,
        "Source" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "ReceivedDate" text NOT NULL,
        "PaymentMode" text NOT NULL,
        CONSTRAINT "PK_IncomeRows" PRIMARY KEY ("ReceiptNumber")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "InvoiceCalcRecords" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CompanyName" text NOT NULL,
        "Description" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "DataJson" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_InvoiceCalcRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "InvoiceConfigSnapshot" (
        "Id" text NOT NULL,
        "InvoiceId" bigint NOT NULL,
        "ConfigKey" text NOT NULL,
        "ConfigValue" text NOT NULL,
        "ConfigType" text NOT NULL,
        "SnapshotDate" timestamp with time zone NOT NULL,
        "IsActive" text NOT NULL,
        "CreatedDate" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_InvoiceConfigSnapshot" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "LovItems" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "Parentname" text,
        "Parentvalue" integer,
        "Name" text NOT NULL,
        "Value" integer,
        "Description" text,
        "Itemtype" text NOT NULL,
        "Displayorder" integer NOT NULL,
        "Isactive" text NOT NULL,
        "Createdby" text NOT NULL,
        "Updatedby" text NOT NULL,
        "Createddt" text NOT NULL,
        "Updateddt" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_LovItems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "MaterialPrices" (
        "Id" text NOT NULL,
        "Material" text NOT NULL,
        "Gsm" integer NOT NULL,
        "BF" numeric(18,4),
        "Price" numeric(18,4) NOT NULL,
        "Unit" text NOT NULL,
        "EffectiveDate" text NOT NULL,
        "Supplier" text NOT NULL,
        "Status" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_MaterialPrices" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "MonthlySalaryCalcs" (
        "Id" text NOT NULL,
        "EmployeeId" text NOT NULL,
        "EmployeeCode" text NOT NULL,
        "FullName" text NOT NULL,
        "Designation" text NOT NULL,
        "Month" text NOT NULL,
        "WeekNumber" integer,
        "SalaryType" character varying(16) NOT NULL DEFAULT 'monthly',
        "BasicSalary" numeric(18,4) NOT NULL,
        "Hra" numeric(18,4) NOT NULL,
        "Allowance" numeric(18,4) NOT NULL,
        "BonusPay" numeric(18,4) NOT NULL,
        "PerformancePay" numeric(18,4) NOT NULL,
        "SalaryMasterDeduction" numeric(18,4) NOT NULL,
        "TotalEarnings" numeric(18,4) NOT NULL,
        "PresentDays" integer NOT NULL,
        "AbsentDays" integer NOT NULL,
        "LeaveDays" integer NOT NULL,
        "TotalOtHours" numeric(18,4) NOT NULL,
        "OtEarnings" numeric(18,4) NOT NULL,
        "AttendanceDeduction" numeric(18,4) NOT NULL,
        "OtherDeductions" numeric(18,4),
        "OtherDeductionsJson" text,
        "SalaryAdvanceDeduction" numeric(18,4),
        "TotalDeductions" numeric(18,4) NOT NULL,
        "NetSalary" numeric(18,4) NOT NULL,
        "CalcStatus" text NOT NULL,
        "CreatedDate" text,
        "UpdatedDate" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_MonthlySalaryCalcs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "PurchaseSalesRows" (
        "VoucherNumber" text NOT NULL,
        "TransactionType" text NOT NULL,
        "CustomerId" text NOT NULL,
        "PartyName" text NOT NULL,
        "PaymentType" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "TotalAmountPaid" numeric(18,4) NOT NULL,
        "TaxPercent" numeric(18,4) NOT NULL,
        "TaxAmount" numeric(18,4) NOT NULL,
        "VoucherDate" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_PurchaseSalesRows" PRIMARY KEY ("VoucherNumber")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "QuotationCalcRecords" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "CompanyName" text NOT NULL,
        "Description" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "DataJson" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_QuotationCalcRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "QuotationConfigSnapshot" (
        "Id" text NOT NULL,
        "QuotationId" bigint NOT NULL,
        "ConfigKey" text NOT NULL,
        "ConfigValue" text NOT NULL,
        "ConfigType" text NOT NULL,
        "SnapshotDate" timestamp with time zone NOT NULL,
        "IsActive" text NOT NULL,
        "CreatedDate" timestamp with time zone NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_QuotationConfigSnapshot" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "Quotations" (
        "Id" text NOT NULL,
        "QuoteNumber" character varying(40) NOT NULL,
        "CustomerId" character varying(40) NOT NULL,
        "CustomerName" character varying(120) NOT NULL,
        "Email" character varying(160) NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "Description" character varying(500) NOT NULL,
        "ValidityDays" integer NOT NULL,
        "Status" integer NOT NULL,
        "CreatedDate" timestamp with time zone NOT NULL,
        "ModifiedDate" timestamp with time zone,
        "DeliveryDate" timestamp with time zone,
        "CreatedBy" character varying(80) NOT NULL,
        "ModifiedBy" character varying(80),
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Quotations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ReelStocks" (
        "Id" text NOT NULL,
        "ReelNumber" character varying(64) NOT NULL,
        "StockType" text NOT NULL,
        "Material" text NOT NULL,
        "RollSize" text,
        "Gsm" integer NOT NULL,
        "Bf" numeric(18,4),
        "Quantity" numeric(18,4) NOT NULL,
        "UnitCost" numeric(18,4) NOT NULL,
        "Weight" numeric(18,4) NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "PurchaseVoucherNumber" text,
        "DealerName" text,
        "PurchaseInvoiceNumber" text,
        "ReceivedDate" text,
        "Remarks" text,
        "TaxPercent" numeric(18,4) NOT NULL,
        "TaxAmount" numeric(18,4) NOT NULL,
        "FinalAmount" numeric(18,4) NOT NULL,
        "CurrentStock" numeric(18,4) NOT NULL,
        "ReorderLevel" numeric(18,4) NOT NULL,
        "Unit" text NOT NULL,
        "LastUpdated" text NOT NULL,
        "Status" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_ReelStocks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "Roles" (
        "Id" text NOT NULL,
        "Name" character varying(50) NOT NULL,
        "Description" character varying(120) NOT NULL,
        CONSTRAINT "PK_Roles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "RollSales" (
        "Id" text NOT NULL,
        "CustomerId" text,
        "CustomerName" text NOT NULL,
        "WeightKg" numeric(18,4) NOT NULL,
        "UnitPrice" numeric(18,4) NOT NULL,
        "PaperPricePerKg" numeric(18,4) NOT NULL,
        "RollSize" text,
        "Description" text,
        "TotalIncome" numeric(18,4) NOT NULL,
        "PaperCost" numeric(18,4) NOT NULL,
        "GumUsedKg" numeric(18,4) NOT NULL,
        "GumCost" numeric(18,4) NOT NULL,
        "EbUsedUnits" numeric(18,4) NOT NULL,
        "EbCost" numeric(18,4) NOT NULL,
        "Profit" numeric(18,4) NOT NULL,
        "SaleDate" text NOT NULL,
        "Status" text NOT NULL,
        "CreatedDate" text NOT NULL,
        "UpdatedDate" text,
        "OutstandingId" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_RollSales" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "SalaryAdvances" (
        "Id" text NOT NULL,
        "EmployeeId" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "RequestDate" text NOT NULL,
        "Reason" text NOT NULL,
        "Status" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_SalaryAdvances" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "SalaryMasters" (
        "Id" text NOT NULL,
        "EmployeeId" text NOT NULL,
        "SalaryType" character varying(16) NOT NULL DEFAULT 'monthly',
        "BasicSalary" numeric(18,4) NOT NULL,
        "Hra" numeric(18,4) NOT NULL,
        "Allowance" numeric(18,4) NOT NULL,
        "Deduction" numeric(18,4) NOT NULL,
        "OtMultiplier" numeric(18,4) NOT NULL,
        "OtRatePerHour" numeric(18,4),
        "EffectiveFrom" text NOT NULL,
        "DeductionsJson" text,
        "Description" text,
        "CreatedDate" text,
        "UpdatedDate" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_SalaryMasters" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "TaxPaymentRows" (
        "ChallanNo" text NOT NULL,
        "TaxType" text NOT NULL,
        "Amount" numeric(18,4) NOT NULL,
        "OtherInputCredit" numeric(18,4) NOT NULL,
        "PaymentDate" text NOT NULL,
        "Period" text NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_TaxPaymentRows" PRIMARY KEY ("ChallanNo")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "Users" (
        "Id" text NOT NULL,
        "Username" character varying(50) NOT NULL,
        "Email" character varying(120) NOT NULL,
        "FirstName" character varying(80) NOT NULL,
        "LastName" character varying(80) NOT NULL,
        "PasswordHash" character varying(256) NOT NULL,
        "IsActive" boolean NOT NULL,
        "LastLogin" timestamp with time zone,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "WasteSales" (
        "Id" text NOT NULL,
        "CustomerId" text,
        "CustomerName" text NOT NULL,
        "WeightKg" numeric(18,4) NOT NULL,
        "UnitPrice" numeric(18,4) NOT NULL,
        "TotalAmount" numeric(18,4) NOT NULL,
        "Description" text,
        "SaleDate" text NOT NULL,
        "Status" text NOT NULL,
        "CreatedDate" text NOT NULL,
        "UpdatedDate" text,
        "OutstandingId" text,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_WasteSales" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ZohoCustomerRecords" (
        "Id" text NOT NULL,
        "CustomerName" text NOT NULL,
        "Email" text,
        "Phone" text,
        "OutstandingAmount" numeric(18,4) NOT NULL,
        "LastModifiedTimeUtc" timestamp with time zone,
        "PulledAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ZohoCustomerRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ZohoInvoiceRecords" (
        "Id" text NOT NULL,
        "CustomerId" text,
        "CustomerName" text,
        "InvoiceNumber" text,
        "InvoiceDate" timestamp with time zone,
        "DueDate" timestamp with time zone,
        "Total" numeric(18,4) NOT NULL,
        "Balance" numeric(18,4) NOT NULL,
        "Status" text,
        "LastModifiedTimeUtc" timestamp with time zone,
        "PulledAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ZohoInvoiceRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ZohoOutstandingRecords" (
        "Id" text NOT NULL,
        "CustomerName" text,
        "OutstandingAmount" numeric(18,4) NOT NULL,
        "PulledAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ZohoOutstandingRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "ZohoSyncStates" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "LastCustomersSyncUtc" timestamp with time zone,
        "LastInvoicesSyncUtc" timestamp with time zone,
        "LastOutstandingSyncUtc" timestamp with time zone,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ZohoSyncStates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "QuotationLineItems" (
        "Id" text NOT NULL,
        "QuotationId" text NOT NULL,
        "ItemDescription" character varying(250) NOT NULL,
        "Quantity" integer NOT NULL,
        "UnitPrice" numeric(18,4) NOT NULL,
        "TotalPrice" numeric(18,4) NOT NULL,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_QuotationLineItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_QuotationLineItems_Quotations_QuotationId" FOREIGN KEY ("QuotationId") REFERENCES "Quotations" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE TABLE "UserRoles" (
        "UserId" text NOT NULL,
        "RoleId" text NOT NULL,
        CONSTRAINT "PK_UserRoles" PRIMARY KEY ("UserId", "RoleId"),
        CONSTRAINT "FK_UserRoles_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserRoles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE UNIQUE INDEX "IX_CustomerMasters_Code" ON "CustomerMasters" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE UNIQUE INDEX "IX_Employees_EmployeeCode" ON "Employees" ("EmployeeCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE INDEX "IX_QuotationLineItems_QuotationId" ON "QuotationLineItems" ("QuotationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE UNIQUE INDEX "IX_Quotations_QuoteNumber" ON "Quotations" ("QuoteNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE UNIQUE INDEX "IX_ReelStocks_ReelNumber" ON "ReelStocks" ("ReelNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE INDEX "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260414221646_InitialPostgresSchema') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260414221646_InitialPostgresSchema', '8.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415135251_AddUserAccessRequestWorkflowFields') THEN
    ALTER TABLE "Users" ADD "AccessRequestNotes" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415135251_AddUserAccessRequestWorkflowFields') THEN
    ALTER TABLE "Users" ADD "AccessRequestedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415135251_AddUserAccessRequestWorkflowFields') THEN
    ALTER TABLE "Users" ADD "AccessReviewNotes" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415135251_AddUserAccessRequestWorkflowFields') THEN
    ALTER TABLE "Users" ADD "AccessReviewedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415135251_AddUserAccessRequestWorkflowFields') THEN
    ALTER TABLE "Users" ADD "AccessReviewedBy" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415135251_AddUserAccessRequestWorkflowFields') THEN
    ALTER TABLE "Users" ADD "AccessStatus" character varying(20) NOT NULL DEFAULT 'Pending';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415135251_AddUserAccessRequestWorkflowFields') THEN
    ALTER TABLE "Users" ADD "RequestedRoleName" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260415135251_AddUserAccessRequestWorkflowFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260415135251_AddUserAccessRequestWorkflowFields', '8.0.12');
    END IF;
END $EF$;
COMMIT;

