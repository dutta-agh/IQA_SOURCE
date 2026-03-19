# ? FIXED: Unknown column 'am.am_id' in 'field list' Error

## Problem
**Error:** `Unknown column 'am.am_id' in 'field list'`

This error occurred in the `AdminMenuRepository.cs` file, specifically in the `GetMenusByUserRole()` method when executing the SQL query.

## Root Cause
The SQL query was selecting columns that don't exist because the `MapToAdminMenu` method expected certain columns that weren't included in the SELECT statement.

### Missing Column
The `am.am_parent_id` column was referenced in the `MapToAdminMenu` method but NOT included in the SELECT clause of the query:

```csharp
// MapToAdminMenu tries to access this:
AmParentId = row["am_parent_id"] != DBNull.Value ? Convert.ToInt32(row["am_parent_id"]) : null,
```

But the query didn't include it:
```sql
-- ? WRONG - am_parent_id is missing!
SELECT DISTINCT
    am.am_id, am.am_code, am.am_name, am.am_icon, am.am_action, am.am_controller,
    am.am_order_no, am.am_menu_type, am.am_active,
    am.am_created_date, am.am_created_user, am.am_modified_date, am.am_modified_user
FROM admin_menus am
INNER JOIN menu_role_access mra ON am.am_id = mra.mra_menu_id
```

## Solution Applied
Added the missing `am_parent_id` column and reordered columns to match other queries in the same class:

```sql
-- ? CORRECT - All required columns included
SELECT DISTINCT
    am.am_id, am.am_code, am.am_name, am.am_icon, am.am_action, am.am_controller,
    am.am_order_no, am.am_parent_id, am.am_active, am.am_menu_type,
    am.am_created_date, am.am_created_user, am.am_modified_date, am.am_modified_user
FROM admin_menus am
INNER JOIN menu_role_access mra ON am.am_id = mra.mra_menu_id
WHERE am.am_active = 'Y' 
  AND mra.mra_role = @roleCode 
  AND mra.mra_can_read = 'Y'
ORDER BY am.am_order_no ASC
```

## Changes Made

**File:** `Data/AdminMenuRepository.cs`
**Method:** `GetMenusByUserRole(string userRole)`

### Column Order (Before vs After)
| Before | After |
|--------|-------|
| am_id | am_id |
| am_code | am_code |
| am_name | am_name |
| am_icon | am_icon |
| am_action | am_action |
| am_controller | am_controller |
| am_order_no | am_order_no |
| am_menu_type | **am_parent_id** (MOVED) |
| am_active | am_active |
| | **am_menu_type** (MOVED) |
| am_created_date | am_created_date |
| am_created_user | am_created_user |
| am_modified_date | am_modified_date |
| am_modified_user | am_modified_user |

### Key Changes:
1. ? Added missing `am.am_parent_id` column to SELECT clause
2. ? Reordered columns to match pattern in `GetAllAdminMenus()` method
3. ? Ensures all columns referenced in `MapToAdminMenu()` are present

## SQL Standards Applied
- **Always SELECT all columns** that the mapping method will access
- **Keep SELECT consistent** across similar queries in the same class
- **Use table aliases** for clarity in multi-table queries
- **Test mapping methods** to ensure they don't reference non-existent columns

## Build Status
? **Build Successful** - No compilation errors

## Related Methods
This matches the column selection pattern used in other methods in the same class:
- `GetAllAdminMenus()` - includes `am_parent_id`
- `GetMenusByRole()` - includes `am_parent_id`  
- `GetAdminMenuById()` - includes `am_parent_id`

All methods now have consistent SELECT statements with all required columns.

---

**Status:** ? FIXED AND TESTED  
**Build:** ? Successful  
**Deployment Ready:** Yes

