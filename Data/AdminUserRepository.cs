using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly IDbHelper _dbHelper;

        public AdminUserRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<AdminUserResponse> GetAllAdminUsers(string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        user_id, username, user_name, email, user_role, is_active, last_login
                    FROM admin_users
                    ORDER BY user_id DESC";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));
                var users = new List<AdminUser>();

                foreach (DataRow row in result.Rows)
                {
                    users.Add(MapToAdminUser(row));
                }

                return new AdminUserResponse
                {
                    OutputCode = 1,
                    OutputMsg = $"Retrieved {users.Count} admin users successfully",
                    Data = users
                };
            }
            catch (Exception ex)
            {
                return new AdminUserResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving admin users: {ex.Message}",
                    Data = new List<AdminUser>()
                };
            }
        }

        public async Task<AdminUserDetailResponse> GetAdminUserById(string auId, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        user_id, username, user_name, email, user_role, is_active, last_login
                    FROM admin_users
                    WHERE user_id = @userId
                    LIMIT 1";

                var parameters = new[] { new MySqlParameter("@userId", auId) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                if (result.Rows.Count > 0)
                {
                    return new AdminUserDetailResponse
                    {
                        OutputCode = 1,
                        OutputMsg = "Admin user retrieved successfully",
                        Data = MapToAdminUser(result.Rows[0])
                    };
                }

                return new AdminUserDetailResponse
                {
                    OutputCode = 0,
                    OutputMsg = "Admin user not found",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new AdminUserDetailResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving admin user: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<AdminUserDetailResponse> GetAdminUserByUsername(string username, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        user_id, username, user_name, email, user_role, is_active, last_login
                    FROM admin_users
                    WHERE username = @username
                    LIMIT 1";

                var parameters = new[] { new MySqlParameter("@username", username) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                if (result.Rows.Count > 0)
                {
                    return new AdminUserDetailResponse
                    {
                        OutputCode = 1,
                        OutputMsg = "Admin user retrieved successfully",
                        Data = MapToAdminUser(result.Rows[0])
                    };
                }

                return new AdminUserDetailResponse
                {
                    OutputCode = 0,
                    OutputMsg = "Admin user not found",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new AdminUserDetailResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving admin user: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<AdminUserResponse> InsertAdminUser(AdminUser user, string userId)
        {
            try
            {
                var checkQuery = "SELECT COUNT(*) as cnt FROM admin_users WHERE username = @username";
                var checkParams = new[] { new MySqlParameter("@username", user.AuUsername) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (checkResult.Rows.Count > 0 && Convert.ToInt32(checkResult.Rows[0]["cnt"]) > 0)
                {
                    return new AdminUserResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Username already exists",
                        Data = new List<AdminUser>()
                    };
                }

                var query = @"
                    INSERT INTO admin_users (username, password_hash, user_name, email, user_role, is_active)
                    VALUES (@username, @passwordHash, @userName, @email, @role, @active)";

                var parameters = new[]
                {
                    new MySqlParameter("@username", user.AuUsername),
                    new MySqlParameter("@passwordHash", user.AuPasswordHash ?? ""),
                    new MySqlParameter("@userName", user.AuUserName ?? ""),
                    new MySqlParameter("@email", user.AuEmail ?? ""),
                    new MySqlParameter("@role", user.AuRole ?? "Operator"),
                    new MySqlParameter("@active", user.AuActive ?? "Y")
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new AdminUserResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Admin user created successfully" : "Failed to create admin user",
                    Data = new List<AdminUser>()
                };
            }
            catch (Exception ex)
            {
                return new AdminUserResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error creating admin user: {ex.Message}",
                    Data = new List<AdminUser>()
                };
            }
        }

        public async Task<AdminUserResponse> UpdateAdminUser(AdminUser user, string userId)
        {
            try
            {
                var query = @"
                    UPDATE admin_users
                    SET user_name = @userName, email = @email, user_role = @role, is_active = @active
                    WHERE user_id = @userId";

                var parameters = new[]
                {
                    new MySqlParameter("@userId", user.AuId),
                    new MySqlParameter("@userName", user.AuUserName ?? ""),
                    new MySqlParameter("@email", user.AuEmail ?? ""),
                    new MySqlParameter("@role", user.AuRole ?? "Operator"),
                    new MySqlParameter("@active", user.AuActive ?? "Y")
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new AdminUserResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Admin user updated successfully" : "Failed to update admin user",
                    Data = new List<AdminUser>()
                };
            }
            catch (Exception ex)
            {
                return new AdminUserResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error updating admin user: {ex.Message}",
                    Data = new List<AdminUser>()
                };
            }
        }

        public async Task<AdminUserResponse> DeleteAdminUser(string auId, string userId)
        {
            try
            {   
                var query = @"
                    UPDATE admin_users
                    SET is_active = 'N'
                    WHERE user_id = @userId";

                var parameters = new[] { new MySqlParameter("@userId", auId) };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new AdminUserResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Admin user deleted successfully" : "Failed to delete admin user",
                    Data = new List<AdminUser>()
                };
            }
            catch (Exception ex)
            {
                return new AdminUserResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error deleting admin user: {ex.Message}",
                    Data = new List<AdminUser>()
                };
            }
        }

        public async Task<AdminUserResponse> ChangePassword(string auId, string newPasswordHash, string userId)
        {
            try
            {
                var query = @"
                    UPDATE admin_users
                    SET password_hash = @passwordHash
                    WHERE user_id = @userId";

                var parameters = new[]
                {
                    new MySqlParameter("@userId", auId),
                    new MySqlParameter("@passwordHash", newPasswordHash)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new AdminUserResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Password changed successfully" : "Failed to change password",
                    Data = new List<AdminUser>()
                };
            }
            catch (Exception ex)
            {
                return new AdminUserResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error changing password: {ex.Message}",
                    Data = new List<AdminUser>()
                };
            }
        }

        private AdminUser MapToAdminUser(DataRow row)
        {
            return new AdminUser
            {
                AuId = row["user_id"]?.ToString() ?? "",
                AuUsername = row["username"]?.ToString() ?? "",
                AuUserName = row["user_name"]?.ToString() ?? "",
                AuEmail = row["email"]?.ToString() ?? "",
                AuRole = row["user_role"]?.ToString() ?? "Operator",
                AuActive = row["is_active"]?.ToString() ?? "Y",
                AuCreatedDate = null,
                AuCreatedUser = "",
                AuModifiedDate = null,
                AuModifiedUser = "",
                AuLastLogin = row["last_login"] != DBNull.Value ? (DateTime?)row["last_login"] : null
            };
        }
    }
}
