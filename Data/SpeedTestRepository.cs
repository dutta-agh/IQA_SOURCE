using System.Data;
using MySqlConnector;
using IQA_SOURCE.Models.Admin;
using YourApp.Data;

namespace IQA_SOURCE.Data
{
    public class SpeedTestRepository : ISpeedTestRepository
    {
        private readonly IDbHelper _dbHelper;

        public SpeedTestRepository(IDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public async Task<SpeedTestLogResponse> SaveSpeedTestLog(SpeedTestLogSaveRequest request, string ipAddress, string userAgent, string? referrerUrl)
        {
            try
            {
                var query = @"
                    INSERT INTO SpeedTestLog (
                        IpAddress, UserAgent, ScreenWidth, ScreenHeight,
                        DownloadSpeedMbps, ResolutionPassed, SpeedPassed, OverallPassed,
                        TestDateTime, SessionId, ReferrerUrl, BrowserInfo, AssessmentCode,
                        PrivateModeDetected, PrivateModeBrowser, DeviceType, UploadSpeedMbps, Latency
                    )
                    VALUES (
                        @ipAddress, @userAgent, @screenWidth, @screenHeight,
                        @downloadSpeed, @resolutionPassed, @speedPassed, @overallPassed,
                        UTC_TIMESTAMP(), @sessionId, @referrerUrl, @browserInfo, @assessmentCode,
                        @privateModeDetected, @privateModeBrowser, @deviceType, @uploadSpeed, @latency
                    )";

                var parameters = new[]
                {
                    new MySqlParameter("@ipAddress",           ipAddress ?? "Unknown"),
                    new MySqlParameter("@userAgent",           userAgent ?? "Unknown"),
                    new MySqlParameter("@screenWidth",         (object?)request.ScreenWidth         ?? DBNull.Value),
                    new MySqlParameter("@screenHeight",        (object?)request.ScreenHeight        ?? DBNull.Value),
                    new MySqlParameter("@downloadSpeed",       (object?)request.DownloadSpeedMbps   ?? DBNull.Value),
                    new MySqlParameter("@resolutionPassed",    request.ResolutionPassed),
                    new MySqlParameter("@speedPassed",         request.SpeedPassed),
                    new MySqlParameter("@overallPassed",       request.OverallPassed),
                    new MySqlParameter("@sessionId",           request.SessionId ?? Guid.NewGuid().ToString()),
                    new MySqlParameter("@referrerUrl",         (object?)referrerUrl                 ?? DBNull.Value),
                    new MySqlParameter("@browserInfo",         userAgent ?? "Unknown"),
                    new MySqlParameter("@assessmentCode",      (object?)request.AssessmentCode      ?? DBNull.Value),
                    new MySqlParameter("@privateModeDetected", request.PrivateModeDetected),
                    new MySqlParameter("@privateModeBrowser",  (object?)request.PrivateModeBrowser  ?? DBNull.Value),
                    new MySqlParameter("@deviceType",          (object?)request.DeviceType          ?? DBNull.Value),
                    new MySqlParameter("@uploadSpeed",         (object?)request.UploadSpeedMbps     ?? DBNull.Value),
                    new MySqlParameter("@latency",             (object?)request.Latency             ?? DBNull.Value)
                };

                var rowsAffected = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));
                return new SpeedTestLogResponse
                {
                    OutputCode = rowsAffected > 0 ? 1 : 0,
                    OutputMsg  = rowsAffected > 0 ? "Speed test log saved successfully" : "Failed to save speed test log"
                };
            }
            catch (Exception ex)
            {
                return new SpeedTestLogResponse { OutputCode = 0, OutputMsg = $"Error saving speed test log: {ex.Message}" };
            }
        }

        public async Task<SpeedTestLogResponse> GetAllSpeedTestLogs(string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        Id,
                        IpAddress,
                        UserAgent,
                        ScreenWidth,
                        ScreenHeight,
                        DownloadSpeedMbps,
                        ResolutionPassed,
                        SpeedPassed,
                        OverallPassed,
                        TestDateTime,
                        SessionId,
                        ReferrerUrl,
                        BrowserInfo,
                        AssessmentCode,
                        PrivateModeDetected,
                        PrivateModeBrowser,
                        DeviceType,
                        UploadSpeedMbps,
                        Latency
                    FROM SpeedTestLog
                    ORDER BY TestDateTime DESC";

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query));

                var logs = new List<SpeedTestLog>();
                foreach (DataRow row in result.Rows)
                {
                    logs.Add(MapToSpeedTestLog(row));
                }

                return new SpeedTestLogResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Speed test logs retrieved successfully",
                    Data = logs
                };
            }
            catch (Exception ex)
            {
                return new SpeedTestLogResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving speed test logs: {ex.Message}",
                    Data = new List<SpeedTestLog>()
                };
            }
        }

        public async Task<SpeedTestLogResponse> GetSpeedTestLogsByAssessment(string assessmentCode, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        Id,
                        IpAddress,
                        UserAgent,
                        ScreenWidth,
                        ScreenHeight,
                        DownloadSpeedMbps,
                        ResolutionPassed,
                        SpeedPassed,
                        OverallPassed,
                        TestDateTime,
                        SessionId,
                        ReferrerUrl,
                        BrowserInfo,
                        AssessmentCode,
                        PrivateModeDetected,
                        PrivateModeBrowser,
                        DeviceType,
                        UploadSpeedMbps,
                        Latency
                    FROM SpeedTestLog
                    WHERE AssessmentCode = @assessmentCode
                    ORDER BY TestDateTime DESC";

                var parameters = new[]
                {
                    new MySqlParameter("@assessmentCode", assessmentCode)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var logs = new List<SpeedTestLog>();
                foreach (DataRow row in result.Rows)
                {
                    logs.Add(MapToSpeedTestLog(row));
                }

                return new SpeedTestLogResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Speed test logs retrieved successfully",
                    Data = logs
                };
            }
            catch (Exception ex)
            {
                return new SpeedTestLogResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving speed test logs: {ex.Message}",
                    Data = new List<SpeedTestLog>()
                };
            }
        }

        public async Task<SpeedTestLogResponse> GetSpeedTestLogsByDateRange(DateTime startDate, DateTime endDate, string userId)
        {
            try
            {
                var query = @"
                    SELECT 
                        Id,
                        IpAddress,
                        UserAgent,
                        ScreenWidth,
                        ScreenHeight,
                        DownloadSpeedMbps,
                        ResolutionPassed,
                        SpeedPassed,
                        OverallPassed,
                        TestDateTime,
                        SessionId,
                        ReferrerUrl,
                        BrowserInfo,
                        AssessmentCode,
                        PrivateModeDetected,
                        PrivateModeBrowser,
                        DeviceType,
                        UploadSpeedMbps,
                        Latency
                    FROM SpeedTestLog
                    WHERE TestDateTime BETWEEN @startDate AND @endDate
                    ORDER BY TestDateTime DESC";

                var parameters = new[]
                {
                    new MySqlParameter("@startDate", startDate),
                    new MySqlParameter("@endDate", endDate)
                };

                var result = await Task.Run(() => _dbHelper.ExecuteQuery(query, parameters));

                var logs = new List<SpeedTestLog>();
                foreach (DataRow row in result.Rows)
                {
                    logs.Add(MapToSpeedTestLog(row));
                }

                return new SpeedTestLogResponse
                {
                    OutputCode = 1,
                    OutputMsg = "Speed test logs retrieved successfully",
                    Data = logs
                };
            }
            catch (Exception ex)
            {
                return new SpeedTestLogResponse
                {
                    OutputCode = 0,
                    OutputMsg = $"Error retrieving speed test logs: {ex.Message}",
                    Data = new List<SpeedTestLog>()
                };
            }
        }

        public async Task<(int OutputCode, string OutputMsg, int DeletedCount)> BulkDeleteByAssessmentCode(string assessmentCode, string userId)
        {
            try
            {
                var query = @"
                    DELETE FROM SpeedTestLog
                    WHERE AssessmentCode = @assessmentCode";
                var parameters = new[]
                {
                    new MySqlParameter("@assessmentCode", assessmentCode)
                };
                var deletedCount = await Task.Run(() => _dbHelper.ExecuteNonQuery(query, parameters));
                return (
                    OutputCode: deletedCount > 0 ? 1 : 0,
                    OutputMsg: deletedCount > 0 ? "Speed test logs deleted successfully" : "No logs found to delete",
                    DeletedCount: deletedCount
                );
            }
            catch (Exception ex)
            {
                return (
                    OutputCode: 0,
                    OutputMsg: $"Error deleting speed test logs: {ex.Message}",
                    DeletedCount: 0
                );
            }
        }

        private SpeedTestLog MapToSpeedTestLog(DataRow row)
        {
            return new SpeedTestLog
            {
                Id = Convert.ToInt32(row["Id"]),
                IpAddress = row["IpAddress"]?.ToString(),
                UserAgent = row["UserAgent"]?.ToString(),
                ScreenWidth = row["ScreenWidth"] != DBNull.Value ? Convert.ToInt32(row["ScreenWidth"]) : null,
                ScreenHeight = row["ScreenHeight"] != DBNull.Value ? Convert.ToInt32(row["ScreenHeight"]) : null,
                DownloadSpeedMbps = row["DownloadSpeedMbps"] != DBNull.Value ? Convert.ToDecimal(row["DownloadSpeedMbps"]) : null,
                ResolutionPassed = row["ResolutionPassed"] != DBNull.Value ? Convert.ToDecimal(row["ResolutionPassed"]) : 0,
                SpeedPassed = Convert.ToBoolean(row["SpeedPassed"]),
                OverallPassed = Convert.ToBoolean(row["OverallPassed"]),
                TestDateTime = row["TestDateTime"] != DBNull.Value ? Convert.ToDateTime(row["TestDateTime"]) : null,
                SessionId = row["SessionId"]?.ToString(),
                ReferrerUrl = row["ReferrerUrl"]?.ToString(),
                BrowserInfo = row["BrowserInfo"]?.ToString(),
                AssessmentCode = row["AssessmentCode"]?.ToString(),
                PrivateModeDetected = Convert.ToBoolean(row["PrivateModeDetected"]),
                PrivateModeBrowser = row["PrivateModeBrowser"]?.ToString(),
                DeviceType = row["DeviceType"]?.ToString(),
                UploadSpeedMbps = row["UploadSpeedMbps"] != DBNull.Value ? Convert.ToDecimal(row["UploadSpeedMbps"]) : null,
                Latency = row["Latency"] != DBNull.Value ? Convert.ToInt32(row["Latency"]) : null
            };
        }
    }
}