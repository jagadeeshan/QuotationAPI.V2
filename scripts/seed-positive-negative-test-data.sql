/*
  Positive + Negative test data seed for Quotation V2
  Generated: 2026-04-14
  Scope: Auth/Admin/Customer/Accounts/Inventory/Expense/Employee/Sales/Quotation/Invoice/LOV

  Notes:
  1) Run after schema sync script: scripts/ef-migration-sync.sql
  2) This script inserts deterministic positive data and executes negative probes in TRY/CATCH blocks.
  3) Negative probes are expected to fail by validation/business rules in API layer; SQL-level constraints are limited for some modules.
*/

SET NOCOUNT ON;

DECLARE @runTag NVARCHAR(40) = CONCAT('TST-', FORMAT(SYSUTCDATETIME(), 'yyyyMMddHHmmss'));
DECLARE @today NVARCHAR(10) = CONVERT(NVARCHAR(10), GETUTCDATE(), 23);
DECLARE @nowIso NVARCHAR(33) = CONVERT(NVARCHAR(33), SYSUTCDATETIME(), 126) + 'Z';
DECLARE @period NVARCHAR(7) = LEFT(@today, 7);

DECLARE @customerId NVARCHAR(40) = CONCAT('cust-', @runTag);
DECLARE @employeeId NVARCHAR(40) = CONCAT('emp-', @runTag);
DECLARE @groupId NVARCHAR(40) = CONCAT('group-', @runTag);
DECLARE @featureId NVARCHAR(40) = CONCAT('feature-', @runTag);
DECLARE @permId NVARCHAR(40) = CONCAT('perm-', @runTag);
DECLARE @companyId NVARCHAR(40) = CONCAT('company-', @runTag);
DECLARE @userId NVARCHAR(40) = CONCAT('user-', @runTag);
DECLARE @wasteId NVARCHAR(40) = CONCAT('waste-', @runTag);
DECLARE @rollId NVARCHAR(40) = CONCAT('roll-', @runTag);
DECLARE @outstandingId NVARCHAR(40) = CONCAT('out-', @runTag);
DECLARE @expenseRecordId NVARCHAR(40) = LEFT(CONCAT('EXP-', REPLACE(NEWID(), '-', '')), 12);
DECLARE @incomeId NVARCHAR(40) = CONCAT('inc-', @runTag);
DECLARE @expenseId NVARCHAR(40) = CONCAT('exp-', @runTag);
DECLARE @cashTransferId NVARCHAR(40) = CONCAT('ctr-', @runTag);
DECLARE @reelId NVARCHAR(40) = REPLACE(NEWID(), '-', '');
DECLARE @materialPriceId NVARCHAR(40) = REPLACE(NEWID(), '-', '');
DECLARE @adminSettingId NVARCHAR(40) = CONCAT('setting-', @runTag);
DECLARE @challanNo NVARCHAR(40) = CONCAT('CH-', RIGHT(@runTag, 8));
DECLARE @voucherNo NVARCHAR(40) = CONCAT('PUR-', RIGHT(@runTag, 8));

DECLARE @NegativeCases TABLE(
  CaseName NVARCHAR(120),
  ExpectedOutcome NVARCHAR(200),
  ActualOutcome NVARCHAR(200),
  IsExpected BIT
);

BEGIN TRY
  BEGIN TRANSACTION;

  /* ------------------------------------------------------------
     POSITIVE DATA INSERTS
     ------------------------------------------------------------ */

  IF NOT EXISTS (SELECT 1 FROM BankCashBalances WHERE Type = 'cash')
  BEGIN
    INSERT INTO BankCashBalances (Id, Type, Balance, Description, LastUpdated)
    VALUES (CONCAT('bal-cash-', @runTag), 'cash', 150000, 'Test opening cash', @nowIso);
  END

  IF NOT EXISTS (SELECT 1 FROM BankCashBalances WHERE Type = 'bank')
  BEGIN
    INSERT INTO BankCashBalances (Id, Type, Balance, Description, LastUpdated)
    VALUES (CONCAT('bal-bank-', @runTag), 'bank', 250000, 'Test opening bank', @nowIso);
  END

  INSERT INTO CustomerMasters
    (Id, Code, Name, Phone, Email, Address, GstNumber, CustomerType, Status, OpeningBalance, CreatedDate, UpdatedDate, IsDeleted)
  VALUES
    (@customerId, CONCAT('CUST', RIGHT(@runTag, 6)), CONCAT('Customer ', @runTag), '9876543210', CONCAT(@runTag, '@example.com'),
     'Test Address', '33ABCDE1234F1Z5', 'Retail', 'Active', 0, @nowIso, @nowIso, 0);

  INSERT INTO PurchaseSalesRows
    (VoucherNumber, TransactionType, CustomerId, PartyName, PaymentType, Amount, TotalAmountPaid, TaxPercent, TaxAmount, VoucherDate, IsDeleted)
  VALUES
    (@voucherNo, 'Purchase', @customerId, CONCAT('Supplier ', @runTag), 'bank', 12500, 12500, 18, 1906.78, @today, 0);

  INSERT INTO IncomeEntries
    (Id, CustomerId, CustomerName, Description, Amount, Type, IncomeType, Date, Category, OutstandingId, Reference, CreatedDate, Status, IsDeleted)
  VALUES
    (@incomeId, @customerId, CONCAT('Customer ', @runTag), 'Positive income entry', 1500, 'bank', 'independent', @today, 'Misc', NULL, CONCAT('INC-', @runTag), @nowIso, 'active', 0);

  INSERT INTO ExpenseEntries
    (Id, Description, Amount, Type, Date, Category, Reference, Status, IsDeleted)
  VALUES
    (@expenseId, 'Positive expense entry', 600, 'cash', @today, 'Office', CONCAT('EXP-', @runTag), 'active', 0);

  INSERT INTO CashTransfers
    (Id, FromAccount, ToAccount, Amount, TransferDate, Remarks, CreatedDate, IsDeleted)
  VALUES
    (@cashTransferId, 'bank', 'cash', 1000, @today, 'Positive transfer', @nowIso, 0);

  INSERT INTO CustomerOutstandings
    (Id, CustomerId, CustomerName, OrderId, Amount, Description, Date, DueDate, CreatedDate, Status, PaidAmount, IsDeleted)
  VALUES
    (@outstandingId, @customerId, CONCAT('Customer ', @runTag), CONCAT('ORD-', @runTag), 5000, 'Positive outstanding', @today, @today, @nowIso, 'pending', 0, 0);

  INSERT INTO MaterialPrices
    (Id, Material, Gsm, BF, Price, Unit, EffectiveDate, Supplier, Status, IsDeleted)
  VALUES
    (@materialPriceId, 'Kraft', 120, 18, 64.50, 'kg', @today, 'Test Supplier', 'active', 0);

  INSERT INTO ReelStocks
    (Id, ReelNumber, StockType, Material, RollSize, Gsm, Bf, Quantity, UnitCost, Weight, Amount,
     PurchaseVoucherNumber, DealerName, PurchaseInvoiceNumber, ReceivedDate, Remarks,
     TaxPercent, TaxAmount, FinalAmount, CurrentStock, ReorderLevel, Unit, LastUpdated, Status, IsDeleted)
  VALUES
    (@reelId, CONCAT('RL-', RIGHT(@runTag, 8)), 'reel', 'Kraft', '40-inch', 120, 18, 10, 64.5, 100, 6450,
     @voucherNo, CONCAT('Supplier ', @runTag), CONCAT('PI-', RIGHT(@runTag, 8)), @today, 'Positive reel stock',
     18, 1161, 7611, 100, 20, 'kg', @nowIso, 'active', 0);

  INSERT INTO ExpenseRecords
    (Id, ExpenseNumber, Category, Amount, ExpenseDate, PaidBy, PaymentMethod, Remarks, Status, CreatedAt, UpdatedAt, IsDeleted)
  VALUES
    (@expenseRecordId, CONCAT('EXPR-', RIGHT(@runTag, 8)), 'Transport', 850, @today, 'accounts-team', 'cash',
     'Approved expense for impact test', 'approved', SYSUTCDATETIME(), SYSUTCDATETIME(), 0);

  INSERT INTO Employees
    (Id, EmployeeCode, FullName, Phone, Designation, JoiningDate, MonthlySalary, Status, Department, IsDeleted)
  VALUES
    (@employeeId, CONCAT('EMP', RIGHT(@runTag, 6)), CONCAT('Employee ', @runTag), '9000000001', 'Operator', @today, 24000, 'active', 'Production', 0);

  INSERT INTO SalaryMasters
    (Id, EmployeeId, SalaryType, BasicSalary, Hra, Allowance, Deduction, OtMultiplier, OtRatePerHour,
     EffectiveFrom, DeductionsJson, Description, CreatedDate, UpdatedDate, IsDeleted)
  VALUES
    (CONCAT('sm-', @runTag), @employeeId, 'monthly', 18000, 3000, 2000, 500, 1.5, 150,
     @today, '[]', 'Positive salary master', @nowIso, @nowIso, 0);

  INSERT INTO AttendanceRecords
    (Id, EmployeeId, Date, Status, AttendanceHours, OtHours, HourlyRate, OtRate, RegularPay, OtPay, TotalPay, Notes, IsDeleted)
  VALUES
    (CONCAT('att-', @runTag), @employeeId, @today, 'present', 8, 0, 100, 150, 800, 0, 800, 'Positive attendance', 0);

  INSERT INTO MonthlySalaryCalcs
    (Id, EmployeeId, EmployeeCode, FullName, Designation, Month, WeekNumber, SalaryType, BasicSalary, Hra, Allowance,
     BonusPay, PerformancePay, SalaryMasterDeduction, TotalEarnings, PresentDays, AbsentDays, LeaveDays,
     TotalOtHours, OtEarnings, AttendanceDeduction, OtherDeductions, OtherDeductionsJson,
     SalaryAdvanceDeduction, TotalDeductions, NetSalary, CalcStatus, CreatedDate, UpdatedDate, IsDeleted)
  VALUES
    (CONCAT('msc-', @runTag), @employeeId, CONCAT('EMP', RIGHT(@runTag, 6)), CONCAT('Employee ', @runTag), 'Operator', @period, NULL, 'monthly',
     18000, 3000, 2000, 0, 0, 500, 23000, 26, 0, 0, 0, 0, 0, 0, '[]', 0, 500, 22500, 'draft', @nowIso, @nowIso, 0);

  INSERT INTO AdminUserGroups (Id, Name, Description, PermissionsJson, ParentGroup, MembersJson, IsDeleted)
  VALUES (@groupId, CONCAT('Group ', @runTag), 'Positive admin group', '["inventory.read","accounts.read"]', NULL, '[]', 0);

  INSERT INTO AdminFeatures (Id, Name, Description, [Key], IsActive, EnabledRolesJson, CreatedAt, UpdatedAt, IsDeleted)
  VALUES (@featureId, CONCAT('Feature ', @runTag), 'Positive admin feature', CONCAT('feature_', @runTag), 1, '["Admin"]', SYSUTCDATETIME(), SYSUTCDATETIME(), 0);

  INSERT INTO AdminPermissions (Id, GroupId, FeatureId, PermissionsJson, GrantedAt, GrantedBy, IsDeleted)
  VALUES (@permId, @groupId, @featureId, '["view","edit"]', SYSUTCDATETIME(), 'seed-script', 0);

  INSERT INTO AdminUsers (Id, Username, Email, FirstName, LastName, Role, Status, CreatedAt, LastLoginAt, GroupsJson, IsDeleted)
  VALUES (@userId, CONCAT('admin_', @runTag), CONCAT('admin_', @runTag, '@example.com'), 'Admin', 'Seed', 'Admin', 'active', SYSUTCDATETIME(), NULL, CONCAT('["', @groupId, '"]'), 0);

  INSERT INTO AdminSystemSettings (Id, [Key], [Value], Description, Category, [Type], IsEditable, UpdatedAt, UpdatedBy, IsDeleted)
  VALUES (@adminSettingId, CONCAT('setting_', @runTag), '42', 'Positive setting', 'General', 'number', 1, SYSUTCDATETIME(), 'seed-script', 0);

  INSERT INTO AdminCompanyProfiles (Id, CompanyName, Address, GstNo, IsActive, CreatedAt, UpdatedAt, UpdatedBy, IsDeleted)
  VALUES (@companyId, CONCAT('Company ', @runTag), 'Seed Address', '33ABCDE1234F1Z5', 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 'seed-script', 0);

  INSERT INTO TaxPaymentRows (ChallanNo, TaxType, Amount, OtherInputCredit, PaymentDate, Period, IsDeleted)
  VALUES (@challanNo, 'GST', 500, 0, @today, @period, 0);

  INSERT INTO WasteSales
    (Id, CustomerId, CustomerName, WeightKg, UnitPrice, TotalAmount, Description, SaleDate, Status, CreatedDate, UpdatedDate, OutstandingId, IsDeleted)
  VALUES
    (@wasteId, @customerId, CONCAT('Customer ', @runTag), 250, 12, 3000, 'Positive waste sale', @today, 'active', @nowIso, NULL, @outstandingId, 0);

  INSERT INTO RollSales
    (Id, CustomerId, CustomerName, WeightKg, UnitPrice, PaperPricePerKg, RollSize, Description,
     TotalIncome, PaperCost, GumUsedKg, GumCost, EbUsedUnits, EbCost, Profit,
     SaleDate, Status, CreatedDate, UpdatedDate, OutstandingId, IsDeleted)
  VALUES
    (@rollId, @customerId, CONCAT('Customer ', @runTag), 500, 28, 18, '40-inch', 'Positive roll sale',
     14000, 9000, 10, 200, 5, 100, 4700,
     @today, 'active', @nowIso, NULL, @outstandingId, 0);

  INSERT INTO QuotationCalcRecords (CompanyName, Description, Amount, DataJson, CreatedAt, UpdatedAt, IsDeleted)
  VALUES (CONCAT('Company ', @runTag), 'Positive quotation calc', 25000,
          '{"box":{"company":"Seed Company"},"item":{"itemName":"Test Box","customerName":"Seed Customer"},"price":{"actualAmount":25000,"profit":3200}}',
          SYSUTCDATETIME(), SYSUTCDATETIME(), 0);

  INSERT INTO InvoiceCalcRecords (CompanyName, Description, Amount, DataJson, CreatedAt, UpdatedAt, IsDeleted)
  VALUES (CONCAT('Company ', @runTag), 'Positive invoice calc', 27000,
          '{"box":{"company":"Seed Company"},"item":{"itemName":"Test Order","customerName":"Seed Customer"},"price":{"actualAmount":27000,"profit":3600}}',
          SYSUTCDATETIME(), SYSUTCDATETIME(), 0);

  INSERT INTO Quotations
    (Id, QuoteNumber, CustomerId, CustomerName, Email, Amount, Description, ValidityDays, Status, CreatedDate, ModifiedDate, DeliveryDate, CreatedBy, ModifiedBy, IsDeleted)
  VALUES
    (CONCAT('qt-', @runTag), CONCAT('Q-', RIGHT(@runTag, 8)), @customerId, CONCAT('Customer ', @runTag), CONCAT(@runTag, '@example.com'),
     18000, 'Positive quotation', 30, 0, SYSUTCDATETIME(), NULL, NULL, 'seed-script', NULL, 0);

  INSERT INTO QuotationLineItems (Id, QuotationId, ItemDescription, Quantity, UnitPrice, TotalPrice, IsDeleted)
  VALUES (CONCAT('qli-', @runTag), CONCAT('qt-', @runTag), 'Seed line item', 100, 180, 18000, 0);

  /* LOV category + item for expense category usage */
  IF NOT EXISTS (SELECT 1 FROM LovItems WHERE Parentvalue IS NULL AND Name = 'expensecategory')
  BEGIN
    INSERT INTO LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt, IsDeleted)
    VALUES (NULL, NULL, 'expensecategory', NULL, 'Expense categories', 'category', 1, 'Y', 'seed-script', 'seed-script', @nowIso, @nowIso, 0);
  END

  DECLARE @expenseCategoryParentId INT = (
      SELECT TOP 1 Id FROM LovItems WHERE Parentvalue IS NULL AND Name = 'expensecategory' ORDER BY Id DESC
  );

  INSERT INTO LovItems (Parentname, Parentvalue, Name, Value, Description, Itemtype, Displayorder, Isactive, Createdby, Updatedby, Createddt, Updateddt, IsDeleted)
  VALUES ('expensecategory', @expenseCategoryParentId, CONCAT('Fuel-', RIGHT(@runTag, 5)), 1, 'Positive LOV item', 'value', 1, 'Y', 'seed-script', 'seed-script', @nowIso, @nowIso, 0);

  /* ------------------------------------------------------------
     NEGATIVE PROBES (expected failures / invalid records)
     ------------------------------------------------------------ */

  BEGIN TRY
    INSERT INTO BankCashBalances (Id, Type, Balance, Description, LastUpdated)
    VALUES (CONCAT('bad-bal-', @runTag), 'wallet', 10, 'Invalid type', @nowIso);
    INSERT INTO @NegativeCases VALUES ('Invalid balance type', 'Should fail by regex/business validation', 'Inserted at SQL level', 0);
  END TRY
  BEGIN CATCH
    INSERT INTO @NegativeCases VALUES ('Invalid balance type', 'Should fail by regex/business validation', ERROR_MESSAGE(), 1);
  END CATCH;

  BEGIN TRY
    INSERT INTO CustomerMasters
      (Id, Code, Name, Phone, Email, Address, GstNumber, CustomerType, Status, OpeningBalance, CreatedDate, UpdatedDate, IsDeleted)
    VALUES
      (CONCAT('bad-cust-', @runTag), CONCAT('CUST', RIGHT(@runTag, 6)), 'Duplicate Code Customer', '9000000000', 'dup@example.com',
       'Dup Address', '33ABCDE1234F1Z5', 'Retail', 'Active', 0, @nowIso, @nowIso, 0);
    INSERT INTO @NegativeCases VALUES ('Duplicate customer code', 'Should fail by unique index', 'Inserted unexpectedly', 0);
  END TRY
  BEGIN CATCH
    INSERT INTO @NegativeCases VALUES ('Duplicate customer code', 'Should fail by unique index', ERROR_MESSAGE(), 1);
  END CATCH;

  BEGIN TRY
    INSERT INTO ReelStocks
      (Id, ReelNumber, StockType, Material, RollSize, Gsm, Bf, Quantity, UnitCost, Weight, Amount,
       PurchaseVoucherNumber, DealerName, PurchaseInvoiceNumber, ReceivedDate, Remarks,
       TaxPercent, TaxAmount, FinalAmount, CurrentStock, ReorderLevel, Unit, LastUpdated, Status, IsDeleted)
    VALUES
      (REPLACE(NEWID(), '-', ''), CONCAT('RL-', RIGHT(@runTag, 8)), 'reel', 'Kraft', '40-inch', 120, 18, 1, 10, 10, 100,
       @voucherNo, 'Dup', 'PI-DUP', @today, 'Duplicate reel number', 18, 18, 118, 10, 2, 'kg', @nowIso, 'active', 0);
    INSERT INTO @NegativeCases VALUES ('Duplicate reel number', 'Should fail by unique index', 'Inserted unexpectedly', 0);
  END TRY
  BEGIN CATCH
    INSERT INTO @NegativeCases VALUES ('Duplicate reel number', 'Should fail by unique index', ERROR_MESSAGE(), 1);
  END CATCH;

  BEGIN TRY
    INSERT INTO Quotations
      (Id, QuoteNumber, CustomerId, CustomerName, Email, Amount, Description, ValidityDays, Status, CreatedDate, CreatedBy, IsDeleted)
    VALUES
      (CONCAT('qt-bad-', @runTag), CONCAT('Q-', RIGHT(@runTag, 8)), @customerId, 'Dup Quote', 'dup@example.com', 1000, 'Dup quote number', 30, 0, SYSUTCDATETIME(), 'seed-script', 0);
    INSERT INTO @NegativeCases VALUES ('Duplicate quote number', 'Should fail by unique index', 'Inserted unexpectedly', 0);
  END TRY
  BEGIN CATCH
    INSERT INTO @NegativeCases VALUES ('Duplicate quote number', 'Should fail by unique index', ERROR_MESSAGE(), 1);
  END CATCH;

  COMMIT TRANSACTION;
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
  THROW;
END CATCH;

SELECT
  @runTag AS RunTag,
  @customerId AS SeedCustomerId,
  @employeeId AS SeedEmployeeId,
  @voucherNo AS SeedPurchaseVoucher,
  @reelId AS SeedReelStockId,
  @outstandingId AS SeedOutstandingId;

SELECT * FROM @NegativeCases ORDER BY CaseName;
