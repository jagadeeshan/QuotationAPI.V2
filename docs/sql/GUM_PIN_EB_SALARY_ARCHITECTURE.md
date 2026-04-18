# GUM, PIN, EB, SALARY Rate Management Architecture

## Overview
These four rates (GUM, PIN, EB, SALARY) are critical for calculating expenses in quotations and invoices. They flow through the system in multiple layers with some gaps in tracking and synchronization.

---

## 1. DATA SOURCES (Database Layer)

### A. Primary Storage: AdminSystemSettings Table
**Location**: `QuotationDbContext` 

Stores system-wide rate defaults:
```
✓ ebRateDefault        = 3200 (stored value, default unit)
✓ pinRateDefault       = 940  (stored value, default unit)
✓ gumRateDefault       = 610  (stored value, default unit)
✓ salaryPerShiftDefault = 850 (stored value, per person)
```

**Updated by**: `ConfigurationController.UpdateSystemSetting()`

**Access via API**: `GET /api/configuration/rate-defaults`

### B. Secondary Storage: LovItems (List of Values) Table
**Location**: `QuotationDbContext.LovItems`

Same rates cached as LOV items for flexibility:
```
Parentname: "Financial Rates"
├── Name: "eb"         Value: "3200"
├── Name: "pin"        Value: "940"
├── Name: "gum"        Value: "610"
└── Name: "salary"     Value: "850"
```

**Accessed via**: `GET /api/list-of-values` → `LovService.getListOfValues()`

---

## 2. QUOTATION/INVOICE EXPENSE MAPPING

### Rate Usage Formula:

```typescript
// Per-unit costs calculated from rates:
const perPin = pinRate / 3300;           // ₹/pin per kg
const perGum = gumRate / 170;            // ₹/gum per bag
const ebRate = ebRate;                   // Direct ₹/unit
const salaryRate = salaryPerShift / 8;   // ₹/hour (8-hour shift)

// Expenses calculated:
pinCharges = perPin × quantity × pin;
gumCharges = perGum × (totalWeight / 10);
ebCharges = (workHours / 8) × ebRate;
salaryCharges = (salaryPerShift / 8) × persons × workHours;
```

**Related Model**: `QuotationLineItemExpense`, `InvoiceExpense`

**Storage**: 
- `QuotationLineItem.ExpenseDetails` (JSON)
- `InvoiceCalculation` (similar structure)

---

## 3. FRONTEND RATE LOADING FLOW

### A. Component: QuotationFormComponent / InvoiceFormComponent

**Sequence:**
```
1. Component Initializes
   ↓
2. loadConfiguration() called
   ↓
3. ConfigurationService.getRateDefaults()
   ├─ First fallback to hardcoded defaults
   ├─ Then try LOV service
   └─ Update session snapshots
   ↓
4. LovService.getListOfValues() called
   ├─ Fetch ALL LOV items from API
   ├─ Build lookup map (normalize names)
   ├─ Extract: ebRate, pinRate, gumRate, salaryPerShift
   └─ detect if rates changed vs session snapshot
   ↓
5. Store in component properties:
   ├─ this.ebRate = 3200
   ├─ this.pinRate = 940
   ├─ this.gumRate = 610
   ├─ this.salaryPerShift = 850
   └─ (Also store session snapshots for change detection)
   ↓
6. Display rates in UI
   ↓
7. Use rates for calculations (calculateExpense())
```

### B. Rate Reading Logic (readRate method)

```typescript
private readRate(
  lookup: Map<string, number>, 
  keywords: string[], 
  defaultValue: number
): number {
  // Search for keywords in LOV items
  for (const keyword of keywords) {
    if (lookup.has(keyword)) {
      return lookup.get(keyword)!;
    }
  }
  // Fall back to default if not found
  return defaultValue;
}

// Example usage:
this.ebRate = this.readRate(lookup, ['ebrate', 'eb'], 3200);
this.gumRate = this.readRate(lookup, ['gumrate', 'gum'], 610);
this.pinRate = this.readRate(lookup, ['pinrate', 'pin'], 940);
this.salaryPerShift = this.readRate(lookup, ['salary', 'salarypershift'], 850);
```

**Files**:
- `quotation-form.component.ts` (lines 512-548)
- `invoice-form.component.ts` (lines 1754-1796)

---

## 4. CONFIGURATION HISTORY TRACKING (Gap Analysis)

### Current State:
✓ **ConfigurationHistoryController** exists
✓ **ConfigurationHistory table** tracks changes
✓ **Endpoints available**: `GET /api/configuration-history/history`

### GAP #1: Rates NOT logged in ConfigurationHistory
**Issue**: When rates change via ConfigurationController.UpdateSystemSetting(), 
         ConfigurationHistory is NOT updated

**Current Flow**:
```
ConfigurationController.UpdateSystemSetting()
├─ Read old value
├─ Update AdminSystemSettings
├─ Save to DB
└─ ❌ NO ConfigurationHistory entry created!
```

**Should Be**:
```
ConfigurationController.UpdateSystemSetting()
├─ Read old value
├─ Update AdminSystemSettings
├─ Save to DB
├─ ✓ Create ConfigurationHistory entry with:
│   ├─ SettingKey: "pinRateDefault"
│   ├─ OldValue: "940"
│   ├─ NewValue: "950"
│   ├─ ChangeType: "UPDATE"
│   ├─ ChangedBy: username
│   └─ Description: "Pin rate updated"
└─ Return updated value
```

### GAP #2: Rates NOT linked to Quotation/Invoice snapshots
**Issue**: When creating quotation/invoice, rates should be:
1. ✓ Stored in QuotationConfigSnapshot (exists)
2. ❌ Validated against current rates
3. ❌ Logged in ConfigurationHistory if discrepancy found

**Flow Should Include**:
```
On Quotation Save:
├─ Fetch current rates from AdminSystemSettings
├─ If rates differ from used rates:
│   ├─ Log in ConfigurationHistory: "Quotation created with rate values"
│   └─ Mark snapshot with rate source/date
└─ Store in QuotationConfigSnapshot
```

### GAP #3: No audit trail for rate-related changes
**Issue**: Cannot trace which rate values were used for a specific quotation
         across different time periods

**Example Problem**:
```
Timeline:
t1: Pin rate = 940
t2: Create Quotation A (uses 940)
t3: Pin rate changed to 950
t4: Create Quotation B (uses 950)
t5: Generate report

❌ No way to track: "Quotation A used pinRate=940" in ConfigurationHistory
```

---

## 5. QUOTATION ORDER DETAILS RELATIONSHIP

### How Rates Flow to Create Quote:

```
User Creates Quotation
    ↓
Load Current Rates (from LOV + AdminSettings)
    ├─ ebRate = 3200
    ├─ pinRate = 940
    ├─ gumRate = 610
    └─ salaryPerShift = 850
    ↓
User Fills Box Details (quantity, material, weight, etc.)
    ↓
Calculate Expense:
    - pinCharges = (pinRate/3300) × quantity × pinWeight
    - gumCharges = (gumRate/170) × totalWeight
    - ebCharges = (workHrs/8) × ebRate
    - salaryCharges = (salaryPerShift/8) × people × workHrs
    ↓
Store in QuotationLineItem.ExpenseDetails (JSON):
{
  "pinCharges": 125.45,
  "gumCharges": 87.23,
  "ebCharges": 400.00,
  "salaryCharges": 2125.00,
  "rates": {  // ✓ This should be captured
    "pinRate": 940,
    "gumRate": 610,
    "ebRate": 3200,
    "salaryPerShift": 850
  }
}
    ↓
Save to Database
    ↓
Quotation Created ✓
```

**Files**:
- `QuotationFormComponent.calculat​eExpense()` (line 1170)
- `QuotationModel.cs` (ExpenseDetails field)

---

## 6. CURRENT DATA FLOW DIAGRAM

```
┌─────────────────────────────────────────┐
│     AdminSystemSettings Table           │
│   ebRateDefault: 3200                   │
│   pinRateDefault: 940                   │
│   gumRateDefault: 610                   │
│   salaryPerShiftDefault: 850            │
└──────────────┬──────────────────────────┘
               │
     ┌─────────┼─────────┐
     │                   │
     ↓                   ↓
┌─────────────┐    ┌──────────────┐
│LovItems tbl │    │ConfigController
│(Cached)     │    │GET rate-defaults
└─────────────┘    └──────────────┘
     │                   │
     └─────────┬─────────┘
               │
    ┌──────────┴──────────┐
    ↓                     ↓
Frontend         (API Layer)
┌──────────────────────────────┐
│ LovService.getListOfValues() │
│ ConfigurationService load()  │
└────────────────┬─────────────┘
                 │
    ┌────────────┴────────────┐
    ↓                         ↓
QuotationForm            InvoiceForm
├─ ebRate: 3200         ├─ ebRate: 3200
├─ pinRate: 940         ├─ pinRate: 940
├─ gumRate: 610         ├─ gumRate: 610
└─ salaryPerShift: 850  └─ salaryPerShift: 850
    │                         │
    └─────────────┬───────────┘
                  │
         ┌────────▼─────────┐
         │ calculateExpense()
         │ ├─ pinCharges
         │ ├─ gumCharges
         │ ├─ ebCharges
         │ └─ salaryCharges
         └────────┬─────────┘
                  │
        ┌─────────▼──────────┐
        │ Save Quotation/    │
        │ Invoice with       │
        │ ExpenseDetails     │
        │ (INCLUDES RATES)   │
        └────────────────────┘
```

---

## 7. IDENTIFIED GAPS & ISSUES

| Gap | Severity | Impact | Location |
|-----|----------|--------|----------|
| **G1** | HIGH | Rate changes not logged in ConfigurationHistory | ConfigurationController |
| **G2** | HIGH | No audit trail linking rates to quotations | QuotationController |
| **G3** | MEDIUM | Rate snapshots not validated at calc time | InvoiceFormComponent |
| **G4** | MEDIUM | No warning when rates change between create & save | QuotationFormComponent |
| **G5** | LOW | LOV and AdminSettings can go out of sync | Program.cs seed data |
| **G6** | MEDIUM | Fallback hardcoded values not in ConfigurationHistory | ConfigurationService |

---

## 8. PROPOSED FIXES

### Fix #1: Auto-log ConfigurationHistory on Rate Updates
**File**: `ConfigurationController.cs`

```csharp
private async Task LogConfigurationChange(
    string settingKey, 
    string oldValue, 
    string newValue,
    string changedBy)
{
    var historyEntry = new ConfigurationHistory
    {
        Id = Guid.NewGuid().ToString(),
        SettingKey = settingKey,
        OldValue = oldValue,
        NewValue = newValue,
        ChangeType = string.IsNullOrEmpty(oldValue) ? "CREATE" : "UPDATE",
        Description = $"Rate updated via Admin panel",
        ChangedBy = changedBy,
        ChangedDate = DateTime.UtcNow,
        IsActive = "Y"
    };
    
    _context.ConfigurationHistory.Add(historyEntry);
    await _context.SaveChangesAsync();
}

// Call this in UpdateSystemSetting()
```

### Fix #2: Capture Rates in Quotation Expense Details
**File**: `QuotationFormComponent.ts`

```typescript
// In calculateExpense(), add rates to JSON:
const expenseDetails = {
  pinCharges: Number(pinCharges.toFixed(3)),
  gumCharges: Number(gumCharges.toFixed(3)),
  salaryCharges: Number(salaryCharges.toFixed(3)),
  ebCharges: Number(ebCharges.toFixed(3)),
  // NEW: Store the rates used
  ratesUsed: {
    pinRate: this.pinRate,
    gumRate: this.gumRate,
    ebRate: this.ebRate,
    salaryPerShift: this.salaryPerShift,
    capturedAt: new Date().toISOString()
  }
};
```

### Fix #3: Add Rate Validation Before Save
**File**: `QuotationFormComponent.ts`

```typescript
private async validateRatesBeforeSave(): Promise<{valid: boolean, warni​ng: string | null}> {
  // Fetch latest rates
  const latestRates = await this.configService.getRateDefaults().toPromise();
  
  const changes = [];
  if (latestRates.ebRate !== this.ebRate) 
    changes.push(`EB: ${this.ebRate} → ${latestRates.ebRate}`);
  if (latestRates.pinRate !== this.pinRate) 
    changes.push(`PIN: ${this.pinRate} → ${latestRates.pinRate}`);
  if (latestRates.gumRate !== this.gumRate) 
    changes.push(`GUM: ${this.gumRate} → ${latestRates.gumRate}`);
  if (latestRates.salaryPerShift !== this.salaryPerShift) 
    changes.push(`SALARY: ${this.salaryPerShift} → ${latestRates.salaryPerShift}`);
  
  return {
    valid: true,
    warning: changes.length > 0 ? `Rates changed: ${changes.join(', ')}` : null
  };
}

// Call in saveQuotation() before submission
```

### Fix #4: Link Quotation Rates to ConfigurationHistory
**File**: `QuotationController.cs` (Create endpoint)

```csharp
// After saving quotation, log rate snapshot
private async Task LogQuotationRateSnapshot(
    long quotationId,
    decimal pinRate,
    decimal gumRate,
    decimal ebRate,
    decimal salaryPerShift)
{
    foreach (var (key, value) in new Dictionary<string, decimal>
    {
        { "GUM used in Quotation " + quotationId, gumRate },
        { "PIN used in Quotation " + quotationId, pinRate },
        { "EB used in Quotation " + quotationId, ebRate },
        { "SALARY used in Quotation " + quotationId, salaryPerShift }
    })
    {
        _context.ConfigurationHistory.Add(new ConfigurationHistory
        {
            Id = Guid.NewGuid().ToString(),
            SettingKey = key,
            OldValue = null,
            NewValue = value.ToString(),
            ChangeType = "CREATE",
            Description = $"Snapshot of rate at quotation creation",
            ChangedBy = userId,
            ChangedDate = DateTime.UtcNow,
            IsActive = "Y"
        });
    }
    
    await _context.SaveChangesAsync();
}
```

### Fix #5: ConfigurationHistory View with Rate History Query
**File**: Create new endpoint in ConfigurationHistoryController

```csharp
[HttpGet("rate-history")]
public async Task<ActionResult<IEnumerable<RateHistoryDto>>> GetRateChangeHistory()
{
    var rateKeys = new[] { "ebRateDefault", "pinRateDefault", "gumRateDefault", "salaryPerShiftDefault" };
    
    var history = await _context.ConfigurationHistory
        .Where(h => rateKeys.Contains(h.SettingKey) && h.IsActive == "Y")
        .OrderByDescending(h => h.ChangedDate)
        .Select(h => new RateHistoryDto
        {
            RateName = h.SettingKey.Replace("Default", ""),
            OldValue = h.OldValue,
            NewValue = h.NewValue,
            ChangedBy = h.ChangedBy,
            ChangedDate = h.ChangedDate,
            ImpactedQuotations = _context.QuotationLineItems
                .Where(q => q.CreatedDate >= h.ChangedDate)
                .Count()
        })
        .ToListAsync();
    
    return Ok(history);
}
```

---

## 9. SUMMARY OF COMPLETE FLOW (After Fixes)

```
┌─── Configuration Management (Admin) ───┐
│ 1. Admin updates rate in UI              │
│ 2. ConfigurationController receives req  │
│ 3. Old value → ConfigurationHistory      │
│ 4. New value → AdminSystemSettings       │
│ 5. New value → LOV Items (sync)          │
│ 6. ConfigurationHistoryController feeds  │
│    historical view to UI                 │
└──────────────────────────────────────────┘
             ↓ (APIs broadcast changes)
┌─── Quotation Creation Flow ────────┐
│ 1. User opens quotation form       │
│ 2. Load current rates from API     │
│ 3. Display rates + session history │
│ 4. User fills details             │
│ 5. System calculates expenses     │
│ 6. ✓ Rates captured in expense JSON
│ 7. Before save: validate rate changes │
│ 8. Save quotation + rate snapshot │
│ 9. Log rate usage to ConfigHistory │
└────────────────────────────────────┘
```

---

## Implementation Priority

**Phase 1 (Critical):**
- Fix G1: Auto-log rate changes in ConfigurationHistory
- Fix G2: Capture rates in Quotation/Invoice expense details

**Phase 2 (Important):**
- Fix G3: Rate validation before save
- Fix G4: Warning on rate changes
- Create ConfigurationHistoryUI showing rate trends

**Phase 3 (Enhancement):**
- Fix G6: Centralize hardcoded values in database
- Create reporting dashboard for rate impact analysis
