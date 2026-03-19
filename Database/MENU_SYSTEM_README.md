# Dynamic Menu System Setup Guide

## Overview
This guide explains how to set up and use the dynamic menu management system for the IQA Admin panel. The system allows you to:

- Store menus in the database instead of hardcoding them in views
- Control menu access based on user roles
- Add, edit, and delete menus without code changes
- Assign different menu visibility and permissions to different user roles

## Database Setup

### Step 1: Run the Database Migration Script
Execute the SQL script to create the necessary database tables:

```bash
# On Windows (using MySQL Workbench or Command Line)
mysql -h localhost -u root -p your_database < Database/Scripts/01_CreateMenuStructure.sql
```

### Step 2: Verify Table Creation
The script creates three main tables:

1. **admin_roles** - User role definitions (Admin, Manager, Operator, Viewer)
2. **admin_menus** - Menu items and navigation structure
3. **menu_role_access** - Role-based access control for menus

All current menus from `_AdminLayout.cshtml` are automatically inserted with proper role assignments.

## Role-Based Access Control

### Default Roles and Permissions

| Role | Dashboard | Masters | Images | Reports | Settings | Admin | Notes |
|------|-----------|---------|--------|---------|----------|-------|-------|
| **Admin** | ? CRUD | ? CRUD | ? CRUD | ? CRUD | ? CRUD | ? CRUD | Full system access |
| **Manager** | ? CRUD | ? CRUD | ? CRUD | ? CRUD | ? R | ? | No admin features |
| **Operator** | ? CRE | ? - | ? CRE | ? CRE | ? | ? | Data entry only, no delete |
| **Viewer** | ? R | ? | ? R | ? R | ? | ? | Read-only access |

*Legend: C=Create, R=Read, E=Edit, D=Delete*

## Implementation Steps

### Step 1: Update Admin User Login
Modify the `AdminRepository.AuthenticateUser` method to store the user's role in the session:

```csharp
if (result.Rows.Count > 0)
{
    var row = result.Rows[0];
    var userRole = row["au_role"]?.ToString() ?? "Operator";
    
    // Store in session for later use
    HttpContext.Session.SetString("UserRole", userRole);
    
    return new LoginResponse
    {
        Success = true,
        Message = "Login successful",
        UserId = row["user_id"]?.ToString(),
        UserName = row["user_name"]?.ToString(),
        UserRole = userRole
    };
}
```

### Step 2: Update Layout Configuration
In `Program.cs`, ensure the repository is registered:

```csharp
// Already added during initialization
builder.Services.AddScoped<IAdminMenuRepository, AdminMenuRepository>();
```

### Step 3: Replace Layout in Views
Update your admin views to use the new dynamic layout:

**Option A: Update existing views (minimal change)**
```razor
@{
    Layout = "_AdminLayoutDynamic";
}
```

**Option B: Update _ViewStart.cshtml to use new layout for all admin views**
```razor
@{
    if (ViewContext.RouteData.Values["controller"]?.ToString() == "Admin")
    {
        Layout = "_AdminLayoutDynamic";
    }
}
```

### Step 4: Store User Role in Session
Update the Login method in `AdminController`:

```csharp
if (result.Success)
{
    HttpContext.Session.SetString("UserId", result.UserId);
    HttpContext.Session.SetString("UserName", result.UserName);
    HttpContext.Session.SetString("UserRole", result.UserRole ?? "Operator");
}
```

## API Endpoints

### Get User Menus
**Endpoint:** `GET /Admin/GetUserMenus`

**Response:**
```json
{
  "success": true,
  "message": "Retrieved X menus for role Manager",
  "data": [
    {
      "menuId": 1,
      "menuCode": "dashboard",
      "menuName": "Dashboard",
      "menuIcon": "fas fa-home",
      "menuController": "Admin",
      "menuAction": "Dashboard",
      "menuOrder": 10,
      "menuType": "page",
      "isActive": "Y"
    }
  ]
}
```

### Manage Menus (Admin Only)
- `GET /Admin/GetAllAdminMenus` - List all menus
- `POST /Admin/SaveAdminMenu` - Create/update menu
- `POST /Admin/DeleteAdminMenu` - Delete menu
- `GET /Admin/GetMenuRoleAccess?menuId=1` - Get role access for a menu
- `POST /Admin/SaveMenuRoleAccess` - Update role permissions

## Adding New Menus

### Via Database (SQL)
```sql
INSERT INTO admin_menus (menu_code, menu_name, menu_icon, menu_controller, menu_action, menu_order, menu_type, is_active)
VALUES ('new_feature', 'New Feature', 'fas fa-star', 'Admin', 'NewFeature', 140, 'page', 'Y');

-- Grant access to specific roles
INSERT INTO menu_role_access (menu_id, role_code, can_read, can_create, can_edit, can_delete)
VALUES (
    (SELECT menu_id FROM admin_menus WHERE menu_code = 'new_feature'),
    'Admin', 'Y', 'Y', 'Y', 'Y'
);

INSERT INTO menu_role_access (menu_id, role_code, can_read, can_create, can_edit, can_delete)
VALUES (
    (SELECT menu_id FROM admin_menus WHERE menu_code = 'new_feature'),
    'Manager', 'Y', 'Y', 'Y', 'N'
);
```

### Via Admin Interface
Use the "Admin Menus" page (accessible only to Admin users) to:
1. Navigate to Admin > Admin Menus
2. Click "Add New Menu"
3. Fill in menu details:
   - Menu Code (unique identifier)
   - Menu Name (display name)
   - Icon (Font Awesome class)
   - Controller & Action
   - Order (display sequence)
   - Menu Type
4. Save
5. Configure role access for each role

## Customizing Menu Structure

### Adding Menu Dividers
Menu dividers are used to visually group menu items. They're rendered automatically from the database:

```sql
-- Already created in the setup script
INSERT INTO admin_menus (menu_code, menu_name, menu_order, menu_type, is_active)
VALUES ('divider_5', 'divider_5', 145, 'divider', 'Y');
```

### Creating Menu Sections
Group related menus by using order numbers and dividers:

```
10: Dashboard
15-40: Masters Section (with divider)
50-70: Images Section (with divider)
80-90: Reports Section (with divider)
100: Settings
105: Admin Section (with divider)
```

### External Links
For external URLs (outside the application):

```sql
INSERT INTO admin_menus (menu_code, menu_name, menu_icon, menu_url, menu_order, menu_type, is_active)
VALUES ('documentation', 'Documentation', 'fas fa-book', 'https://docs.example.com', 150, 'external', 'Y');
```

## Security Considerations

### Admin-Only Access
1. **Menus table** - Only Admins can view/manage
2. **User Management** - Only Admins can create/edit users and assign roles
3. **Role Assignment** - Admin must explicitly grant menu access to each role

### Automatic Permission Check
When menus are loaded on the frontend, they're filtered by the user's role:

```javascript
// The API automatically filters by user's role
// Users cannot see menus they don't have access to
$.ajax({
    url: buildApiUrl('/Admin/GetUserMenus'),
    type: 'GET',
    success: function (response) {
        // Only menus user has 'can_read' permission are returned
        renderMenus(response.data);
    }
});
```

## Troubleshooting

### Menus Not Showing
1. Verify user is logged in and role is stored in session
2. Check that role has `can_read = 'Y'` in `menu_role_access` table
3. Verify menu has `is_active = 'Y'` in `admin_menus` table
4. Check browser console for JavaScript errors

### User Can't Access Menu
1. In Admin > Admin Menus, select the menu
2. Check role access permissions
3. Ensure the role has `can_read = 'Y'`
4. User may need to log out and log back in for changes to take effect

### New Roles Not Showing in Dropdown
1. Add role to `admin_roles` table:
```sql
INSERT INTO admin_roles (role_code, role_name, role_description, is_active)
VALUES ('Editor', 'Editor', 'Content editor role', 'Y');
```
2. Add menu access entries for the new role
3. Assign role to users via Admin > Admin Users

## View Files Location

- **New Dynamic Layout:** `Views\Shared\_AdminLayoutDynamic.cshtml`
- **Sidebar Partial:** `Views\Shared\_AdminSidebar.cshtml`
- **Old Hardcoded Layout:** `Views\Shared\_AdminLayout.cshtml` (kept for reference)

## Migration Path

### Phase 1: Setup (Current)
- ? Database tables created
- ? Current menus inserted
- ? Role-based access configured

### Phase 2: Implementation
- Update Admin login to store user role
- Replace layout in admin views
- Test menu visibility by role

### Phase 3: Migration
- Gradually migrate admin views to new layout
- Test each role's menu access
- Remove old hardcoded layout once all views migrated

### Phase 4: Enhancement
- Add new menus via Admin interface
- Fine-tune role permissions
- Add sub-menus/hierarchical structure if needed

## Database Backup

Before making changes, backup your database:

```sql
-- Export tables
SELECT * FROM admin_roles
INTO OUTFILE '/path/to/backup/admin_roles.csv'
FIELDS TERMINATED BY ',' 
ENCLOSED BY '"' 
LINES TERMINATED BY '\n';

-- Or use mysqldump
mysqldump -h localhost -u root -p your_database admin_roles admin_menus menu_role_access > backup_menus.sql
```

## Support

For issues or questions:
1. Check the database structure using: `DESCRIBE admin_menus;`
2. Verify user role: `SELECT * FROM admin_roles WHERE role_code = 'Admin';`
3. Check menu access: `SELECT * FROM menu_role_access WHERE role_code = 'Manager';`
4. Review browser console for JavaScript errors
