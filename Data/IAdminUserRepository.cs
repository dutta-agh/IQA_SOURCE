using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IAdminUserRepository
    {
        Task<AdminUserResponse> GetAllAdminUsers(string userId);
        Task<AdminUserDetailResponse> GetAdminUserById(string auId, string userId);
        Task<AdminUserDetailResponse> GetAdminUserByUsername(string username, string userId);
        Task<AdminUserResponse> InsertAdminUser(AdminUser user, string userId);
        Task<AdminUserResponse> UpdateAdminUser(AdminUser user, string userId);
        Task<AdminUserResponse> DeleteAdminUser(string auId, string userId);
        Task<AdminUserResponse> ChangePassword(string auId, string newPasswordHash, string userId);
    }
}
