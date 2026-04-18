# Rate Values Update - April 10, 2026

## Overview
This document describes the process to update rate values in the QuotationAPI database.

## New Values
The following rate values have been updated:

| Rate | Previous Value | New Value | Unit | Notes |
|------|--------|-----------|------|-------|
| EB Rate | 3200 | 150.00 | per day | EB/day |
| Salary Per Shift | 850 | 600.00 | per person | Salary / person |
| Gum Rate | 610 | 1320.00 | per bag | Gum/bag |
| Pin Rate | 940 | 98.00 | per kg | Pin price/kg |

## How to Apply the Updates

### Method 1: Using SQL Script (Recommended)
1. Open SQL Server Management Studio (SSMS)
2. Connect to your QuotationAPI database
3. Open the file: `SQL/UpdateRateValues.sql`
4. Execute the script
5. Verify the updates by running the SELECT statement at the end

### Method 2: Using Azure Portal
1. Navigate to your SQL Server in Azure Portal
2. Open Query Editor
3. Paste the SQL commands from `SQL/UpdateRateValues.sql`
4. Execute and verify

### Method 3: Using Entity Framework (Code-First Update)
If you prefer to apply updates through Entity Framework migrations:
1. Create a new migration: `dotnet ef migrations add UpdateRateValues`
2. Update the migration with proper UPDATE statements
3. Apply the migration: `dotnet ef database update`

## Impact On Application
- **Quotation Calculations**: All new quotation entries will use the updated rates
- **Invoice Calculations**: All invoices will use the updated rates
- **Existing Data**: Historical quotations and invoices are not affected
- **Configuration Service**: The frontend will load the updated values automatically on next page refresh

## Database Schema
The values are stored in the `AdminSystemSetting` table with the following keys:
- `ebRateDefault` - EB Rate
- `salaryPerShiftDefault` - Salary Per Shift
- `gumRateDefault` - Gum Rate  
- `pinRateDefault` - Pin Rate

## Reverting Changes
If you need to revert to previous values:

```sql
UPDATE AdminSystemSetting SET Settingvalue = '3200' WHERE Settingkey = 'ebRateDefault';
UPDATE AdminSystemSetting SET Settingvalue = '850' WHERE Settingkey = 'salaryPerShiftDefault';
UPDATE AdminSystemSetting SET Settingvalue = '610' WHERE Settingkey = 'gumRateDefault';
UPDATE AdminSystemSetting SET Settingvalue = '940' WHERE Settingkey = 'pinRateDefault';
```

## Frontend Changes Required
None. The frontend ConfigurationService will:
1. Load these values from the API on application startup
2. Cache them in memory
3. Use them for all calculations automatically

**No code changes or recompilation required.**

---

**Last Updated**: April 10, 2026  
**Applied By**: Admin  
**Status**: Ready for deployment
