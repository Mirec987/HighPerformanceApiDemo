using Microsoft.AspNetCore.RateLimiting;
using OrderManagement.Api.Extensions;
using OrderManagement.Application;
using OrderManagement.Infrastructure;
using Serilog;
using System.Threading.RateLimiting;

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

    options.AddFixedWindowLimiter("write-policy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 2;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();
app.UseGlobalExceptionHandling();
app.UseCorrelationId();
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseRateLimiter();

app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
    //public static void Main(string[] args)
    //{
    //}
}