using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class UserResponseRepository : IUserResponseRepository
    {
        private readonly IDbHelper _dbHelper;

        public UserResponseRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<QuestionPageViewModel> GetQuestionsForAssessment(string assessmentCode, string userId)
        {
            try
            {
                var assessmentQuery = @"
                    SELECT atm_code, atm_name 
                    FROM assessment_type_mstr 
                    WHERE atm_code = @assessmentCode 
                    AND atm_active = 'Y'
                    LIMIT 1";

                var assessmentParams = new[] { new MySqlParameter("@assessmentCode", assessmentCode) };
                var assessmentResult = await Task.Run(() => _dbHelper.ExecuteQuery(assessmentQuery, assessmentParams));

                if (assessmentResult.Rows.Count == 0)
                {
                    return null;
                }

                var viewModel = new QuestionPageViewModel
                {
                    AssessmentCode = assessmentCode,
                    AssessmentName = assessmentResult.Rows[0]["atm_name"]?.ToString(),
                    Questions = new List<QuestionWithOptions>()
                };

                var questionsQuery = @"
                    SELECT 
                        QsId, QsCode, QsText, QsType, QsCategory,
                        QsMaxSelections, QsOrderNo, QsActive
                    FROM tbl_questions
                    WHERE QsActive = 1
                    ORDER BY QsOrderNo, QsId";

                var questionsResult = await Task.Run(() => _dbHelper.ExecuteQuery(questionsQuery, null));

                foreach (DataRow qRow in questionsResult.Rows)
                {
                    var question = new QuestionMaster
                    {
                        QId = Convert.ToInt32(qRow["QsId"]),
                        QsCode = qRow["QsCode"]?.ToString(),
                        QsText = qRow["QsText"]?.ToString(),
                        QsType = qRow["QsType"]?.ToString(),
                        QsCategory = qRow["QsCategory"]?.ToString(),
                        QsMaxSelections = qRow["QsMaxSelections"] != DBNull.Value ? (int?)Convert.ToInt32(qRow["QsMaxSelections"]) : null,
                        QsOrderNo = qRow["QsOrderNo"] != DBNull.Value ? (int?)Convert.ToInt32(qRow["QsOrderNo"]) : null,
                        QsActive = Convert.ToInt32(qRow["QsActive"])
                    };

                    var optionsQuery = @"
                        SELECT 
                            QoId, QoQsId, QoText, QoValue, QoOrderNo, QoActive
                        FROM tbl_question_options
                        WHERE QoQsId = @questionId
                        AND QoActive = 1
                        ORDER BY QoOrderNo, QoId";

                    var optionsParams = new[] { new MySqlParameter("@questionId", question.QId) };
                    var optionsResult = await Task.Run(() => _dbHelper.ExecuteQuery(optionsQuery, optionsParams));

                    var options = new List<QuestionOption>();
                    foreach (DataRow oRow in optionsResult.Rows)
                    {
                        options.Add(new QuestionOption
                        {
                            QoId = Convert.ToInt32(oRow["QoId"]),
                            QoQId = Convert.ToInt32(oRow["QoQsId"]),
                            QoText = oRow["QoText"]?.ToString(),
                            QoValue = oRow["QoValue"]?.ToString(),
                            QoOrderNo = oRow["QoOrderNo"] != DBNull.Value ? (int?)Convert.ToInt32(oRow["QoOrderNo"]) : null,
                            QoActive = Convert.ToInt32(oRow["QoActive"])
                        });
                    }

                    viewModel.Questions.Add(new QuestionWithOptions
                    {
                        Question = question,
                        Options = options
                    });
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading questions: {ex.Message}", ex);
            }
        }

        public async Task<QuestionAnswerResponse> GetOrCreateUserResponse(string sessionId, string assessmentCode, string ipAddress, string userAgent, string userId)
        {
            try
            {
                var checkQuery = @"
                    SELECT Urid, UrSessionid, UrAssessmentCode, UrIpAddress, 
                           UrUserAgent, UrSubmitTime, UrStatus, UrCreatedDate
                    FROM tbl_question_answers
                    WHERE UrSessionid = @sessionId 
                    AND UrAssessmentCode = @assessmentCode
                    LIMIT 1";

                var checkParams = new[]
                {
                    new MySqlParameter("@sessionId", sessionId),
                    new MySqlParameter("@assessmentCode", assessmentCode)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    return new QuestionAnswerResponse
                    {
                        UrId = Convert.ToInt32(row["Urid"]),
                        UrSessionId = row["UrSessionid"]?.ToString(),
                        UrAssessmentCode = row["UrAssessmentCode"]?.ToString(),
                        UrIpAddress = row["UrIpAddress"]?.ToString(),
                        UrUserAgent = row["UrUserAgent"]?.ToString(),
                        UrStartTime = null,
                        UrSubmitTime = row["UrSubmitTime"] != DBNull.Value ? (DateTime?)row["UrSubmitTime"] : null,
                        UrStatus = row["UrStatus"]?.ToString(),
                        UrCreatedDate = row["UrCreatedDate"] != DBNull.Value ? (DateTime?)row["UrCreatedDate"] : null
                    };
                }

                var insertQuery = @"
                    INSERT INTO tbl_question_answers 
                    (UrSessionid, UrAssessmentCode, UrIpAddress, UrUserAgent, UrStatus, UrCreatedDate)
                    VALUES 
                    (@sessionId, @assessmentCode, @ipAddress, @userAgent, 'IN_PROGRESS', NOW());
                    SELECT LAST_INSERT_ID();";

                var insertParams = new[]
                {
                    new MySqlParameter("@sessionId", sessionId),
                    new MySqlParameter("@assessmentCode", assessmentCode),
                    new MySqlParameter("@ipAddress", ipAddress),
                    new MySqlParameter("@userAgent", userAgent)
                };

                var insertResult = await Task.Run(() => _dbHelper.ExecuteQuery(insertQuery, insertParams));
                var newId = Convert.ToInt32(insertResult.Rows[0][0]);

                return new QuestionAnswerResponse
                {
                    UrId = newId,
                    UrSessionId = sessionId,
                    UrAssessmentCode = assessmentCode,
                    UrIpAddress = ipAddress,
                    UrUserAgent = userAgent,
                    UrStartTime = null,
                    UrStatus = "IN_PROGRESS",
                    UrCreatedDate = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating user response: {ex.Message}", ex);
            }
        }

        public async Task<SubmissionResponse> SaveUserResponses(QuestionSubmissionModel submission, string userId)
        {
            try
            {
                var userResponse = await GetOrCreateUserResponse(
                    submission.SessionId,
                    submission.AssessmentCode,
                    "::1",
                    "User Agent",
                    userId
                );

                // Delete existing responses
                var deleteQuery = @"
                    DELETE FROM tbl_question_answer_details 
                    WHERE UrdUrid = @responseId 
                    AND UrdAssessmentCode = @assessmentCode";

                var deleteParams = new[]
                {
                    new MySqlParameter("@responseId", userResponse.UrId),
                    new MySqlParameter("@assessmentCode", submission.AssessmentCode)
                };

                await Task.Run(() => _dbHelper.ExecuteNonQuery(deleteQuery, deleteParams));

                // Insert new responses with both option ID and option text
                int insertedCount = 0;
                foreach (var answer in submission.Answers)
                {
                    if (answer.SelectedOptionIds != null && answer.SelectedOptionIds.Any())
                    {
                        for (int i = 0; i < answer.SelectedOptionIds.Count; i++)
                        {
                            // FIXED: Added UrdQoId column to save option ID, UrdAnswerText saves option description
                            var insertQuery = @"
                                INSERT INTO tbl_question_answer_details 
                                (UrdUrid, UrdAssessmentCode, UrdQsId, UrdQoId, UrdAnswerText, UrdCreatedDate)
                                VALUES 
                                (@responseId, @assessmentCode, @questionId, @optionId, @answerText, NOW())";

                            var insertParams = new[]
                            {
                                new MySqlParameter("@responseId", userResponse.UrId),
                                new MySqlParameter("@assessmentCode", submission.AssessmentCode),
                                new MySqlParameter("@questionId", answer.QuestionId),
                                new MySqlParameter("@optionId", answer.SelectedOptionIds[i]),
                                new MySqlParameter("@answerText", 
                                    answer.SelectedOptionValues != null && i < answer.SelectedOptionValues.Count 
                                        ? answer.SelectedOptionValues[i] 
                                        : (object)DBNull.Value)
                            };

                            await Task.Run(() => _dbHelper.ExecuteNonQuery(insertQuery, insertParams));
                            insertedCount++;
                        }
                    }
                }

                // Update submission status
                var updateQuery = @"
                    UPDATE tbl_question_answers 
                    SET UrSubmitTime = NOW(), UrStatus = 'COMPLETED'
                    WHERE Urid = @responseId";

                var updateParams = new[] { new MySqlParameter("@responseId", userResponse.UrId) };
                await Task.Run(() => _dbHelper.ExecuteNonQuery(updateQuery, updateParams));

                return new SubmissionResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Responses saved successfully",
                    Data = new SubmissionResult
                    {
                        ResponseId = userResponse.UrId,
                        TotalQuestions = submission.Answers.Count,
                        AnsweredQuestions = insertedCount,
                        SubmittedAt = DateTime.Now
                    }
                };
            }
            catch (Exception ex)
            {
                return new SubmissionResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error saving responses: {ex.Message}"
                };
            }
        }
    }
}