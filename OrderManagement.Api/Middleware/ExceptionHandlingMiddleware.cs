using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace OrderManagement.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation error",
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request");

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Bad request",
                ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict");

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                "The order was modified by another user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (context.Items.TryGetValue("X-Correlation-Id", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId?.ToString();
        }

        var json = JsonSerializer.Serialize(problemDetails);

        await context.Response.WriteAsync(json);
    }
}