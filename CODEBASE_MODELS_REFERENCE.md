# QuotationAPI.V2 - Codebase Models & DTO Reference

**Generated:** April 17, 2026  
**Purpose:** Complete reference guide for quotation, invoice, and related model definitions

---

## Table of Contents
1. [Main Models - File Paths & Class Names](#main-models---file-paths--class-names)
2. [Price/Box/Sheet Related Fields](#priceboxsheet-related-fields)
3. [Services & Controllers](#services--controllers)
4. [Database Context (DbSet Mappings)](#database-context-dbset-mappings)

---

## Main Models - File Paths & Class Names

### Core Quotation & Calculation Models

| File Path | Class Name | Purpose |
|-----------|-----------|---------|
| `Models/Quotations/QuotationModels.cs` | `Quotation` | Main quotation entity with customer details and status tracking |
| `Models/Quotations/QuotationModels.cs` | `QuotationLineItem` | Line items within a quotation |
| `Models/Quotations/QuotationModels.cs` | `QuotationStatus` (Enum) | Status values: Draft, Sent, Approved, Rejected, Expired, Converted |
| `Models/Calculations/CalculationModels.cs` | `QuotationCalcRecord` | JSON blob storage for quotation form box calculations |
| `Models/Calculations/CalculationModels.cs` | `InvoiceCalcRecord` | JSON blob storage for invoice form box calculations |

### Item Definition Model (Frontend Reference)
**Location:** `Quotation-v2.0/src/app/shared/models/item.model.ts`

**Key Interfaces:**
```typescript
export interface ItemDefinition {
  id: number;
  name: string;
  customerId?: string;
  customerName: string;
  box: ItemSnapshotBoxDetails;
  sheet: ItemSnapshotSheetDetails;
  expense: ItemSnapshotExpenseDetails;
  price: ItemSnapshotPriceDetails;
  links: ItemRecordLinks;
  createdAt: string;
  updatedAt: string;
}

export interface ItemReference {
  itemId?: number;
  itemName?: string;
  customerId?: string;
  customerName?: string;
}
```

### Sales & Inventory Models

| File Path | Class Name | Purpose |
|-----------|-----------|---------|
| `Models/Sales/SalesModels.cs` | `WasteSale` | Waste material sales tracking |
| `Models/Sales/SalesModels.cs` | `RollSale` | Paper roll sales with cost calculations |
| `Models/Inventory/InventoryModels.cs` | `ReelStock` | Reel inventory management |
| `Models/Inventory/InventoryModels.cs` | `MaterialPrice` | Material pricing by GSM and BF |

### Employee & Payroll Models

| File Path | Class Name | Purpose |
|-----------|-----------|---------|
| `Models/Employee/EmployeeApiModels.cs` | `EmpEmployee` | Employee master data |
| `Models/Employee/EmployeeApiModels.cs` | `EmpAttendanceRecord` | Daily attendance tracking |
| `Models/Employee/EmployeeApiModels.cs` | `EmpHoliday` | Holiday definitions |
| `Models/Employee/EmployeeApiModels.cs` | `EmpSalaryMaster` | Employee salary structure |
| `Models/Employee/EmployeeApiModels.cs` | `EmpSalaryAdvance` | Salary advance requests |
| `Models/Employee/EmployeeApiModels.cs` | `EmpMonthlySalaryCalc` | Monthly salary calculations |

### Accounting & Finance Models

| File Path | Class Name | Purpose |
|-----------|-----------|---------|
| `Models/Accounts/AccountModels.cs` | `BankCashBalance` | Bank and cash balances |
| `Models/Accounts/AccountModels.cs` | `IncomeEntry` | Income transaction entries |
| `Models/Accounts/AccountModels.cs` | `CustomerOutstanding` | Customer payment dues tracking |
| `Models/Accounts/AccountModels.cs` | `ExpenseEntry` | General expense entries |
| `Models/Accounts/AccountModels.cs` | `AccountTransaction` | Generic account transactions |
| `Models/Accounts/AccountModels.cs` | `CashTransfer` | Bank-to-cash or cash-to-bank transfers |
| `Models/Accounts/AccountModels.cs` | `ExpenseLedgerRow` | Expense ledger entries |
| `Models/Accounts/AccountModels.cs` | `PurchaseSalesRow` | Purchase and sales transaction rows |

### Expense Records & Workflow

| File Path | Class Name | Purpose |
|-----------|-----------|---------|
| `Models/Expense/ExpenseApiModels.cs` | `ExpenseRecord` | Expense with draft/submitted/approved workflow |

### Admin & Configuration Models

| File Path | Class Name | Purpose |
|-----------|-----------|---------|
| `Models/Admin/AdminApiModels.cs` | `AdminUser` | System users with roles |
| `Models/Admin/AdminApiModels.cs` | `AdminUserGroup` | User group definitions |
| `Models/Admin/AdminApiModels.cs` | `AdminFeature` | Feature toggles |
| `Models/Admin/AdminApiModels.cs` | `AdminPermission` | Permission matrix (group × feature × permission) |
| `Models/Admin/AdminApiModels.cs` | `AdminSystemSetting` | System configuration settings |
| `Models/Admin/AdminAuditLog` | `AdminAuditLog` | Audit trail for all operations |
| `Models/Admin/AdminApiModels.cs` | `AdminCompanyProfile` | Company details (GST, address) |

### Reference & Configuration Models

| File Path | Class Name | Purpose |
|-----------|-----------|---------|
| `Models/Customer/CustomerApiModels.cs` | `CustomerMaster` | Customer master with GST and type |
| `Models/LOV/LovModels.cs` | `LovItem` | List of Values (LOV) - dropdown/reference data |
| `Models/Integrations/ZohoBooksModels.cs` | `ZohoSyncState`, `ZohoCustomerRecord`, etc. | Zoho Books integration tracking |

---

## Price/Box/Sheet Related Fields

### Box Details (ItemSnapshotBoxDetails)

**Location:** `Models/Quotations/` (stored as JSON in `QuotationCalcRecord.DataJson`)

```csharp
public interface ItemSnapshotBoxDetails {
  // Dimensions
  length: number;              // Box length
  width: number;               // Box width
  height: number;              // Box height
  
  // Specifications
  unit: number;                // Box unit (qty type)
  model: number;               // Box model
  quantity: number;            // Number of boxes
  joint: number;               // Joint type/code
  
  // Allowances (in mm or %)
  allowanceR: number;          // Allowance on radius
  allowanceC: number;          // Allowance on corner
  
  // Board Configuration
  isCustomBoard: boolean;      // Whether using custom board
  reelHeight: number;          // Reel height (critical for cutting)
  cuttingSize: number;         // Cutting size for reel
  
  // Internal dimensions
  lengthIn: number;            // Inside length
  widthIn: number;             // Inside width
  heightIn: number;            // Inside height
  
  // Optional fields
  deliveryDate?: string;       // Expected delivery date
  description: string;         // Box description
}
```

### Sheet Details (ItemSnapshotSheetDetails)

**Storage:** Nested in QuotationCalcRecord.DataJson as `sheet` object

```csharp
public interface ItemSnapshotSheetDetails {
  // Configuration
  ply: number;                 // Number of plies (layers)
  gsmAdjust: number;           // GSM adjustment factor
  conversionAmount: number;    // Conversion factor (0 or 4)
  
  // Calculated Weights
  totalGsm: number;            // Total GSM for all plies
  boardWeight: number;         // Board weight per sheet
  boxWeight: string;           // Total weight per box
  totalWeight: number;         // Total weight for all boxes
  
  // Calculations
  boxExpense: number;          // Expense per box (from sheet)
  requiredBoard: number;       // Board area required
  requiredSheet: number;       // Number of sheets required
  
  // Sheet Rows (array of materials/plies)
  sheetRows: ItemSnapshotSheetRow[];
}

public interface ItemSnapshotSheetRow {
  sheetPlyId?: number;        // Optional ply ID reference
  material: number;            // Material ID/code
  rate: number;                // Material rate per unit
  conversion: number;          // Conversion (0 or 4) - per-row based on material
  gsm: number;                 // GSM weight
  flute: number;               // Flute type (0=kraft, 1=test, etc.)
  calculatedGsm: number;       // Computed GSM
  expense: number;             // Expense/cost for this ply
  weight: number;              // Weight for this ply
  bf: number;                  // Basis film/weight
  order: number;               // Order in stack (top to bottom)
}
```

**Key Fields Explanation:**
- **Conversion Amount**: Per-row field (0 or 4) - determines if GSM is adjusted by 4
- **Material**: References material ID (not ItemDefinition)
- **Flute**: Corrugated flute type (0=kraft/solid, 1=test, etc.)
- **Ply Order**: 1=inner liner, 2=flute, 3=outer liner (for 3-ply)

### Price Details (ItemSnapshotPriceDetails)

**Storage:** Nested in QuotationCalcRecord.DataJson as `price` object

```csharp
public interface ItemSnapshotPriceDetails {
  // Base Pricing
  priceKg: number;             // Price per kg
  suggestedBoxPrice: number;   // System-calculated box price
  boxPrice: number;            // Our box selling price
  boxPriceKg: number;          // Box price per kg
  
  // Charges
  printPrice: number;          // Print/branding cost
  taxPercent: number;          // Tax rate %
  taxAmount: number;           // Calculated tax amount
  
  // Final Prices
  finalBoxPrice: number;       // Total price (box + print + tax)
  customerBoxPrice: number;    // Customer's negotiated price
  
  // Settlement & Profit
  settlementAmount: number;    // Amount for settlement
  actualAmount: number;        // Actual invoice amount
  commissionAmount: number;    // Commission in amount
  commission: number;          // Commission %
  
  // Profit Analysis
  profit: number;              // Absolute profit
  profitPercent: number;       // Profit margin %
  rentAmount: number;          // Rent allocation
  profitExcludingRent: number; // Profit without rent
  profitPercentExcludingRent: number; // Profit margin excluding rent
}
```

### Expense Details (ItemSnapshotExpenseDetails)

**Storage:** Nested in QuotationCalcRecord.DataJson as `expense` object

```csharp
public interface ItemSnapshotExpenseDetails {
  // Workflow Control
  includeRent?: boolean;       // Whether to include rent in calculations
  
  // Direct Expenses per Box
  print: number;               // Print cost per box
  printUps: number;            // Print UPS (copies)
  pin: number;                 // Pin cost
  commission: number;          // Commission per box
  commissionType: number;      // Commission type (0=amount, 1=%)
  transportCharges: number;    // Transport cost
  pinCharges: number;          // Pin charges
  gumCharges: number;          // Gum/adhesive charges
  
  // Facility Expenses
  rentCharges: number;         // Rent per box
  ebCharges: number;           // EB (electricity) charges
  salaryCharges: number;       // Labor/salary charges
  
  // Additional Expenses
  otherCharges: number;        // Miscellaneous charges
  otherDesc: string;           // Description of other charges
  printCharges: number;        // Duplicate of print?
  extraCharges: number;        // Extra/contingency charges
  
  // Aggregates
  commissionCharges: number;   // Total commission
  boxCharges: number;          // Total box cost
  totalExpenses: number;       // Sum of all expenses
}
```

---

## Services & Controllers

### Core API Controllers for Quotations & Invoices

#### 1. QuotationsController
**File:** `Controllers/QuotationsController.cs`
**Route:** `api/quotations`

**Endpoints:**
- `GET /` - List all quotations (with pagination, search, filtering)
- `GET /{id}` - Get quotation details by ID
- `POST /` - Create new quotation
- `PUT /{id}` - Update quotation
- `DELETE /{id}` - Delete quotation (soft delete)
- `POST /{id}/approve` - Approve quotation
- `POST /{id}/reject` - Reject quotation
- `POST /{id}/send` - Send quotation
- `GET /statistics/count-by-status` - Status summary
- `GET /{id}/export/pdf` - Export as PDF

**Query Parameters:**
- `searchText` - Search in customer name, quote number, description
- `status` - Filter by QuotationStatus enum
- `dateFrom`, `dateTo` - Date range filtering
- `pageNumber`, `pageSize` - Pagination
- `sortBy` - amount, customername, quotenumber, status
- `sortOrder` - asc, desc

#### 2. QuotationCalcController
**File:** `Controllers/QuotationCalcController.cs`
**Route:** `api/quotation-calc-records`

**Endpoints:**
- `GET /` - List all quotation calculations (ordered by ID desc)
- `GET /{id}` - Get specific quotation calculation record
- `POST /` - Create quotation calculation record
- `PUT /{id}` - Update quotation calculation
- `DELETE /{id}` - Delete quotation calculation (soft delete)
- `POST /{id}/duplicate` - Clone a quotation calculation

**Request DTO:**
```csharp
public class CalcRecordSaveRequest
{
    public string CompanyName { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string DataJson { get; set; }  // Contains: box, sheet, expense, price, item
}
```

#### 3. InvoiceCalcController
**File:** `Controllers/InvoiceCalcController.cs`
**Route:** `api/invoice-calc-records`

**Endpoints:** (Same as QuotationCalcController)
- `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/duplicate`

**Usage:** Stores invoice (order) form calculations with same JSON structure as quotations

#### 4. ItemsController
**File:** `Controllers/ItemsController.cs`
**Route:** `api/items`

**Purpose:** Builds unified ItemDefinition list from both quotation and invoice calculations

**Endpoints:**
- `GET /` - Get all items (aggregated from quotation + invoice records)
- `GET /{id:int}` - Get specific item by ID

**Logic:**
- Parses `QuotationCalcRecords.DataJson` and `InvoiceCalcRecords.DataJson`
- Extracts: `box`, `sheet`, `expense`, `price`, `item` objects
- Groups by customer + item name
- Generates stable item ID (hash-based)
- Tracks which quotations/invoices reference each item

### Sales & Inventory Controllers

#### 5. SalesController
**File:** `Controllers/SalesController.cs`
**Route:** `api/sales`

**Waste Sales Endpoints:**
- `GET /waste` - List all waste sales
- `GET /waste/{id}` - Get waste sale by ID
- `POST /waste` - Create waste sale
- `PUT /waste/{id}` - Update waste sale
- `DELETE /waste/{id}` - Delete waste sale

**Roll Sales Endpoints:**
- `GET /roll` - List all roll sales
- `GET /roll/{id}` - Get roll sale by ID
- `POST /roll` - Create roll sale (auto-calculates costs)
- `PUT /roll/{id}` - Update roll sale
- `DELETE /roll/{id}` - Delete roll sale

**Key Fields in RollSale:**
```
WeightKg, UnitPrice → TotalIncome
PaperPricePerKg → PaperCost
GumUsedKg, GumCost (calculated)
EbUsedUnits, EbCost (calculated)
Profit = TotalIncome - PaperCost - GumCost - EbCost
```

#### 6. InventoryController
**File:** `Controllers/InventoryController.cs`
**Route:** `api/inventory`

**Endpoints:**
- Reel stock CRUD operations
- Material price lookup and management

**Models:**
- `ReelStock` - Physical inventory with purchase tracking
- `MaterialPrice` - Material rates by GSM and BF

### Data Retrieval Services

#### 7. AccountsController
**File:** `Controllers/AccountsController.cs`
**Route:** `api/accounts`

Manages customer outstanding, payments, and financial transactions

#### 8. CustomerMastersController
**File:** `Controllers/CustomerMastersController.cs`
**Route:** `api/customers`

Customer master data including GST lookup

#### 9. EmployeesController, PayslipsController, ExpenseRecordsController
Handles employee operations, payslip generation, and expense workflows

#### 10. AdminModuleController
Manages users, groups, features, permissions, audit logs

---

## Database Context (DbSet Mappings)

**File:** `Data/QuotationDbContext.cs`

```csharp
public class QuotationDbContext : DbContext
{
    // Authentication
    public DbSet<AppUser> Users { get; set; }
    public DbSet<AppRole> Roles { get; set; }
    public DbSet<AppUserRole> UserRoles { get; set; }

    // Quotations
    public DbSet<Quotation> Quotations { get; set; }
    public DbSet<QuotationLineItem> QuotationLineItems { get; set; }

    // Calculations (JSON blob storage)
    public DbSet<QuotationCalcRecord> QuotationCalcRecords { get; set; }
    public DbSet<InvoiceCalcRecord> InvoiceCalcRecords { get; set; }

    // Accounts & Finance
    public DbSet<BankCashBalance> BankCashBalances { get; set; }
    public DbSet<CustomerOutstanding> CustomerOutstandings { get; set; }
    public DbSet<IncomeEntry> IncomeEntries { get; set; }
    public DbSet<ExpenseEntry> ExpenseEntries { get; set; }
    public DbSet<AccountTransaction> AccountTransactions { get; set; }
    public DbSet<CashTransfer> CashTransfers { get; set; }
    public DbSet<ExpenseLedgerRow> ExpenseLedgerRows { get; set; }
    public DbSet<IncomeRow> IncomeRows { get; set; }
    public DbSet<PurchaseSalesRow> PurchaseSalesRows { get; set; }
    public DbSet<TaxPaymentRow> TaxPaymentRows { get; set; }

    // Employees
    public DbSet<EmpEmployee> Employees { get; set; }
    public DbSet<EmpAttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<EmpHoliday> Holidays { get; set; }
    public DbSet<EmpSalaryMaster> SalaryMasters { get; set; }
    public DbSet<EmpSalaryAdvance> SalaryAdvances { get; set; }
    public DbSet<EmpMonthlySalaryCalc> MonthlySalaryCalcs { get; set; }

    // Expenses
    public DbSet<ExpenseRecord> ExpenseRecords { get; set; }

    // Inventory
    public DbSet<ReelStock> ReelStocks { get; set; }
    public DbSet<MaterialPrice> MaterialPrices { get; set; }

    // Reference Data
    public DbSet<CustomerMaster> CustomerMasters { get; set; }
    public DbSet<LovItem> LovItems { get; set; }

    // Admin & Configuration
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<AdminUserGroup> AdminUserGroups { get; set; }
    public DbSet<AdminFeature> AdminFeatures { get; set; }
    public DbSet<AdminPermission> AdminPermissions { get; set; }
    public DbSet<AdminSystemSetting> AdminSystemSettings { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
    public DbSet<AdminCompanyProfile> AdminCompanyProfiles { get; set; }

    // Configuration Snapshots
    public DbSet<ConfigurationHistory> ConfigurationHistory { get; set; }
    public DbSet<QuotationConfigSnapshot> QuotationConfigSnapshot { get; set; }
    public DbSet<InvoiceConfigSnapshot> InvoiceConfigSnapshot { get; set; }

    // Sales
    public DbSet<WasteSale> WasteSales { get; set; }
    public DbSet<RollSale> RollSales { get; set; }

    // Integrations
    public DbSet<ZohoSyncState> ZohoSyncStates { get; set; }
    public DbSet<ZohoCustomerRecord> ZohoCustomerRecords { get; set; }
    public DbSet<ZohoInvoiceRecord> ZohoInvoiceRecords { get; set; }
    public DbSet<ZohoOutstandingRecord> ZohoOutstandingRecords { get; set; }
}
```

---

## Key Data Flow Patterns

### Quotation/Invoice Calculation Storage

The system uses a **JSON blob storage pattern** for complex form data:

```
QuotationCalcRecord / InvoiceCalcRecord
    ├── DataJson (contains)
    │   ├── box: ItemSnapshotBoxDetails
    │   ├── sheet: ItemSnapshotSheetDetails (with array of sheetRows)
    │   ├── expense: ItemSnapshotExpenseDetails
    │   ├── price: ItemSnapshotPriceDetails
    │   └── item: ItemReference
    └── Computed Fields
        ├── Amount (decimal)
        ├── CreatedAt (DateTime)
        └── UpdatedAt (DateTime)
```

### Item Definition Aggregation

The `ItemsController` aggregates items from **both** quotation and invoice records:

1. Queries `QuotationCalcRecords` and `InvoiceCalcRecords`
2. Parses JSON to extract `box`, `sheet`, `expense`, `price`, `item`
3. Groups by canonical key: `name:{customerName}|item:{itemName}` or `id:{customerId}|item:{itemName}`
4. Computes stable item ID using hash function
5. Returns `ItemDefinition` with full snapshot and links to source records

---

## Important Notes

1. **Conversion Field:** Per-row in `ItemSnapshotSheetRow` (replaces global conversion)
2. **Material Reference:** Sheet rows reference material ID, not ItemDefinition
3. **Expense Defaults:** `includeRent=true`, `contractLabour=0` (from controller normalization)
4. **Soft Deletes:** All models use `IsDeleted` flag (not physical deletion)
5. **Decimal Precision:** All decimal fields configured to 18 digits, 4 decimal places
6. **No Navigation Properties:** Models avoid complex EF relationships (mostly use IDs and JSON)

