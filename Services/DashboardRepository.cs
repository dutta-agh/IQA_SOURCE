using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbHelper _dbHelper;

        public DashboardRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<DashboardStatsResponse> GetDashboardStats(string userId)
        {
            try
            {
                var stats = new DashboardStats();

                // Get total assessment types
                var atmQuery = "SELECT COUNT(*) as cnt FROM assessment_type_mstr WHERE atm_active = 'Y'";
                var atmResult = await Task.Run(() => _dbHelper.ExecuteQuery(atmQuery, null));
                stats.TotalAssessmentTypes = atmResult.Rows.Count > 0 ? Convert.ToInt32(atmResult.Rows[0]["cnt"]) : 0;

                // Get total questions
                var qsQuery = "SELECT COUNT(*) as cnt FROM tbl_questions WHERE QsActive = 1";
                var qsResult = await Task.Run(() => _dbHelper.ExecuteQuery(qsQuery, null));
                stats.TotalQuestions = qsResult.Rows.Count > 0 ? Convert.ToInt32(qsResult.Rows[0]["cnt"]) : 0;

                // Get total images
                var imQuery = "SELECT COUNT(*) as cnt FROM image_master WHERE im_active = 1";
                var imResult = await Task.Run(() => _dbHelper.ExecuteQuery(imQuery, null));
                stats.TotalImages = imResult.Rows.Count > 0 ? Convert.ToInt32(imResult.Rows[0]["cnt"]) : 0;

                // Get total linked images
                var ilQuery = "SELECT COUNT(*) as cnt FROM image_linked WHERE im_active = 1";
                var ilResult = await Task.Run(() => _dbHelper.ExecuteQuery(ilQuery, null));
                stats.TotalLinkedImages = ilResult.Rows.Count > 0 ? Convert.ToInt32(ilResult.Rows[0]["cnt"]) : 0;

                // Get assessment-wise image summary
                var summaryResponse = await GetAssessmentImageSummary(userId);
                stats.AssessmentImageSummaries = summaryResponse.Data ?? new List<AssessmentImageSummary>();

                return new DashboardStatsResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Dashboard stats retrieved successfully",
                    Data = stats
                };
            }
            catch (Exception ex)
            {
                return new DashboardStatsResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving dashboard stats: {ex.Message}",
                    Data = new DashboardStats()
                };
            }
        }

        public async Task<AssessmentImageSummaryResponse> GetAssessmentImageSummary(string userId)
        {
            try
            {
                // ✅ FIXED: Added im_group_code to SELECT and GROUP BY
                var query = @"
                    SELECT 
                        im.im_assessment_type,
                        atm.atm_name as assessment_name,
                        im.im_group_code as group_code,
                        COUNT(DISTINCT im.im_id) as total_master_images,
                        (SELECT COUNT(*) 
                         FROM image_linked il2 
                         WHERE il2.il_master_id IN (
                             SELECT im3.im_id 
                             FROM image_master im3 
                             WHERE im3.im_assessment_type COLLATE utf8mb4_unicode_ci = im.im_assessment_type COLLATE utf8mb4_unicode_ci
                             AND im3.im_active = 1
                         )
                         AND il2.im_active = 1) as total_linked_images,
                        COUNT(DISTINCT il.il_master_id) as images_with_linked,
                        MAX(im.im_created_date) as last_upload_date,
                        (SELECT im2.im_created_user 
                         FROM image_master im2 
                         WHERE im2.im_assessment_type COLLATE utf8mb4_unicode_ci = im.im_assessment_type COLLATE utf8mb4_unicode_ci
                         AND im2.im_active = 1
                         ORDER BY im2.im_created_date DESC 
                         LIMIT 1) as last_upload_by,
                        COALESCE(AVG(im.im_file_size), 0) as avg_file_size,
                        COALESCE(SUM(im.im_file_size), 0) + COALESCE((
                            SELECT SUM(il3.il_file_size)
                            FROM image_linked il3
                            WHERE il3.il_master_id IN (
                                SELECT im4.im_id 
                                FROM image_master im4 
                                WHERE im4.im_assessment_type COLLATE utf8mb4_unicode_ci = im.im_assessment_type COLLATE utf8mb4_unicode_ci
                                AND im4.im_active = 1
                            )
                            AND il3.im_active = 1
                        ), 0) as total_storage_used
                    FROM image_master im
                    LEFT JOIN assessment_type_mstr atm 
                        ON im.im_assessment_type COLLATE utf8mb4_unicode_ci = atm.atm_code COLLATE utf8mb4_unicode_ci
                    LEFT JOIN image_linked il 
                        ON im.im_id = il.il_master_id AND il.im_active = 1
                    WHERE im.im_active = 1
                    GROUP BY im.im_assessment_type, atm.atm_name, im.im_group_code
                    ORDER BY im.im_assessment_type, im.im_group_code";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var summaries = new List<AssessmentImageSummary>();
                foreach (DataRow row in result.Rows)
                {
                    summaries.Add(new AssessmentImageSummary
                    {
                        AssessmentType = row["im_assessment_type"]?.ToString(),
                        AssessmentName = row["assessment_name"]?.ToString() ?? row["im_assessment_type"]?.ToString(),
                        GroupCode = row["group_code"]?.ToString(),  // ✅ NEW: Map GroupCode from database
                        TotalMasterImages = row["total_master_images"] != DBNull.Value ? Convert.ToInt32(row["total_master_images"]) : 0,
                        TotalLinkedImages = row["total_linked_images"] != DBNull.Value ? Convert.ToInt32(row["total_linked_images"]) : 0,
                        ImagesWithLinked = row["images_with_linked"] != DBNull.Value ? Convert.ToInt32(row["images_with_linked"]) : 0,
                        LastUploadDate = row["last_upload_date"] != DBNull.Value ? (DateTime?)row["last_upload_date"] : null,
                        LastUploadBy = row["last_upload_by"]?.ToString(),
                        AverageFileSize = row["avg_file_size"] != DBNull.Value ? Convert.ToDecimal(row["avg_file_size"]) : 0,
                        TotalStorageUsed = row["total_storage_used"] != DBNull.Value ? Convert.ToInt64(row["total_storage_used"]) : 0,
                        GroupName = null  // ✅ Will be populated by controller enrichment
                    });
                }

                return new AssessmentImageSummaryResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Assessment image summary retrieved successfully",
                    Data = summaries
                };
            }
            catch (Exception ex)
            {
                return new AssessmentImageSummaryResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving assessment image summary: {ex.Message}",
                    Data = new List<AssessmentImageSummary>()
                };
            }
        }
        // ADD this new method

        public async Task<EnhancedDashboardResponse> GetEnhancedDashboardStats(string userId)
        {
            try
            {
                var query = @"
            SELECT 
                atm.atm_code as AssessmentType,
                atm.atm_name as AssessmentName,
                ig.ig_code as GroupCode,
                ig.ig_name as GroupName,
                COUNT(DISTINCT CASE WHEN im.im_id IS NOT NULL THEN im.im_id END) as TotalMasterImages,
                COUNT(DISTINCT CASE WHEN il.il_id IS NOT NULL THEN il.il_id END) as TotalLinkedImages,
                COUNT(DISTINCT CASE WHEN im.im_id IS NOT NULL AND il.il_id IS NOT NULL THEN im.im_id END) as ImagesWithLinked,
                MAX(im.im_created_date) as LastUploadDate,
                (SELECT im_created_user FROM image_master 
                 WHERE im_assessment_type = atm.atm_code AND im_group_code = ig.ig_code 
                 ORDER BY im_created_date DESC LIMIT 1) as LastUploadBy,
                AVG(CASE WHEN im.im_file_size > 0 THEN im.im_file_size ELSE 0 END) as AverageFileSize,
                SUM(COALESCE(im.im_file_size, 0) + COALESCE(il.il_file_size, 0)) as TotalStorageUsed
            FROM assessment_type_master atm
            LEFT JOIN image_group ig ON ig.ig_assessment_type = atm.atm_code AND ig.ig_active = 'Y'
            LEFT JOIN image_master im ON im.im_assessment_type = atm.atm_code AND im.im_group_code = ig.ig_code AND im.im_active = 1
            LEFT JOIN image_linked il ON il.il_master_id = im.im_id AND il.im_active = 1
            WHERE atm.atm_active = 'Y'
            GROUP BY atm.atm_code, atm.atm_name, ig.ig_code, ig.ig_name
            ORDER BY atm.atm_name, ig.ig_name";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var assessmentGroupSummaries = new List<AssessmentGroupImageSummary>();

                foreach (DataRow row in result.Rows)
                {
                    assessmentGroupSummaries.Add(new AssessmentGroupImageSummary
                    {
                        AssessmentType = row["AssessmentType"]?.ToString(),
                        AssessmentName = row["AssessmentName"]?.ToString(),
                        GroupCode = row["GroupCode"]?.ToString(),
                        GroupName = row["GroupName"]?.ToString(),
                        TotalMasterImages = row["TotalMasterImages"] != DBNull.Value ? Convert.ToInt32(row["TotalMasterImages"]) : 0,
                        TotalLinkedImages = row["TotalLinkedImages"] != DBNull.Value ? Convert.ToInt32(row["TotalLinkedImages"]) : 0,
                        ImagesWithLinked = row["ImagesWithLinked"] != DBNull.Value ? Convert.ToInt32(row["ImagesWithLinked"]) : 0,
                        LastUploadDate = row["LastUploadDate"] != DBNull.Value ? (DateTime?)row["LastUploadDate"] : null,
                        LastUploadBy = row["LastUploadBy"]?.ToString(),
                        AverageFileSize = row["AverageFileSize"] != DBNull.Value ? Convert.ToDecimal(row["AverageFileSize"]) : 0,
                        TotalStorageUsed = row["TotalStorageUsed"] != DBNull.Value ? Convert.ToInt64(row["TotalStorageUsed"]) : 0
                    });
                }

                // Get total counts
                var countQuery = @"
            SELECT 
                (SELECT COUNT(*) FROM assessment_type_master WHERE atm_active = 'Y') as TotalAssessmentTypes,
                (SELECT COUNT(*) FROM question_master WHERE qs_active = 1) as TotalQuestions,
                (SELECT COUNT(*) FROM image_master WHERE im_active = 1) as TotalMasterImages,
                (SELECT COUNT(*) FROM image_linked WHERE im_active = 1) as TotalLinkedImages,
                (SELECT COUNT(*) FROM image_group WHERE ig_active = 'Y') as TotalImageGroups";

                var countResult = await Task.Run(() => _dbHelper.ExecuteQuery(countQuery, null));

                var dashboardStats = new EnhancedDashboardStats
                {
                    TotalAssessmentTypes = countResult.Rows.Count > 0 && countResult.Rows[0]["TotalAssessmentTypes"] != DBNull.Value
                        ? Convert.ToInt32(countResult.Rows[0]["TotalAssessmentTypes"]) : 0,
                    TotalQuestions = countResult.Rows.Count > 0 && countResult.Rows[0]["TotalQuestions"] != DBNull.Value
                        ? Convert.ToInt32(countResult.Rows[0]["TotalQuestions"]) : 0,
                    TotalImages = countResult.Rows.Count > 0 && countResult.Rows[0]["TotalMasterImages"] != DBNull.Value
                        ? Convert.ToInt32(countResult.Rows[0]["TotalMasterImages"]) : 0,
                    TotalLinkedImages = countResult.Rows.Count > 0 && countResult.Rows[0]["TotalLinkedImages"] != DBNull.Value
                        ? Convert.ToInt32(countResult.Rows[0]["TotalLinkedImages"]) : 0,
                    TotalImageGroups = countResult.Rows.Count > 0 && countResult.Rows[0]["TotalImageGroups"] != DBNull.Value
                        ? Convert.ToInt32(countResult.Rows[0]["TotalImageGroups"]) : 0,
                    AssessmentGroupSummaries = assessmentGroupSummaries
                };

                return new EnhancedDashboardResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Enhanced dashboard statistics retrieved successfully",
                    Data = dashboardStats
                };
            }
            catch (Exception ex)
            {
                return new EnhancedDashboardResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving enhanced dashboard stats: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}