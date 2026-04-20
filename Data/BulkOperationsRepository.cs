using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class BulkOperationsRepository : IBulkOperationsRepository
    {
        private readonly IDbHelper _dbHelper;
        private readonly IQuestionAnswerRepository _questionAnswerRepository;
        private readonly IImageQualityRepository _imageQualityRepository;
        private readonly ISpeedTestRepository _speedTestRepository;

        public BulkOperationsRepository(
            IDbHelper dbHelper,
            IQuestionAnswerRepository questionAnswerRepository,
            IImageQualityRepository imageQualityRepository,
            ISpeedTestRepository speedTestRepository)
        {
            _dbHelper = dbHelper;
            _questionAnswerRepository = questionAnswerRepository;
            _imageQualityRepository = imageQualityRepository;
            _speedTestRepository = speedTestRepository;
        }

        public async Task<BulkDeleteAssessmentResponse> BulkDeleteAssessmentData(BulkDeleteAssessmentRequest request, string userId)
        {
            var result = new BulkDeleteAssessmentResult
            {
                AssessmentCode = request.AssessmentCode
            };

            try
            {
                // Delete Question Answers
                if (request.DeleteQuestionAnswers)
                {
                    var (qaCode, qaMsg, qaCount) = await _questionAnswerRepository.BulkDeleteByAssessmentCode(request.AssessmentCode, userId);
                    result.DeletedQuestionAnswers = qaCount;
                    if (qaCode != 1)
                    {
                        result.ErrorMessages.Add($"Question Answers: {qaMsg}");
                    }
                }

                // Delete Image Ratings
                if (request.DeleteImageRatings)
                {
                    var (irCode, irMsg, irCount) = await _imageQualityRepository.BulkDeleteRatingsByAssessmentCode(request.AssessmentCode, userId);
                    result.DeletedImageRatings = irCount;
                    if (irCode != 1)
                    {
                        result.ErrorMessages.Add($"Image Ratings: {irMsg}");
                    }
                }

                // Delete Speed Test Logs
                if (request.DeleteSpeedTestLogs)
                {
                    var (stCode, stMsg, stCount) = await _speedTestRepository.BulkDeleteByAssessmentCode(request.AssessmentCode, userId);
                    result.DeletedSpeedTestLogs = stCount;
                    if (stCode != 1)
                    {
                        result.ErrorMessages.Add($"Speed Test Logs: {stMsg}");
                    }
                }

                result.TotalRecordsDeleted = result.DeletedQuestionAnswers + result.DeletedImageRatings + result.DeletedSpeedTestLogs;

                return new BulkDeleteAssessmentResponse
                {
                    OutputCode = result.ErrorMessages.Count == 0 ? 1 : 0,
                    OutputMsg = result.ErrorMessages.Count == 0 
                        ? $"Successfully deleted {result.TotalRecordsDeleted} records for assessment '{request.AssessmentCode}'"
                        : $"Completed with {result.ErrorMessages.Count} error(s). Deleted {result.TotalRecordsDeleted} records.",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"General error: {ex.Message}");
                return new BulkDeleteAssessmentResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error during bulk delete: {ex.Message}",
                    Data = result
                };
            }
        }

        public async Task<(int OutputCode, string OutputMsg, List<string> Data)> GetAssessmentCodesWithData(string userId)
        {
            try
            {
                var query = @"
                    SELECT DISTINCT assessment_code
                    FROM (
                        SELECT qa_assessment_code as assessment_code FROM question_answers WHERE qa_assessment_code IS NOT NULL
                        UNION
                        SELECT iqr_assessment_code as assessment_code FROM image_quality_ratings WHERE iqr_assessment_code IS NOT NULL
                        UNION
                        SELECT AssessmentCode as assessment_code FROM SpeedTestLog WHERE AssessmentCode IS NOT NULL
                    ) AS combined
                    ORDER BY assessment_code";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var assessmentCodes = new List<string>();
                foreach (DataRow row in result.Rows)
                {
                    var code = row["assessment_code"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        assessmentCodes.Add(code);
                    }
                }

                return (1, "Assessment codes retrieved successfully", assessmentCodes);
            }
            catch (Exception ex)
            {
                return (0, $"Error retrieving assessment codes: {ex.Message}", new List<string>());
            }
        }

        public async Task<BulkDeleteResponse> BulkDeleteByImageGroup(BulkDeleteImageGroupRequest request, string userId)
        {
            try
            {
                var result = new BulkDeleteResult();

                // Soft delete linked images first
                if (request.DeleteImages)
                {
                    var deleteLinkedQuery = @"
                        UPDATE image_linked il
                        INNER JOIN image_master im ON il.il_master_id = im.im_id
                        SET il.im_active = 0
                        WHERE im.im_group_code = @groupCode";

                    var linkedParams = new List<MySqlParameter>
                    {
                        new MySqlParameter("@groupCode", request.ImageGroupCode)
                    };

                    if (!string.IsNullOrEmpty(request.AssessmentType))
                    {
                        deleteLinkedQuery += " AND im.im_assessment_type = @assessmentType";
                        linkedParams.Add(new MySqlParameter("@assessmentType", request.AssessmentType));
                    }

                    result.LinkedImagesDeleted = await Task.Run(() => _dbHelper.ExecuteNonQuery(deleteLinkedQuery, linkedParams.ToArray()));

                    // Soft delete master images
                    var deleteMasterQuery = @"
                        UPDATE image_master
                        SET im_active = 0, im_modified_user = @userId, im_modified_date = NOW()
                        WHERE im_group_code = @groupCode";

                    var masterParams = new List<MySqlParameter>
                    {
                        new MySqlParameter("@groupCode", request.ImageGroupCode),
                        new MySqlParameter("@userId", userId)
                    };

                    if (!string.IsNullOrEmpty(request.AssessmentType))
                    {
                        deleteMasterQuery += " AND im_assessment_type = @assessmentType";
                        masterParams.Add(new MySqlParameter("@assessmentType", request.AssessmentType));
                    }

                    result.ImagesDeleted = await Task.Run(() => _dbHelper.ExecuteNonQuery(deleteMasterQuery, masterParams.ToArray()));
                }

                // Delete related image quality ratings
                if (request.DeleteRelatedRatings)
                {
                    var deleteRatingsQuery = @"
                        DELETE iqr FROM image_quality_ratings iqr
                        INNER JOIN image_master im ON iqr.iqr_master_image_id = im.im_id
                        WHERE im.im_group_code = @groupCode";

                    var ratingParams = new List<MySqlParameter>
                    {
                        new MySqlParameter("@groupCode", request.ImageGroupCode)
                    };

                    if (!string.IsNullOrEmpty(request.AssessmentType))
                    {
                        deleteRatingsQuery += " AND im.im_assessment_type = @assessmentType";
                        ratingParams.Add(new MySqlParameter("@assessmentType", request.AssessmentType));
                    }

                    result.ImageRatingsDeleted = await Task.Run(() => _dbHelper.ExecuteNonQuery(deleteRatingsQuery, ratingParams.ToArray()));
                }

                // Build summary
                var summary = new System.Text.StringBuilder();
                if (result.ImagesDeleted > 0)
                    summary.Append($"{result.ImagesDeleted} master image(s) deactivated. ");
                if (result.LinkedImagesDeleted > 0)
                    summary.Append($"{result.LinkedImagesDeleted} linked image(s) deactivated. ");
                if (result.ImageRatingsDeleted > 0)
                    summary.Append($"{result.ImageRatingsDeleted} image rating(s) deleted. ");

                result.Summary = summary.ToString().Trim();
                result.TotalDeleted = result.ImagesDeleted + result.LinkedImagesDeleted + result.ImageRatingsDeleted;

                return new BulkDeleteResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Bulk delete by image group completed successfully",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new BulkDeleteResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error during bulk delete: {ex.Message}",
                    Data = new BulkDeleteResult
                    {
                        Errors = new List<string> { ex.Message }
                    }
                };
            }
        }

        public async Task<BulkDeleteResponse> BulkDeleteByAssessment(BulkDeleteByAssessmentRequest request, string userId)
        {
            try
            {
                var result = new BulkDeleteResult();

                // Delete User Responses (Question Answers)
                if (request.DeleteQuestionAnswers)
                {
                    var (qaCode, qaMsg, qaCount) = await _questionAnswerRepository.BulkDeleteByAssessmentCode(request.AssessmentCode, userId);
                    result.UserResponsesDeleted = qaCount;
                    if (qaCode != 1)
                    {
                        result.Errors.Add($"Question Answers: {qaMsg}");
                    }
                }

                // Delete Image Ratings
                if (request.DeleteImageRatings)
                {
                    var (irCode, irMsg, irCount) = await _imageQualityRepository.BulkDeleteRatingsByAssessmentCode(request.AssessmentCode, userId);
                    result.ImageRatingsDeleted = irCount;
                    if (irCode != 1)
                    {
                        result.Errors.Add($"Image Ratings: {irMsg}");
                    }
                }

                // Delete Speed Test Logs
                if (request.DeleteSpeedTestLogs)
                {
                    var (stCode, stMsg, stCount) = await _speedTestRepository.BulkDeleteByAssessmentCode(request.AssessmentCode, userId);
                    result.SpeedTestLogsDeleted = stCount;
                    if (stCode != 1)
                    {
                        result.Errors.Add($"Speed Test Logs: {stMsg}");
                    }
                }

                // Delete/Deactivate Images
                if (request.DeleteImages)
                {
                    string imageQuery;
                    List<MySqlParameter> imageParams;

                    if (!string.IsNullOrEmpty(request.ImageGroupCode))
                    {
                        // Delete only specific image group
                        imageQuery = @"
                            UPDATE image_master
                            SET im_active = 0, im_modified_user = @userId, im_modified_date = NOW()
                            WHERE im_assessment_type = @assessmentCode 
                            AND im_group_code = @groupCode 
                            AND im_active = 1";

                        imageParams = new List<MySqlParameter>
                        {
                            new MySqlParameter("@assessmentCode", request.AssessmentCode),
                            new MySqlParameter("@groupCode", request.ImageGroupCode),
                            new MySqlParameter("@userId", userId)
                        };
                    }
                    else
                    {
                        // Delete all images for assessment
                        imageQuery = @"
                            UPDATE image_master
                            SET im_active = 0, im_modified_user = @userId, im_modified_date = NOW()
                            WHERE im_assessment_type = @assessmentCode 
                            AND im_active = 1";

                        imageParams = new List<MySqlParameter>
                        {
                            new MySqlParameter("@assessmentCode", request.AssessmentCode),
                            new MySqlParameter("@userId", userId)
                        };
                    }

                    result.ImagesDeleted = await Task.Run(() => _dbHelper.ExecuteNonQuery(imageQuery, imageParams.ToArray()));

                    // Also deactivate linked images
                    var linkedQuery = @"
                        UPDATE image_linked il
                        INNER JOIN image_master im ON il.il_master_id = im.im_id
                        SET il.im_active = 0
                        WHERE im.im_assessment_type = @assessmentCode";

                    var linkedParams = new[] { new MySqlParameter("@assessmentCode", request.AssessmentCode) };
                    result.LinkedImagesDeleted = await Task.Run(() => _dbHelper.ExecuteNonQuery(linkedQuery, linkedParams));
                }

                // Build summary
                var summary = new System.Text.StringBuilder();
                if (result.UserResponsesDeleted > 0)
                    summary.Append($"{result.UserResponsesDeleted} user response(s) deleted. ");
                if (result.ImageRatingsDeleted > 0)
                    summary.Append($"{result.ImageRatingsDeleted} image rating(s) deleted. ");
                if (result.SpeedTestLogsDeleted > 0)
                    summary.Append($"{result.SpeedTestLogsDeleted} speed test log(s) deleted. ");
                if (result.ImagesDeleted > 0)
                    summary.Append($"{result.ImagesDeleted} master image(s) deactivated. ");
                if (result.LinkedImagesDeleted > 0)
                    summary.Append($"{result.LinkedImagesDeleted} linked image(s) deactivated. ");

                result.Summary = summary.ToString().Trim();
                result.TotalDeleted = result.UserResponsesDeleted + result.ImageRatingsDeleted + 
                                     result.SpeedTestLogsDeleted + result.ImagesDeleted + result.LinkedImagesDeleted;

                return new BulkDeleteResponse
                {
                    OutputCode = result.Errors.Count == 0 ? 1 : 0,
                    OutputMsg = result.Errors.Count == 0 
                        ? "Bulk delete by assessment completed successfully"
                        : $"Completed with {result.Errors.Count} error(s)",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new BulkDeleteResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error during bulk delete: {ex.Message}",
                    Data = new BulkDeleteResult
                    {
                        Errors = new List<string> { ex.Message }
                    }
                };
            }
        }

        public async Task<(int OutputCode, string OutputMsg, object Data)> GetBulkDeletePreview(string assessmentCode, string? groupCode, string userId)
        {
            try
            {
                var userResponseCount = 0;
                var imageRatingCount = 0;
                var speedTestLogCount = 0;
                var masterImagesCount = 0;
                var linkedImagesCount = 0;

                // Count question answers
                var qaQuery = @"
                    SELECT COUNT(*) as cnt 
                    FROM question_answers 
                    WHERE qa_assessment_code = @code";
                var qaParams = new[] { new MySqlParameter("@code", assessmentCode) };
                var qaResult = await Task.Run(() => _dbHelper.ExecuteQuery(qaQuery, qaParams));
                userResponseCount = qaResult.Rows.Count > 0 ? Convert.ToInt32(qaResult.Rows[0]["cnt"]) : 0;

                // Count image ratings
                var irQuery = @"
                    SELECT COUNT(*) as cnt 
                    FROM image_quality_ratings 
                    WHERE iqr_assessment_code = @code";
                var irParams = new[] { new MySqlParameter("@code", assessmentCode) };
                var irResult = await Task.Run(() => _dbHelper.ExecuteQuery(irQuery, irParams));
                imageRatingCount = irResult.Rows.Count > 0 ? Convert.ToInt32(irResult.Rows[0]["cnt"]) : 0;

                // Count speed test logs
                var stQuery = @"
                    SELECT COUNT(*) as cnt 
                    FROM SpeedTestLog 
                    WHERE AssessmentCode = @code";
                var stParams = new[] { new MySqlParameter("@code", assessmentCode) };
                var stResult = await Task.Run(() => _dbHelper.ExecuteQuery(stQuery, stParams));
                speedTestLogCount = stResult.Rows.Count > 0 ? Convert.ToInt32(stResult.Rows[0]["cnt"]) : 0;

                // Count images
                string imQuery;
                MySqlParameter[] imParams;

                if (string.IsNullOrEmpty(groupCode))
                {
                    imQuery = @"
                        SELECT 
                            (SELECT COUNT(*) FROM image_master WHERE im_assessment_type = @code AND im_active = 1) as masters,
                            (SELECT COUNT(*) FROM image_linked WHERE im_active = 1 
                             AND il_master_id IN (SELECT im_id FROM image_master WHERE im_assessment_type = @code)) as linked";
                    imParams = new[] { new MySqlParameter("@code", assessmentCode) };
                }
                else
                {
                    imQuery = @"
                        SELECT 
                            (SELECT COUNT(*) FROM image_master WHERE im_assessment_type = @code AND im_group_code = @group AND im_active = 1) as masters,
                            (SELECT COUNT(*) FROM image_linked WHERE im_active = 1 
                             AND il_master_id IN (SELECT im_id FROM image_master WHERE im_assessment_type = @code AND im_group_code = @group)) as linked";
                    imParams = new[]
                    {
                        new MySqlParameter("@code", assessmentCode),
                        new MySqlParameter("@group", groupCode)
                    };
                }

                var imResult = await Task.Run(() => _dbHelper.ExecuteQuery(imQuery, imParams));
                masterImagesCount = imResult.Rows.Count > 0 ? Convert.ToInt32(imResult.Rows[0]["masters"]) : 0;
                linkedImagesCount = imResult.Rows.Count > 0 ? Convert.ToInt32(imResult.Rows[0]["linked"]) : 0;

                return (1, "Preview generated successfully", new
                {
                    assessmentCode = assessmentCode,
                    imageGroupCode = groupCode,
                    userResponseCount = userResponseCount,
                    imageRatingCount = imageRatingCount,
                    speedTestLogCount = speedTestLogCount,
                    masterImagesCount = masterImagesCount,
                    linkedImagesCount = linkedImagesCount
                });
            }
            catch (Exception ex)
            {
                return (0, $"Error generating preview: {ex.Message}", null);
            }
        }
    }
}