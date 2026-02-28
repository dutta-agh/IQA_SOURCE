using IQA_SOURCE.Models.Admin;

namespace IQA_SOURCE.Data
{
    public interface IDashboardRepository
    {
        Task<DashboardStatsResponse> GetDashboardStats(string userId);
        Task<AssessmentImageSummaryResponse> GetAssessmentImageSummary(string userId);
    }
}   