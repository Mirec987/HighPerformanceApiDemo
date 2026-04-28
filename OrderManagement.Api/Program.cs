using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OrderManagement.Api.Configuration;
using OrderManagement.Api.Extensions;
using OrderManagement.Application;
using OrderManagement.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var writePermitLimit = builder.Environment.IsEnvironment(ApiConstants.Environments.LoadTesting) ? 100_000 : 2;

if (!builder.Environment.IsEnvironment(ApiConstants.Environments.Testing))
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.ContentType = "application/json";

            await context.HttpContext.Response.WriteAsync(
                """
                {
                  "title": "Too many requests",
                  "status": 429,
                  "detail": "Rate limit exceeded. Please try again later."
                }
                """,
                token);
        };

        options.AddFixedWindowLimiter(ApiConstants.RateLimitPolicies.WritePolicy, limiterOptions =>
        {
            limiterOptions.PermitLimit = writePermitLimit;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });
    });
}

var app = builder.Build();

Log.Information(
    "Application starting in {EnvironmentName}. Write rate limit: {WritePermitLimit} requests/minute.",
    app.Environment.EnvironmentName,
    app.Environment.IsEnvironment(ApiConstants.Environments.Testing) ? 0 : writePermitLimit);

app.UseGlobalExceptionHandling();
app.UseCorrelationId();
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment(ApiConstants.Environments.Testing))
{
    app.UseRateLimiter();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live");

app.Run();

public partial class Program
{
}