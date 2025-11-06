using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cliq.Api.AdminApiAttribute
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Get IConfiguration from DI
            var configuration = context.HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) 
                                as Microsoft.Extensions.Configuration.IConfiguration;

            var requiredKey = configuration["AdminSecretKey"]; // your key from appsettings.json

            // 1️⃣ Check header exists
            if (!context.HttpContext.Request.Headers.TryGetValue("X-API-KEY", out var extractedKey))
            {
                context.Result = new UnauthorizedObjectResult("❌ Missing API Key");
                return;
            }

            // 2️⃣ Validate key
            if (extractedKey != requiredKey)
            {
                context.Result = new ForbidResult("🚫 Invalid API Key for this endpoint");
                return;
            }

            // 3️⃣ Continue to action
            await next();
        }
    }
}
