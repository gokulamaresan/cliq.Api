using Cliq.Api.Interface;
using Cliq.Api.Repository;
using Cliq.Api.Services;
using Cliq.Api.Attributes;
using Cliq.Api.AdminApiAttribute;
using cliq.Api.Interface;
using cliq.Api.Repository;

namespace Cliq.Api
{
    public class Startup
    {
        private readonly string _userApiKey;
        private readonly string _adminApiKey;
        private readonly IConfiguration _configuration;

        public Startup(string userApiKey, string adminApiKey, IConfiguration configuration)
        {
            _userApiKey = userApiKey;
            _adminApiKey = adminApiKey;
            _configuration = configuration;
        }

        // ✅ Register services
        public void ConfigureServices(IServiceCollection services)
        {
            // CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            // Dependency Injection
            services.AddSingleton<CliqAuthService>();
            services.AddScoped<IAccountInterface, AccountRepository>();
            services.AddScoped<IAuthtInterface, AuthRepository>();
            services.AddScoped<IMessageInterface, MessageRepository>();
            services.AddScoped<IChannelInterface, ChannelRepository>();
        }

        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            // User API key middleware
            app.Use(async (context, next) =>
            {
                var endpoint = context.GetEndpoint();

                if (endpoint?.Metadata.GetMetadata<AdminApiKeyAttribute>() != null ||
                    endpoint?.Metadata.GetMetadata<SkipApiKeyAttribute>() != null)
                {
                    await next();
                    return;
                }

                var path = context.Request.Path.Value?.ToLower();
                if (path != null && (path.Contains("/swagger") || path.Contains("/index.html")))
                {
                    await next();
                    return;
                }

                if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedKey))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(" Missing API Key header");
                    return;
                }

                if (_userApiKey != extractedKey)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Invalid API Key");
                    return;
                }

                await next();
            });

            app.UseCors("AllowAll");

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.MapControllers();
        }
    }
}
