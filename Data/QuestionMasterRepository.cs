using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class QuestionMasterRepository : IQuestionMasterRepository
    {
        private readonly IDbHelper _dbHelper;

        public QuestionMasterRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<QuestionMasterResponse> GetAllQuestions(string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        QsId,
                        QsCode,
                        QsText,
                        QsType,
                        QsCategory,
                        QsMaxSelections,
                        QsOrderNo,
                        QsActive,
                        QsCreatedBy,
                        QsCreatedDate,
                        QsModifiedBy,
                        QsModifiedDate
                    FROM dutta.tbl_questions
                    ORDER BY QsOrderNo, QsCreatedDate DESC";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, null));

                var questions = new List<QuestionMaster>();
                foreach (DataRow row in result.Rows)
                {
                    questions.Add(MapToQuestionMaster(row));
                }

                return new QuestionMasterResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Questions retrieved successfully",
                    Data = questions
                };
            }
            catch (Exception ex)
            {
                return new QuestionMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving questions: {ex.Message}",
                    Data = new List<QuestionMaster>()
                };
            }
        }

        public async Task<QuestionWithOptionsResponse> GetQuestionWithOptions(int qId, string userId)
        {
            try
            {
                // Get question details
                var questionQuery = @"
                    SELECT 
                        QsId,
                        QsCode,
                        QsText,
                        QsType,
                        QsCategory,
                        QsMaxSelections,
                        QsOrderNo,
                        QsActive,
                        QsCreatedBy,
                        QsCreatedDate,
                        QsModifiedBy,
                        QsModifiedDate
                    FROM dutta.tbl_questions
                    WHERE QsId = @qId
                    LIMIT 1";

                var questionParams = new[] { new MySqlParameter("@qId", qId) };
                var questionResult = await Task.Run(() => _dbHelper.ExecuteQuery(questionQuery, questionParams));

                if (questionResult.Rows.Count == 0)
                {
                    return new QuestionWithOptionsResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Question not found",
                        Data = null
                    };
                }

                var question = MapToQuestionMaster(questionResult.Rows[0]);

                // Get question options
                var optionsQuery = @"
                    SELECT 
                        QoId,
                        QoQsId,
                        QoText,
                        QoValue,
                        QoOrderNo,
                        QoActive,
                        QoCreatedDate,
                        QoModifiedDate
                    FROM dutta.tbl_question_options
                    WHERE QoQsId = @qId
                    ORDER BY QoOrderNo";

                var optionsParams = new[] { new MySqlParameter("@qId", qId) };
                var optionsResult = await Task.Run(() => _dbHelper.ExecuteQuery(optionsQuery, optionsParams));

                var options = new List<QuestionOption>();
                foreach (DataRow row in optionsResult.Rows)
                {
                    options.Add(MapToQuestionOption(row));
                }

                return new QuestionWithOptionsResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Question with options retrieved successfully",
                    Data = new QuestionMasterWithOptions
                    {
                        Question = question,
                        Options = options
                    }
                };
            }
            catch (Exception ex)
            {
                return new QuestionWithOptionsResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving question with options: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<QuestionMasterResponse> InsertQuestion(QuestionMaster question, string userId)
        {
            try
            {
                // Check if question code already exists
                var checkQuery = "SELECT COUNT(*) as cnt FROM dutta.tbl_questions WHERE QsCode = @qsCode";
                var checkParams = new[] { new MySqlParameter("@qsCode", question.QsCode) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (checkResult.Rows.Count > 0 && Convert.ToInt32(checkResult.Rows[0]["cnt"]) > 0)
                {
                    return new QuestionMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Question code already exists"
                    };
                }

                var query = @"
                    INSERT INTO dutta.tbl_questions (
                        QsCode,
                        QsText,
                        QsType,
                        QsCategory,
                        QsMaxSelections,
                        QsOrderNo,
                        QsActive,
                        QsCreatedBy,
                        QsCreatedDate
                    )
                    VALUES (
                        @qsCode,
                        @qsText,
                        @qsType,
                        @qsCategory,
                        @qsMaxSelections,
                        @qsOrderNo,
                        @qsActive,
                        @userId,
                        NOW()
                    )";

                var parameters = new[]
                {
                    new MySqlParameter("@qsCode", question.QsCode),
                    new MySqlParameter("@qsText", (object)question.QsText ?? DBNull.Value),
                    new MySqlParameter("@qsType", (object)question.QsType ?? DBNull.Value),
                    new MySqlParameter("@qsCategory", (object)question.QsCategory ?? DBNull.Value),
                    new MySqlParameter("@qsMaxSelections", (object)question.QsMaxSelections ?? DBNull.Value),
                    new MySqlParameter("@qsOrderNo", (object)question.QsOrderNo ?? DBNull.Value),
                    new MySqlParameter("@qsActive", question.QsActive == 0 ? 0 : 1),
                    new MySqlParameter("@userId", userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new QuestionMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Question inserted successfully" : "Insert failed"
                };
            }
            catch (Exception ex)
            {
                return new QuestionMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error inserting question: {ex.Message}"
                };
            }
        }

        public async Task<QuestionMasterResponse> UpdateQuestion(QuestionMaster question, string userId)
        {
            try
            {
                // ✅ REMOVED: No existence check needed - just update directly
                var query = @"
                    UPDATE dutta.tbl_questions
                    SET 
                        QsCode = @qsCode,
                        QsText = @qsText,
                        QsType = @qsType,
                        QsCategory = @qsCategory,
                        QsMaxSelections = @qsMaxSelections,
                        QsOrderNo = @qsOrderNo,
                        QsActive = @qsActive,
                        QsModifiedBy = @userId,
                        QsModifiedDate = NOW()
                    WHERE QsId = @qId";

                var parameters = new[]
                {
                    new MySqlParameter("@qId", question.QId),
                    new MySqlParameter("@qsCode", question.QsCode ?? ""),
                    new MySqlParameter("@qsText", (object)question.QsText ?? DBNull.Value),
                    new MySqlParameter("@qsType", (object)question.QsType ?? DBNull.Value),
                    new MySqlParameter("@qsCategory", (object)question.QsCategory ?? DBNull.Value),
                    new MySqlParameter("@qsMaxSelections", (object)question.QsMaxSelections ?? DBNull.Value),
                    new MySqlParameter("@qsOrderNo", (object)question.QsOrderNo ?? DBNull.Value),
                    new MySqlParameter("@qsActive", question.QsActive),
                    new MySqlParameter("@userId", userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new QuestionMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Question updated successfully" : "Update failed"
                };
            }
            catch (Exception ex)
            {
                return new QuestionMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error updating question: {ex.Message}"
                };
            }
        }

        public async Task<QuestionMasterResponse> DeleteQuestion(int qId, string userId)
        {
            try
            {
                // Check if question exists
                var checkQuery = "SELECT COUNT(*) as cnt FROM dutta.tbl_questions WHERE QsId = @qId";
                var checkParams = new[] { new MySqlParameter("@qId", qId) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (checkResult.Rows.Count == 0 || Convert.ToInt32(checkResult.Rows[0]["cnt"]) == 0)
                {
                    return new QuestionMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Question not found"
                    };
                }

                // Soft delete - set active to 0
                var query = @"
                    UPDATE dutta.tbl_questions
                    SET 
                        QsActive = 0,
                        QsModifiedBy = @userId,
                        QsModifiedDate = NOW()
                    WHERE QsId = @qId";

                var parameters = new[]
                {
                    new MySqlParameter("@qId", qId),
                    new MySqlParameter("@userId", userId)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new QuestionMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Question deleted successfully" : "Delete failed"
                };
            }
            catch (Exception ex)
            {
                return new QuestionMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error deleting question: {ex.Message}"
                };
            }
        }

        public async Task<QuestionMasterResponse> InsertQuestionOption(QuestionOption option, string userId)
        {
            try
            {
                var query = @"
                    INSERT INTO dutta.tbl_question_options (
                        QoQsId,
                        QoText,
                        QoValue,
                        QoOrderNo,
                        QoActive,
                        QoCreatedDate
                    )
                    VALUES (
                        @qoQsId,
                        @qoText,
                        @qoValue,
                        @qoOrderNo,
                        @qoActive,
                        NOW()
                    )";

                var parameters = new[]
                {
                    new MySqlParameter("@qoQsId", option.QoQId),
                    new MySqlParameter("@qoText", (object)option.QoText ?? DBNull.Value),
                    new MySqlParameter("@qoValue", (object)option.QoValue ?? DBNull.Value),
                    new MySqlParameter("@qoOrderNo", (object)option.QoOrderNo ?? DBNull.Value),
                    new MySqlParameter("@qoActive", option.QoActive == 0 ? 0 : 1)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new QuestionMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Question option inserted successfully" : "Insert failed"
                };
            }
            catch (Exception ex)
            {
                return new QuestionMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error inserting question option: {ex.Message}"
                };
            }
        }

        public async Task<QuestionMasterResponse> UpdateQuestionOption(QuestionOption option, string userId)
        {
            try
            {
                // Check if option exists
                var checkQuery = "SELECT COUNT(*) as cnt FROM dutta.tbl_question_options WHERE QoId = @qoId";
                var checkParams = new[] { new MySqlParameter("@qoId", option.QoId) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (checkResult.Rows.Count == 0 || Convert.ToInt32(checkResult.Rows[0]["cnt"]) == 0)
                {
                    return new QuestionMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Question option not found"
                    };
                }

                var query = @"
                    UPDATE dutta.tbl_question_options
                    SET 
                        QoText = @qoText,
                        QoValue = @qoValue,
                        QoOrderNo = @qoOrderNo,
                        QoActive = @qoActive,
                        QoModifiedDate = NOW()
                    WHERE QoId = @qoId";

                var parameters = new[]
                {
                    new MySqlParameter("@qoId", option.QoId),
                    new MySqlParameter("@qoText", (object)option.QoText ?? DBNull.Value),
                    new MySqlParameter("@qoValue", (object)option.QoValue ?? DBNull.Value),
                    new MySqlParameter("@qoOrderNo", (object)option.QoOrderNo ?? DBNull.Value),
                    new MySqlParameter("@qoActive", option.QoActive)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new QuestionMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Question option updated successfully" : "Update failed"
                };
            }
            catch (Exception ex)
            {
                return new QuestionMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error updating question option: {ex.Message}"
                };
            }
        }

        public async Task<QuestionMasterResponse> DeleteQuestionOption(int qoId, string userId)
        {
            try
            {
                // Check if option exists
                var checkQuery = "SELECT COUNT(*) as cnt FROM dutta.tbl_question_options WHERE QoId = @qoId";
                var checkParams = new[] { new MySqlParameter("@qoId", qoId) };
                var checkResult = await Task.Run(() => _dbHelper.ExecuteQuery(checkQuery, checkParams));

                if (checkResult.Rows.Count == 0 || Convert.ToInt32(checkResult.Rows[0]["cnt"]) == 0)
                {
                    return new QuestionMasterResponse
                    {
                        OutputCode = 0,
                        OutputMsg = "Question option not found"
                    };
                }

                // Soft delete - set active to 0
                var query = @"
                    UPDATE dutta.tbl_question_options
                    SET QoActive = 0,
                        QoModifiedDate = NOW()
                    WHERE QoId = @qoId";

                var parameters = new[] { new MySqlParameter("@qoId", qoId) };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));

                return new QuestionMasterResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg = rowsAffected > 0 ? "Question option deleted successfully" : "Delete failed"
                };
            }
            catch (Exception ex)
            {
                return new QuestionMasterResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error deleting question option: {ex.Message}"
                };
            }
        }

        public async Task<List<QuestionOption>> GetQuestionOptions(int qId, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        QoId,
                        QoQsId,
                        QoText,
                        QoValue,
                        QoOrderNo,
                        QoActive,
                        QoCreatedDate,
                        QoModifiedDate
                    FROM dutta.tbl_question_options
                    WHERE QoQsId = @qId
                    ORDER BY QoOrderNo";

                var parameters = new[] { new MySqlParameter("@qId", qId) };
                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var options = new List<QuestionOption>();
                foreach (DataRow row in result.Rows)
                {
                    options.Add(MapToQuestionOption(row));
                }

                return options;
            }
            catch (Exception)
            {
                return new List<QuestionOption>();
            }
        }

        private QuestionMaster MapToQuestionMaster(DataRow row)
        {
            return new QuestionMaster
            {
                QId = Convert.ToInt32(row["QsId"]),
                QsCode = row["QsCode"]?.ToString(),
                QsText = row["QsText"]?.ToString(),
                QsType = row["QsType"]?.ToString(),
                QsCategory = row["QsCategory"]?.ToString(),
                QsMaxSelections = row["QsMaxSelections"] != DBNull.Value ? (int?)row["QsMaxSelections"] : null,
                QsOrderNo = row["QsOrderNo"] != DBNull.Value ? (int?)row["QsOrderNo"] : null,
                QsActive = row["QsActive"] != DBNull.Value ? Convert.ToInt32(row["QsActive"]) : 1,
                QsCreatedBy = row["QsCreatedBy"]?.ToString(),
                QsCreatedDate = row["QsCreatedDate"] != DBNull.Value ? (DateTime?)row["QsCreatedDate"] : null,
                QsModifiedBy = row["QsModifiedBy"] != DBNull.Value ? row["QsModifiedBy"]?.ToString() : null,
                QsModifiedDate = row["QsModifiedDate"] != DBNull.Value ? (DateTime?)row["QsModifiedDate"] : null
            };
        }

        private QuestionOption MapToQuestionOption(DataRow row)
        {
            return new QuestionOption
            {
                QoId = Convert.ToInt32(row["QoId"]),
                QoQId = Convert.ToInt32(row["QoQsId"]),
                QoText = row["QoText"]?.ToString(),
                QoValue = row["QoValue"]?.ToString(),
                QoOrderNo = row["QoOrderNo"] != DBNull.Value ? (int?)row["QoOrderNo"] : null,
                QoActive = row["QoActive"] != DBNull.Value ? Convert.ToInt32(row["QoActive"]) : 1,
                QoCreatedDate = row["QoCreatedDate"] != DBNull.Value ? (DateTime?)row["QoCreatedDate"] : null,
                QoModifiedDate = row["QoModifiedDate"] != DBNull.Value ? (DateTime?)row["QoModifiedDate"] : null
            };
        }
    }
}