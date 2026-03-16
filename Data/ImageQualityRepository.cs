using System.Data;
using IQA_SOURCE.Helpers;
using IQA_SOURCE.Models;
using MySqlConnector;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class ImageQualityRepository : IImageQualityRepository
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<ImageQualityRepository> _logger;

        public ImageQualityRepository(IDbHelper dbHelper, ILogger<ImageQualityRepository> logger)
        {
            _dbHelper = dbHelper;
            _logger   = logger;
        }

        public async Task<(int OutputCode, string OutputMsg, List<RawImageSet> Data)> GetRawImageSetsByAssessment(string assessmentCode, string userCode)
        {
            try
            {
                var query = @"
                    SELECT 
                        im_id, im_assessment_type, im_file_name, 
                        im_file_path, im_active, im_created_date, im_created_user
                    FROM image_master
                    WHERE im_assessment_type = @assessmentCode
                    AND im_active = 1
                    ORDER BY im_created_date, im_id";

                var parameters = new[] { new MySqlParameter("@assessmentCode", assessmentCode) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var data = new List<RawImageSet>();
                foreach (DataRow row in result.Rows)
                {
                    data.Add(new RawImageSet
                    {
                        RisId             = Convert.ToInt32(row["im_id"]),
                        RisAssessmentCode = row["im_assessment_type"]?.ToString() ?? string.Empty,
                        RisCode           = row["im_file_name"]?.ToString() ?? string.Empty,
                        RisName           = row["im_file_name"]?.ToString() ?? string.Empty,
                        RisRawImagePath   = ImageUrlHelper.BuildImageUrl(row["im_file_path"]?.ToString()),
                        RisActive         = row["im_active"] != DBNull.Value && Convert.ToInt32(row["im_active"]) == 1 ? "Y" : "N",
                        RisDisplayOrder   = 0,
                        RisCreatedDate    = row["im_created_date"] != DBNull.Value ? Convert.ToDateTime(row["im_created_date"]) : null,
                        RisCreatedUser    = row["im_created_user"]?.ToString() ?? string.Empty
                    });
                }

                return (1, "Success", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRawImageSetsByAssessment");
                return (-1, ex.Message, new List<RawImageSet>());
            }
        }

        public async Task<(int OutputCode, string OutputMsg, List<int> Data)> GetCompletedRawImageSetIds(string sessionId, string assessmentCode, string userCode)
        {
            try
            {
                var query = @"
                    SELECT DISTINCT iqr_im_id
                    FROM tbl_image_quality_ratings
                    WHERE iqr_session_id = @sessionId
                    AND iqr_assessment_code = @assessmentCode
                    ORDER BY iqr_im_id";

                var parameters = new[]
                {
                    new MySqlParameter("@sessionId",      sessionId),
                    new MySqlParameter("@assessmentCode", assessmentCode)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var data = new List<int>();
                foreach (DataRow row in result.Rows)
                    data.Add(Convert.ToInt32(row["iqr_im_id"]));

                return (1, "Success", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCompletedRawImageSetIds");
                return (-1, ex.Message, new List<int>());
            }
        }

        public async Task<(int OutputCode, string OutputMsg, RawImageSet? Data)> GetNextRandomRawImageSet(string sessionId, string assessmentCode, string userCode)
        {
            try
            {
                var query = @"
                    SELECT 
                        im.im_id, im.im_assessment_type, im.im_file_name, 
                        im.im_file_path, im.im_active, im.im_created_date, im.im_created_user
                    FROM image_master im
                    WHERE im.im_assessment_type = @assessmentCode
                    AND im.im_active = 1
                    AND im.im_id NOT IN (
                        SELECT DISTINCT iqr_im_id
                        FROM tbl_image_quality_ratings
                        WHERE iqr_session_id = @sessionId
                        AND iqr_assessment_code = @assessmentCode
                    )
                    ORDER BY RAND()
                    LIMIT 1";

                var parameters = new[]
                {
                    new MySqlParameter("@assessmentCode", assessmentCode),
                    new MySqlParameter("@sessionId",      sessionId)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                if (result.Rows.Count == 0)
                {
                    var countQuery  = @"
                        SELECT COUNT(*) as total
                        FROM image_master
                        WHERE im_assessment_type = @assessmentCode
                        AND im_active = 1";
                    var countParams = new[] { new MySqlParameter("@assessmentCode", assessmentCode) };
                    var countResult = await Task.Run(() => _dbHelper.ExecuteQuery(countQuery, countParams));
                    var totalCount  = countResult.Rows.Count > 0 ? Convert.ToInt32(countResult.Rows[0]["total"]) : 0;

                    return totalCount == 0
                        ? (-1, "No image sets found for this assessment", null)
                        : (0,  "All image sets completed",                null);
                }

                var row = result.Rows[0];
                var rawImageSet = new RawImageSet
                {
                    RisId             = Convert.ToInt32(row["im_id"]),
                    RisAssessmentCode = row["im_assessment_type"]?.ToString() ?? string.Empty,
                    RisCode           = row["im_file_name"]?.ToString() ?? string.Empty,
                    RisName           = row["im_file_name"]?.ToString() ?? string.Empty,
                    RisRawImagePath   = ImageUrlHelper.BuildImageUrl(row["im_file_path"]?.ToString()),
                    RisActive         = row["im_active"] != DBNull.Value && Convert.ToInt32(row["im_active"]) == 1 ? "Y" : "N",
                    RisDisplayOrder   = 0,
                    RisCreatedDate    = row["im_created_date"] != DBNull.Value ? Convert.ToDateTime(row["im_created_date"]) : null,
                    RisCreatedUser    = row["im_created_user"]?.ToString() ?? string.Empty
                };

                _logger.LogInformation($"Selected random image set: ID={rawImageSet.RisId}, Name={rawImageSet.RisName}");
                return (1, "Success", rawImageSet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNextRandomRawImageSet");
                return (-1, ex.Message, null);
            }
        }

        public async Task<(int OutputCode, string OutputMsg, List<LinkedImage> Data)> GetLinkedImagesByRawImageSetId(int rawImageSetId, string userCode)
        {
            try
            {
                var query = @"
                    SELECT 
                        il_id, il_master_id, il_file_name, il_file_path,
                        il_quality_level, il_quality_type, im_active,
                        il_created_date, il_created_user
                    FROM image_linked
                    WHERE il_master_id = @rawImageSetId
                    AND im_active = 1
                    ORDER BY RAND()";

                var parameters = new[] { new MySqlParameter("@rawImageSetId", rawImageSetId) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var data = new List<LinkedImage>();
                foreach (DataRow row in result.Rows)
                {
                    data.Add(new LinkedImage
                    {
                        LiId           = Convert.ToInt32(row["il_id"]),
                        LiRisId        = Convert.ToInt32(row["il_master_id"]),
                        LiImagePath    = ImageUrlHelper.BuildImageUrl(row["il_file_path"]?.ToString()),
                        LiImageLabel   = row["il_file_name"]?.ToString() ?? string.Empty,
                        LiActive       = row["im_active"] != DBNull.Value && Convert.ToInt32(row["im_active"]) == 1 ? "Y" : "N",
                        LiDisplayOrder = 0,
                        LiCreatedDate  = row["il_created_date"] != DBNull.Value ? Convert.ToDateTime(row["il_created_date"]) : null,
                        LiCreatedUser  = row["il_created_user"]?.ToString() ?? string.Empty
                    });
                }

                _logger.LogInformation($"Retrieved {data.Count} linked images for master image {rawImageSetId}");
                return (1, "Success", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLinkedImagesByRawImageSetId");
                return (-1, ex.Message, new List<LinkedImage>());
            }
        }

        public async Task<(int OutputCode, string OutputMsg)> SaveImageQualityRating(ImageQualitySubmission submission, string ipAddress, string userAgent, string userCode)
        {
            try
            {
                // Single atomic upsert — eliminates race condition between check and insert
                var upsertQuery = @"
                    INSERT INTO tbl_image_quality_ratings
                        (iqr_session_id, iqr_assessment_code, iqr_im_id, iqr_il_id,
                         iqr_quality_rating, iqr_ip_address, iqr_user_agent, iqr_created_date)
                    VALUES
                        (@sessionId, @assessmentCode, @rawImageSetId, @linkedImageId,
                         @qualityRating, @ipAddress, @userAgent, UTC_TIMESTAMP())
                    ON DUPLICATE KEY UPDATE
                        iqr_il_id          = VALUES(iqr_il_id),
                        iqr_quality_rating = VALUES(iqr_quality_rating),
                        iqr_ip_address     = VALUES(iqr_ip_address),
                        iqr_user_agent     = VALUES(iqr_user_agent),
                        iqr_created_date   = UTC_TIMESTAMP()";

                var parameters = new[]
                {
                    new MySqlParameter("@sessionId",      submission.SessionId),
                    new MySqlParameter("@assessmentCode", submission.AssessmentCode),
                    new MySqlParameter("@rawImageSetId",  submission.RawImageSetId),
                    new MySqlParameter("@linkedImageId",  submission.SelectedLinkedImageId),
                    new MySqlParameter("@qualityRating",  submission.QualityRating),
                    new MySqlParameter("@ipAddress",      ipAddress),
                    new MySqlParameter("@userAgent",      userAgent)
                };

                await Task.Run(() => _dbHelper.ExecuteNonQuery(upsertQuery, parameters));
                _logger.LogInformation("Upserted rating: Session={SessionId}, Master={RawImageSetId}, Linked={LinkedImageId}, Rating={Rating}",
                    submission.SessionId, submission.RawImageSetId, submission.SelectedLinkedImageId, submission.QualityRating);

                return (1, "Rating saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveImageQualityRating");
                return (-1, ex.Message);
            }
        }

        /// <summary>
        /// Save Sort assessment ratings — one row per image in tbl_image_quality_ratings.
        /// The raw image is stored with iqr_il_id = 0 to distinguish it from linked images.
        /// Uses UPSERT logic: deletes any prior rows for this session+assessment+master first,
        /// then inserts one row per rated image so re-submissions are clean.
        /// </summary>
        public async Task<(int OutputCode, string OutputMsg)> SaveSortRatings(
            SortRatingSubmission submission, string ipAddress, string userAgent, string userCode)
        {
            try
            {
                // Delete existing ratings for this session + master image set to allow clean resubmission
                var deleteQuery = @"
                    DELETE FROM tbl_image_quality_ratings
                    WHERE iqr_session_id      = @sessionId
                    AND   iqr_assessment_code = @assessmentCode
                    AND   iqr_im_id           = @rawImageSetId";

                var deleteParams = new[]
                {
                    new MySqlParameter("@sessionId",      submission.SessionId),
                    new MySqlParameter("@assessmentCode", submission.AssessmentCode),
                    new MySqlParameter("@rawImageSetId",  submission.RawImageSetId)
                };

                await Task.Run(() => _dbHelper.ExecuteNonQuery(deleteQuery, deleteParams));

                // Insert one row per rated image
                var insertQuery = @"
                    INSERT INTO tbl_image_quality_ratings
                    (iqr_session_id, iqr_assessment_code, iqr_im_id, iqr_il_id,
                     iqr_quality_rating, iqr_ip_address, iqr_user_agent, iqr_created_date)
                    VALUES
                    (@sessionId, @assessmentCode, @rawImageSetId, @linkedImageId,
                     @qualityRating, @ipAddress, @userAgent, UTC_TIMESTAMP())";

                foreach (var entry in submission.Ratings)
                {
                    // Raw image is stored with il_id = 0
                    var linkedImageId = entry.IsRawImage ? 0 : entry.ImageId;

                    var insertParams = new[]
                    {
                        new MySqlParameter("@sessionId",      submission.SessionId),
                        new MySqlParameter("@assessmentCode", submission.AssessmentCode),
                        new MySqlParameter("@rawImageSetId",  submission.RawImageSetId),
                        new MySqlParameter("@linkedImageId",  linkedImageId),
                        new MySqlParameter("@qualityRating",  entry.Rating),
                        new MySqlParameter("@ipAddress",      ipAddress),
                        new MySqlParameter("@userAgent",      userAgent)
                    };

                    await Task.Run(() => _dbHelper.ExecuteNonQuery(insertQuery, insertParams));
                }

                _logger.LogInformation(
                    "Saved {Count} sort ratings: Session={Session}, Master={Master}",
                    submission.Ratings.Count, submission.SessionId, submission.RawImageSetId);

                return (1, "Sort ratings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveSortRatings");
                return (-1, ex.Message);
            }
        }

        public async Task<(int OutputCode, string OutputMsg, ImageAssessmentProgress Data)> GetAssessmentProgress(string sessionId, string assessmentCode, string userCode)
        {
            try
            {
                var query = @"
                    SELECT 
                        COUNT(DISTINCT im.im_id) as total_sets,
                        COUNT(DISTINCT iqr.iqr_im_id) as completed_sets
                    FROM image_master im
                    LEFT JOIN tbl_image_quality_ratings iqr 
                        ON im.im_id = iqr.iqr_im_id 
                        AND iqr.iqr_session_id = @sessionId
                        AND iqr.iqr_assessment_code = @assessmentCode
                    WHERE im.im_assessment_type = @assessmentCode
                    AND im.im_active = 1";

                var parameters = new[]
                {
                    new MySqlParameter("@sessionId",      sessionId),
                    new MySqlParameter("@assessmentCode", assessmentCode)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                int totalSets = 0, completedSets = 0;
                if (result.Rows.Count > 0)
                {
                    var row       = result.Rows[0];
                    totalSets     = row["total_sets"]     != DBNull.Value ? Convert.ToInt32(row["total_sets"])     : 0;
                    completedSets = row["completed_sets"] != DBNull.Value ? Convert.ToInt32(row["completed_sets"]) : 0;
                }

                var completedIdsResult = await GetCompletedRawImageSetIds(sessionId, assessmentCode, userCode);

                var progress = new ImageAssessmentProgress
                {
                    SessionId               = sessionId,
                    AssessmentCode          = assessmentCode,
                    CompletedRawImageSetIds = completedIdsResult.Data ?? new List<int>(),
                    TotalSets               = totalSets,
                    CompletedSets           = completedSets
                };

                progress.IsCompleted = progress.TotalSets > 0 && progress.CompletedSets >= progress.TotalSets;

                _logger.LogInformation($"Progress: {completedSets}/{totalSets} completed for session {sessionId}");
                return (1, "Success", progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAssessmentProgress");
                return (-1, ex.Message, new ImageAssessmentProgress());
            }
        }

        public async Task<(int OutputCode, string OutputMsg, List<ImageRatingAdminRow> Data)> GetImageRatingsForAdmin(string? assessmentCode, string userCode)
        {
            try
            {
                var filters = new List<string> { "iqr.iqr_il_id <> 0" };
                if (!string.IsNullOrWhiteSpace(assessmentCode))
                    filters.Add("iqr.iqr_assessment_code = @assessmentCode");

                var whereClause = $"WHERE {string.Join(" AND ", filters)}";

                var query = $@"
                    SELECT
                        iqr.iqr_id,
                        iqr.iqr_session_id,
                        iqr.iqr_assessment_code,
                        iqr.iqr_ip_address,
                        iqr.iqr_created_date,
                        iqr.iqr_quality_rating,
                        iqr.iqr_il_id,
                        im.im_id,
                        im.im_file_name        AS master_file_name,
                        im.im_file_path        AS master_file_path,
                        im.im_width            AS master_width,
                        im.im_height           AS master_height,
                        im.im_format           AS master_format,
                        im.im_dpi_x            AS master_dpi_x,
                        im.im_dpi_y            AS master_dpi_y,
                        im.im_exif_data        AS master_exif_data,
                        il.il_id,
                        il.il_file_name        AS linked_file_name,
                        il.il_file_path        AS linked_file_path,
                        il.il_width            AS linked_width,
                        il.il_height           AS linked_height,
                        il.il_format           AS linked_format,
                        il.il_dpi_x            AS linked_dpi_x,
                        il.il_dpi_y            AS linked_dpi_y,
                        il.il_exif_data        AS linked_exif_data,
                        il.il_quality_level    AS linked_quality_level,
                        il.il_quality_type     AS linked_quality_type,
                        (
                            SELECT iqr2.iqr_quality_rating
                            FROM   tbl_image_quality_ratings iqr2
                            WHERE  iqr2.iqr_session_id      = iqr.iqr_session_id
                            AND    iqr2.iqr_assessment_code = iqr.iqr_assessment_code
                            AND    iqr2.iqr_im_id           = iqr.iqr_im_id
                            AND    iqr2.iqr_il_id           = 0
                            LIMIT  1
                        )                      AS master_image_rating
                    FROM tbl_image_quality_ratings iqr
                    INNER JOIN image_master  im ON im.im_id  = iqr.iqr_im_id
                    LEFT  JOIN image_linked  il ON il.il_id  = iqr.iqr_il_id
                    {whereClause}
                    ORDER BY iqr.iqr_created_date DESC";

                MySqlParameter[]? parameters = string.IsNullOrWhiteSpace(assessmentCode)
                    ? null
                    : new[] { new MySqlParameter("@assessmentCode", assessmentCode) };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var data = new List<ImageRatingAdminRow>();
                foreach (DataRow row in result.Rows)
                {
                    data.Add(new ImageRatingAdminRow
                    {
                        RatingId           = Convert.ToInt32(row["iqr_id"]),
                        SessionId          = row["iqr_session_id"]?.ToString()      ?? string.Empty,
                        AssessmentCode     = row["iqr_assessment_code"]?.ToString() ?? string.Empty,
                        IpAddress          = row["iqr_ip_address"]?.ToString()      ?? string.Empty,
                        RatedAt            = row["iqr_created_date"] != DBNull.Value ? Convert.ToDateTime(row["iqr_created_date"]) : null,
                        QualityRating      = Convert.ToInt32(row["iqr_quality_rating"]),
                        MasterImageId      = Convert.ToInt32(row["im_id"]),
                        MasterImageName    = row["master_file_name"]?.ToString()    ?? string.Empty,
                        MasterImageUrl     = ImageUrlHelper.BuildImageUrl(row["master_file_path"]?.ToString()),
                        MasterWidth        = row["master_width"]  != DBNull.Value ? Convert.ToInt32(row["master_width"])  : null,
                        MasterHeight       = row["master_height"] != DBNull.Value ? Convert.ToInt32(row["master_height"]) : null,
                        MasterFormat       = row["master_format"]?.ToString(),
                        MasterDpiX         = row["master_dpi_x"]  != DBNull.Value ? Convert.ToDouble(row["master_dpi_x"])  : null,
                        MasterDpiY         = row["master_dpi_y"]  != DBNull.Value ? Convert.ToDouble(row["master_dpi_y"])  : null,
                        MasterExifData     = row["master_exif_data"]?.ToString(),
                        MasterImageRating  = row["master_image_rating"] != DBNull.Value ? Convert.ToInt32(row["master_image_rating"]) : null,
                        LinkedImageId      = row["il_id"]        != DBNull.Value ? Convert.ToInt32(row["il_id"])          : 0,
                        LinkedImageName    = row["linked_file_name"]?.ToString()    ?? string.Empty,
                        LinkedImageUrl     = ImageUrlHelper.BuildImageUrl(row["linked_file_path"]?.ToString()),
                        LinkedWidth        = row["linked_width"]  != DBNull.Value ? Convert.ToInt32(row["linked_width"])  : null,
                        LinkedHeight       = row["linked_height"] != DBNull.Value ? Convert.ToInt32(row["linked_height"]) : null,
                        LinkedFormat       = row["linked_format"]?.ToString(),
                        LinkedDpiX         = row["linked_dpi_x"]  != DBNull.Value ? Convert.ToDouble(row["linked_dpi_x"])  : null,
                        LinkedDpiY         = row["linked_dpi_y"]  != DBNull.Value ? Convert.ToDouble(row["linked_dpi_y"])  : null,
                        LinkedExifData     = row["linked_exif_data"]?.ToString(),
                        LinkedQualityLevel = row["linked_quality_level"]?.ToString(),
                        LinkedQualityType  = row["linked_quality_type"]?.ToString(),
                    });
                }

                return (1, "Success", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetImageRatingsForAdmin");
                return (-1, ex.Message, new List<ImageRatingAdminRow>());
            }
        }
    }
}