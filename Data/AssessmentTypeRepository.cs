using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class AssessmentTypeRepository : IAssessmentTypeRepository
    {
        private readonly IDbHelper _dbHelper;

        public AssessmentTypeRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<AssessmentTypeMasterResponse> GetAllAssessmentTypes(string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        atm_code,
                        atm_name,
                        atm_url_slug,
                        atm_description,
                        atm_duration_minutes,
                        atm_start_date,
                        atm_end_date,
                        atm_created_user,
                        atm_created_date,   
                        atm_modified_user,
                        atm_modified_date,
                        atm_active
                    FROM assessment_type_mstr
                    ORDER BY atm_created_date DESC";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var assessmentTypes = new List<AssessmentTypeMaster>();
                foreach (DataRow row in result.Rows)
                {
                    assessmentTypes.Add(MapToAssessmentTypeMaster(row));
                }

                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Assessment types retrieved successfully",
                    Data = assessmentTypes
                };
            }
            catch (Exception ex)
            {
                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving assessment types: {ex.Message}",
                    Data = new List<AssessmentTypeMaster>()
                };
            }
        }

        public async Task<AssessmentTypeMasterResponse> GetAssessmentTypeByCode(string atmCode, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        atm_code,
                        atm_name,
                        atm_url_slug,
                        atm_description,
                        atm_duration_minutes,
                        atm_start_date,
                        atm_end_date,
                        atm_created_user,
                        atm_created_date,
                        atm_modified_user,
                        atm_modified_date,
                        atm_active
                    FROM assessment_type_mstr
                    WHERE atm_code = @atmCode OR atm_url_slug = @atmCode
                    LIMIT 1";

                var parameters = new[]
                {
                    new MySqlParameter("@atmCode", atmCode)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var assessmentTypes = new List<AssessmentTypeMaster>();
                if (result.Rows.Count > 0)
                {
                    assessmentTypes.Add(MapToAssessmentTypeMaster(result.Rows[0]));
                }

                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = result.Rows.Count > 0 ? "Assessment type retrieved successfully" : "Assessment type not found",
                    Data = assessmentTypes
                };
            }
            catch (Exception ex)
            {
                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving assessment type: {ex.Message}",
                    Data = new List<AssessmentTypeMaster>()
                };
            }
        }

        public async Task<AssessmentTypeMasterResponse> GetAssessmentTypeByUrlSlug(string urlSlug)
        {
            try
            {
                var query = @"
                    SELECT 
                        atm_code,
                        atm_name,
                        atm_url_slug,
                        atm_description,
                        atm_duration_minutes,
                        atm_start_date,
                        atm_end_date,
                        atm_created_user,
                        atm_created_date,
                        atm_modified_user,
                        atm_modified_date,
                        atm_active
                    FROM assessment_type_mstr
                    WHERE atm_url_slug = @urlSlug
                    AND atm_active = 'Y'
                    AND (atm_start_date IS NULL OR atm_start_date <= NOW())
                    AND (atm_end_date IS NULL OR atm_end_date >= NOW())
                    LIMIT 1";

                var parameters = new[]
                {
                    new MySqlParameter("@urlSlug", urlSlug)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var assessmentTypes = new List<AssessmentTypeMaster>();
                if (result.Rows.Count > 0)
                {
                    assessmentTypes.Add(MapToAssessmentTypeMaster(result.Rows[0]));
                }

                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = result.Rows.Count > 0 ? "Assessment type retrieved successfully" : "Assessment type not found or not available",
                    Data = assessmentTypes
                };
            }
            catch (Exception ex)
            {
                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving assessment type: {ex.Message}",
                    Data = new List<AssessmentTypeMaster>()
                };
            }
        }

        public async Task<AssessmentTypeMasterResponse> InsertAssessmentType(AssessmentTypeMaster assessmentType, string userId)
        {
            try
            {
                // IMPORTANT: Ensure slug matches code for URL routing
                if (string.IsNullOrEmpty(assessmentType.AtmUrlSlug))
                {
                    assessmentType.AtmUrlSlug = assessmentType.AtmCode;
                }

                // Check if code already exists
                var checkQuery = "SELECT COUNT(*) as cnt FROM assessment_type_mstr WHERE atm_code = @atmCode";
                var checkParams = new[] { new MySqlParameter("@atmCode", assessmentType.AtmCode) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));
                
                if (checkResult.Rows.Count > 0 && Convert.ToInt32(checkResult.Rows[0]["cnt"]) > 0)
                {
                    return new AssessmentTypeMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Assessment type code already exists"
                    };
                }

                // Check if URL slug already exists
                var slugCheckQuery = "SELECT COUNT(*) as cnt FROM assessment_type_mstr WHERE atm_url_slug = @urlSlug";
                var slugCheckParams = new[] { new MySqlParameter("@urlSlug", assessmentType.AtmUrlSlug) };
                var slugCheckResult = await Task.Run(() => _dbHelper.ExecuteQuery(slugCheckQuery, slugCheckParams));
                
                if (slugCheckResult.Rows.Count > 0 && Convert.ToInt32(slugCheckResult.Rows[0]["cnt"]) > 0)
                {
                    return new AssessmentTypeMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "URL slug already exists"
                    };
                }

                // Validate date range
                if (assessmentType.AtmStartDate.HasValue && assessmentType.AtmEndDate.HasValue)
                {
                    if (assessmentType.AtmEndDate.Value < assessmentType.AtmStartDate.Value)
                    {
                        return new AssessmentTypeMasterResponse
                        {
                            OutputCode = 0,
                            OutputMsg = "End date must be greater than or equal to start date"
                        };
                    }
                }

                var query = @"
                    INSERT INTO assessment_type_mstr (
                        atm_code,
                        atm_name,
                        atm_url_slug,
                        atm_description,
                        atm_duration_minutes,
                        atm_start_date,
                        atm_end_date,
                        atm_created_user,
                        atm_created_date,
                        atm_active
                    )
                    VALUES (
                        @atmCode,
                        @atmName,
                        @atmUrlSlug,
                        @atmDescription,
                        @atmDurationMinutes,
                        @atmStartDate,
                        @atmEndDate,
                        @userId,
                        NOW(),
                        @atmActive
                    )";

                var parameters = new[]
                {
                    new MySqlParameter("@atmCode", assessmentType.AtmCode),
                    new MySqlParameter("@atmName", assessmentType.AtmName),
                    new MySqlParameter("@atmUrlSlug", assessmentType.AtmUrlSlug),
                    new MySqlParameter("@atmDescription", (object)assessmentType.AtmDescription ?? DBNull.Value),
                    new MySqlParameter("@atmDurationMinutes", assessmentType.AtmDurationMinutes),
                    new MySqlParameter("@atmStartDate", (object)assessmentType.AtmStartDate ?? DBNull.Value),
                    new MySqlParameter("@atmEndDate", (object)assessmentType.AtmEndDate ?? DBNull.Value),
                    new MySqlParameter("@userId", userId),
                    new MySqlParameter("@atmActive", assessmentType.AtmActive ?? "Y")
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new AssessmentTypeMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Assessment type created successfully" : "Insert failed"
                };
            }
            catch (Exception ex)
            {
                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error inserting assessment type: {ex.Message}"
                };
            }
        }

        public async Task<AssessmentTypeMasterResponse> UpdateAssessmentType(AssessmentTypeMaster assessmentType, string userId)
        {
            try
            {
                // IMPORTANT: Ensure slug matches code for URL routing
                if (string.IsNullOrEmpty(assessmentType.AtmUrlSlug))
                {
                    assessmentType.AtmUrlSlug = assessmentType.AtmCode;
                }

                // Check if assessment type exists
                var checkQuery = "SELECT COUNT(*) as cnt FROM assessment_type_mstr WHERE atm_code = @atmCode";
                var checkParams = new[] { new MySqlParameter("@atmCode", assessmentType.AtmCode) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));
                
                if (checkResult.Rows.Count == 0 || Convert.ToInt32(checkResult.Rows[0]["cnt"]) == 0)
                {
                    return new AssessmentTypeMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Assessment type not found"
                    };
                }

                // Check if URL slug already exists for different code
                var slugCheckQuery = "SELECT COUNT(*) as cnt FROM assessment_type_mstr WHERE atm_url_slug = @urlSlug AND atm_code != @atmCode";
                var slugCheckParams = new[] 
                { 
                    new MySqlParameter("@urlSlug", assessmentType.AtmUrlSlug),
                    new MySqlParameter("@atmCode", assessmentType.AtmCode)
                };
                var slugCheckResult = await Task.Run(() => _dbHelper.ExecuteQuery(slugCheckQuery, slugCheckParams));
                
                if (slugCheckResult.Rows.Count > 0 && Convert.ToInt32(slugCheckResult.Rows[0]["cnt"]) > 0)
                {
                    return new AssessmentTypeMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "URL slug already exists"
                    };
                }

                // Validate date range
                if (assessmentType.AtmStartDate.HasValue && assessmentType.AtmEndDate.HasValue)
                {
                    if (assessmentType.AtmEndDate.Value < assessmentType.AtmStartDate.Value)
                    {
                        return new AssessmentTypeMasterResponse
                        {
                            OutputCode = 0,
                            OutputMsg = "End date must be greater than or equal to start date"
                        };
                    }
                }

                var query = @"
                    UPDATE assessment_type_mstr
                    SET 
                        atm_name = @atmName,
                        atm_url_slug = @atmUrlSlug,
                        atm_description = @atmDescription,
                        atm_duration_minutes = @atmDurationMinutes,
                        atm_start_date = @atmStartDate,
                        atm_end_date = @atmEndDate,
                        atm_modified_user = @userId,
                        atm_modified_date = NOW(),
                        atm_active = @atmActive
                    WHERE atm_code = @atmCode";

                var parameters = new[]
                {
                    new MySqlParameter("@atmCode", assessmentType.AtmCode),
                    new MySqlParameter("@atmName", assessmentType.AtmName),
                    new MySqlParameter("@atmUrlSlug", assessmentType.AtmUrlSlug),
                    new MySqlParameter("@atmDescription", (object)assessmentType.AtmDescription ?? DBNull.Value),
                    new MySqlParameter("@atmDurationMinutes", assessmentType.AtmDurationMinutes),
                    new MySqlParameter("@atmStartDate", (object)assessmentType.AtmStartDate ?? DBNull.Value),
                    new MySqlParameter("@atmEndDate", (object)assessmentType.AtmEndDate ?? DBNull.Value),
                    new MySqlParameter("@userId", userId),
                    new MySqlParameter("@atmActive", assessmentType.AtmActive ?? "Y")
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new AssessmentTypeMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Assessment type updated successfully" : "Update failed"
                };
            }
            catch (Exception ex)
            {
                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error updating assessment type: {ex.Message}"
                };
            }
        }

        public async Task<AssessmentTypeMasterResponse> DeleteAssessmentType(string atmCode, string userId)
        {
            try
            {
                // Check if assessment type exists
                var checkQuery = "SELECT COUNT(*) as cnt FROM assessment_type_mstr WHERE atm_code = @atmCode";
                var checkParams = new[] { new MySqlParameter("@atmCode", atmCode) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));
                
                if (checkResult.Rows.Count == 0 || Convert.ToInt32(checkResult.Rows[0]["cnt"]) == 0)
                {
                    return new AssessmentTypeMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Assessment type not found"
                    };
                }

                // Soft delete - set active to 'N'
                var query = @"
                    UPDATE assessment_type_mstr
                    SET 
                        atm_active = 'N',
                        atm_modified_user = @userId,
                        atm_modified_date = NOW()
                    WHERE atm_code = @atmCode";

                var parameters = new[]
                {
                    new MySqlParameter("@atmCode", atmCode),
                    new MySqlParameter("@userId", userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new AssessmentTypeMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Assessment type deleted successfully" : "Delete failed"
                };
            }
            catch (Exception ex)
            {
                return new AssessmentTypeMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error deleting assessment type: {ex.Message}"
                };
            }
        }

        private AssessmentTypeMaster MapToAssessmentTypeMaster(DataRow row)
        {
            return new AssessmentTypeMaster
            {
                AtmCode = row["atm_code"]?.ToString(),
                AtmName = row["atm_name"]?.ToString(),
                AtmUrlSlug = row["atm_url_slug"]?.ToString(),
                AtmDescription = row["atm_description"]?.ToString(),
                AtmDurationMinutes = row["atm_duration_minutes"] != DBNull.Value ? Convert.ToInt32(row["atm_duration_minutes"]) : 0,
                AtmStartDate = row["atm_start_date"] != DBNull.Value ? (DateTime?)row["atm_start_date"] : null,
                AtmEndDate = row["atm_end_date"] != DBNull.Value ? (DateTime?)row["atm_end_date"] : null,
                AtmCreatedUser = row["atm_created_user"]?.ToString(),
                AtmCreatedDate = row["atm_created_date"] != DBNull.Value ? (DateTime?)row["atm_created_date"] : null,
                AtmModifiedUser = row["atm_modified_user"]?.ToString(),
                AtmModifiedDate = row["atm_modified_date"] != DBNull.Value ? (DateTime?)row["atm_modified_date"] : null,
                AtmActive = row["atm_active"]?.ToString()
            };
        }
    }
}