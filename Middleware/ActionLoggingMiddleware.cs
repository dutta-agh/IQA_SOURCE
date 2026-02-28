using System.Diagnostics;
using System.Text;
using IQA_SOURCE.Data;
using IQA_SOURCE.Models;
using IQA_SOURCE.Services;

namespace IQA_SOURCE.Middleware
{
    public class ActionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ActionLoggingMiddleware> _logger;

        public ActionLoggingMiddleware(RequestDelegate next, ILogger<ActionLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISessionService sessionService, IActionLogRepository actionLogRepository)
        {
            // Skip logging for static files, health checks, and non-assessment routes
            if (ShouldSkipLogging(context))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var sessionId = sessionService.GetOrCreateSessionId();
            var assessmentType = sessionService.GetAssessmentType();

            // Capture request body
            string? requestBody = null;
            if (context.Request.ContentLength > 0 && context.Request.ContentType?.Contains("application/json") == true)
            {
                context.Request.EnableBuffering();
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }
            }

            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Capture response body
                string? responseBodyText = null;
                if (context.Response.ContentType?.Contains("application/json") == true)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                    responseBody.Seek(0, SeekOrigin.Begin);
                }

                await responseBody.CopyToAsync(originalBodyStream);

                // Log the action
                var actionLog = new UserActionLog
                {
                    SessionId = sessionId,
                    AssessmentType = assessmentType,
                    ActionType = DetermineActionType(context),
                    ActionName = $"{context.Request.Method} {context.Request.Path}",
                    ControllerName = context.GetRouteValue("controller")?.ToString(),
                    ActionMethod = context.GetRouteValue("action")?.ToString(),
                    RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
                    HttpMethod = context.Request.Method,
                    RequestPayload = TruncateString(requestBody, 4000),
                    ResponsePayload = TruncateString(responseBodyText, 4000),
                    StatusCode = context.Response.StatusCode,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    ActionTimestamp = DateTime.UtcNow,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    ErrorMessage = context.Response.StatusCode >= 400 ? $"HTTP {context.Response.StatusCode}" : null
                };

                // Log asynchronously without blocking
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await actionLogRepository.LogUserAction(actionLog);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log user action");
                    }
                });
            }
        }

        private bool ShouldSkipLogging(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var controller = context.GetRouteValue("controller")?.ToString()?.ToLower();

            // Skip static files and health checks
            if (path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path.StartsWith("/images") ||
                path.StartsWith("/lib") ||
                path.StartsWith("/favicon.ico") ||
                path.StartsWith("/health") ||
                path.StartsWith("/isalive"))
            {
                return true;
            }

            // Skip admin controller - only log user assessment actions
            if (controller == "admin")
            {
                return true;
            }

            // Only log requests to Home controller (user assessment)
            if (controller != "home")
            {
                return true;
            }

            return false;
        }

        private string DetermineActionType(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var method = context.Request.Method.ToUpper();
            var action = context.GetRouteValue("action")?.ToString()?.ToLower();

            // Specific action type mapping for Home controller
            return action switch
            {
                "index" => "PAGE_VIEW",
                "speedtest" => "SPEED_TEST",
                "getintrocontent" => "LOAD_CONTENT",
                "getsessioninfo" => "SESSION_INFO",
                "assessment" => "ASSESSMENT_START",
                "getquestions" => "LOAD_QUESTIONS",
                "saveanswer" => "SAVE_ANSWER",
                "submitassessment" => "SUBMIT_ASSESSMENT",
                "userinfo" => "USER_INFO",
                "saveuserinfo" => "SAVE_USER_INFO",
                "instructions" => "INSTRUCTIONS",
                _ => method == "POST" ? "POST_ACTION" : "GET_ACTION"
            };
        }

        private string? TruncateString(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }
    }
}