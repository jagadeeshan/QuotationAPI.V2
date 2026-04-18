# E2E Data Flow Charts

Generated: 2026-04-11

## 1) Authentication + Authorization Flow

```mermaid
flowchart LR
  U[Client] --> A1[POST /api/auth/register]
  A1 --> T1[(Users)]
  A1 --> T2[(Roles)]
  A1 --> T3[(UserRoles)]
  U --> A2[POST /api/auth/login]
  A2 --> T1
  A2 --> JWT[Access Token + Refresh Token]
```

## 2) Admin Module Flow

```mermaid
flowchart LR
  U[Admin UI] --> G[POST /api/admin-module/user-groups]
  U --> F[POST /api/admin-module/features]
  U --> P[POST /api/admin-module/permissions]
  U --> AU[POST /api/admin-module/users]
  U --> CP[POST /api/admin-module/company-profiles]
  G --> TG[(AdminUserGroups)]
  F --> TF[(AdminFeatures)]
  P --> TP[(AdminPermissions)]
  AU --> TU[(AdminUsers)]
  CP --> TCP[(AdminCompanyProfiles)]
  G --> TA[(AdminAuditLogs)]
  F --> TA
  P --> TA
  AU --> TA
  CP --> TA
```

## 3) Accounts + Ledger Flow

```mermaid
flowchart LR
  U[Accounts UI] --> IB[POST /api/accounts/initial-balance]
  U --> PS[POST /api/accounts/ledger/purchase-sales]
  U --> CT[POST /api/accounts/cash-transfers]
  U --> IN[POST /api/accounts/income-entries]
  U --> EX[POST /api/accounts/expense-entries]
  U --> TX[POST /api/accounts/ledger/tax-payments]
  U --> CO[POST /api/accounts/customer-outstanding/additional]
  U --> ST[POST /api/accounts/customer-outstanding/settlements]

  IB --> B[(BankCashBalances)]
  PS --> PUR[(PurchaseSalesRows)]
  PS --> EXP[(ExpenseEntries)]
  CT --> CTF[(CashTransfers)]
  IN --> INC[(IncomeEntries)]
  EX --> EXP
  TX --> TAX[(TaxPaymentRows)]
  CO --> OUT[(CustomerOutstandings)]
  ST --> INC
  ST --> B
  PS --> B
  CT --> B
  IN --> B
  EX --> B
  TX --> B
```

## 4) Inventory -> Material Price Master Flow

```mermaid
flowchart LR
  U[Inventory UI or API Client] --> RS[POST /api/inventory/reel-stocks]
  RS --> RST[(ReelStocks)]
  RS --> UPS[Backend UpsertMaterialPriceFromStockAsync]
  UPS --> MP[(MaterialPrices)]
  Q1[Quotation/Invoice Rate Lookup] --> LKP[GET /api/inventory/material-prices/lookup]
  LKP --> MP
```

## 5) Sales Flow

```mermaid
flowchart LR
  U[Sales UI] --> WS[POST /api/sales/waste]
  U --> RL[POST /api/sales/roll]
  WS --> WST[(WasteSales)]
  RL --> RLS[(RollSales)]
  WS --> OUT[(CustomerOutstandings)]
  RL --> OUT
```

## 6) Expense Workflow Flow

```mermaid
flowchart LR
  U[Expense UI] --> ER[POST /api/expense-records]
  U --> SY[POST /api/expense-records/sync-accounting]
  ER --> ERT[(ExpenseRecords)]
  ER --> EX[(ExpenseEntries)]
  SY --> EX
  EX --> B[(BankCashBalances)]
```

## 7) Employee Flow

```mermaid
flowchart LR
  U[Employee UI] --> E1[POST /api/employees]
  U --> E2[POST /api/employees/attendance]
  U --> E3[POST /api/employees/holidays]
  U --> E4[POST /api/employees/salary-masters]
  U --> E5[POST /api/employees/salary-advances]
  U --> E6[POST /api/employees/monthly-salary-calcs]

  E1 --> T1[(Employees)]
  E2 --> T2[(AttendanceRecords)]
  E3 --> T3[(Holidays)]
  E4 --> T4[(SalaryMasters)]
  E5 --> T5[(SalaryAdvances)]
  E6 --> T6[(MonthlySalaryCalcs)]
```

## 8) Quotation + Calculation Snapshot Flow

```mermaid
flowchart LR
  U[Quotation UI] --> Q[POST /api/quotations]
  Q --> QT[(Quotations)]
  Q --> QL[(QuotationLineItems)]

  U --> QC[POST /api/quotation-calc-records]
  U --> IC[POST /api/invoice-calc-records]
  QC --> QCR[(QuotationCalcRecords)]
  IC --> ICR[(InvoiceCalcRecords)]

  U --> CH[POST /api/configuration-history/record-change]
  U --> QS[POST /api/configuration-history/create-quotation-snapshot/1]
  U --> IS[POST /api/configuration-history/create-invoice-snapshot/1]

  CH --> CHT[(ConfigurationHistory)]
  QS --> QSNP[(QuotationConfigSnapshot)]
  IS --> ISNP[(InvoiceConfigSnapshot)]
  SS[(AdminSystemSettings)] --> QS
  SS --> IS
```

## 9) Legacy Ledger Tables Seeded for Full-Table Coverage

```mermaid
flowchart LR
  S[SQL Seed Script] --> AT[(AccountTransactions)]
  S --> EL[(ExpenseLedgerRows)]
  S --> IR[(IncomeRows)]
```
