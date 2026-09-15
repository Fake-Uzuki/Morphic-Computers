using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ERP.api.Middleware
{
    /// <summary>
    /// Security Middleware enforcing internal API Key authorization on all incoming requests.
    /// Blocks unauthorized direct access from browsers, external scanners, or third-party tools.
    /// </summary>
    public class ApiKeyAuthMiddleware
    {
        public const string ApiKeyHeaderName = "X-API-KEY";
        public const string ExpectedApiKey = "MorphicErp_SecureKey_2026";

        private readonly RequestDelegate _next;

        public ApiKeyAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Allow OpenAPI / Swagger documentation endpoints in development
            if (path.StartsWith("/openapi") || path.StartsWith("/swagger"))
            {
                await _next(context);
                return;
            }

            // Check for API Key header
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey) ||
                extractedApiKey != ExpectedApiKey)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"statusCode\": 401, \"error\": \"Unauthorized\", \"message\": \"Access Denied: Missing or invalid API Key. Direct access to this API is restricted.\"}");
                return;
            }

            await _next(context);
        }
    }
}
