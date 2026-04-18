IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [AccountTransactions] (
        [Id] nvarchar(450) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [BalanceType] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Date] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NULL,
        [Reference] nvarchar(max) NULL,
        CONSTRAINT [PK_AccountTransactions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [AdminAuditLogs] (
        [Id] nvarchar(450) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [Action] nvarchar(max) NOT NULL,
        [Resource] nvarchar(max) NOT NULL,
        [ResourceId] nvarchar(max) NULL,
        [Details] nvarchar(max) NOT NULL,
        [IpAddress] nvarchar(max) NOT NULL,
        [UserAgent] nvarchar(max) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [Severity] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AdminAuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [AdminFeatures] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Key] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [EnabledRolesJson] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AdminFeatures] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [AdminPermissions] (
        [Id] nvarchar(450) NOT NULL,
        [GroupId] nvarchar(max) NOT NULL,
        [FeatureId] nvarchar(max) NOT NULL,
        [PermissionsJson] nvarchar(max) NOT NULL,
        [GrantedAt] datetime2 NOT NULL,
        [GrantedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AdminPermissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [AdminSystemSettings] (
        [Id] nvarchar(450) NOT NULL,
        [Key] nvarchar(max) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [IsEditable] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AdminSystemSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [AdminUserGroups] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [PermissionsJson] nvarchar(max) NOT NULL,
        [ParentGroup] nvarchar(max) NULL,
        [MembersJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AdminUserGroups] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [AdminUsers] (
        [Id] nvarchar(450) NOT NULL,
        [Username] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [GroupsJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AdminUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [AttendanceRecords] (
        [Id] nvarchar(450) NOT NULL,
        [EmployeeId] nvarchar(max) NOT NULL,
        [Date] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [AttendanceHours] decimal(18,2) NOT NULL,
        [OtHours] decimal(18,2) NOT NULL,
        [HourlyRate] decimal(18,2) NOT NULL,
        [OtRate] decimal(18,2) NOT NULL,
        [RegularPay] decimal(18,2) NOT NULL,
        [OtPay] decimal(18,2) NOT NULL,
        [TotalPay] decimal(18,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [BankCashBalances] (
        [Id] nvarchar(450) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Balance] decimal(18,2) NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [LastUpdated] nvarchar(max) NULL,
        CONSTRAINT [PK_BankCashBalances] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [CashTransfers] (
        [Id] nvarchar(450) NOT NULL,
        [FromAccount] nvarchar(max) NOT NULL,
        [ToAccount] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransferDate] nvarchar(max) NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [CreatedDate] nvarchar(max) NULL,
        CONSTRAINT [PK_CashTransfers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [CustomerMasters] (
        [Id] nvarchar(450) NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NULL,
        [Address] nvarchar(max) NOT NULL,
        [GstNumber] nvarchar(max) NULL,
        [CustomerType] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [OpeningBalance] decimal(18,2) NOT NULL,
        [CreatedDate] nvarchar(max) NOT NULL,
        [UpdatedDate] nvarchar(max) NULL,
        CONSTRAINT [PK_CustomerMasters] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [CustomerOutstandings] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerId] nvarchar(max) NULL,
        [CustomerName] nvarchar(max) NULL,
        [OrderId] nvarchar(max) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Date] nvarchar(max) NOT NULL,
        [DueDate] nvarchar(max) NULL,
        [CreatedDate] nvarchar(max) NULL,
        [Status] nvarchar(max) NOT NULL,
        [PaidAmount] decimal(18,2) NULL,
        CONSTRAINT [PK_CustomerOutstandings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] nvarchar(450) NOT NULL,
        [EmployeeCode] nvarchar(max) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Designation] nvarchar(max) NOT NULL,
        [JoiningDate] nvarchar(max) NOT NULL,
        [MonthlySalary] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Department] nvarchar(max) NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [ExpenseEntries] (
        [Id] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Date] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Reference] nvarchar(max) NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ExpenseEntries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [ExpenseLedgerRows] (
        [VoucherNumber] nvarchar(450) NOT NULL,
        [Head] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ExpenseDate] nvarchar(max) NOT NULL,
        [ApprovedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ExpenseLedgerRows] PRIMARY KEY ([VoucherNumber])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [ExpenseRecords] (
        [Id] nvarchar(450) NOT NULL,
        [ExpenseNumber] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ExpenseDate] nvarchar(max) NOT NULL,
        [PaidBy] nvarchar(max) NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [Remarks] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ExpenseRecords] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [Holidays] (
        [Id] nvarchar(450) NOT NULL,
        [Year] int NOT NULL,
        [Date] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_Holidays] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [IncomeEntries] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerId] nvarchar(max) NULL,
        [CustomerName] nvarchar(max) NULL,
        [Description] nvarchar(300) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [IncomeType] nvarchar(max) NOT NULL,
        [Date] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [OutstandingId] nvarchar(max) NULL,
        [Reference] nvarchar(max) NULL,
        [CreatedDate] nvarchar(max) NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_IncomeEntries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [IncomeRows] (
        [ReceiptNumber] nvarchar(450) NOT NULL,
        [Source] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ReceivedDate] nvarchar(max) NOT NULL,
        [PaymentMode] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_IncomeRows] PRIMARY KEY ([ReceiptNumber])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [InvoiceCalcRecords] (
        [Id] int NOT NULL IDENTITY,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [CompanyName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [DataJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_InvoiceCalcRecords] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [LovItems] (
        [Id] int NOT NULL IDENTITY,
        [Parentname] nvarchar(max) NULL,
        [Parentvalue] int NULL,
        [Name] nvarchar(max) NOT NULL,
        [Value] int NULL,
        [Description] nvarchar(max) NULL,
        [Itemtype] nvarchar(max) NOT NULL,
        [Displayorder] int NOT NULL,
        [Isactive] nvarchar(max) NOT NULL,
        [Createdby] nvarchar(max) NOT NULL,
        [Updatedby] nvarchar(max) NOT NULL,
        [Createddt] nvarchar(max) NOT NULL,
        [Updateddt] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_LovItems] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [MaterialPrices] (
        [Id] nvarchar(450) NOT NULL,
        [Material] nvarchar(max) NOT NULL,
        [Gsm] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [EffectiveDate] nvarchar(max) NOT NULL,
        [Supplier] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_MaterialPrices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [MonthlySalaryCalcs] (
        [Id] nvarchar(450) NOT NULL,
        [EmployeeId] nvarchar(max) NOT NULL,
        [EmployeeCode] nvarchar(max) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [Designation] nvarchar(max) NOT NULL,
        [Month] nvarchar(max) NOT NULL,
        [BasicSalary] decimal(18,2) NOT NULL,
        [Hra] decimal(18,2) NOT NULL,
        [Allowance] decimal(18,2) NOT NULL,
        [SalaryMasterDeduction] decimal(18,2) NOT NULL,
        [TotalEarnings] decimal(18,2) NOT NULL,
        [PresentDays] int NOT NULL,
        [AbsentDays] int NOT NULL,
        [LeaveDays] int NOT NULL,
        [TotalOtHours] decimal(18,2) NOT NULL,
        [OtEarnings] decimal(18,2) NOT NULL,
        [AttendanceDeduction] decimal(18,2) NOT NULL,
        [OtherDeductions] decimal(18,2) NULL,
        [SalaryAdvanceDeduction] decimal(18,2) NULL,
        [TotalDeductions] decimal(18,2) NOT NULL,
        [NetSalary] decimal(18,2) NOT NULL,
        [CalcStatus] nvarchar(max) NOT NULL,
        [CreatedDate] nvarchar(max) NULL,
        [UpdatedDate] nvarchar(max) NULL,
        CONSTRAINT [PK_MonthlySalaryCalcs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [PurchaseSalesRows] (
        [VoucherNumber] nvarchar(450) NOT NULL,
        [TransactionType] nvarchar(max) NOT NULL,
        [PartyName] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [VoucherDate] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PurchaseSalesRows] PRIMARY KEY ([VoucherNumber])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [QuotationCalcRecords] (
        [Id] int NOT NULL IDENTITY,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [CompanyName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [DataJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_QuotationCalcRecords] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [Quotations] (
        [Id] nvarchar(450) NOT NULL,
        [QuoteNumber] nvarchar(40) NOT NULL,
        [CustomerId] nvarchar(40) NOT NULL,
        [CustomerName] nvarchar(120) NOT NULL,
        [Email] nvarchar(160) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [ValidityDays] int NOT NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [CreatedBy] nvarchar(80) NOT NULL,
        [ModifiedBy] nvarchar(80) NULL,
        CONSTRAINT [PK_Quotations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [ReelStocks] (
        [Id] nvarchar(450) NOT NULL,
        [ReelNumber] nvarchar(max) NOT NULL,
        [Material] nvarchar(max) NOT NULL,
        [Gsm] int NOT NULL,
        [UnitCost] decimal(18,2) NOT NULL,
        [CurrentStock] decimal(18,2) NOT NULL,
        [ReorderLevel] decimal(18,2) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [LastUpdated] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ReelStocks] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(120) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [SalaryAdvances] (
        [Id] nvarchar(450) NOT NULL,
        [EmployeeId] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [RequestDate] nvarchar(max) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SalaryAdvances] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [SalaryMasters] (
        [Id] nvarchar(450) NOT NULL,
        [EmployeeId] nvarchar(max) NOT NULL,
        [BasicSalary] decimal(18,2) NOT NULL,
        [Hra] decimal(18,2) NOT NULL,
        [Allowance] decimal(18,2) NOT NULL,
        [Deduction] decimal(18,2) NOT NULL,
        [OtMultiplier] decimal(18,2) NOT NULL,
        [OtRatePerHour] decimal(18,2) NULL,
        [EffectiveFrom] nvarchar(max) NOT NULL,
        [DeductionsJson] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [CreatedDate] nvarchar(max) NULL,
        [UpdatedDate] nvarchar(max) NULL,
        CONSTRAINT [PK_SalaryMasters] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [TaxPaymentRows] (
        [ChallanNo] nvarchar(450) NOT NULL,
        [TaxType] nvarchar(max) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentDate] nvarchar(max) NOT NULL,
        [Period] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_TaxPaymentRows] PRIMARY KEY ([ChallanNo])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        [Username] nvarchar(50) NOT NULL,
        [Email] nvarchar(120) NOT NULL,
        [FirstName] nvarchar(80) NOT NULL,
        [LastName] nvarchar(80) NOT NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [IsActive] bit NOT NULL,
        [LastLogin] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [QuotationLineItems] (
        [Id] nvarchar(450) NOT NULL,
        [QuotationId] nvarchar(450) NOT NULL,
        [ItemDescription] nvarchar(250) NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TotalPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_QuotationLineItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuotationLineItems_Quotations_QuotationId] FOREIGN KEY ([QuotationId]) REFERENCES [Quotations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_QuotationLineItems_QuotationId] ON [QuotationLineItems] ([QuotationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Quotations_QuoteNumber] ON [Quotations] ([QuoteNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407145246_InitialSqlServerSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260407145246_InitialSqlServerSchema', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407204853_AddOtherInputCreditToTaxPayment'
)
BEGIN
    ALTER TABLE [TaxPaymentRows] ADD [OtherInputCredit] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407204853_AddOtherInputCreditToTaxPayment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260407204853_AddOtherInputCreditToTaxPayment', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407213031_AddPurchaseCrudAndTaxCreditFromPurchase'
)
BEGIN
    ALTER TABLE [PurchaseSalesRows] ADD [CustomerId] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407213031_AddPurchaseCrudAndTaxCreditFromPurchase'
)
BEGIN
    ALTER TABLE [PurchaseSalesRows] ADD [PaymentType] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407213031_AddPurchaseCrudAndTaxCreditFromPurchase'
)
BEGIN
    ALTER TABLE [PurchaseSalesRows] ADD [TaxAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407213031_AddPurchaseCrudAndTaxCreditFromPurchase'
)
BEGIN
    ALTER TABLE [PurchaseSalesRows] ADD [TaxPercent] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407213031_AddPurchaseCrudAndTaxCreditFromPurchase'
)
BEGIN
    ALTER TABLE [PurchaseSalesRows] ADD [TotalAmountPaid] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407213031_AddPurchaseCrudAndTaxCreditFromPurchase'
)
BEGIN
    UPDATE PurchaseSalesRows SET TotalAmountPaid = Amount WHERE TotalAmountPaid = 0
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407213031_AddPurchaseCrudAndTaxCreditFromPurchase'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260407213031_AddPurchaseCrudAndTaxCreditFromPurchase', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407214915_AddBFToMaterialPrice'
)
BEGIN
    ALTER TABLE [MaterialPrices] ADD [BF] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260407214915_AddBFToMaterialPrice'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260407214915_AddBFToMaterialPrice', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408001000_AddContractLabourExpenseCategoryLov'
)
BEGIN

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

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408001000_AddContractLabourExpenseCategoryLov'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408001000_AddContractLabourExpenseCategoryLov', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TaxPaymentRows]') AND [c].[name] = N'OtherInputCredit');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [TaxPaymentRows] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [TaxPaymentRows] ALTER COLUMN [OtherInputCredit] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TaxPaymentRows]') AND [c].[name] = N'Amount');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [TaxPaymentRows] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [TaxPaymentRows] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryMasters]') AND [c].[name] = N'OtRatePerHour');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [SalaryMasters] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [SalaryMasters] ALTER COLUMN [OtRatePerHour] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryMasters]') AND [c].[name] = N'OtMultiplier');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [SalaryMasters] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [SalaryMasters] ALTER COLUMN [OtMultiplier] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryMasters]') AND [c].[name] = N'Hra');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [SalaryMasters] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [SalaryMasters] ALTER COLUMN [Hra] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryMasters]') AND [c].[name] = N'Deduction');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [SalaryMasters] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [SalaryMasters] ALTER COLUMN [Deduction] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryMasters]') AND [c].[name] = N'BasicSalary');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [SalaryMasters] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [SalaryMasters] ALTER COLUMN [BasicSalary] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryMasters]') AND [c].[name] = N'Allowance');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [SalaryMasters] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [SalaryMasters] ALTER COLUMN [Allowance] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalaryAdvances]') AND [c].[name] = N'Amount');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [SalaryAdvances] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [SalaryAdvances] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ReelStocks]') AND [c].[name] = N'UnitCost');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [ReelStocks] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [ReelStocks] ALTER COLUMN [UnitCost] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ReelStocks]') AND [c].[name] = N'ReorderLevel');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [ReelStocks] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [ReelStocks] ALTER COLUMN [ReorderLevel] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ReelStocks]') AND [c].[name] = N'CurrentStock');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [ReelStocks] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [ReelStocks] ALTER COLUMN [CurrentStock] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Quotations]') AND [c].[name] = N'Amount');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Quotations] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [Quotations] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuotationLineItems]') AND [c].[name] = N'UnitPrice');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [QuotationLineItems] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [QuotationLineItems] ALTER COLUMN [UnitPrice] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuotationLineItems]') AND [c].[name] = N'TotalPrice');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [QuotationLineItems] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [QuotationLineItems] ALTER COLUMN [TotalPrice] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuotationCalcRecords]') AND [c].[name] = N'Amount');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [QuotationCalcRecords] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [QuotationCalcRecords] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseSalesRows]') AND [c].[name] = N'TotalAmountPaid');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseSalesRows] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [PurchaseSalesRows] ALTER COLUMN [TotalAmountPaid] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseSalesRows]') AND [c].[name] = N'TaxPercent');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseSalesRows] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [PurchaseSalesRows] ALTER COLUMN [TaxPercent] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseSalesRows]') AND [c].[name] = N'TaxAmount');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseSalesRows] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [PurchaseSalesRows] ALTER COLUMN [TaxAmount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PurchaseSalesRows]') AND [c].[name] = N'Amount');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [PurchaseSalesRows] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [PurchaseSalesRows] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'TotalOtHours');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [TotalOtHours] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'TotalEarnings');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var21 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [TotalEarnings] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'TotalDeductions');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var22 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [TotalDeductions] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'SalaryMasterDeduction');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var23 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [SalaryMasterDeduction] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'SalaryAdvanceDeduction');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var24 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [SalaryAdvanceDeduction] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var25 sysname;
    SELECT @var25 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'OtherDeductions');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var25 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [OtherDeductions] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var26 sysname;
    SELECT @var26 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'OtEarnings');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var26 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [OtEarnings] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var27 sysname;
    SELECT @var27 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'NetSalary');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var27 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [NetSalary] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var28 sysname;
    SELECT @var28 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'Hra');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var28 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [Hra] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var29 sysname;
    SELECT @var29 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'BasicSalary');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var29 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [BasicSalary] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var30 sysname;
    SELECT @var30 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'AttendanceDeduction');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var30 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [AttendanceDeduction] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var31 sysname;
    SELECT @var31 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MonthlySalaryCalcs]') AND [c].[name] = N'Allowance');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [MonthlySalaryCalcs] DROP CONSTRAINT [' + @var31 + '];');
    ALTER TABLE [MonthlySalaryCalcs] ALTER COLUMN [Allowance] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    ALTER TABLE [MonthlySalaryCalcs] ADD [BonusPay] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    ALTER TABLE [MonthlySalaryCalcs] ADD [OtherDeductionsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    ALTER TABLE [MonthlySalaryCalcs] ADD [PerformancePay] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var32 sysname;
    SELECT @var32 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MaterialPrices]') AND [c].[name] = N'Price');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [MaterialPrices] DROP CONSTRAINT [' + @var32 + '];');
    ALTER TABLE [MaterialPrices] ALTER COLUMN [Price] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var33 sysname;
    SELECT @var33 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MaterialPrices]') AND [c].[name] = N'BF');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [MaterialPrices] DROP CONSTRAINT [' + @var33 + '];');
    ALTER TABLE [MaterialPrices] ALTER COLUMN [BF] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var34 sysname;
    SELECT @var34 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceCalcRecords]') AND [c].[name] = N'Amount');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceCalcRecords] DROP CONSTRAINT [' + @var34 + '];');
    ALTER TABLE [InvoiceCalcRecords] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var35 sysname;
    SELECT @var35 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IncomeRows]') AND [c].[name] = N'Amount');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [IncomeRows] DROP CONSTRAINT [' + @var35 + '];');
    ALTER TABLE [IncomeRows] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var36 sysname;
    SELECT @var36 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IncomeEntries]') AND [c].[name] = N'Amount');
    IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [IncomeEntries] DROP CONSTRAINT [' + @var36 + '];');
    ALTER TABLE [IncomeEntries] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var37 sysname;
    SELECT @var37 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ExpenseRecords]') AND [c].[name] = N'Amount');
    IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [ExpenseRecords] DROP CONSTRAINT [' + @var37 + '];');
    ALTER TABLE [ExpenseRecords] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var38 sysname;
    SELECT @var38 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ExpenseLedgerRows]') AND [c].[name] = N'Amount');
    IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [ExpenseLedgerRows] DROP CONSTRAINT [' + @var38 + '];');
    ALTER TABLE [ExpenseLedgerRows] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var39 sysname;
    SELECT @var39 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ExpenseEntries]') AND [c].[name] = N'Amount');
    IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [ExpenseEntries] DROP CONSTRAINT [' + @var39 + '];');
    ALTER TABLE [ExpenseEntries] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var40 sysname;
    SELECT @var40 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'MonthlySalary');
    IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var40 + '];');
    ALTER TABLE [Employees] ALTER COLUMN [MonthlySalary] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var41 sysname;
    SELECT @var41 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CustomerOutstandings]') AND [c].[name] = N'PaidAmount');
    IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [CustomerOutstandings] DROP CONSTRAINT [' + @var41 + '];');
    ALTER TABLE [CustomerOutstandings] ALTER COLUMN [PaidAmount] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var42 sysname;
    SELECT @var42 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CustomerOutstandings]') AND [c].[name] = N'Amount');
    IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [CustomerOutstandings] DROP CONSTRAINT [' + @var42 + '];');
    ALTER TABLE [CustomerOutstandings] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var43 sysname;
    SELECT @var43 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CustomerMasters]') AND [c].[name] = N'OpeningBalance');
    IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [CustomerMasters] DROP CONSTRAINT [' + @var43 + '];');
    ALTER TABLE [CustomerMasters] ALTER COLUMN [OpeningBalance] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var44 sysname;
    SELECT @var44 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CashTransfers]') AND [c].[name] = N'Amount');
    IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [CashTransfers] DROP CONSTRAINT [' + @var44 + '];');
    ALTER TABLE [CashTransfers] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var45 sysname;
    SELECT @var45 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BankCashBalances]') AND [c].[name] = N'Balance');
    IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [BankCashBalances] DROP CONSTRAINT [' + @var45 + '];');
    ALTER TABLE [BankCashBalances] ALTER COLUMN [Balance] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var46 sysname;
    SELECT @var46 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AttendanceRecords]') AND [c].[name] = N'TotalPay');
    IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [AttendanceRecords] DROP CONSTRAINT [' + @var46 + '];');
    ALTER TABLE [AttendanceRecords] ALTER COLUMN [TotalPay] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var47 sysname;
    SELECT @var47 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AttendanceRecords]') AND [c].[name] = N'RegularPay');
    IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [AttendanceRecords] DROP CONSTRAINT [' + @var47 + '];');
    ALTER TABLE [AttendanceRecords] ALTER COLUMN [RegularPay] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var48 sysname;
    SELECT @var48 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AttendanceRecords]') AND [c].[name] = N'OtRate');
    IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [AttendanceRecords] DROP CONSTRAINT [' + @var48 + '];');
    ALTER TABLE [AttendanceRecords] ALTER COLUMN [OtRate] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var49 sysname;
    SELECT @var49 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AttendanceRecords]') AND [c].[name] = N'OtPay');
    IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [AttendanceRecords] DROP CONSTRAINT [' + @var49 + '];');
    ALTER TABLE [AttendanceRecords] ALTER COLUMN [OtPay] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var50 sysname;
    SELECT @var50 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AttendanceRecords]') AND [c].[name] = N'OtHours');
    IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [AttendanceRecords] DROP CONSTRAINT [' + @var50 + '];');
    ALTER TABLE [AttendanceRecords] ALTER COLUMN [OtHours] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var51 sysname;
    SELECT @var51 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AttendanceRecords]') AND [c].[name] = N'HourlyRate');
    IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [AttendanceRecords] DROP CONSTRAINT [' + @var51 + '];');
    ALTER TABLE [AttendanceRecords] ALTER COLUMN [HourlyRate] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var52 sysname;
    SELECT @var52 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AttendanceRecords]') AND [c].[name] = N'AttendanceHours');
    IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [AttendanceRecords] DROP CONSTRAINT [' + @var52 + '];');
    ALTER TABLE [AttendanceRecords] ALTER COLUMN [AttendanceHours] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    DECLARE @var53 sysname;
    SELECT @var53 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AccountTransactions]') AND [c].[name] = N'Amount');
    IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [AccountTransactions] DROP CONSTRAINT [' + @var53 + '];');
    ALTER TABLE [AccountTransactions] ALTER COLUMN [Amount] decimal(18,4) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408141524_AddSalaryBonusPerformanceAndDeductionDetails', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408183845_AddSalaryTypeForWeeklyAndMonthly'
)
BEGIN
    ALTER TABLE [SalaryMasters] ADD [SalaryType] nvarchar(16) NOT NULL DEFAULT N'monthly';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408183845_AddSalaryTypeForWeeklyAndMonthly'
)
BEGIN
    ALTER TABLE [MonthlySalaryCalcs] ADD [SalaryType] nvarchar(16) NOT NULL DEFAULT N'monthly';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408183845_AddSalaryTypeForWeeklyAndMonthly'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408183845_AddSalaryTypeForWeeklyAndMonthly', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190041_AddSalesModule'
)
BEGIN
    CREATE TABLE [RollSales] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerId] nvarchar(max) NULL,
        [CustomerName] nvarchar(max) NOT NULL,
        [WeightKg] decimal(18,4) NOT NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        [PaperPricePerKg] decimal(18,4) NOT NULL,
        [Description] nvarchar(max) NULL,
        [TotalIncome] decimal(18,4) NOT NULL,
        [PaperCost] decimal(18,4) NOT NULL,
        [GumUsedKg] decimal(18,4) NOT NULL,
        [GumCost] decimal(18,4) NOT NULL,
        [EbUsedUnits] decimal(18,4) NOT NULL,
        [EbCost] decimal(18,4) NOT NULL,
        [Profit] decimal(18,4) NOT NULL,
        [SaleDate] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedDate] nvarchar(max) NOT NULL,
        [UpdatedDate] nvarchar(max) NULL,
        [OutstandingId] nvarchar(max) NULL,
        CONSTRAINT [PK_RollSales] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190041_AddSalesModule'
)
BEGIN
    CREATE TABLE [WasteSales] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerId] nvarchar(max) NULL,
        [CustomerName] nvarchar(max) NOT NULL,
        [WeightKg] decimal(18,4) NOT NULL,
        [UnitPrice] decimal(18,4) NOT NULL,
        [TotalAmount] decimal(18,4) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SaleDate] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedDate] nvarchar(max) NOT NULL,
        [UpdatedDate] nvarchar(max) NULL,
        [OutstandingId] nvarchar(max) NULL,
        CONSTRAINT [PK_WasteSales] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190041_AddSalesModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408190041_AddSalesModule', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190055_AddSalesLovSeed'
)
BEGIN

                    IF NOT EXISTS (SELECT 1 FROM LovItems WHERE Name = 'Roll Sales Rates' AND Parentvalue IS NULL)
                    BEGIN
                        INSERT INTO LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
                        VALUES (NULL, NULL, 'Roll Sales Rates', NULL, 'Configuration rates for roll sales gum and EB calculation', 'CATEGORY', 1, 'Y', 'system', 'system', GETUTCDATE(), GETUTCDATE())
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190055_AddSalesLovSeed'
)
BEGIN

                    IF NOT EXISTS (SELECT 1 FROM LovItems WHERE Name = 'RollSalesGumKgPerTon')
                    BEGIN
                        DECLARE @parentId INT = (SELECT TOP 1 Id FROM LovItems WHERE Name = 'Roll Sales Rates' AND Parentvalue IS NULL)
                        INSERT INTO LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
                        VALUES ('Roll Sales Rates', @parentId, 'RollSalesGumKgPerTon', 23, 'Kg of gum used per 1 ton of paper rolled (default 23)', 'RATE', 1, 'Y', 'system', 'system', GETUTCDATE(), GETUTCDATE())
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190055_AddSalesLovSeed'
)
BEGIN

                    IF NOT EXISTS (SELECT 1 FROM LovItems WHERE Name = 'RollSalesEbUnitsPerTon')
                    BEGIN
                        DECLARE @parentId2 INT = (SELECT TOP 1 Id FROM LovItems WHERE Name = 'Roll Sales Rates' AND Parentvalue IS NULL)
                        INSERT INTO LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt)
                        VALUES ('Roll Sales Rates', @parentId2, 'RollSalesEbUnitsPerTon', 10, 'Electricity units consumed per 1 ton of paper rolled (default 10)', 'RATE', 2, 'Y', 'system', 'system', GETUTCDATE(), GETUTCDATE())
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190055_AddSalesLovSeed'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408190055_AddSalesLovSeed', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190801_AddAdminCompanyProfiles'
)
BEGIN
    CREATE TABLE [AdminCompanyProfiles] (
        [Id] nvarchar(450) NOT NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [GstNo] nvarchar(30) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AdminCompanyProfiles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408190801_AddAdminCompanyProfiles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408190801_AddAdminCompanyProfiles', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [RollSales] ADD [RollSize] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [Amount] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [DealerName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [PurchaseInvoiceNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [PurchaseVoucherNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [ReceivedDate] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [Remarks] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [RollSize] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [StockType] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408204643_AddInventoryPurchaseLinkedStockAndRollSize', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408211620_AddInventoryStockFinancialFieldsAndRopeType'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [Bf] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408211620_AddInventoryStockFinancialFieldsAndRopeType'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [FinalAmount] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408211620_AddInventoryStockFinancialFieldsAndRopeType'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [Quantity] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408211620_AddInventoryStockFinancialFieldsAndRopeType'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [TaxAmount] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408211620_AddInventoryStockFinancialFieldsAndRopeType'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [TaxPercent] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408211620_AddInventoryStockFinancialFieldsAndRopeType'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [Weight] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260408211620_AddInventoryStockFinancialFieldsAndRopeType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260408211620_AddInventoryStockFinancialFieldsAndRopeType', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260409203630_AddConfigurationAuditAndSnapshots'
)
BEGIN
    CREATE TABLE [ConfigurationHistory] (
        [Id] nvarchar(450) NOT NULL,
        [SettingKey] nvarchar(max) NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NOT NULL,
        [ChangeType] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [ChangedDate] datetime2 NOT NULL,
        [IsActive] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_ConfigurationHistory] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260409203630_AddConfigurationAuditAndSnapshots'
)
BEGIN
    CREATE TABLE [InvoiceConfigSnapshot] (
        [Id] nvarchar(450) NOT NULL,
        [InvoiceId] bigint NOT NULL,
        [ConfigKey] nvarchar(max) NOT NULL,
        [ConfigValue] nvarchar(max) NOT NULL,
        [ConfigType] nvarchar(max) NOT NULL,
        [SnapshotDate] datetime2 NOT NULL,
        [IsActive] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_InvoiceConfigSnapshot] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260409203630_AddConfigurationAuditAndSnapshots'
)
BEGIN
    CREATE TABLE [QuotationConfigSnapshot] (
        [Id] nvarchar(450) NOT NULL,
        [QuotationId] bigint NOT NULL,
        [ConfigKey] nvarchar(max) NOT NULL,
        [ConfigValue] nvarchar(max) NOT NULL,
        [ConfigType] nvarchar(max) NOT NULL,
        [SnapshotDate] datetime2 NOT NULL,
        [IsActive] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_QuotationConfigSnapshot] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260409203630_AddConfigurationAuditAndSnapshots'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260409203630_AddConfigurationAuditAndSnapshots', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410133338_SeedAdminSystemSettings'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Key', N'Value', N'Description', N'Category', N'Type', N'IsEditable', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[AdminSystemSettings]'))
        SET IDENTITY_INSERT [AdminSystemSettings] ON;
    EXEC(N'INSERT INTO [AdminSystemSettings] ([Id], [Key], [Value], [Description], [Category], [Type], [IsEditable], [UpdatedAt], [UpdatedBy])
    VALUES (N''calc-001'', N''gsmFactor'', N''1550'', N''GSM Factor for paper calculation'', N''Calculations'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743532Z'', N''system''),
    (N''calc-002'', N''unitConversion'', N''1000'', N''Unit conversion factor'', N''Calculations'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743544Z'', N''system''),
    (N''calc-003'', N''gsmAdjustment'', N''10'', N''GSM Adjustment factor'', N''Calculations'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743547Z'', N''system''),
    (N''calc-004'', N''flutePercentageBase'', N''100'', N''Flute percentage base'', N''Calculations'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743549Z'', N''system''),
    (N''calc-005'', N''flapSize'', N''2'', N''Flap size'', N''Calculations'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743553Z'', N''system''),
    (N''calc-006'', N''lockSize'', N''1.25'', N''Lock size'', N''Calculations'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743555Z'', N''system''),
    (N''calc-007'', N''mmToInch'', N''0.0393701'', N''Millimeter to inch conversion'', N''Calculations'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743556Z'', N''system''),
    (N''calc-008'', N''cmToInch'', N''0.393701'', N''Centimeter to inch conversion'', N''Calculations'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743558Z'', N''system''),
    (N''calc-009'', N''decimalPrecision'', N''3'', N''Decimal precision for calculations'', N''Calculations'', N''integer'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743560Z'', N''system''),
    (N''rate-001'', N''paperRateDefault'', N''58'', N''Default paper rate'', N''RateDefaults'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743562Z'', N''system''),
    (N''rate-002'', N''duplexRateDefault'', N''72'', N''Default duplex rate'', N''RateDefaults'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743577Z'', N''system''),
    (N''rate-003'', N''ebRateDefault'', N''3200'', N''Default EB rate'', N''RateDefaults'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743580Z'', N''system''),
    (N''rate-004'', N''pinRateDefault'', N''940'', N''Default pin rate'', N''RateDefaults'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743582Z'', N''system''),
    (N''rate-005'', N''gumRateDefault'', N''610'', N''Default gum rate'', N''RateDefaults'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743583Z'', N''system''),
    (N''rate-006'', N''salaryPerShiftDefault'', N''850'', N''Default salary per shift'', N''RateDefaults'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743585Z'', N''system''),
    (N''rate-007'', N''rentMonthlyDefault'', N''12000'', N''Default monthly rent'', N''RateDefaults'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743587Z'', N''system''),
    (N''joint-001'', N''joint1Multiplier'', N''2'', N''Joint 1 multiplier'', N''JointMultipliers'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743589Z'', N''system''),
    (N''joint-002'', N''joint2Multiplier'', N''1'', N''Joint 2 multiplier'', N''JointMultipliers'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743592Z'', N''system''),
    (N''joint-003'', N''joint4Multiplier'', N''0.5'', N''Joint 4 multiplier'', N''JointMultipliers'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743594Z'', N''system''),
    (N''model-001'', N''modelBaseAddition'', N''1'', N''Model base addition'', N''ModelConstants'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743596Z'', N''system''),
    (N''model-002'', N''model5HeightIncrement'', N''3'', N''Model 5 height increment'', N''ModelConstants'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743597Z'', N''system''),
    (N''model-003'', N''model9WidthFactor'', N''0.5'', N''Model 9 width factor'', N''ModelConstants'', N''decimal'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743599Z'', N''system''),
    (N''quot-001'', N''quotationPrefix'', N''QTN'', N''Quotation number prefix'', N''QuotationSettings'', N''string'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743602Z'', N''system''),
    (N''quot-002'', N''quotationYear'', N''2026'', N''Quotation year'', N''QuotationSettings'', N''integer'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743604Z'', N''system''),
    (N''quot-003'', N''recordIdPadding'', N''3'', N''Record ID padding length'', N''QuotationSettings'', N''integer'', CAST(1 AS bit), ''2026-04-14T21:49:29.9743606Z'', N''system'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Key', N'Value', N'Description', N'Category', N'Type', N'IsEditable', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[AdminSystemSettings]'))
        SET IDENTITY_INSERT [AdminSystemSettings] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410133338_SeedAdminSystemSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260410133338_SeedAdminSystemSettings', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410162107_SeedConfigurationHistory'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'SettingKey', N'OldValue', N'NewValue', N'ChangeType', N'Description', N'ChangedBy', N'ChangedDate', N'IsActive', N'Notes') AND [object_id] = OBJECT_ID(N'[ConfigurationHistory]'))
        SET IDENTITY_INSERT [ConfigurationHistory] ON;
    EXEC(N'INSERT INTO [ConfigurationHistory] ([Id], [SettingKey], [OldValue], [NewValue], [ChangeType], [Description], [ChangedBy], [ChangedDate], [IsActive], [Notes])
    VALUES (N''ch-gstrate-001'', N''GstRate'', NULL, N''18'', N''CREATE'', N''Initial GST rate setting'', N''system'', ''2026-04-14T21:49:30.0511186Z'', N''Y'', N''Standard GST rate for quotations''),
    (N''ch-currency-001'', N''DefaultCurrency'', NULL, N''INR'', N''CREATE'', N''Default currency setting'', N''system'', ''2026-04-09T21:49:30.0511186Z'', N''Y'', N''All quotations default to INR''),
    (N''ch-gstrate-upd'', N''GstRate'', N''18'', N''18'', N''UPDATE'', N''GST rate verification'', N''admin'', ''2026-04-11T21:49:30.0511186Z'', N''Y'', N''Verified GST rate is correct''),
    (N''ch-invprefix-001'', N''InvoicePrefix'', NULL, N''INV-'', N''CREATE'', N''Invoice number prefix'', N''system'', ''2026-04-04T21:49:30.0511186Z'', N''Y'', N''All invoices prefixed with INV-''),
    (N''ch-quotvalid-001'', N''QuotationValidityDays'', NULL, N''30'', N''CREATE'', N''Quotation validity period'', N''system'', ''2026-04-07T21:49:30.0511186Z'', N''Y'', N''Quotations valid for 30 days''),
    (N''ch-discount-001'', N''DiscountAllowedPercentage'', NULL, N''10'', N''CREATE'', N''Maximum discount percentage'', N''system'', ''2026-03-30T21:49:30.0511186Z'', N''Y'', N''Maximum 10% discount allowed on quotations''),
    (N''ch-minorder-001'', N''MinimumOrderValue'', NULL, N''10000'', N''CREATE'', N''Minimum order value'', N''admin'', ''2026-04-02T21:49:30.0511186Z'', N''Y'', N''Minimum order value set to 10,000''),
    (N''ch-company-001'', N''CompanyName'', NULL, N''Quotation Management System'', N''CREATE'', N''Company name setting'', N''system'', ''2026-03-25T21:49:30.0511186Z'', N''Y'', N''Default company name''),
    (N''ch-taxded-001'', N''TaxDeductionRate'', NULL, N''10'', N''CREATE'', N''Tax deduction rate'', N''system'', ''2026-04-06T21:49:30.0511186Z'', N''Y'', N''Standard tax deduction rate''),
    (N''ch-payment-001'', N''PaymentTermsDays'', NULL, N''7'', N''CREATE'', N''Payment terms in days'', N''admin'', ''2026-04-08T21:49:30.0511186Z'', N''Y'', N''Payment due within 7 days of invoice'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'SettingKey', N'OldValue', N'NewValue', N'ChangeType', N'Description', N'ChangedBy', N'ChangedDate', N'IsActive', N'Notes') AND [object_id] = OBJECT_ID(N'[ConfigurationHistory]'))
        SET IDENTITY_INSERT [ConfigurationHistory] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260410162107_SeedConfigurationHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260410162107_SeedConfigurationHistory', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411091224_AddWeekNumberToMonthlySalaryCalc'
)
BEGIN
    ALTER TABLE [MonthlySalaryCalcs] ADD [WeekNumber] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411091224_AddWeekNumberToMonthlySalaryCalc'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260411091224_AddWeekNumberToMonthlySalaryCalc', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [WasteSales] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [RollSales] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [ReelStocks] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [MaterialPrices] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [IncomeEntries] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [ExpenseRecords] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [ExpenseEntries] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [Employees] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [CustomerOutstandings] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [CustomerMasters] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    ALTER TABLE [CashTransfers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260413210834_AddIsDeletedSoftDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260413210834_AddIsDeletedSoftDelete', N'8.0.12');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [TaxPaymentRows] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [SalaryMasters] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [SalaryAdvances] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [Quotations] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [QuotationLineItems] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [QuotationConfigSnapshot] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [QuotationCalcRecords] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [PurchaseSalesRows] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [MonthlySalaryCalcs] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [LovItems] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [InvoiceConfigSnapshot] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [InvoiceCalcRecords] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [Holidays] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [AttendanceRecords] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [AdminUsers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [AdminUserGroups] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [AdminSystemSettings] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [AdminPermissions] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [AdminFeatures] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    ALTER TABLE [AdminCompanyProfiles] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN

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
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN

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
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN

    IF OBJECT_ID(N'[ZohoOutstandingRecords]', N'U') IS NULL
    BEGIN
        CREATE TABLE [ZohoOutstandingRecords] (
            [Id] nvarchar(450) NOT NULL,
            [CustomerName] nvarchar(max) NULL,
            [OutstandingAmount] decimal(18,4) NOT NULL,
            [PulledAtUtc] datetime2 NOT NULL,
            CONSTRAINT [PK_ZohoOutstandingRecords] PRIMARY KEY ([Id])
        );
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN

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
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414102316_AddSoftDeleteAcrossDomainTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260414102316_AddSoftDeleteAcrossDomainTables', N'8.0.12');
END;
GO

COMMIT;
GO

