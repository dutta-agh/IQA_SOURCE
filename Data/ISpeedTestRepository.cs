using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface ISpeedTestRepository
    {
        Task<SpeedTestLogResponse> SaveSpeedTestLog(SpeedTestLogSaveRequest request, string ipAddress, string userAgent, string? referrerUrl);
        Task<SpeedTestLogResponse> GetAllSpeedTestLogs(string userId);
        Task<SpeedTestLogResponse> GetSpeedTestLogsByAssessment(string assessmentCode, string userId);
        Task<SpeedTestLogResponse> GetSpeedTestLogsByDateRange(DateTime startDate, DateTime endDate, string userId);
        Task<(int OutputCode, string OutputMsg, int DeletedCount)> BulkDeleteByAssessmentCode(string assessmentCode, string userId);
    }
}