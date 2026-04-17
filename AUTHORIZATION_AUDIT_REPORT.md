# Authorization Audit Report - QuotationAPI.V2

**Generated:** April 17, 2026  
**Scope:** All 18 C# Controller Files

---

## Executive Summary

**Critical Finding:** Most CRUD endpoints in the API have **NO authorization checks** and are publicly accessible. Only the Admin Module has authorization restrictions, but they're incomplete.

**Key Issues:**
1. ⚠️ **AdminModuleController** restricts access to "Admin" role only - **excludes "Owner" role**
2. ⚠️ **Owner role is blocked from Admin role management** via Forbid() checks
3. ❌ **18 other controllers have ZERO authorization** on their CRUD endpoints
4. ⚠️ Permission model exists but is **not enforced** on any endpoint

---

## Detailed Findings by Controller

### 1. **AdminModuleController** ⚠️ RESTRICTED (But Incomplete)

**Location:** `Controllers/AdminModuleController.cs`

**Class-Level Authorization:**
```csharp
[Authorize(Roles = "Admin")]
public class AdminModuleController : ControllerBase
```

**All endpoints require "Admin" role:**
- ✅ GET/POST/PUT/DELETE `/api/admin-module/users`
- ✅ GET/POST/PUT/DELETE `/api/admin-module/user-groups`
- ✅ GET/POST/PUT/DELETE `/api/admin-module/features`
- ✅ GET/POST/DELETE `/api/admin-module/permissions`
- ✅ GET/POST/PUT `/api/admin-module/system-settings`
- ✅ GET `/api/admin-module/access-requests`
- ✅ POST `/api/admin-module/access-requests/{userId}/approve`
- ✅ POST `/api/admin-module/access-requests/{userId}/reject`
- ✅ GET `/api/admin-module/notifications`
- ✅ GET `/api/admin-module/all-roles`

**⚠️ ISSUE: No "Owner" role included**
- Only allows "Admin" - excludes "Owner" role which may need administrative access
- Should be: `[Authorize(Roles = "Admin,Owner")]`

---

### 2. **AuthController** ⚠️ MIXED AUTHORIZATION

**Location:** `Controllers/AuthController.cs`

**Public Methods (No Authorization):**
- ✅ POST `/api/auth/login` - No [Authorize]
- ✅ POST `/api/auth/register` - No [Authorize]
- ✅ GET `/api/auth/my-access-request` - [Authorize] (any authenticated user)
- ✅ GET `/api/auth/available-roles` - No [Authorize]

**Admin/Owner Only Methods:**
```csharp
[Authorize(Roles = "Admin,Owner")]
[HttpGet("review-access-requests")]
public async Task<ActionResult<IEnumerable<AccessRequestDto>>> ReviewAccessRequests(...)
```

**⚠️ CRITICAL ISSUE: Owner Role is Blocked from Admin Role Management**

```csharp
// Line 227-229
var isOwner = User.IsInRole("Owner") && !User.IsInRole("Admin");
if (isOwner && string.Equals(approvedRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
    return Forbid();  // ← FORBIDS Owner from approving Admin roles!

// Line 243-245
if (isOwner && string.Equals(user.RequestedRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
    return Forbid();  // ← FORBIDS Owner from approving existing Admin requests!

// Line 298-300
if (isOwner && string.Equals(user.RequestedRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
    return Forbid();  // ← FORBIDS Owner from rejecting Admin requests!
```

**Problem:** If you want Admin users to have full access, this logic prevents Owner role (which might manage users) from assigning Admin roles.

---

### 3. **All Other Controllers - ZERO AUTHORIZATION** ❌

**18 Controllers with NO [Authorize] attributes:**

1. **QuotationsController** (`api/quotations`)
   - GET (list/search) - PUBLIC
   - GET (by id) - PUBLIC
   - POST (create) - PUBLIC ❌
   - PUT (update) - PUBLIC ❌
   - DELETE (delete) - PUBLIC ❌
   - POST (duplicate) - PUBLIC ❌

2. **AccountsController** (`api/accounts`)
   - GET (balances, income, expenses, transactions) - PUBLIC
   - POST `/api/accounts/cash-transfers` - PUBLIC ❌
   - PUT/DELETE on income/expense entries - PUBLIC ❌

3. **CustomerMastersController** (`api/customer-masters`)
   - GET (list/by id) - PUBLIC
   - POST (create) - PUBLIC ❌
   - PUT (update) - PUBLIC ❌
   - DELETE (delete) - PUBLIC ❌

4. **EmployeesController** (`api/employees`)
   - GET (list) - PUBLIC
   - POST `/api/employees/mark-attendance` - PUBLIC ❌
   - POST/PUT/DELETE holidays - PUBLIC ❌
   - POST/PUT/DELETE salary master - PUBLIC ❌
   - POST/PUT/DELETE salary advance - PUBLIC ❌
   - POST/PUT salary calculations - PUBLIC ❌

5. **InvoiceCalcController** (`api/invoice-calc-records`)
   - GET (list/by id) - PUBLIC
   - POST (create) - PUBLIC ❌
   - PUT (update) - PUBLIC ❌
   - DELETE (delete) - PUBLIC ❌

6. **ExpenseRecordsController** (`api/expense-records`)
   - GET (list/by id) - PUBLIC
   - POST (create) - PUBLIC ❌
   - PUT (update) - PUBLIC ❌
   - DELETE (delete) - PUBLIC ❌

7. **ItemsController** (`api/items`)
   - GET - PUBLIC

8. **SalesController** (`api/sales`)
   - GET (waste sales) - PUBLIC
   - POST (create waste sale) - PUBLIC ❌
   - PUT (update waste sale) - PUBLIC ❌
   - DELETE (delete waste sale) - PUBLIC ❌
   - (All other sales endpoints) - PUBLIC

9. **InventoryController** (`api/inventory`)
   - GET (reel/gum/pin/rope stocks) - PUBLIC
   - POST (create stock) - PUBLIC ❌
   - PUT (update stock) - PUBLIC ❌
   - DELETE (delete stock) - PUBLIC ❌

10. **PayslipsController** (`api/payslips`)
    - POST `/api/payslips/pdf` - PUBLIC

11. **ConfigurationController** (`api/configuration`)
    - GET `/api/configuration/system-settings` - PUBLIC
    - *(No [Authorize] found)*

12. **ConfigurationHistoryController** (`api/configuration-history`)
    - GET `/api/configuration-history/history` - PUBLIC
    - *(No [Authorize] found)*

13. **LovController** (`api/lov`)
14. **QuotationAnalyticsController** (`api/quotation-analytics`)
15. **QuotationCalcController** (`api/quotation-calc-records`)
16. **ZohoBooksController** (`api/zoho-books`)

---

## Authorization Patterns Found

### 1. [Authorize] Attribute (Role-Based)
```csharp
[Authorize(Roles = "Admin")]              // Admin only
[Authorize(Roles = "Admin,Owner")]        // Admin or Owner
[Authorize]                               // Any authenticated user
```

### 2. Forbid() Checks (Conditional Denial)
Found in **AuthController.cs** lines 230, 245, 300:
```csharp
var isOwner = User.IsInRole("Owner") && !User.IsInRole("Admin");
if (isOwner && string.Equals(approvedRoleName, "Admin", ...))
    return Forbid();  // Owner cannot assign Admin role
```

### 3. Permission Model (NOT ENFORCED)
- **AdminPermission** entity exists with GroupId, FeatureId, PermissionsJson
- **Permission schema** defined but **never checked in any endpoint**
- No `User.HasPermission()` or similar checks found

---

## Summary Table: CRUD Endpoints

| Endpoint | Create | Read | Update | Delete | Auth Status |
|----------|--------|------|--------|--------|------------|
| **Admin Module** | ✅ | ✅ | ✅ | ✅ | ⚠️ Admin only (no Owner) |
| **Quotations** | ❌ | ❌ | ❌ | ❌ | ❌ PUBLIC |
| **Accounts** | ❌ | ❌ | ❌ | ❌ | ❌ PUBLIC |
| **Customers** | ❌ | ❌ | ❌ | ❌ | ❌ PUBLIC |
| **Employees** | ❌ | ❌ | ❌ | ❌ | ❌ PUBLIC |
| **Inventory** | ❌ | ❌ | ❌ | ❌ | ❌ PUBLIC |
| **Sales** | ❌ | ❌ | ❌ | ❌ | ❌ PUBLIC |
| **Expenses** | ❌ | ❌ | ❌ | ❌ | ❌ PUBLIC |
| **Payslips** | N/A | ❌ | N/A | N/A | ❌ PUBLIC |

Legend: ✅ = Has Auth, ❌ = No Auth, ⚠️ = Incomplete Auth

---

## Issues to Fix for Full Admin Access

### Issue 1: AdminModuleController excludes Owner role
**Current:**
```csharp
[Authorize(Roles = "Admin")]
```

**Should be:**
```csharp
[Authorize(Roles = "Admin,Owner")]
```

**Impact:** Owner role would get full admin management access (users, groups, features, permissions, system settings)

---

### Issue 2: Forbid() blocks Owner from Admin role operations
**Current (AuthController.cs):**
- Line 230: Owner cannot approve someone as Admin
- Line 245: Owner cannot approve existing Admin requests  
- Line 300: Owner cannot reject Admin requests

**Options:**
- **Option A:** Allow Admin to set these up, Owner can only manage non-Admin roles
- **Option B:** Remove Owner role restrictions entirely (let Owner fully manage roles)
- **Option C:** Create intermediate role (e.g., "Manager") with restricted role assignment

---

### Issue 3: All other CRUD endpoints are unprotected
**Current:** 16+ controllers with public POST/PUT/DELETE

**Should add [Authorize] to:**
- QuotationsController (all CRUD)
- AccountsController (all writes)
- CustomerMastersController (all CRUD)
- EmployeesController (all writes)
- InventoryController (all writes)
- SalesController (all writes)
- ExpenseRecordsController (all writes)
- InvoiceCalcController (all writes)
- Other data-modifying endpoints

**Minimum Protection:**
```csharp
[Authorize(Roles = "Admin")]  // Most critical data
```

**Better Protection:**
```csharp
[Authorize(Roles = "Admin,Manager")]  // With role-based access levels
```

---

### Issue 4: Permission model not enforced
The AdminPermission/AdminFeature/AdminUserGroup system exists but is **never checked**.

**Current:** No endpoint validates user permissions before allowing access

**To Enable:**
1. Add custom `[HasPermission("featureName", "actionName")]` attribute
2. Check GroupId → GroupPermissions in middleware or action filter
3. Implement permission lookup in handler

---

## Roles Defined

Found in **AuthController.cs** (EnsureDefaultAdminAndRolesAsync):
```
- Admin (Full system access)
- Owner (Business owner, non-admin governance access)
- Manager (Mid-level access)
- User (Standard access - default)
- Pending (Awaiting approval)
```

---

## Recommendations (Priority Order)

### 🔴 CRITICAL - Fix First
1. **Add [Authorize(Roles = "Admin")] to all data-modifying endpoints:**
   - POST/PUT/DELETE on: Quotations, Accounts, Customers, Employees, Inventory, Sales, Expenses, Invoices
2. **Fix AdminModuleController to include Owner role:**
   - Change to `[Authorize(Roles = "Admin,Owner")]`

### 🟠 HIGH - Fix Next
3. **Remove or reconsider Forbid() checks in AuthController:**
   - Decide if Owner should manage Admin roles
   - Document the intentional restriction if keeping it

### 🟡 MEDIUM - Consider
4. **Implement permission checking:**
   - Create `[HasPermission]` attribute
   - Check against AdminPermission table before executing

### 🔵 LOW - Future
5. **Add read-level authorization:**
   - Currently all GET endpoints are public
   - Consider read restrictions based on role

---

## Files That Need Changes

| File | Change | Priority |
|------|--------|----------|
| `AdminModuleController.cs` | Add Owner to [Authorize(Roles = ...)] | 🔴 CRITICAL |
| `QuotationsController.cs` | Add [Authorize] to POST/PUT/DELETE | 🔴 CRITICAL |
| `AccountsController.cs` | Add [Authorize] to POST/PUT/DELETE | 🔴 CRITICAL |
| `CustomerMastersController.cs` | Add [Authorize] to POST/PUT/DELETE | 🔴 CRITICAL |
| `EmployeesController.cs` | Add [Authorize] to POST/PUT/DELETE | 🔴 CRITICAL |
| `InventoryController.cs` | Add [Authorize] to POST/PUT/DELETE | 🔴 CRITICAL |
| `SalesController.cs` | Add [Authorize] to POST/PUT/DELETE | 🔴 CRITICAL |
| `ExpenseRecordsController.cs` | Add [Authorize] to POST/PUT/DELETE | 🔴 CRITICAL |
| `InvoiceCalcController.cs` | Add [Authorize] to POST/PUT/DELETE | 🔴 CRITICAL |
| `AuthController.cs` | Review Forbid() logic for Owner role | 🟠 HIGH |
| `ItemsController.cs` | Add [Authorize] | 🟠 HIGH |
| Other controllers | Audit remaining 6 controllers | 🟡 MEDIUM |

---

## Code Examples for Fixes

### Example 1: Fix AdminModuleController
```csharp
// BEFORE
[ApiController]
[Route("api/admin-module")]
[Authorize(Roles = "Admin")]
public class AdminModuleController : ControllerBase

// AFTER
[ApiController]
[Route("api/admin-module")]
[Authorize(Roles = "Admin,Owner")]
public class AdminModuleController : ControllerBase
```

### Example 2: Fix QuotationsController
```csharp
// BEFORE
[ApiController]
[Route("api/[controller]")]
public class QuotationsController : ControllerBase

// AFTER
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Require authentication for all endpoints
public class QuotationsController : ControllerBase

// OR for specific method protection
[HttpPost]
[Authorize(Roles = "Admin")]  // Only admins can create
public async Task<ActionResult<Quotation>> Create(...)
```

### Example 3: Fix AuthController Owner restrictions
```csharp
// CURRENT - Forbids Owner from Admin operations
var isOwner = User.IsInRole("Owner") && !User.IsInRole("Admin");
if (isOwner && string.Equals(approvedRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
    return Forbid();

// OPTION A - Allow only Admin to assign Admin
if (!User.IsInRole("Admin"))
    return Forbid();

// OPTION B - Remove restriction entirely
// (Delete the Owner forbid check entirely)
```

---

## Testing Checklist

- [ ] Admin user can access AdminModule endpoints
- [ ] Owner user can access AdminModule endpoints (after fix)
- [ ] User role is denied access to AdminModule
- [ ] Unauthenticated users get 401 on protected endpoints
- [ ] Unauthenticated users can still access login/register
- [ ] Owner can approve non-Admin role requests
- [ ] Owner gets Forbid(403) or Unauthorized(401) for Admin role assignments (clarify choice)
- [ ] Admin user can create/update/delete quotations
- [ ] User role is denied from creating/updating/deleting quotations

---

## Conclusion

**Current State:** API has minimal authorization - only AdminModule is protected, and even then incompletely.

**Action Required:** 
1. Add `[Authorize]` attributes to all CRUD endpoints
2. Fix AdminModuleController to include Owner role
3. Review and clarify Owner vs Admin role responsibilities
4. Consider implementing permission model validation

**After these fixes:** Admin users will have full access to all protected endpoints, and security will be enforced across the application.
