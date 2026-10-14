using Messenger.Application.Common; // 👈 Import đúng Namespace
using Messenger.Application.Common.ConflictException.cs;
using System.ComponentModel.DataAnnotations;
using Messenger.Application.Common.Exceptions;
using System.Net;

namespace Messenger.Api.Middleware;

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

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        int statusCode;
        string message;

        switch (exception)
        {
            case BadRequestException:
                statusCode = StatusCodes.Status400BadRequest;
                message = exception.Message;
                _logger.LogWarning(exception, "Bad Request: {Message}", exception.Message);
                break;

            case ConflictException:
                statusCode = StatusCodes.Status409Conflict;
                message = exception.Message;
                _logger.LogWarning(exception, "Conflict: {Message}", exception.Message);
                break;

            case UnauthorizedException:
                statusCode = StatusCodes.Status401Unauthorized;
                message = exception.Message;
                _logger.LogWarning(exception, "Unauthorized: {Message}", exception.Message);
                break;

            case NotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                message = exception.Message;
                _logger.LogWarning(exception, "Not Found: {Message}", exception.Message);
                break;

            case ValidationException:
                statusCode = StatusCodes.Status400BadRequest;
                message = exception.Message;
                _logger.LogWarning(exception, "Validation Error: {Message}", exception.Message);
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "An unexpected error occurred.";
                // Chỉ Log Critical/Error đối với lỗi hệ thống 500 không lường trước được
                _logger.LogError(exception, "Unhandled Exception: {Message}", exception.Message);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            statusCode,
            message
        });
    }
}