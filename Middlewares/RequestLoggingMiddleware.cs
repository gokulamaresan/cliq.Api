using Serilog;
using System.Diagnostics;

namespace Cliq.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        public RequestLoggingMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            await _next(context);
            sw.Stop();

            var controller = context.GetRouteValue("controller")?.ToString() ?? "Unknown";
            var action = context.GetRouteValue("action")?.ToString() ?? "Unknown";
            var statusCode = context.Response.StatusCode;

            if (statusCode >= 200 && statusCode < 300)
            {
                Log.Information("✅ Success | Controller: {Controller} | Action: {Action} | StatusCode: {StatusCode} | Time: {Time}ms",
                    controller, action, statusCode, sw.ElapsedMilliseconds);
            }
            else
            {
                var responseMessage = context.Items["ErrorMessage"]?.ToString() ?? "Request failed.";
                Log.Error("❌ Error | Controller: {Controller} | Action: {Action} | StatusCode: {StatusCode} | Time: {Time}ms | Message: {Message}",
                    controller, action, statusCode, sw.ElapsedMilliseconds, responseMessage);
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLoggingMiddleware(this IApplicationBuilder app)
            => app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
