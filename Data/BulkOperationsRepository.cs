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
                        SELECT ur_assessment_code as assessment_code FROM user_responses WHERE ur_assessment_code IS NOT NULL
                        UNION
                        SELECT iqr_assessment_code as assessment_code FROM image_quality_ratings WHERE iqr_assessment_code IS NOT NULL
                        UNION
                        SELECT stl_assessment_code as assessment_code FROM speed_test_logs WHERE stl_assessment_code IS NOT NULL
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
    }
}