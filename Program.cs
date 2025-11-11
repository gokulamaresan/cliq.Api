// using cliq.Api.Interface;
// using cliq.Api.Repository;
// using Cliq.Api.Interface;
// using Cliq.Api.Repository;
// using Cliq.Api.Services;
// using Cliq.Api.Attributes; // For SkipApiKeyAttribute
// using Microsoft.OpenApi.Models;

// var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddControllers();

// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cliq API", Version = "v1" });

//     // Add API Key header support
//     c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
//     {
//         Description = "Enter your API Key below:",
//         Type = SecuritySchemeType.ApiKey,
//         Name = "X-API-KEY",
//         In = ParameterLocation.Header,
//         Scheme = "ApiKeyScheme"
//     });

//     c.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference
//                 {
//                     Type = ReferenceType.SecurityScheme,
//                     Id = "ApiKey"
//                 }
//             },
//             new string[] {}
//         }
//     });
// });

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll", policy =>
//         policy.AllowAnyOrigin()
//               .AllowAnyMethod()
//               .AllowAnyHeader());
// });

// builder.Services.AddSingleton<CliqAuthService>();
// builder.Services.AddScoped<IAccountInterface, AccountRepository>();
// builder.Services.AddScoped<IAuthtInterface, AuthRepository>();
// builder.Services.AddScoped<IMessageInterface, MessageRepository>();
// builder.Services.AddScoped<IChannelInterface, ChannelRepository>();

// var app = builder.Build();

// var apiKey = builder.Configuration["secretKey"];

// app.Use(async (context, next) =>
// {
//     var endpoint = context.GetEndpoint();

//     if (endpoint?.Metadata.GetMetadata<SkipApiKeyAttribute>() != null)
//     {
//         await next();
//         return;
//     }

//     var path = context.Request.Path.Value?.ToLower();
//     if (path != null && (path.Contains("/swagger") || path.Contains("/index.html")))
//     {
//         await next();
//         return;
//     }

//     if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedKey))
//     {
//         context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//         await context.Response.WriteAsync("❌ Missing API Key header");
//         return;
//     }

//     if (apiKey != extractedKey)
//     {
//         context.Response.StatusCode = StatusCodes.Status403Forbidden;
//         await context.Response.WriteAsync("🚫 Invalid API Key");
//         return;
//     }

//     await next();
// });

// app.UseCors("AllowAll");

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();

// app.MapControllers();

// app.Run();




using cliq.Api.Interface;
using cliq.Api.Repository;
using Cliq.Api.Interface;
using Cliq.Api.Repository;
using Cliq.Api.Services;
using Cliq.Api.Attributes; // For SkipApiKeyAttribute
using Cliq.Api.AdminApiAttribute; // For AdminApiKeyAttribute
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add controller services
builder.Services.AddControllers();

// ✅ Swagger setup with API Key support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cliq API", Version = "v1" });

    // Normal API Key definition
    c.AddSecurityDefinition("UserApiKey", new OpenApiSecurityScheme
    {
        Description = "Enter your *User* API Key below:",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-KEY",
        In = ParameterLocation.Header,
        Scheme = "UserApiKeyScheme"
    });

    // Admin API Key definition
    c.AddSecurityDefinition("AdminApiKey", new OpenApiSecurityScheme
    {
        Description = "Enter your *Admin* API Key below:",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "UserApiKey"
                }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "AdminApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ✅ CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ✅ Dependency Injection
builder.Services.AddSingleton<CliqAuthService>();
builder.Services.AddScoped<IAccountInterface, AccountRepository>();
builder.Services.AddScoped<IAuthtInterface, AuthRepository>();
builder.Services.AddScoped<IMessageInterface, MessageRepository>();
builder.Services.AddScoped<IChannelInterface, ChannelRepository>();

var app = builder.Build();

// ✅ Retrieve both API keys from appsettings.json
var userApiKey = builder.Configuration["secretKey"];
var adminApiKey = builder.Configuration["AdminSecretKey"];

// ✅ Global middleware for validating *User* API key
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();

    // 🚫 Skip Admin endpoints (they use [AdminApiKey])
    if (endpoint?.Metadata.GetMetadata<AdminApiKeyAttribute>() != null)
    {
        await next();
        return;
    }

    // 🚫 Skip endpoints with [SkipApiKey]
    if (endpoint?.Metadata.GetMetadata<SkipApiKeyAttribute>() != null)
    {
        await next();
        return;
    }

    // 🚫 Allow Swagger and root/index
    var path = context.Request.Path.Value?.ToLower();
    if (path != null && (path.Contains("/swagger") || path.Contains("/index.html")))
    {
        await next();
        return;
    }

    // 🔐 Validate normal user API key
    if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedKey))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("❌ Missing API Key header");
        return;
    }

    if (userApiKey != extractedKey)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("🚫 Invalid API Key");
        return;
    }

    await next();
});

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
