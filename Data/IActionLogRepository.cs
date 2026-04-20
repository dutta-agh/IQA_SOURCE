using IQA_SOURCE.Models;

namespace IQA_SOURCE.Data
{
    public interface IActionLogRepository
    {
        Task<ActionLogResponse> LogUserAction(UserActionLog actionLog);
        Task<ActionLogResponse> LogUserActionAsync(
            string sessionId,
            string actionType,
            string actionName,
            string? additionalData = null);
        Task<List<UserActionLog>> GetUserActionsBySession(string sessionId);
        Task<List<UserActionLog>> GetUserActionsByDateRange(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetActionStatisticsBySession(string sessionId);
    }
}