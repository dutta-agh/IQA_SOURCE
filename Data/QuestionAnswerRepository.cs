using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class QuestionAnswerRepository : IQuestionAnswerRepository
    {
        private readonly IDbHelper _dbHelper;

        public QuestionAnswerRepository(IDbHelper dbHelper)
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

                // REVERTED: Keep your original column names
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

                    // REVERTED: Keep your original column names
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

        public async Task<QuestionAnswerResponse> GetOrCreateQuestionAnswer(string sessionId, string assessmentCode, string ipAddress, string userAgent, string userId)
        {
            try
            {
                // FIXED: Correct column names from database screenshots
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
                        UrStartTime = null, // Not in database
                        UrSubmitTime = row["UrSubmitTime"] != DBNull.Value ? (DateTime?)row["UrSubmitTime"] : null,
                        UrStatus = row["UrStatus"]?.ToString(),
                        UrCreatedDate = row["UrCreatedDate"] != DBNull.Value ? (DateTime?)row["UrCreatedDate"] : null
                    };
                }

                // FIXED: Correct column names
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
                throw new Exception($"Error creating question answer record: {ex.Message}", ex);
            }
        }

        public async Task<SubmissionResponse> SaveQuestionAnswers(QuestionSubmissionModel submission, string userId)
        {
            try
            {
                var questionAnswer = await GetOrCreateQuestionAnswer(
                    submission.SessionId,
                    submission.AssessmentCode,
                    "::1",
                    "User Agent",
                    userId
                );

                // FIXED: Correct column names - UrdUrid instead of UrdUrId
                var deleteQuery = @"
                    DELETE FROM tbl_question_answer_details 
                    WHERE UrdUrid = @responseId 
                    AND UrdAssessmentCode = @assessmentCode";

                var deleteParams = new[]
                {
                    new MySqlParameter("@responseId", questionAnswer.UrId),
                    new MySqlParameter("@assessmentCode", submission.AssessmentCode)
                };

                await Task.Run(() => _dbHelper.ExecuteNonQuery(deleteQuery, deleteParams));

                // FIXED: Correct column names - UrdUrid and UrdQoid
                int insertedCount = 0;
                foreach (var answer in submission.Answers)
                {
                    if (answer.SelectedOptionIds != null && answer.SelectedOptionIds.Any())
                    {
                        for (int i = 0; i < answer.SelectedOptionIds.Count; i++)
                        {
                            var insertQuery = @"
                                INSERT INTO tbl_question_answer_details 
                                (UrdUrid, UrdAssessmentCode, UrdQoid, UrdQoId, UrdCreatedDate)
                                VALUES 
                                (@responseId, @assessmentCode, @questionId, @answerText, NOW())";

                            var insertParams = new[]
                            {
                                new MySqlParameter("@responseId", questionAnswer.UrId),
                                new MySqlParameter("@assessmentCode", submission.AssessmentCode),
                                new MySqlParameter("@questionId", answer.QuestionId),
                                new MySqlParameter("@answerText", answer.SelectedOptionValues != null && i < answer.SelectedOptionValues.Count 
                                    ? answer.SelectedOptionValues[i] 
                                    : (object)DBNull.Value)
                            };

                            await Task.Run(() => _dbHelper.ExecuteNonQuery(insertQuery, insertParams));
                            insertedCount++;
                        }
                    }
                }

                // FIXED: Correct column name
                var updateQuery = @"
                    UPDATE tbl_question_answers 
                    SET UrSubmitTime = NOW(), UrStatus = 'COMPLETED'
                    WHERE Urid = @responseId";

                var updateParams = new[] { new MySqlParameter("@responseId", questionAnswer.UrId) };
                await Task.Run(() => _dbHelper.ExecuteNonQuery(updateQuery, updateParams));

                return new SubmissionResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Question answers saved successfully",
                    Data = new SubmissionResult
                    {
                        ResponseId = questionAnswer.UrId,
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
                    OutputMsg = $"Error saving question answers: {ex.Message}"
                };
            }
        }

        public async Task<QuestionAnswerResultsResponse> GetAllQuestionAnswers(string userId)
        {
            try
            {
                // FIXED: Correct column names
                var query = @"
                    SELECT 
                        qa.Urid, qa.UrSessionid, qa.UrAssessmentCode, qa.UrIpAddress,
                        qa.UrSubmitTime, qa.UrStatus,
                        atm.atm_name as AssessmentName,
                        COUNT(DISTINCT qad.UrdQoid) as AnsweredQuestions
                    FROM tbl_question_answers qa
                    LEFT JOIN assessment_type_mstr atm ON qa.UrAssessmentCode = atm.atm_code
                    LEFT JOIN tbl_question_answer_details qad ON qa.Urid = qad.UrdUrid
                    GROUP BY qa.Urid, qa.UrSessionid, qa.UrAssessmentCode, qa.UrIpAddress,
                             qa.UrSubmitTime, qa.UrStatus, atm.atm_name
                    ORDER BY qa.UrSubmitTime DESC";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var summaries = new List<QuestionAnswerSummary>();
                foreach (DataRow row in result.Rows)
                {
                    summaries.Add(new QuestionAnswerSummary
                    {
                        ResponseId = Convert.ToInt32(row["Urid"]),
                        SessionId = row["UrSessionid"]?.ToString(),
                        AssessmentCode = row["UrAssessmentCode"]?.ToString(),
                        AssessmentName = row["AssessmentName"]?.ToString(),
                        IpAddress = row["UrIpAddress"]?.ToString(),
                        StartTime = null,
                        SubmitTime = row["UrSubmitTime"] != DBNull.Value ? (DateTime?)row["UrSubmitTime"] : null,
                        Status = row["UrStatus"]?.ToString(),
                        AnsweredQuestions = Convert.ToInt32(row["AnsweredQuestions"])
                    });
                }

                return new QuestionAnswerResultsResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Question answers retrieved successfully",
                    Data = summaries
                };
            }
            catch (Exception ex)
            {
                return new QuestionAnswerResultsResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving question answers: {ex.Message}",
                    Data = new List<QuestionAnswerSummary>()
                };
            }
        }

        public async Task<QuestionAnswerResultsResponse> GetQuestionAnswersByCode(string assessmentCode, string userId)
        {
            try
            {
                // FIXED: Correct column names
                var query = @"
                    SELECT 
                        qa.Urid, qa.UrSessionid, qa.UrAssessmentCode, qa.UrIpAddress,
                        qa.UrSubmitTime, qa.UrStatus,
                        atm.atm_name as AssessmentName
                    FROM tbl_question_answers qa
                    LEFT JOIN assessment_type_mstr atm ON qa.UrAssessmentCode = atm.atm_code
                    WHERE qa.UrAssessmentCode = @assessmentCode
                    ORDER BY qa.UrSubmitTime DESC";

                var parameters = new[] { new MySqlParameter("@assessmentCode", assessmentCode) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var summaries = new List<QuestionAnswerSummary>();
                
                foreach (DataRow row in result.Rows)
                {
                    var responseId = Convert.ToInt32(row["Urid"]);
                    
                    // FIXED: UrdQoid and keep QsCode, QsText, QsType as they were
                    var answersQuery = @"
                        SELECT 
                            qad.UrdQoid, q.QsCode, q.QsText, q.QsType,
                            GROUP_CONCAT(qo.QoText SEPARATOR ', ') as SelectedOptions
                        FROM tbl_question_answer_details qad
                        INNER JOIN tbl_questions q ON qad.UrdQoid = q.QsId
                        LEFT JOIN tbl_question_options qo ON qad.UrdQoId = qo.QoId
                        WHERE qad.UrdUrid = @responseId
                        GROUP BY qad.UrdQoid, q.QsCode, q.QsText, q.QsType
                        ORDER BY q.QsOrderNo";

                    var answersParams = new[] { new MySqlParameter("@responseId", responseId) };
                    var answersResult = await Task.Run(() => _dbHelper.ExecuteQuery(answersQuery, answersParams));

                    var answers = new List<ResponseAnswer>();
                    foreach (DataRow aRow in answersResult.Rows)
                    {
                        answers.Add(new ResponseAnswer
                        {
                            QuestionId = Convert.ToInt32(aRow["UrdQoid"]),
                            QuestionCode = aRow["QsCode"]?.ToString(),
                            QuestionText = aRow["QsText"]?.ToString(),
                            QuestionType = aRow["QsType"]?.ToString(),
                            SelectedOptionsText = aRow["SelectedOptions"]?.ToString()
                        });
                    }

                    summaries.Add(new QuestionAnswerSummary
                    {
                        ResponseId = responseId,
                        SessionId = row["UrSessionid"]?.ToString(),
                        AssessmentCode = row["UrAssessmentCode"]?.ToString(),
                        AssessmentName = row["AssessmentName"]?.ToString(),
                        IpAddress = row["UrIpAddress"]?.ToString(),
                        StartTime = null,
                        SubmitTime = row["UrSubmitTime"] != DBNull.Value ? (DateTime?)row["UrSubmitTime"] : null,
                        Status = row["UrStatus"]?.ToString(),
                        TotalQuestions = answers.Count,
                        AnsweredQuestions = answers.Count,
                        Answers = answers
                    });
                }

                return new QuestionAnswerResultsResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Question answers retrieved successfully",
                    Data = summaries
                };
            }
            catch (Exception ex)
            {
                return new QuestionAnswerResultsResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving question answers: {ex.Message}",
                    Data = new List<QuestionAnswerSummary>()
                };
            }
        }

        public async Task<List<QuestionAnswerExcelRow>> GetQuestionAnswersForExcel(string assessmentCode, string userId)
        {
            try
            {
                // FIXED: Only tbl_question_answers and tbl_question_answer_details columns changed
                var query = @"
                    SELECT 
                        qa.UrSessionid, qa.UrAssessmentCode, qa.UrIpAddress, qa.UrSubmitTime,
                        q.QsId, q.QsCode, q.QsText, q.QsType,
                        GROUP_CONCAT(qo.QoText ORDER BY qad.Urdid SEPARATOR ', ') as SelectedOptions
                    FROM tbl_question_answers qa
                    INNER JOIN tbl_question_answer_details qad ON qa.Urid = qad.UrdUrid
                    INNER JOIN tbl_questions q ON qad.UrdQoid = q.QsId
                    LEFT JOIN tbl_question_options qo ON qad.UrdQoId = qo.QoId
                    WHERE qa.UrAssessmentCode = @assessmentCode
                    GROUP BY qa.UrSessionid, qa.UrAssessmentCode, qa.UrIpAddress, qa.UrSubmitTime,
                             q.QsId, q.QsCode, q.QsText, q.QsType
                    ORDER BY qa.UrSubmitTime DESC, q.QsOrderNo";

                var parameters = new[] { new MySqlParameter("@assessmentCode", assessmentCode) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var rows = new List<QuestionAnswerExcelRow>();
                
                foreach (DataRow row in result.Rows)
                {
                    rows.Add(new QuestionAnswerExcelRow
                    {
                        SessionId = row["UrSessionid"]?.ToString(),
                        AssessmentCode = row["UrAssessmentCode"]?.ToString(),
                        IpAddress = row["UrIpAddress"]?.ToString(),
                        SubmitTime = row["UrSubmitTime"] != DBNull.Value ? (DateTime?)row["UrSubmitTime"] : null,
                        QuestionCode = row["QsCode"]?.ToString(),
                        QuestionText = row["QsText"]?.ToString(),
                        QuestionType = row["QsType"]?.ToString(),
                        OptionColumns = new Dictionary<string, string>()
                    });
                }

                return rows;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error preparing Excel data: {ex.Message}", ex);
            }
        }
    }
}