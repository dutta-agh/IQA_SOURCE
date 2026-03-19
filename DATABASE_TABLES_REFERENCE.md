# Database Tables - Admin Menus & Role Access

## Quick Execution Guide

### Run the SQL Script
Execute `Database/Scripts/01_CreateMenuStructure.sql` in your MySQL database:

```bash
mysql -h <host> -u <user> -p <database> < Database/Scripts/01_CreateMenuStructure.sql
```

Or paste the script directly in MySQL Workbench/PhpMyAdmin.

---

## Table Structure

### 1. admin_menus Table
**Purpose:** Stores all admin menu items

| Column | Type | Description |
|--------|------|-------------|
| am_id | INT | Primary Key, Auto-increment |
| am_code | VARCHAR(100) | Unique menu code (e.g., 'dashboard') |
| am_name | VARCHAR(150) | Display name (e.g., 'Dashboard') |
| am_icon | VARCHAR(100) | Font Awesome icon class |
| am_controller | VARCHAR(100) | Controller name for navigation |
| am_action | VARCHAR(100) | Action/method name |
| am_order_no | INT | Menu display order |
| am_parent_id | INT | Parent menu ID (for hierarchical menus) |
| am_menu_type | VARCHAR(20) | 'page', 'divider', 'section' |
| am_active | CHAR(1) | Y/N flag |
| am_created_date | TIMESTAMP | Created timestamp |
| am_created_user | VARCHAR(100) | User who created |
| am_modified_date | TIMESTAMP | Last modified timestamp |
| am_modified_user | VARCHAR(100) | User who last modified |

### 2. menu_role_access Table
**Purpose:** Stores role-based access permissions for menus

| Column | Type | Description |
|--------|------|-------------|
| mra_id | INT | Primary Key, Auto-increment |
| mra_menu_id | INT | Foreign Key ? admin_menus.am_id |
| mra_role | VARCHAR(50) | Role code (Admin, Manager, Operator) |
| mra_can_read | CHAR(1) | Y/N - Can view menu |
| mra_can_create | CHAR(1) | Y/N - Can create items |
| mra_can_edit | CHAR(1) | Y/N - Can edit items |
| mra_can_delete | CHAR(1) | Y/N - Can delete items |
| mra_created_date | TIMESTAMP | Creation timestamp |

---

## Sample Data Inserted

### Menus (16 items)
1. Dashboard (page)
2. Content Master (page)
3. Assessment Types (page)
4. Question Master (page)
5. Divider 1 (divider)
6. Images Upload (page)
7. Image Groups (page)
8. Assessment Images (page)
9. Divider 2 (divider)
10. Question Answers (page)
11. Speed Test Logs (page)
12. Divider 3 (divider)
13. System Check Params (page)
14. Divider 4 (divider)
15. Bulk Operations (page)
16. Admin Users (page)
17. Admin Menus (page)

### Roles & Permissions

**Admin Role**
- Access: All menus (except dividers)
- Permissions: Read ? Create ? Edit ? Delete ?

**Manager Role**
- Access: All menus except (Admin Users, Admin Menus, Bulk Operations)
- Permissions: Read ? Create ? Edit ? Delete ?

**Operator Role**
- Access: Dashboard, Images Master, Image Groups, Assessment Images, Question Answers, Speed Test Logs
- Permissions: Read ? Create ? Edit ? Delete ?

---

## Code Integration

### The code expects these column names:

**AdminMenuRepository.cs queries use:**
```csharp
am_id, am_code, am_name, am_icon, am_controller, am_action,
am_order_no, am_parent_id, am_active, am_menu_type,
am_created_date, am_created_user, am_modified_date, am_modified_user
```

**menu_role_access columns:**
```csharp
mra_id, mra_menu_id, mra_role, mra_can_read, mra_can_create,
mra_can_edit, mra_can_delete, mra_created_date
```

---

## Verification Queries

### View all menus:
```sql
SELECT am_id, am_code, am_name, am_order_no, am_menu_type, am_active
FROM admin_menus
ORDER BY am_order_no;
```

### View access summary:
```sql
SELECT 
    mra_role,
    COUNT(DISTINCT mra_menu_id) AS accessible_menus,
    SUM(IF(mra_can_read = 'Y', 1, 0)) AS can_read,
    SUM(IF(mra_can_create = 'Y', 1, 0)) AS can_create,
    SUM(IF(mra_can_edit = 'Y', 1, 0)) AS can_edit,
    SUM(IF(mra_can_delete = 'Y', 1, 0)) AS can_delete
FROM menu_role_access
GROUP BY mra_role
ORDER BY mra_role;
```

### View specific role access:
```sql
SELECT am.am_name, mra.mra_can_read, mra.mra_can_create, mra.mra_can_edit, mra.mra_can_delete
FROM menu_role_access mra
JOIN admin_menus am ON mra.mra_menu_id = am.am_id
WHERE mra.mra_role = 'Manager'
ORDER BY am.am_order_no;
```

---

## Important Notes

? **Constraints Applied:**
- Primary Keys on am_id and mra_id
- Foreign Key: mra_menu_id ? admin_menus.am_id
- Self-referencing Foreign Key: am_parent_id ? admin_menus.am_id
- Unique constraints to prevent duplicates
- ON DELETE CASCADE for menu_role_access when menu is deleted
- ON DELETE SET NULL for hierarchical menu structure

? **Indexes for Performance:**
- am_code (unique)
- am_order_no (for sorting)
- am_active (for filtering)
- mra_role (for role lookups)
- mra_menu_id (for joins)

? **Character Set:** UTF-8mb4 (supports emoji and special characters)

---

**Script Location:** `Database/Scripts/01_CreateMenuStructure.sql`  
**Status:** ? Ready to execute
