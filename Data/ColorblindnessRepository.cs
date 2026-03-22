using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Colorblindness;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class ColorblindnessRepository : IColorblindnessRepository
    {
        private readonly IDbHelper _dbHelper;

        public ColorblindnessRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<ColorblindnessImageResponse> GetAllColorblindnessImages(string assessmentType, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        cb_image_id, assessment_type, image_sequence, image_url,
                        correct_answer, incorrect_option_1, incorrect_option_2,
                        cant_read_option, description, is_active,
                        created_date, created_by, modified_date, modified_by
                    FROM colorblindness_images
                    WHERE assessment_type = @assessmentType
                    ORDER BY image_sequence ASC";

                var parameters = new[] { new MySqlParameter("@assessmentType", assessmentType) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var images = new List<ColorblindnessImage>();
                foreach (DataRow row in result.Rows)
                {
                    images.Add(MapToColorblindnessImage(row));
                }

                return new ColorblindnessImageResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Colorblindness images retrieved successfully",
                    Data = images
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessImageResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving colorblindness images: {ex.Message}",
                    Data = new List<ColorblindnessImage>()
                };
            }
        }

        public async Task<ColorblindnessImageResponse> GetColorblindnessImageById(int imageId, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        cb_image_id, assessment_type, image_sequence, image_url,
                        correct_answer, incorrect_option_1, incorrect_option_2,
                        cant_read_option, description, is_active,
                        created_date, created_by, modified_date, modified_by
                    FROM colorblindness_images
                    WHERE cb_image_id = @imageId
                    LIMIT 1";

                var parameters = new[] { new MySqlParameter("@imageId", imageId) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var images = new List<ColorblindnessImage>();
                if (result.Rows.Count > 0)
                {
                    images.Add(MapToColorblindnessImage(result.Rows[0]));
                }

                return new ColorblindnessImageResponse
                {
                    OutputCode = result.Rows.Count > 0 ? 1 : 0,
                    OutputMsg = result.Rows.Count > 0 ? "Image retrieved successfully" : "Image not found",
                    Data = images
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessImageResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving image: {ex.Message}",
                    Data = new List<ColorblindnessImage>()
                };
            }
        }

        public async Task<ColorblindnessImageResponse> SaveColorblindnessImage(ColorblindnessImage model, string userId)
        {
            try
            {
                if (model.CbImageId > 0)
                {
                    // Update
                    var updateQuery = @"
                        UPDATE colorblindness_images
                        SET image_sequence = @sequence, image_url = @url,
                            correct_answer = @correct, incorrect_option_1 = @incorrect1,
                            incorrect_option_2 = @incorrect2, cant_read_option = @cantRead,
                            description = @description, is_active = @isActive,
                            modified_date = UTC_TIMESTAMP(), modified_by = @userId
                        WHERE cb_image_id = @imageId";

                    var parameters = new[]
                    {
                        new MySqlParameter("@imageId", model.CbImageId),
                        new MySqlParameter("@sequence", model.ImageSequence),
                        new MySqlParameter("@url", model.ImageUrl ?? ""),
                        new MySqlParameter("@correct", model.CorrectAnswer ?? ""),
                        new MySqlParameter("@incorrect1", model.IncorrectOptionOne ?? ""),
                        new MySqlParameter("@incorrect2", model.IncorrectOptionTwo ?? ""),
                        new MySqlParameter("@cantRead", model.CantReadOption ?? "Can't Read"),
                        new MySqlParameter("@description", model.Description ?? ""),
                        new MySqlParameter("@isActive", model.IsActive),
                        new MySqlParameter("@userId", userId)
                    };

                    var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(updateQuery, parameters));
                    return new ColorblindnessImageResponse
                    {
                        OutputCode = rowsAffected > 0 ? 1 : 0,
                        OutputMsg = rowsAffected > 0 ? "Image updated successfully" : "Update failed"
                    };
                }
                else
                {
                    // Insert
                    var insertQuery = @"
                        INSERT INTO colorblindness_images
                        (assessment_type, image_sequence, image_url, correct_answer,
                         incorrect_option_1, incorrect_option_2, cant_read_option,
                         description, is_active, created_date, created_by)
                        VALUES
                        (@assessmentType, @sequence, @url, @correct,
                         @incorrect1, @incorrect2, @cantRead, @description, @isActive,
                         UTC_TIMESTAMP(), @userId)";

                    var parameters = new[]
                    {
                        new MySqlParameter("@assessmentType", model.AssessmentType ?? ""),
                        new MySqlParameter("@sequence", model.ImageSequence),
                        new MySqlParameter("@url", model.ImageUrl ?? ""),
                        new MySqlParameter("@correct", model.CorrectAnswer ?? ""),
                        new MySqlParameter("@incorrect1", model.IncorrectOptionOne ?? ""),
                        new MySqlParameter("@incorrect2", model.IncorrectOptionTwo ?? ""),
                        new MySqlParameter("@cantRead", model.CantReadOption ?? "Can't Read"),
                        new MySqlParameter("@description", model.Description ?? ""),
                        new MySqlParameter("@isActive", model.IsActive),
                        new MySqlParameter("@userId", userId)
                    };

                    var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(insertQuery, parameters));
                    return new ColorblindnessImageResponse
                    {
                        OutputCode = rowsAffected > 0 ? 1 : 0,
                        OutputMsg = rowsAffected > 0 ? "Image inserted successfully" : "Insert failed"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ColorblindnessImageResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error saving image: {ex.Message}"
                };
            }
        }

        public async Task<ColorblindnessImageResponse> DeleteColorblindnessImage(int imageId, string userId)
        {
            try
            {
                var query = @"
                    UPDATE colorblindness_images
                    SET is_active = 0, modified_date = UTC_TIMESTAMP(), modified_by = @userId
                    WHERE cb_image_id = @imageId";

                var parameters = new[]
                {
                    new MySqlParameter("@imageId", imageId),
                    new MySqlParameter("@userId", userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));
                return new ColorblindnessImageResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Image deleted successfully" : "Delete failed"
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessImageResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error deleting image: {ex.Message}"
                };
            }
        }

        public async Task<ColorblindnessTestResponse> GetColorblindnessImagesForTest(string assessmentType)
        {
            try
            {
                var query = @"
                    SELECT 
                        cb_image_id, image_sequence, image_url,
                        correct_answer, incorrect_option_1, incorrect_option_2, cant_read_option
                    FROM colorblindness_images
                    WHERE assessment_type = @assessmentType AND is_active = 1
                    ORDER BY image_sequence ASC
                    LIMIT 12";

                var parameters = new[] { new MySqlParameter("@assessmentType", assessmentType) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var images = new List<ColorblindnessImageForTest>();
                foreach (DataRow row in result.Rows)
                {
                    var correctAnswer = row["correct_answer"]?.ToString() ?? "";
                    var options = new List<string>
                    {
                        correctAnswer,
                        row["incorrect_option_1"]?.ToString() ?? "",
                       
                        row["cant_read_option"]?.ToString() ?? "Can't Read"
                    };
                    
                    var image = new ColorblindnessImageForTest
                    {
                        CbImageId = Convert.ToInt32(row["cb_image_id"]),
                        ImageSequence = Convert.ToInt32(row["image_sequence"]),
                        ImageUrl = row["image_url"]?.ToString() ?? "",
                        CorrectAnswer = correctAnswer,
                        Options = options
                    };
                    images.Add(image);
                }

                // Shuffle images list using better algorithm
                images = images.OrderBy(_ => Guid.NewGuid()).ToList();

                return new ColorblindnessTestResponse
                {
                    OutputCode = images.Count > 0 ? 1 : 0,
                    OutputMsg = images.Count > 0 ? "Colorblindness test images loaded successfully" : "No images found",
                    Data = images
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessTestResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error loading test images: {ex.Message}",
                    Data = new List<ColorblindnessImageForTest>()
                };
            }
        }

        public async Task<ColorblindnessTestResultResponse> SaveUserResponses(List<UserColorblindnessResponse> responses, string userId)
        {
            try
            {
                if (responses == null || responses.Count == 0)
                {
                    return new ColorblindnessTestResultResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "No responses to save"
                    };
                }

                var insertQuery = @"
                    INSERT INTO user_colorblindness_responses
                    (session_id, assessment_type, image_id, image_sequence, selected_answer,
                     correct_answer, is_correct, time_taken_seconds, response_time,
                     ip_address, user_agent)
                    VALUES
                    (@sessionId, @assessmentType, @imageId, @sequence, @selectedAnswer,
                     @correctAnswer, @isCorrect, @timeTaken, UTC_TIMESTAMP(),
                     @ipAddress, @userAgent)";

                int successCount = 0;
                int failureCount = 0;
                var failedResponses = new List<string>();

                foreach (var response in responses)
                {
                    try
                    {
                        // Validate response data
                        if (string.IsNullOrWhiteSpace(response.SessionId))
                        {
                            failureCount++;
                            failedResponses.Add($"Image {response.ImageId}: Missing session ID");
                            continue;
                        }

                        if (response.ImageId <= 0)
                        {
                            failureCount++;
                            failedResponses.Add($"Invalid image ID");
                            continue;
                        }

                        var parameters = new[]
                        {
                            new MySqlParameter("@sessionId", response.SessionId),
                            new MySqlParameter("@assessmentType", response.AssessmentType ?? ""),
                            new MySqlParameter("@imageId", response.ImageId),
                            new MySqlParameter("@sequence", response.ImageSequence ?? 0),
                            new MySqlParameter("@selectedAnswer", response.SelectedAnswer ?? ""),
                            new MySqlParameter("@correctAnswer", response.CorrectAnswer ?? ""),
                            new MySqlParameter("@isCorrect", response.IsCorrect ? 1 : 0),
                            new MySqlParameter("@timeTaken", response.TimeTakenSeconds ?? 0),
                            new MySqlParameter("@ipAddress", response.IpAddress ?? ""),
                            new MySqlParameter("@userAgent", response.UserAgent ?? "")
                        };

                        int rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(insertQuery, parameters));
                        
                        if (rowsAffected > 0)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                            failedResponses.Add($"Image {response.ImageId}: Failed to insert response");
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        failedResponses.Add($"Image {response.ImageId}: {ex.Message}");
                    }
                }

                string message = $"Saved {successCount} responses successfully";
                if (failureCount > 0)
                {
                    message += $". {failureCount} response(s) failed.";
                    if (failedResponses.Count > 0 && failedResponses.Count <= 5)
                    {
                        message += " Details: " + string.Join("; ", failedResponses);
                    }
                }

                return new ColorblindnessTestResultResponse
                {
                    OutputCode = successCount > 0 ? 1 : 0,
                    OutputMsg = message,
                    SuccessCount = successCount,
                    FailureCount = failureCount
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessTestResultResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error saving responses: {ex.Message}",
                    SuccessCount = 0,
                    FailureCount = responses?.Count ?? 0
                };
            }
        }

        public async Task<ColorblindnessTestResultResponse> GetColorblindnessResultsBySession(string sessionId, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        session_id, assessment_type,
                        COUNT(*) as total_attempted,
                        SUM(CASE WHEN is_correct = 1 THEN 1 ELSE 0 END) as correct_answers,
                        SUM(CASE WHEN is_correct = 0 AND selected_answer != 'Can''t Read' THEN 1 ELSE 0 END) as wrong_answers,
                        SUM(CASE WHEN selected_answer IS NULL OR selected_answer = '' THEN 1 ELSE 0 END) as skipped_answers,
                        SUM(CASE WHEN selected_answer = 'Can''t Read' THEN 1 ELSE 0 END) as cant_read_answers,
                        AVG(time_taken_seconds) as avg_time,
                        MIN(response_time) as start_time,
                        MAX(response_time) as end_time,
                        ip_address
                    FROM user_colorblindness_responses
                    WHERE session_id = @sessionId
                    GROUP BY session_id, assessment_type, ip_address";

                var parameters = new[] { new MySqlParameter("@sessionId", sessionId) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var results = new List<ColorblindnessTestResult>();
                foreach (DataRow row in result.Rows)
                {
                    var totalAttempted = Convert.ToInt32(row["total_attempted"] ?? 0);
                    var correctAnswers = Convert.ToInt32(row["correct_answers"] ?? 0);
                    var accuracy = totalAttempted > 0 ? (correctAnswers * 100.0) / totalAttempted : 0;

                    results.Add(new ColorblindnessTestResult
                    {
                        SessionId = row["session_id"]?.ToString() ?? "",
                        AssessmentType = row["assessment_type"]?.ToString() ?? "",
                        TotalQuestions = totalAttempted,
                        CorrectAnswers = correctAnswers,
                        WrongAnswers = Convert.ToInt32(row["wrong_answers"] ?? 0),
                        SkippedQuestions = Convert.ToInt32(row["skipped_answers"] ?? 0),
                        CantReadAnswers = Convert.ToInt32(row["cant_read_answers"] ?? 0),
                        AccuracyPercentage = accuracy,
                        AverageTimePerQuestion = row["avg_time"] != DBNull.Value ? Convert.ToDouble(row["avg_time"]) : 0,
                        TestDateTime = row["start_time"] != DBNull.Value ? (DateTime)row["start_time"] : DateTime.UtcNow,
                        TestEndTime = row["end_time"] != DBNull.Value ? (DateTime?)row["end_time"] : null,
                        IpAddress = row["ip_address"]?.ToString() ?? "",
                        TestStatus = totalAttempted == 12 ? "Completed" : "In Progress",
                        IsPassed = accuracy >= 80
                    });
                }

                return new ColorblindnessTestResultResponse
                {
                    OutputCode = results.Count > 0 ? 1 : 0,
                    OutputMsg = results.Count > 0 ? "Results retrieved successfully" : "No results found",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessTestResultResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving results: {ex.Message}",
                    Data = new List<ColorblindnessTestResult>()
                };
            }
        }

        public async Task<ColorblindnessTestResultResponse> GetAllColorblindnessResults(string assessmentType, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        session_id, assessment_type,
                        COUNT(*) as total_attempted,
                        SUM(CASE WHEN is_correct = 1 THEN 1 ELSE 0 END) as correct_answers,
                        SUM(CASE WHEN is_correct = 0 AND selected_answer != 'Can''t Read' THEN 1 ELSE 0 END) as wrong_answers,
                        SUM(CASE WHEN selected_answer IS NULL OR selected_answer = '' THEN 1 ELSE 0 END) as skipped_answers,
                        SUM(CASE WHEN selected_answer = 'Can''t Read' THEN 1 ELSE 0 END) as cant_read_answers,
                        AVG(time_taken_seconds) as avg_time,
                        MIN(response_time) as start_time,
                        MAX(response_time) as end_time,
                        ip_address
                    FROM user_colorblindness_responses
                    WHERE assessment_type = @assessmentType
                    GROUP BY session_id, assessment_type, ip_address
                    ORDER BY start_time DESC";

                var parameters = new[] { new MySqlParameter("@assessmentType", assessmentType) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var results = new List<ColorblindnessTestResult>();
                foreach (DataRow row in result.Rows)
                {
                    var totalAttempted = Convert.ToInt32(row["total_attempted"] ?? 0);
                    var correctAnswers = Convert.ToInt32(row["correct_answers"] ?? 0);
                    var accuracy = totalAttempted > 0 ? (correctAnswers * 100.0) / totalAttempted : 0;

                    results.Add(new ColorblindnessTestResult
                    {
                        SessionId = row["session_id"]?.ToString() ?? "",
                        AssessmentType = row["assessment_type"]?.ToString() ?? "",
                        TotalQuestions = totalAttempted,
                        CorrectAnswers = correctAnswers,
                        WrongAnswers = Convert.ToInt32(row["wrong_answers"] ?? 0),
                        SkippedQuestions = Convert.ToInt32(row["skipped_answers"] ?? 0),
                        CantReadAnswers = Convert.ToInt32(row["cant_read_answers"] ?? 0),
                        AccuracyPercentage = accuracy,
                        AverageTimePerQuestion = row["avg_time"] != DBNull.Value ? Convert.ToDouble(row["avg_time"]) : 0,
                        TestDateTime = row["start_time"] != DBNull.Value ? (DateTime)row["start_time"] : DateTime.UtcNow,
                        TestEndTime = row["end_time"] != DBNull.Value ? (DateTime?)row["end_time"] : null,
                        IpAddress = row["ip_address"]?.ToString() ?? "",
                        TestStatus = totalAttempted == 12 ? "Completed" : "In Progress",
                        IsPassed = accuracy >= 80
                    });
                }

                return new ColorblindnessTestResultResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Results retrieved successfully",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessTestResultResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving results: {ex.Message}",
                    Data = new List<ColorblindnessTestResult>()
                };
            }
        }

        public async Task<ColorblindnessTestResultResponse> GetColorblindnessResultsByDateRange(string assessmentType, DateTime startDate, DateTime endDate, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        session_id, assessment_type,
                        COUNT(*) as total_attempted,
                        SUM(CASE WHEN is_correct = 1 THEN 1 ELSE 0 END) as correct_answers,
                        SUM(CASE WHEN is_correct = 0 AND selected_answer != 'Can''t Read' THEN 1 ELSE 0 END) as wrong_answers,
                        SUM(CASE WHEN selected_answer IS NULL OR selected_answer = '' THEN 1 ELSE 0 END) as skipped_answers,
                        SUM(CASE WHEN selected_answer = 'Can''t Read' THEN 1 ELSE 0 END) as cant_read_answers,
                        AVG(time_taken_seconds) as avg_time,
                        MIN(response_time) as start_time,
                        MAX(response_time) as end_time,
                        ip_address
                    FROM user_colorblindness_responses
                    WHERE assessment_type = @assessmentType
                    AND DATE(response_time) BETWEEN DATE(@startDate) AND DATE(@endDate)
                    GROUP BY session_id, assessment_type, ip_address
                    ORDER BY start_time DESC";

                var parameters = new[]
                {
                    new MySqlParameter("@assessmentType", assessmentType),
                    new MySqlParameter("@startDate", startDate),
                    new MySqlParameter("@endDate", endDate)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var results = new List<ColorblindnessTestResult>();
                foreach (DataRow row in result.Rows)
                {
                    var totalAttempted = Convert.ToInt32(row["total_attempted"] ?? 0);
                    var correctAnswers = Convert.ToInt32(row["correct_answers"] ?? 0);
                    var accuracy = totalAttempted > 0 ? (correctAnswers * 100.0) / totalAttempted : 0;

                    results.Add(new ColorblindnessTestResult
                    {
                        SessionId = row["session_id"]?.ToString() ?? "",
                        AssessmentType = row["assessment_type"]?.ToString() ?? "",
                        TotalQuestions = totalAttempted,
                        CorrectAnswers = correctAnswers,
                        WrongAnswers = Convert.ToInt32(row["wrong_answers"] ?? 0),
                        SkippedQuestions = Convert.ToInt32(row["skipped_answers"] ?? 0),
                        CantReadAnswers = Convert.ToInt32(row["cant_read_answers"] ?? 0),
                        AccuracyPercentage = accuracy,
                        AverageTimePerQuestion = row["avg_time"] != DBNull.Value ? Convert.ToDouble(row["avg_time"]) : 0,
                        TestDateTime = row["start_time"] != DBNull.Value ? (DateTime)row["start_time"] : DateTime.UtcNow,
                        TestEndTime = row["end_time"] != DBNull.Value ? (DateTime?)row["end_time"] : null,
                        IpAddress = row["ip_address"]?.ToString() ?? "",
                        TestStatus = totalAttempted == 12 ? "Completed" : "In Progress",
                        IsPassed = accuracy >= 80
                    });
                }

                return new ColorblindnessTestResultResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Results retrieved successfully",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessTestResultResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving results: {ex.Message}",
                    Data = new List<ColorblindnessTestResult>()
                };
            }
        }

        public async Task<ColorblindnessTestResultResponse> GetColorblindnessResultsBySessionId(string sessionId, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        session_id, assessment_type,
                        COUNT(*) as total_attempted,
                        SUM(CASE WHEN is_correct = 1 THEN 1 ELSE 0 END) as correct_answers,
                        SUM(CASE WHEN is_correct = 0 AND selected_answer != 'Can''t Read' THEN 1 ELSE 0 END) as wrong_answers,
                        SUM(CASE WHEN selected_answer IS NULL OR selected_answer = '' THEN 1 ELSE 0 END) as skipped_answers,
                        SUM(CASE WHEN selected_answer = 'Can''t Read' THEN 1 ELSE 0 END) as cant_read_answers,
                        AVG(time_taken_seconds) as avg_time,
                        MIN(response_time) as start_time,
                        MAX(response_time) as end_time,
                        ip_address
                    FROM user_colorblindness_responses
                    WHERE session_id = @sessionId
                    GROUP BY session_id, assessment_type, ip_address";

                var parameters = new[] { new MySqlParameter("@sessionId", sessionId) };
                var resultData = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var results = new List<ColorblindnessTestResult>();
                
                if (resultData.Rows.Count > 0)
                {
                    var row = resultData.Rows[0];
                    var totalAttempted = Convert.ToInt32(row["total_attempted"] ?? 0);
                    var correctAnswers = Convert.ToInt32(row["correct_answers"] ?? 0);
                    var accuracy = totalAttempted > 0 ? (correctAnswers * 100.0) / totalAttempted : 0;

                    var testResult = new ColorblindnessTestResult
                    {
                        SessionId = row["session_id"]?.ToString() ?? "",
                        AssessmentType = row["assessment_type"]?.ToString() ?? "",
                        TotalQuestions = totalAttempted,
                        CorrectAnswers = correctAnswers,
                        WrongAnswers = Convert.ToInt32(row["wrong_answers"] ?? 0),
                        SkippedQuestions = Convert.ToInt32(row["skipped_answers"] ?? 0),
                        CantReadAnswers = Convert.ToInt32(row["cant_read_answers"] ?? 0),
                        AccuracyPercentage = accuracy,
                        AverageTimePerQuestion = row["avg_time"] != DBNull.Value ? Convert.ToDouble(row["avg_time"]) : 0,
                        TestDateTime = row["start_time"] != DBNull.Value ? (DateTime)row["start_time"] : DateTime.UtcNow,
                        TestEndTime = row["end_time"] != DBNull.Value ? (DateTime?)row["end_time"] : null,
                        IpAddress = row["ip_address"]?.ToString() ?? "",
                        TestStatus = totalAttempted == 12 ? "Completed" : "In Progress",
                        IsPassed = accuracy >= 80
                    };

                    // ✅ Get image-wise results with imageUrl
                    var imageWiseQuery = @"
                        SELECT 
                            ucr.image_sequence, ucr.image_id, ucr.selected_answer,
                            ucr.correct_answer, ucr.is_correct, ucr.time_taken_seconds,
                            ci.image_url
                        FROM user_colorblindness_responses ucr
                        LEFT JOIN colorblindness_images ci ON ucr.image_id = ci.cb_image_id
                        WHERE ucr.session_id = @sessionId
                        ORDER BY ucr.image_sequence ASC";

                    var imageData = await Task.Run(() => _dbHelper.ExecuteQuery(imageWiseQuery, parameters));

                    foreach (DataRow imgRow in imageData.Rows)
                    {
                        var selectedAnswer = imgRow["selected_answer"]?.ToString() ?? "";
                        var correctAnswer = imgRow["correct_answer"]?.ToString() ?? "";
                        var isCorrect = Convert.ToInt32(imgRow["is_correct"] ?? 0) == 1;
                        var imageUrl = imgRow["image_url"]?.ToString() ?? "";

                        string resultStatus = "Incorrect";
                        if (string.IsNullOrEmpty(selectedAnswer) || selectedAnswer == "null")
                        {
                            resultStatus = "Skipped";
                        }
                        else if (isCorrect)
                        {
                            resultStatus = "Correct";
                        }

                        testResult.ImageWiseResults.Add(new ImageWiseResult
                        {
                            ImageSequence = Convert.ToInt32(imgRow["image_sequence"] ?? 0),
                            ImageId = Convert.ToInt32(imgRow["image_id"] ?? 0),
                            ImageUrl = imageUrl,
                            SelectedAnswer = selectedAnswer,
                            CorrectAnswer = correctAnswer,
                            IsCorrect = isCorrect,
                            ResultStatus = resultStatus,
                            TimeTakenSeconds = Convert.ToInt32(imgRow["time_taken_seconds"] ?? 0)
                        });
                    }

                    results.Add(testResult);
                }

                return new ColorblindnessTestResultResponse
                {
                    OutputCode = results.Count > 0 ? 1 : 0,
                    OutputMsg = results.Count > 0 ? "Results retrieved successfully" : "No results found",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                return new ColorblindnessTestResultResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving results: {ex.Message}",
                    Data = new List<ColorblindnessTestResult>()
                };
            }
        }

        private ColorblindnessImage MapToColorblindnessImage(DataRow row)
        {
            return new ColorblindnessImage
            {
                CbImageId = Convert.ToInt32(row["cb_image_id"]),
                AssessmentType = row["assessment_type"]?.ToString() ?? "",
                ImageSequence = Convert.ToInt32(row["image_sequence"]),
                ImageUrl = row["image_url"]?.ToString() ?? "",
                CorrectAnswer = row["correct_answer"]?.ToString() ?? "",
                IncorrectOptionOne = row["incorrect_option_1"]?.ToString() ?? "",
                IncorrectOptionTwo = row["incorrect_option_2"]?.ToString() ?? "",
                CantReadOption = row["cant_read_option"]?.ToString() ?? "Can't Read",
                Description = row["description"]?.ToString() ?? "",
                IsActive = row["is_active"] != DBNull.Value ? Convert.ToInt32(row["is_active"]) : 1,
                CreatedDate = row["created_date"] != DBNull.Value ? (DateTime)row["created_date"] : DateTime.UtcNow,
                CreatedBy = row["created_by"]?.ToString() ?? "",
                ModifiedDate = row["modified_date"] != DBNull.Value ? (DateTime?)row["modified_date"] : null,
                ModifiedBy = row["modified_by"]?.ToString()
            };
        }
    }
}