using Cliq.Api.Interface;  // Assuming this is where IAccountinterface is defined
using Cliq.Api.Repository;
using Cliq.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});




builder.Services.AddSingleton<CliqAuthService>();

builder.Services.AddScoped<IAccountInterface, AccountRepository>();
builder.Services.AddScoped<IAuthtInterface, AuthRepository>();
builder.Services.AddScoped<IMessageInterface, MessageRepository>();



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