using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IAdminMenuRepository
    {
        Task<AdminMenuResponse> GetAllAdminMenus(string userId);
        Task<AdminMenuResponse> GetMenusByRole(string role, string userId);
        Task<AdminMenuDetailResponse> GetAdminMenuById(int amId, string userId);
        Task<AdminMenuResponse> InsertAdminMenu(AdminMenu menu, string userId);
        Task<AdminMenuResponse> UpdateAdminMenu(AdminMenu menu, string userId);
        Task<AdminMenuResponse> DeleteAdminMenu(int amId, string userId);
        
        // Role Access Management
        Task<MenuRoleAccessResponse> GetMenuRoleAccess(int menuId, string userId);
        Task<MenuRoleAccessResponse> SaveMenuRoleAccess(List<MenuRoleAccess> roleAccess, string userId);
        
        // Get menus by user role for sidebar rendering
        Task<List<AdminMenu>> GetMenusByUserRole(string userRole);
    }

    public class AdminMenuDetailResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public AdminMenu Data { get; set; }
    }

    public class UserMenuResponse
    {
        public int OutputCode { get; set; }
        public string OutputMsg { get; set; } = string.Empty;
        public List<AdminMenu> Data { get; set; } = new();
    }
}
