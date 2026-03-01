using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class AdminRepository : IAdminRepository
    {
        private readonly IDbHelper _dbHelper;

        public AdminRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<LoginResponse> AuthenticateUser(string username, string password)
        {
            try
            {
                var query = @"
                    SELECT user_id, user_name 
                    FROM admin_users 
                    WHERE username = @username 
                    AND password_hash = @password
                    AND is_active = 'Y'
                    LIMIT 1";

                var parameters = new[]
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@password", password)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                if (result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    return new LoginResponse
                    {
                        Success = true,
                        Message = "Login successful",
                        UserId = row["user_id"]?.ToString(),
                        UserName = row["user_name"]?.ToString()
                    };
                }

                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Login error: {ex.Message}"
                };
            }
        }

        public async Task<ContentMasterResponse> GetAllContents(string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        cm_code,
                        cm_content,
                        cm_created_user,
                        cm_created_date,
                        cm_modified_user,
                        cm_modified_date,
                        cm_active
                    FROM content_mstr
                    ORDER BY cm_created_date DESC";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var contents = new List<ContentMaster>();
                foreach (DataRow row in result.Rows)
                {
                    contents.Add(MapToContentMaster(row));
                }

                return new ContentMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Contents retrieved successfully",
                    Data = contents
                };
            }
            catch (Exception ex)
            {
                return new ContentMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving contents: {ex.Message}",
                    Data = new List<ContentMaster>()
                };
            }
        }

        public async Task<ContentMasterResponse> GetContentByCode(string cmCode, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        cm_code,
                        cm_content,
                        cm_created_user,
                        cm_created_date,
                        cm_modified_user,
                        cm_modified_date,
                        cm_active
                    FROM content_mstr
                    WHERE cm_code = @cmCode
                    LIMIT 1";

                var parameters = new[]
                {
                    new MySqlParameter("@cmCode", cmCode)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var contents = new List<ContentMaster>();
                if (result.Rows.Count > 0)
                {
                    contents.Add(MapToContentMaster(result.Rows[0]));
                }

                return new ContentMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = result.Rows.Count > 0 ? "Content retrieved successfully" : "Content not found",
                    Data = contents
                };
            }
            catch (Exception ex)
            {
                return new ContentMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving content: {ex.Message}",
                    Data = new List<ContentMaster>()
                };
            }
        }

        public async Task<ContentMasterResponse> InsertContent(ContentMaster content, string userId)
        {
            try
            {
                var checkQuery = "SELECT COUNT(*) as cnt FROM content_mstr WHERE cm_code = @cmCode";
                var checkParams = new[] { new MySqlParameter("@cmCode", content.CmCode) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (checkResult.Rows.Count > 0 && Convert.ToInt32(checkResult.Rows[0]["cnt"]) > 0)
                {
                    return new ContentMasterResponse { OutputCode = 0, OutputMsg = "Content code already exists" };
                }

                var query = @"
                    INSERT INTO content_mstr (cm_code, cm_content, cm_created_user, cm_created_date, cm_active)
                    VALUES (@cmCode, @cmContent, @userId, UTC_TIMESTAMP(), @cmActive)";

                var parameters = new[]
                {
                    new MySqlParameter("@cmCode",    content.CmCode),
                    new MySqlParameter("@cmContent", (object?)content.CmContent ?? DBNull.Value),
                    new MySqlParameter("@userId",    userId),
                    new MySqlParameter("@cmActive",  content.CmActive ?? "Y")
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));
                return new ContentMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg  = rowsAffected > 0 ? "Content inserted successfully" : "Insert failed"
                };
            }
            catch (Exception ex)
            {
                return new ContentMasterResponse { OutputCode = 0, OutputMsg = $"Error inserting content: {ex.Message}" };
            }
        }

        public async Task<ContentMasterResponse> UpdateContent(ContentMaster content, string userId)
        {
            try
            {
                var checkQuery  = "SELECT COUNT(*) as cnt FROM content_mstr WHERE cm_code = @cmCode";
                var checkParams = new[] { new MySqlParameter("@cmCode", content.CmCode) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (checkResult.Rows.Count == 0 || Convert.ToInt32(checkResult.Rows[0]["cnt"]) == 0)
                    return new ContentMasterResponse { OutputCode = 0, OutputMsg = "Content not found" };

                var query = @"
                    UPDATE content_mstr
                    SET cm_content = @cmContent, cm_modified_user = @userId,
                        cm_modified_date = UTC_TIMESTAMP(), cm_active = @cmActive
                    WHERE cm_code = @cmCode";

                var parameters = new[]
                {
                    new MySqlParameter("@cmCode",    content.CmCode),
                    new MySqlParameter("@cmContent", (object?)content.CmContent ?? DBNull.Value),
                    new MySqlParameter("@userId",    userId),
                    new MySqlParameter("@cmActive",  content.CmActive ?? "Y")
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));
                return new ContentMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg  = rowsAffected > 0 ? "Content updated successfully" : "Update failed"
                };
            }
            catch (Exception ex)
            {
                return new ContentMasterResponse { OutputCode = 0, OutputMsg = $"Error updating content: {ex.Message}" };
            }
        }

        public async Task<ContentMasterResponse> DeleteContent(string cmCode, string userId)
        {
            try
            {
                var checkQuery  = "SELECT COUNT(*) as cnt FROM content_mstr WHERE cm_code = @cmCode";
                var checkParams = new[] { new MySqlParameter("@cmCode", cmCode) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (checkResult.Rows.Count == 0 || Convert.ToInt32(checkResult.Rows[0]["cnt"]) == 0)
                    return new ContentMasterResponse { OutputCode = 0, OutputMsg = "Content not found" };

                var query = @"
                    UPDATE content_mstr
                    SET cm_active = 'N', cm_modified_user = @userId, cm_modified_date = UTC_TIMESTAMP()
                    WHERE cm_code = @cmCode";

                var parameters = new[]
                {
                    new MySqlParameter("@cmCode",  cmCode),
                    new MySqlParameter("@userId",  userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));
                return new ContentMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg  = rowsAffected > 0 ? "Content deleted successfully" : "Delete failed"
                };
            }
            catch (Exception ex)
            {
                return new ContentMasterResponse { OutputCode = 0, OutputMsg = $"Error deleting content: {ex.Message}" };
            }
        }

        private ContentMaster MapToContentMaster(DataRow row)
        {
            return new ContentMaster
            {
                CmCode = row["cm_code"]?.ToString(),
                CmContent = row["cm_content"]?.ToString(),
                CmCreatedUser = row["cm_created_user"]?.ToString(),
                CmCreatedDate = row["cm_created_date"] != DBNull.Value ? (DateTime?)row["cm_created_date"] : null,
                CmModifiedUser = row["cm_modified_user"]?.ToString(),
                CmModifiedDate = row["cm_modified_date"] != DBNull.Value ? (DateTime?)row["cm_modified_date"] : null,
                CmActive = row["cm_active"]?.ToString()
            };
        }
    }
}