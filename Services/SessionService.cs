using System.Text.Json;
using IQA_SOURCE.Data;

namespace IQA_SOURCE.Services
{
    public class SessionService : ISessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActionLogRepository? _actionLogRepository;
        private readonly ILogger<SessionService>? _logger;
        private const string SESSION_ID_KEY = "AssessmentSessionId";
        private const string ASSESSMENT_TYPE_KEY = "AssessmentType";
        private const string SESSION_START_TIME_KEY = "SessionStartTime";

        public SessionService(
            IHttpContextAccessor httpContextAccessor,
            IActionLogRepository? actionLogRepository = null,
            ILogger<SessionService>? logger = null)
        {
            _httpContextAccessor = httpContextAccessor;
            _actionLogRepository = actionLogRepository;
            _logger = logger;
        }

        public string GetOrCreateSessionId()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                throw new InvalidOperationException("Session is not available");
            }

            var sessionId = session.GetString(SESSION_ID_KEY);
            
            if (string.IsNullOrEmpty(sessionId))
            {
                // Create new unique session ID
                sessionId = Guid.NewGuid().ToString("N").ToUpper();
                session.SetString(SESSION_ID_KEY, sessionId);
                session.SetString(SESSION_START_TIME_KEY, DateTime.UtcNow.ToString("o"));

                // Log session creation
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_actionLogRepository != null)
                        {
                            await _actionLogRepository.LogUserActionAsync(
                                sessionId,
                                "SESSION_START",
                                "New session created",
                                $"{{ \"startTime\": \"{DateTime.UtcNow:o}\" }}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to log session creation");
                    }
                });
            }

            return sessionId;
        }

        public string? GetAssessmentType()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            return session?.GetString(ASSESSMENT_TYPE_KEY);
        }

        public void SetAssessmentType(string assessmentType)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetString(ASSESSMENT_TYPE_KEY, assessmentType);

                // Log assessment type selection
                var sessionId = GetOrCreateSessionId();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_actionLogRepository != null)
                        {
                            await _actionLogRepository.LogUserActionAsync(
                                sessionId,
                                "ASSESSMENT_TYPE_SET",
                                $"Assessment type set to: {assessmentType}",
                                $"{{ \"assessmentType\": \"{assessmentType}\" }}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to log assessment type change");
                    }
                });
            }
        }

        public Dictionary<string, object> GetSessionData()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var data = new Dictionary<string, object>
            {
                ["SessionId"] = GetOrCreateSessionId(),
                ["AssessmentType"] = GetAssessmentType() ?? "",
                ["StartTime"] = session?.GetString(SESSION_START_TIME_KEY) ?? "",
                ["IsActive"] = IsSessionActive()
            };
            return data;
        }

        public void SetSessionData(string key, object value)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                var json = JsonSerializer.Serialize(value);
                session.SetString(key, json);
            }
        }

        public T? GetSessionData<T>(string key) where T : class
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var json = session?.GetString(key);
            
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json);
        }

        public void ClearSession()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var sessionId = session?.GetString(SESSION_ID_KEY);
            
            // Log session end
            if (!string.IsNullOrEmpty(sessionId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_actionLogRepository != null)
                        {
                            await _actionLogRepository.LogUserActionAsync(
                                sessionId,
                                "SESSION_END",
                                "Session cleared",
                                $"{{ \"endTime\": \"{DateTime.UtcNow:o}\" }}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to log session end");
                    }
                });
            }

            session?.Clear();
        }

        public bool IsSessionActive()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var sessionId = session?.GetString(SESSION_ID_KEY);
            return !string.IsNullOrEmpty(sessionId);
        }
    }
}