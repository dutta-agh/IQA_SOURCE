using System.Data;
using System.Text.Json;
using MySqlConnector;
using IQA_SOURCE.Models;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class ActionLogRepository : IActionLogRepository
    {
        private readonly IDbHelper _dbHelper;
        private readonly ILogger<ActionLogRepository> _logger;

        public ActionLogRepository(IDbHelper dbHelper, ILogger<ActionLogRepository> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        public async Task<ActionLogResponse> LogUserAction(UserActionLog actionLog)
        {
            try
            {
                var query = @"
                    INSERT INTO user_action_logs (
                        session_id,
                        assessment_type,
                        action_type,
                        action_name,
                        controller_name,
                        action_method,
                        request_url,
                        http_method,
                        request_payload,
                        response_payload,
                        status_code,
                        ip_address,
                        user_agent,
                        action_timestamp,
                        duration_ms,
                        error_message,
                        additional_data
                    )
                    VALUES (
                        @sessionId,
                        @assessmentType,
                        @actionType,
                        @actionName,
                        @controllerName,
                        @actionMethod,
                        @requestUrl,
                        @httpMethod,
                        @requestPayload,
                        @responsePayload,
                        @statusCode,
                        @ipAddress,
                        @userAgent,
                        @actionTimestamp,
                        @durationMs,
                        @errorMessage,
                        @additionalData
                    );
                    SELECT LAST_INSERT_ID();";

                var parameters = new[]
                {
                    new MySqlParameter("@sessionId", (object)actionLog.SessionId ?? DBNull.Value),
                    new MySqlParameter("@assessmentType", (object)actionLog.AssessmentType ?? DBNull.Value),
                    new MySqlParameter("@actionType", (object)actionLog.ActionType ?? DBNull.Value),
                    new MySqlParameter("@actionName", (object)actionLog.ActionName ?? DBNull.Value),
                    new MySqlParameter("@controllerName", (object)actionLog.ControllerName ?? DBNull.Value),
                    new MySqlParameter("@actionMethod", (object)actionLog.ActionMethod ?? DBNull.Value),
                    new MySqlParameter("@requestUrl", (object)actionLog.RequestUrl ?? DBNull.Value),
                    new MySqlParameter("@httpMethod", (object)actionLog.HttpMethod ?? DBNull.Value),
                    new MySqlParameter("@requestPayload", (object)actionLog.RequestPayload ?? DBNull.Value),
                    new MySqlParameter("@responsePayload", (object)actionLog.ResponsePayload ?? DBNull.Value),
                    new MySqlParameter("@statusCode", (object)actionLog.StatusCode ?? DBNull.Value),
                    new MySqlParameter("@ipAddress", (object)actionLog.IpAddress ?? DBNull.Value),
                    new MySqlParameter("@userAgent", (object)actionLog.UserAgent ?? DBNull.Value),
                    new MySqlParameter("@actionTimestamp", actionLog.ActionTimestamp),
                    new MySqlParameter("@durationMs", (object)actionLog.DurationMs ?? DBNull.Value),
                    new MySqlParameter("@errorMessage", (object)actionLog.ErrorMessage ?? DBNull.Value),
                    new MySqlParameter("@additionalData", (object)actionLog.AdditionalData ?? DBNull.Value)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));
                
                long logId = 0;
                if (result.Rows.Count > 0)
                {
                    logId = Convert.ToInt64(result.Rows[0][0]);
                }

                return new ActionLogResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Action logged successfully",
                    LogId = logId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user action");
                return new ActionLogResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error logging action: {ex.Message}"
                };
            }
        }

        public async Task<ActionLogResponse> LogUserActionAsync(
            string sessionId,
            string actionType,
            string actionName,
            string? additionalData = null)
        {
            var actionLog = new UserActionLog
            {
                SessionId = sessionId,
                ActionType = actionType,
                ActionName = actionName,
                ActionTimestamp = DateTime.UtcNow,
                AdditionalData = additionalData
            };

            return await LogUserAction(actionLog);
        }

        public async Task<List<UserActionLog>> GetUserActionsBySession(string sessionId)
        {
            try
            {
                var query = @"
                    SELECT 
                        log_id,
                        session_id,
                        assessment_type,
                        action_type,
                        action_name,
                        controller_name,
                        action_method,
                        request_url,
                        http_method,
                        request_payload,
                        response_payload,
                        status_code,
                        ip_address,
                        user_agent,
                        action_timestamp,
                        duration_ms,
                        error_message,
                        additional_data
                    FROM user_action_logs
                    WHERE session_id = @sessionId
                    ORDER BY action_timestamp ASC";

                var parameters = new[]
                {
                    new MySqlParameter("@sessionId", sessionId)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var logs = new List<UserActionLog>();
                foreach (DataRow row in result.Rows)
                {
                    logs.Add(MapToUserActionLog(row));
                }

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user actions by session");
                return new List<UserActionLog>();
            }
        }

        public async Task<List<UserActionLog>> GetUserActionsByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = @"
                    SELECT 
                        log_id,
                        session_id,
                        assessment_type,
                        action_type,
                        action_name,
                        controller_name,
                        action_method,
                        request_url,
                        http_method,
                        request_payload,
                        response_payload,
                        status_code,
                        ip_address,
                        user_agent,
                        action_timestamp,
                        duration_ms,
                        error_message,
                        additional_data
                    FROM user_action_logs
                    WHERE action_timestamp BETWEEN @startDate AND @endDate
                    ORDER BY action_timestamp DESC";

                var parameters = new[]
                {
                    new MySqlParameter("@startDate", startDate),
                    new MySqlParameter("@endDate", endDate)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var logs = new List<UserActionLog>();
                foreach (DataRow row in result.Rows)
                {
                    logs.Add(MapToUserActionLog(row));
                }

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user actions by date range");
                return new List<UserActionLog>();
            }
        }

        public async Task<Dictionary<string, int>> GetActionStatisticsBySession(string sessionId)
        {
            try
            {
                var query = @"
                    SELECT 
                        action_type,
                        COUNT(*) as action_count
                    FROM user_action_logs
                    WHERE session_id = @sessionId
                    GROUP BY action_type";

                var parameters = new[]
                {
                    new MySqlParameter("@sessionId", sessionId)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var statistics = new Dictionary<string, int>();
                foreach (DataRow row in result.Rows)
                {
                    var actionType = row["action_type"]?.ToString() ?? "Unknown";
                    var count = Convert.ToInt32(row["action_count"]);
                    statistics[actionType] = count;
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving action statistics");
                return new Dictionary<string, int>();
            }
        }

        private UserActionLog MapToUserActionLog(DataRow row)
        {
            return new UserActionLog
            {
                LogId = Convert.ToInt64(row["log_id"]),
                SessionId = row["session_id"]?.ToString(),
                AssessmentType = row["assessment_type"]?.ToString(),
                ActionType = row["action_type"]?.ToString(),
                ActionName = row["action_name"]?.ToString(),
                ControllerName = row["controller_name"]?.ToString(),
                ActionMethod = row["action_method"]?.ToString(),
                RequestUrl = row["request_url"]?.ToString(),
                HttpMethod = row["http_method"]?.ToString(),
                RequestPayload = row["request_payload"]?.ToString(),
                ResponsePayload = row["response_payload"]?.ToString(),
                StatusCode = row["status_code"] != DBNull.Value ? Convert.ToInt32(row["status_code"]) : null,
                IpAddress = row["ip_address"]?.ToString(),
                UserAgent = row["user_agent"]?.ToString(),
                ActionTimestamp = Convert.ToDateTime(row["action_timestamp"]),
                DurationMs = row["duration_ms"] != DBNull.Value ? Convert.ToInt32(row["duration_ms"]) : null,
                ErrorMessage = row["error_message"]?.ToString(),
                AdditionalData = row["additional_data"]?.ToString()
            };
        }
    }
}