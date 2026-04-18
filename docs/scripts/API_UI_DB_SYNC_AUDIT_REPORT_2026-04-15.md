# API/UI/DB Sync and Test Data Audit Report

Date: 2026-04-15
Workspace: Quotation-v2.0 + QuotationAPI.V2

## 1) Request Coverage Summary

Status: Completed with evidence artifacts.

Validated items:
- Previous-session completion re-check
- No mock data usage for business modules (except Zoho fallback behavior)
- UI model to API/DB sync validation
- EF schema sync verification and script regeneration
- Positive and negative test data insertion across major modules
- Detailed impact mapping by endpoint, UI screen, impacted modules

## 2) Evidence Generated/Updated

- EF idempotent migration script:
  - scripts/ef-migration-sync.sql
- API positive/negative executable suite:
  - scripts/run-positive-negative-api-tests.ps1
- Latest execution result (machine-readable):
  - scripts/positive-negative-api-results.json
- Latest execution result (human-readable matrix):
  - scripts/positive-negative-impact-report.html
- SQL seed for broad positive/negative data insertion:
  - scripts/seed-positive-negative-test-data.sql
- Existing data-flow architecture map:
  - scripts/E2E_DATA_FLOW_CHARTS.md

## 3) No-Mock Verification (Frontend)

Scanned feature service layer in Quotation-v2.0 for non-HTTP data sources.

Findings:
- Business services are API-backed through HttpService/HttpClient.
- Expense, Accounts, Customer, Inventory, Employee, Quotation, Invoice, Sales, Admin services use API endpoints.
- Remaining non-HTTP feature service is calculation-only logic (not data storage/retrieval).
- Allowed exception present: Zoho screen has fallback handling for integration failures.

Conclusion:
- No active mock/in-memory business data source found in feature service layer, except Zoho fallback behavior as requested.

## 4) UI/API/DB Sync Verification

Checks executed:
- Angular production build completed successfully for Quotation-v2.0.
- API project build completed successfully for QuotationAPI.V2.
- EF check reported no pending model changes.
- DB migration state read successfully from __EFMigrationsHistory.

EF note:
- An idempotent EF script was regenerated to scripts/ef-migration-sync.sql.
- This script can be safely applied to align schema on target environments.

## 5) Positive + Negative Test Data Insertion Results

Execution:
- Ran scripts/run-positive-negative-api-tests.ps1 against http://localhost:7502.

Result summary:
- Total cases: 21
- Passed: 21
- Failed: 0

Modules covered:
- Auth
- Admin
- Customer
- Accounts
- Inventory
- Expense
- Sales
- Employee
- Quotation
- Invoice
- LOV

## 6) Impact Mapping (Examples)

1. Auth register
- Endpoint: POST /api/auth/register
- UI: Auth > Register
- Impact: Creates identity + role linkage used by authenticated application access.

2. Admin user group create
- Endpoint: POST /api/admin-module/user-groups
- UI: Admin > User Groups
- Impact: Adds group used by permission matrix and audit tracking.

3. Customer create
- Endpoint: POST /api/customer-masters
- UI: Customer > Customer List
- Impact: Customer becomes selectable in Quotation/Invoice/Sales/Accounts flows.

4. Purchase voucher create
- Endpoint: POST /api/accounts/ledger/purchase-sales
- UI: Accounts > Purchase/Sales Ledger
- Impact: Enables Inventory stock linkage; contributes to tax and FY reporting.

5. Reel stock create (linked)
- Endpoint: POST /api/inventory/reel-stocks
- UI: Inventory > Add Reel Stock
- Impact: Updates stock; auto-upserts material pricing dependencies.

6. Expense record create (approved)
- Endpoint: POST /api/expense-records
- UI: Expense > Expense Form/List
- Impact: Writes ExpenseRecords; syncs ExpenseEntries; updates cash/bank balances.

7. Waste sale create
- Endpoint: POST /api/sales/waste
- UI: Sales > Waste Sale
- Impact: Creates sales row and customer outstanding impact.

8. Invoice calc create
- Endpoint: POST /api/invoice-calc-records
- UI: Invoice > Invoice Form/List
- Impact: Persists order details for invoice analytics and FY calculations.

9. Order details -> customer financial impact
- Flow used by UI: Invoice save then Accounts outstanding sync
- Impact path:
  - Invoice calc record created/updated
  - Corresponding customer outstanding record upserted
  - Outstanding then participates in settlement/income flows in Accounts

For full endpoint/screen/module matrix and status codes, see:
- scripts/positive-negative-impact-report.html
- scripts/positive-negative-api-results.json

## 7) Negative Case Coverage Executed

Examples validated:
- Invalid GST lookup format -> 400
- Stock create without purchase voucher -> 400
- Unknown expense/invoice IDs -> 404
- Roll sale missing required customer name -> 400
- Attendance before joining date -> 400
- Invalid quotation email -> 400
- Blocked LOV placeholder key -> 400

## 8) Final Assessment

- Previous-session targets are in compliant state for API-first data flow.
- Business data flows are DB-backed through API.
- Zoho remains the only tolerated exception for fallback behavior.
- UI/API/DB sync checks pass.
- Positive/negative insertion and impact tests pass across covered modules.
