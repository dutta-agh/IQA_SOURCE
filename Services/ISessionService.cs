namespace IQA_SOURCE.Services
{
    public interface ISessionService
    {
        string GetOrCreateSessionId();
        string? GetAssessmentType();
        void SetAssessmentType(string assessmentType);
        Dictionary<string, object> GetSessionData();
        void SetSessionData(string key, object value);
        T? GetSessionData<T>(string key) where T : class;
        void ClearSession();
        bool IsSessionActive();
    }
}