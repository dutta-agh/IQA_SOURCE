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
                // FIXED: Corrected linked images count query
                var query = @"
                    SELECT 
                        im.im_assessment_type,
                        atm.atm_name as assessment_name,
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
                    GROUP BY im.im_assessment_type, atm.atm_name
                    ORDER BY im.im_assessment_type";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var summaries = new List<AssessmentImageSummary>();
                foreach (DataRow row in result.Rows)
                {
                    summaries.Add(new AssessmentImageSummary
                    {
                        AssessmentType = row["im_assessment_type"]?.ToString(),
                        AssessmentName = row["assessment_name"]?.ToString() ?? row["im_assessment_type"]?.ToString(),
                        TotalMasterImages = row["total_master_images"] != DBNull.Value ? Convert.ToInt32(row["total_master_images"]) : 0,
                        TotalLinkedImages = row["total_linked_images"] != DBNull.Value ? Convert.ToInt32(row["total_linked_images"]) : 0,
                        ImagesWithLinked = row["images_with_linked"] != DBNull.Value ? Convert.ToInt32(row["images_with_linked"]) : 0,
                        LastUploadDate = row["last_upload_date"] != DBNull.Value ? (DateTime?)row["last_upload_date"] : null,
                        LastUploadBy = row["last_upload_by"]?.ToString(),
                        AverageFileSize = row["avg_file_size"] != DBNull.Value ? Convert.ToDecimal(row["avg_file_size"]) : 0,
                        TotalStorageUsed = row["total_storage_used"] != DBNull.Value ? Convert.ToInt64(row["total_storage_used"]) : 0
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
    }
}