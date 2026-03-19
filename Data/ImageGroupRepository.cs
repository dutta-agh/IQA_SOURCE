using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class ImageGroupRepository : IImageGroupRepository
    {
        private readonly IDbHelper _dbHelper;

        public ImageGroupRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<ImageGroupResponse> GetAllImageGroups(string userId)
        {
            try
            {
                // ✅ FIXED: Removed ig_active filter to show ALL groups (active & inactive)
                var query = @"
                    SELECT 
                        ig_id, ig_code, ig_name, ig_description, ig_active, 
                        ig_is_current_active, ig_created_user, ig_created_date,
                        ig_modified_user, ig_modified_date, ig_assessment_type
                    FROM image_groups
                    ORDER BY ig_assessment_type, ig_is_current_active DESC, ig_active DESC, ig_created_date DESC";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var groups = new List<ImageGroup>();
                foreach (DataRow row in result.Rows)
                {
                    groups.Add(MapToImageGroup(row));
                }

                return new ImageGroupResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Groups retrieved successfully",
                    Data = groups
                };
            }
            catch (Exception ex)
            {
                return new ImageGroupResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving groups: {ex.Message}",
                    Data = new List<ImageGroup>()
                };
            }
        }

        public async Task<ImageGroupResponse> GetImageGroupByCode(string groupCode, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        ig_id, ig_code, ig_name, ig_description, ig_active, 
                        ig_is_current_active, ig_created_user, ig_created_date,
                        ig_modified_user, ig_modified_date, ig_assessment_type
                    FROM image_groups
                    WHERE ig_code = @groupCode AND ig_active = 'Y'
                    LIMIT 1";

                var parameters = new[]
                {
                    new MySqlParameter("@groupCode", groupCode)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var groups = new List<ImageGroup>();
                if (result.Rows.Count > 0)
                {
                    groups.Add(MapToImageGroup(result.Rows[0]));
                }

                return new ImageGroupResponse
                {
                    OutputCode = 1,
                    OutputMsg = result.Rows.Count > 0 ? "Group retrieved successfully" : "Group not found",
                    Data = groups
                };
            }
            catch (Exception ex)
            {
                return new ImageGroupResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving group: {ex.Message}",
                    Data = new List<ImageGroup>()
                };
            }
        }

        /// <summary>
        /// Get all image groups for a specific assessment type
        /// </summary>
        public async Task<ImageGroupResponse> GetImageGroupsByAssessment(string assessmentType, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        ig_id, ig_code, ig_name, ig_description, ig_active, 
                        ig_is_current_active, ig_created_user, ig_created_date,
                        ig_modified_user, ig_modified_date, ig_assessment_type
                    FROM image_groups
                    WHERE ig_assessment_type = @assessmentType AND ig_active = 'Y'
                    ORDER BY ig_is_current_active DESC, ig_created_date DESC";

                var parameters = new[]
                {
                    new MySqlParameter("@assessmentType", assessmentType)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var groups = new List<ImageGroup>();
                foreach (DataRow row in result.Rows)
                {
                    groups.Add(MapToImageGroup(row));
                }

                return new ImageGroupResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Groups retrieved successfully",
                    Data = groups
                };
            }
            catch (Exception ex)
            {
                return new ImageGroupResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving groups: {ex.Message}",
                    Data = new List<ImageGroup>()
                };
            }
        }

        public async Task<ImageGroupResponse> GetActiveImageGroup(string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        ig_id, ig_code, ig_name, ig_description, ig_active, 
                        ig_is_current_active, ig_created_user, ig_created_date,
                        ig_modified_user, ig_modified_date, ig_assessment_type
                    FROM image_groups
                    WHERE ig_is_current_active = 'Y' AND ig_active = 'Y'
                    LIMIT 1";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var groups = new List<ImageGroup>();
                if (result.Rows.Count > 0)
                {
                    groups.Add(MapToImageGroup(result.Rows[0]));
                }

                return new ImageGroupResponse
                {
                    OutputCode = 1,
                    OutputMsg = result.Rows.Count > 0 ? "Active group retrieved successfully" : "No active group found",
                    Data = groups
                };
            }
            catch (Exception ex)
            {
                return new ImageGroupResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving active group: {ex.Message}",
                    Data = new List<ImageGroup>()
                };
            }
        }

        public async Task<ImageGroupResponse> SaveImageGroup(ImageGroup group, string userId)
        {
            try
            {
                // If editing existing group, prevent code change
                if (group.IgId > 0)
                {
                    var checkQuery = "SELECT ig_code FROM image_groups WHERE ig_id = @id LIMIT 1";
                    var checkParams = new[] { new MySqlParameter("@id", group.IgId) };
                    var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));
                    if (checkResult.Rows.Count > 0)
                    {
                        var existingCode = checkResult.Rows[0]["ig_code"]?.ToString();
                        if (!string.Equals(existingCode, group.IgCode, StringComparison.Ordinal))
                        {
                            return new ImageGroupResponse
                            {
                                OutputCode = 0,
                                OutputMsg = "Group Code cannot be changed after creation"
                            };
                        }
                    }
                }

                if (group.IgId == 0)
                {
                    // Insert new group - check for duplicate within same assessment type
                    var checkQuery = @"
                        SELECT COUNT(*) as cnt 
                        FROM image_groups 
                        WHERE ig_code = @code 
                        AND ig_assessment_type = @assessmentType";
                    
                    var checkParams = new[] 
                    { 
                        new MySqlParameter("@code", group.IgCode),
                        new MySqlParameter("@assessmentType", group.IgAssessmentType ?? string.Empty)
                    };
                    
                    var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                    if (checkResult.Rows.Count > 0 && Convert.ToInt32(checkResult.Rows[0]["cnt"]) > 0)
                    {
                        return new ImageGroupResponse 
                        { 
                            OutputCode = 0, 
                            OutputMsg = "Group code already exists for this assessment type" 
                        };
                    }

                    var insertQuery = @"
                        INSERT INTO image_groups (ig_code, ig_name, ig_description, ig_active, 
                                                ig_is_current_active, ig_assessment_type, 
                                                ig_created_user, ig_created_date)
                        VALUES (@code, @name, @description, @active, @isCurrentActive, 
                                @assessmentType, @userId, UTC_TIMESTAMP())";

                    var insertParams = new[]
                    {
                        new MySqlParameter("@code", group.IgCode),
                        new MySqlParameter("@name", group.IgName),
                        new MySqlParameter("@description", (object?)group.IgDescription ?? DBNull.Value),
                        new MySqlParameter("@active", group.IgActive ?? "Y"),
                        new MySqlParameter("@isCurrentActive", group.IgIsCurrentActive ?? "N"),
                        new MySqlParameter("@assessmentType", group.IgAssessmentType ?? string.Empty),
                        new MySqlParameter("@userId", userId)
                    };

                    var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(insertQuery, insertParams));
                    return new ImageGroupResponse
                    {
                        OutputCode = rowsAffected > 0 ? 1 : 0,
                        OutputMsg = rowsAffected > 0 ? "Group created successfully" : "Insert failed"
                    };
                }
                else
                {
                    // Update existing group
                    var updateQuery = @"
                        UPDATE image_groups
                        SET ig_name = @name, ig_description = @description, ig_active = @active,
                            ig_modified_user = @userId, ig_modified_date = UTC_TIMESTAMP()
                        WHERE ig_id = @id";

                    var updateParams = new[]
                    {
                        new MySqlParameter("@id", group.IgId),
                        new MySqlParameter("@name", group.IgName),
                        new MySqlParameter("@description", (object?)group.IgDescription ?? DBNull.Value),
                        new MySqlParameter("@active", group.IgActive ?? "Y"),
                        new MySqlParameter("@userId", userId)
                    };

                    var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(updateQuery, updateParams));
                    return new ImageGroupResponse
                    {
                        OutputCode = rowsAffected > 0 ? 1 : 0,
                        OutputMsg = rowsAffected > 0 ? "Group updated successfully" : "Update failed"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ImageGroupResponse { OutputCode = 0, OutputMsg = $"Error saving group: {ex.Message}" };
            }
        }

        public async Task<ImageGroupResponse> SetActiveGroup(string groupCode, string userId)
        {
            try
            {
                // Get the assessment type of the group being activated
                var getAssessmentQuery = @"
                    SELECT ig_assessment_type 
                    FROM image_groups 
                    WHERE ig_code = @groupCode
                    LIMIT 1";
                
                var getParams = new[] { new MySqlParameter("@groupCode", groupCode) };
                var getResult = await Task.Run(() => _dbHelper.ExecuteQuery(getAssessmentQuery, getParams));
                
                if (getResult.Rows.Count == 0)
                {
                    return new ImageGroupResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Group not found"
                    };
                }

                var assessmentType = getResult.Rows[0]["ig_assessment_type"]?.ToString() ?? string.Empty;

                // ✅ CRITICAL FIX: Deactivate ALL other groups for the same assessment type
                // This ensures only ONE group is active per assessment type
                var deactivateQuery = @"
                    UPDATE image_groups 
                    SET ig_is_current_active = 'N'
                    WHERE ig_assessment_type = @assessmentType 
                    AND ig_is_current_active = 'Y'";
                
                var deactivateParams = new[] { new MySqlParameter("@assessmentType", assessmentType) };
                await Task.Run(() => _dbHelper.ExecuteNonQuery(deactivateQuery, deactivateParams));

                // Activate the specified group
                var activateQuery = @"
                    UPDATE image_groups 
                    SET ig_is_current_active = 'Y', ig_modified_user = @userId, ig_modified_date = UTC_TIMESTAMP()
                    WHERE ig_code = @groupCode";

                var activateParams = new[]
                {
                    new MySqlParameter("@groupCode", groupCode),
                    new MySqlParameter("@userId", userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(activateQuery, activateParams));

                return new ImageGroupResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 
                        ? $"Group activated successfully. Other groups for assessment type '{assessmentType}' have been deactivated." 
                        : "Group not found or already active"
                };
            }
            catch (Exception ex)
            {
                return new ImageGroupResponse { OutputCode = 0, OutputMsg = $"Error setting active group: {ex.Message}" };
            }
        }

        public async Task<ImageGroupResponse> DeleteImageGroup(string groupCode, string userId)
        {
            try
            {
                var query = @"
                    UPDATE image_groups
                    SET ig_active = 'N', ig_modified_user = @userId, ig_modified_date = UTC_TIMESTAMP()
                    WHERE ig_code = @groupCode";

                var parameters = new[]
                {
                    new MySqlParameter("@groupCode", groupCode),
                    new MySqlParameter("@userId", userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));
                return new ImageGroupResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Group deleted successfully" : "Delete failed"
                };
            }
            catch (Exception ex)
            {
                return new ImageGroupResponse { OutputCode = 0, OutputMsg = $"Error deleting group: {ex.Message}" };
            }
        }

        public async Task<BulkDeleteResponse> BulkDeleteImagesByGroup(string groupCode, string assessmentType, string userId)
        {
            try
            {
                var result = new BulkDeleteResult();

                // Delete linked images first
                var deleteLinkedQuery = @"
                    UPDATE image_linked il
                    INNER JOIN image_master im ON il.il_master_id = im.im_id
                    SET il.im_active = 0
                    WHERE im.im_group_code = @groupCode 
                    AND im.im_assessment_type = @assessmentType
                    AND im.im_active = 1 AND il.im_active = 1";

                var linkedParams = new[]
                {
                    new MySqlParameter("@groupCode", groupCode),
                    new MySqlParameter("@assessmentType", assessmentType)
                };

                result.DeletedLinkedImages = await Task.Run(() => _dbHelper.ExecuteNonQuery(deleteLinkedQuery, linkedParams));

                // Delete master images
                var deleteMasterQuery = @"
                    UPDATE image_master
                    SET im_active = 0, im_modified_user = @userId, im_modified_date = UTC_TIMESTAMP()
                    WHERE im_group_code = @groupCode 
                    AND im_assessment_type = @assessmentType
                    AND im_active = 1";

                var masterParams = new[]
                {
                    new MySqlParameter("@groupCode", groupCode),
                    new MySqlParameter("@assessmentType", assessmentType),
                    new MySqlParameter("@userId", userId)
                };

                result.DeletedMasterImages = await Task.Run(() => _dbHelper.ExecuteNonQuery(deleteMasterQuery, masterParams));
                result.TotalDeleted = result.DeletedMasterImages + result.DeletedLinkedImages;

                return new BulkDeleteResponse
                {
                    OutputCode = 1,
                    OutputMsg = $"Bulk delete completed. {result.TotalDeleted} images deleted.",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new BulkDeleteResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error during bulk delete: {ex.Message}",
                    Data = new BulkDeleteResult { ErrorMessages = new List<string> { ex.Message } }
                };
            }
        }

        private ImageGroup MapToImageGroup(DataRow row)
        {
            return new ImageGroup
            {
                IgId = Convert.ToInt32(row["ig_id"]),
                IgCode = row["ig_code"]?.ToString() ?? string.Empty,
                IgName = row["ig_name"]?.ToString() ?? string.Empty,
                IgDescription = row["ig_description"]?.ToString() ?? string.Empty,
                IgActive = row["ig_active"]?.ToString() ?? "Y",
                IgIsCurrentActive = row["ig_is_current_active"]?.ToString() ?? "N",
                IgCreatedUser = row["ig_created_user"]?.ToString() ?? string.Empty,
                IgCreatedDate = row["ig_created_date"] != DBNull.Value ? (DateTime?)row["ig_created_date"] : null,
                IgModifiedUser = row["ig_modified_user"]?.ToString() ?? string.Empty,
                IgModifiedDate = row["ig_modified_date"] != DBNull.Value ? (DateTime?)row["ig_modified_date"] : null,
                IgAssessmentType = row["ig_assessment_type"]?.ToString() ?? string.Empty
            };
        }
    }
}