using IQA_SOURCE.Models.Admin;
using IQA_SOURCE.Services;

namespace IQA_SOURCE.Middleware
{
    public class AssessmentSequenceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AssessmentSequenceMiddleware> _logger;

        // ✅ Mapping of routes to required assessment steps
        private static readonly Dictionary<string, AssessmentSequence.SequenceStep> RouteToStep = new()
        {
            { "colorblindnesstest", AssessmentSequence.SequenceStep.ColorblindnessTest },
            { "speedtest", AssessmentSequence.SequenceStep.SpeedTest },
            { "questions", AssessmentSequence.SequenceStep.Questions },
            { "sortassessment", AssessmentSequence.SequenceStep.ImageAssessment },
            { "imageassessment", AssessmentSequence.SequenceStep.ImageAssessment },
            { "ratingassessment", AssessmentSequence.SequenceStep.ImageAssessment }
        };

        public AssessmentSequenceMiddleware(RequestDelegate next, ILogger<AssessmentSequenceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAssessmentSequenceService sequenceService)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            var method = context.Request.Method;

            // ✅ Only validate GET requests
            if (method != "GET")
            {
                await _next(context);
                return;
            }

            // ✅ Extract assessment type code from path
            // Handles both formats: /Assessment/{assessmentCode}/{action} and /{assessmentType}/{action}
            var assessmentType = ExtractAssessmentType(path);

            if (string.IsNullOrEmpty(assessmentType))
            {
                await _next(context);
                return;
            }

            // ✅ Handle Index routes - Initialize sequence
            if (IsIndexRoute(path))
            {
                var sessionId = Guid.NewGuid().ToString();
                sequenceService.InitializeSequence(assessmentType, sessionId, context);
                _logger.LogInformation($"✅ Sequence initialized - Assessment Type: {assessmentType}");
                await _next(context);
                return;
            }

            // ✅ Extract the action/page name from the path
            var actionName = ExtractActionName(path);

            if (string.IsNullOrEmpty(actionName))
            {
                await _next(context);
                return;
            }

            // ✅ Check if this route requires sequence validation
            if (!RouteToStep.TryGetValue(actionName, out var requiredStep))
            {
                await _next(context);
                return;
            }

            // ✅ Validate sequence and step
            if (!sequenceService.ValidateStep(assessmentType, requiredStep, context))
            {
                var sequence = sequenceService.GetCurrentSequence(context);
                _logger.LogWarning($"❌ Invalid sequence access - User attempting to access {actionName} out of order. Current step: {sequence?.CurrentStep}");

                // Redirect to Index to restart
                var redirectUrl = BuildIndexUrl(assessmentType, context);
                context.Response.Redirect(redirectUrl);
                return;
            }

            // ✅ Move to this step in the sequence
            try
            {
                var sequence = sequenceService.GetCurrentSequence(context);
                if (sequence != null)
                {
                    sequence.MoveToStep(requiredStep);
                    _logger.LogInformation($"✅ Moved to step: {requiredStep}");
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"❌ {ex.Message}");
                var redirectUrl = BuildIndexUrl(assessmentType, context);
                context.Response.Redirect(redirectUrl);
                return;
            }

            await _next(context);
        }

        /// <summary>
        /// Extracts assessment type from the path
        /// Handles formats: /Assessment/{assessmentCode}/{action}, /{assessmentType}/{action}
        /// </summary>
        private string ExtractAssessmentType(string path)
        {
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
                return null;

            // Format: /Assessment/{assessmentCode}/{action}
            if (segments[0] == "assessment" && segments.Length >= 2)
                return segments[1];

            // Format: /{assessmentType}/{action}
            if (segments.Length >= 2 && !IsReservedSegment(segments[0]))
                return segments[0];

            return null;
        }

        /// <summary>
        /// Extracts the action name (last segment of the path)
        /// </summary>
        private string ExtractActionName(string path)
        {
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
                return null;

            var lastSegment = segments[^1];

            // Ignore query strings
            if (lastSegment.Contains('?'))
                lastSegment = lastSegment.Split('?')[0];

            return lastSegment;
        }

        /// <summary>
        /// Checks if the path represents an Index route
        /// </summary>
        private bool IsIndexRoute(string path)
        {
            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
                return false;

            var lastSegment = segments[^1];
            if (lastSegment.Contains('?'))
                lastSegment = lastSegment.Split('?')[0];

            // Single segment path like /{assessmentType} or /assessment/{assessmentCode}
            if (segments.Length == 1 && !IsReservedSegment(segments[0]))
                return true;

            // /Assessment/{assessmentCode} (exactly 2 segments)
            if (segments[0] == "assessment" && segments.Length == 2)
                return true;

            // Explicitly named index
            if (lastSegment.Equals("index", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the segment is a reserved controller/area name
        /// </summary>
        private bool IsReservedSegment(string segment)
        {
            return segment switch
            {
                "admin" or "account" or "api" or "download" or "home" => true,
                _ => false
            };
        }

        /// <summary>
        /// Builds the Index URL to redirect to
        /// </summary>
        private string BuildIndexUrl(string assessmentType, HttpContext context)
        {
            var redirectUrl = $"/Assessment/{assessmentType}";

            // Preserve query string parameters if present
            if (!string.IsNullOrEmpty(context.Request.QueryString.Value))
            {
                redirectUrl += context.Request.QueryString.Value;
            }

            return redirectUrl;
        }
    }

    public static class AssessmentSequenceMiddlewareExtensions
    {
        public static IApplicationBuilder UseAssessmentSequence(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AssessmentSequenceMiddleware>();
        }
    }
}