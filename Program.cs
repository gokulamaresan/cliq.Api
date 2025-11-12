using Cliq.Api;
using Cliq.Api.Middleware;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Serilog — keep only custom Success/Error logs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)   // hides default ASP.NET logs
    .MinimumLevel.Override("System", LogEventLevel.Error)
    .Filter.ByIncludingOnly(logEvent =>
        logEvent.MessageTemplate.Text.Contains("✅ Success") ||
        logEvent.MessageTemplate.Text.Contains("❌ Error"))
    .WriteTo.File("Logs/cliqapi-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 15)
    .Enrich.FromLogContext()
    .CreateLogger();


builder.Host.UseSerilog();

var userApiKey = builder.Configuration["secretKey"];
var adminApiKey = builder.Configuration["AdminSecretKey"];

builder.Services.AddControllers();

// ✅ Swagger setup
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cliq API", Version = "v1" });

    c.AddSecurityDefinition("UserApiKey", new OpenApiSecurityScheme
    {
        Description = "Enter your *User* API Key",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-KEY",
        In = ParameterLocation.Header,
        Scheme = "UserApiKeyScheme"
    });

    c.AddSecurityDefinition("AdminApiKey", new OpenApiSecurityScheme
    {
        Description = "Enter your *Admin* API Key",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-KEY",
        In = ParameterLocation.Header,
        Scheme = "AdminApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "UserApiKey" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "AdminApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

var startup = new Startup(userApiKey, adminApiKey, builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

app.UseRequestLoggingMiddleware();
// app.UseErrorHandlingMiddleware();
 
startup.Configure(app, app.Environment);

app.Run();
