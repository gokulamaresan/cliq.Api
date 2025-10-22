using Cliq.Api.Interface;  // Assuming this is where IAccountinterface is defined
using Cliq.Api.Repository;
using VaultCliqMessageService.Services;  // Assuming this is where AccountRepository is defined

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});




builder.Services.AddSingleton<CliqAuthService>();

// Corrected line: Use angle brackets to specify the interface and implementation
builder.Services.AddScoped<IAccountinterface, AccountRepository>();

var app = builder.Build();
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.MapControllers();
app.UseHttpsRedirection();

app.Run();