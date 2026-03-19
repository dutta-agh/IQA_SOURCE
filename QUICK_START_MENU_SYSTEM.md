# Quick Start Guide - Dynamic Menu System

## ?? Implementation Checklist

### ? Already Done
- [x] Database schema created with all tables
- [x] All current menus migrated to database  
- [x] Role-based access control configured
- [x] Backend API endpoints implemented
- [x] Dynamic sidebar component created
- [x] New dynamic layout created
- [x] Build verification successful

### ?? To Do

#### 1. Execute Database Script (ONE TIME)
```bash
# Connect to your MySQL database and run:
mysql -h YOUR_HOST -u YOUR_USER -p YOUR_DATABASE < Database/Scripts/01_CreateMenuStructure.sql
```

**What it does:**
- Creates 3 new tables: `admin_roles`, `admin_menus`, `menu_role_access`
- Inserts all current menus
- Sets up role-based permissions
- Creates 4 default roles: Admin, Manager, Operator, Viewer

#### 2. Update Login to Store User Role
**File:** `Data/AdminRepository.cs`  
**Method:** `AuthenticateUser()`

Add after login success:
```csharp
// Store user role in session
var userRole = row["au_role"]?.ToString() ?? "Operator";
HttpContext.Session.SetString("UserRole", userRole);
```

Also update LoginResponse model to include role.

#### 3. Update Admin Views Layout
**Option A - Individual Files (Gradual Migration)**
In each admin view, change:
```razor
@{ Layout = "_AdminLayout"; }
```
To:
```razor
@{ Layout = "_AdminLayoutDynamic"; }
```

**Option B - Global (_ViewStart.cshtml)**
```razor
@{
    if (ViewContext.RouteData.Values["controller"]?.ToString() == "Admin")
    {
        Layout = "_AdminLayoutDynamic";
    }
}
```

#### 4. Test the System
**Admin User Test:**
- Login as Admin user (role = Admin)
- Should see all menus
- Check browser console - no errors

**Manager User Test:**
- Create a Manager user  
- Login with Manager credentials
- Should NOT see: Admin Users, Admin Menus, Bulk Operations
- Should see: Dashboard, Content, Assessment, Images, Q&A, Speed Tests

**Operator User Test:**
- Create an Operator user
- Should only see limited menus with no delete permissions

## ?? Verify Setup

### Check Database Tables
```sql
-- Verify tables exist
SELECT * FROM admin_roles;
SELECT * FROM admin_menus ORDER BY menu_order;
SELECT * FROM menu_role_access LIMIT 5;

-- Count menus per role
SELECT role_name, COUNT(*) as menu_count
FROM admin_roles
LEFT JOIN menu_role_access ON admin_roles.role_code = menu_role_access.role_code
GROUP BY role_name;
```

### Test API Endpoint
```bash
# While logged in as a user, call:
curl -X GET http://localhost:YOUR_PORT/Admin/GetUserMenus

# Should return JSON like:
{
  "success": true,
  "message": "Retrieved X menus for role Manager",
  "data": [
    { "menuId": 1, "menuName": "Dashboard", ... },
    { "menuId": 2, "menuName": "Content Master", ... }
  ]
}
```

## ?? Files Reference

### View Files
- **New Dynamic Layout:** `Views/Shared/_AdminLayoutDynamic.cshtml`
- **Dynamic Sidebar:** `Views/Shared/_AdminSidebar.cshtml`
- **Old Layout (Keep for reference):** `Views/Shared/_AdminLayout.cshtml`

### Database Files
- **Setup Script:** `Database/Scripts/01_CreateMenuStructure.sql`
- **Documentation:** `Database/MENU_SYSTEM_README.md`

### Code Files
- **Interface:** `Data/IAdminMenuRepository.cs`
- **Repository:** `Data/AdminMenuRepository.cs`
- **Controller:** `Controllers/AdminController.cs`

## ?? Common Issues & Solutions

### Issue: "Menus not loading"
**Solution:**
1. Open browser DevTools (F12)
2. Check Console tab for JavaScript errors
3. Check Network tab - is `/Admin/GetUserMenus` API call successful?
4. Verify user role is in session: Add console.log in _AdminSidebar.cshtml

### Issue: "User can't see any menus"
**Solution:**
1. Check user role is stored in session: `HttpContext.Session.GetString("UserRole")`
2. Verify in database: `SELECT * FROM menu_role_access WHERE role_code = 'YourRole'`
3. Check if `can_read = 'Y'` for that role

### Issue: "Getting SQL errors"
**Solution:**
1. Column names are case-sensitive on Linux servers
2. Verify table names: `menu_id` vs `am_id` (script uses snake_case)
3. Check column names in your queries

## ?? Tips

1. **Cache Menus:** For production, implement caching:
```csharp
var cacheKey = $"user_menus_{userRole}";
if (!cache.TryGetValue(cacheKey, out var menus))
{
    menus = await _menuRepository.GetMenusByUserRole(userRole);
    cache.Set(cacheKey, menus, TimeSpan.FromHours(1));
}
```

2. **Add New Menus:** Use SQL directly:
```sql
INSERT INTO admin_menus (menu_code, menu_name, menu_icon, ...)
VALUES ('my_feature', 'My Feature', 'fas fa-star', ...);

INSERT INTO menu_role_access (menu_id, role_code, can_read, ...)
SELECT menu_id, 'Admin', 'Y' FROM admin_menus WHERE menu_code = 'my_feature';
```

3. **Debug Menus:** Add this to _AdminSidebar.cshtml after loading:
```javascript
console.log('User Role:', '@UserRole');
console.log('Loaded Menus:', response.data);
```

## ?? Support

For detailed documentation, see: `Database/MENU_SYSTEM_README.md`

---

**Next Step:** Execute the database script and test!
