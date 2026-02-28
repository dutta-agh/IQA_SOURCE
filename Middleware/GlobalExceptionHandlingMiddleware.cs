using System.Net;
using System.Text.Json;

namespace IQA_SOURCE.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. Path: {Path}, Method: {Method}", 
                    context.Request.Path, 
                    context.Request.Method);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var errorId = Guid.NewGuid().ToString();
            
            _logger.LogError(exception, "Error ID: {ErrorId} - {Message}", errorId, exception.Message);

            // For API requests (JSON responses)
            if (context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                var response = new
                {
                    ErrorId = errorId,
                    Message = _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An error occurred processing your request.",
                    Details = _environment.IsDevelopment() 
                        ? exception.StackTrace 
                        : null
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                return;
            }

            // For web requests (HTML responses)
            var html = _environment.IsDevelopment()
                ? GenerateDevelopmentErrorPage(exception, errorId)
                : GenerateProductionErrorPage(errorId);

            await context.Response.WriteAsync(html);
        }

        private string GenerateDevelopmentErrorPage(Exception exception, string errorId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Error - IQA</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }}
        .error-container {{ background: white; padding: 20px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .error-header {{ color: #d32f2f; border-bottom: 2px solid #d32f2f; padding-bottom: 10px; }}
        .error-id {{ color: #666; font-size: 0.9em; margin: 10px 0; }}
        .error-message {{ background: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 15px 0; }}
        .stack-trace {{ background: #f8f9fa; padding: 15px; border-radius: 3px; overflow-x: auto; font-family: monospace; font-size: 0.85em; }}
        .back-link {{ display: inline-block; margin-top: 20px; color: #007bff; text-decoration: none; }}
        .back-link:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <div class='error-container'>
        <h1 class='error-header'>⚠️ An Error Occurred</h1>
        <div class='error-id'><strong>Error ID:</strong> {errorId}</div>
        <div class='error-message'>
            <strong>Exception Type:</strong> {exception.GetType().Name}<br/>
            <strong>Message:</strong> {System.Web.HttpUtility.HtmlEncode(exception.Message)}
        </div>
        <h3>Stack Trace:</h3>
        <div class='stack-trace'>{System.Web.HttpUtility.HtmlEncode(exception.StackTrace ?? "No stack trace available")}</div>
        {(exception.InnerException != null ? $@"
        <h3>Inner Exception:</h3>
        <div class='error-message'>
            <strong>Type:</strong> {exception.InnerException.GetType().Name}<br/>
            <strong>Message:</strong> {System.Web.HttpUtility.HtmlEncode(exception.InnerException.Message)}
        </div>
        <div class='stack-trace'>{System.Web.HttpUtility.HtmlEncode(exception.InnerException.StackTrace ?? "No stack trace available")}</div>
        " : "")}
        <a href='/IQA' class='back-link'>← Back to Home</a>
    </div>
</body>
</html>";
        }

        private string GenerateProductionErrorPage(string errorId)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Error - IQA</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5; display: flex; align-items: center; justify-content: center; min-height: 100vh; }}
        .error-container {{ background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); text-align: center; max-width: 500px; }}
        .error-icon {{ font-size: 4em; margin-bottom: 20px; }}
        h1 {{ color: #333; margin: 0 0 10px 0; }}
        p {{ color: #666; line-height: 1.6; }}
        .error-id {{ background: #f8f9fa; padding: 10px; border-radius: 4px; margin: 20px 0; font-family: monospace; font-size: 0.9em; color: #666; }}
        .back-link {{ display: inline-block; margin-top: 20px; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 4px; }}
        .back-link:hover {{ background: #0056b3; }}
    </style>
</head>
<body>
    <div class='error-container'>
        <div class='error-icon'>⚠️</div>
        <h1>Something Went Wrong</h1>
        <p>We're sorry, but something unexpected happened. Our team has been notified and is working to fix the issue.</p>
        <div class='error-id'>Reference ID: {errorId}</div>
        <a href='/IQA' class='back-link'>Return to Home</a>
    </div>
</body>
</html>";
        }
    }
}