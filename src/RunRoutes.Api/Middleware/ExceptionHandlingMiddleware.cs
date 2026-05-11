using System.Text.Json;
using RunRoutes.Core.Common.Exceptions;

namespace RunRoutes.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                new ErrorResponse(ex.Message, ex.Errors)
            ),
            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                new ErrorResponse(ex.Message)
            ),
            ForbiddenException ex => (
                StatusCodes.Status403Forbidden,
                new ErrorResponse(ex.Message)
            ),
            ConflictException ex => (
                StatusCodes.Status409Conflict,
                new ErrorResponse(ex.Message, Code: ex.Code)
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ErrorResponse("サーバーエラーが発生しました")
            )
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "想定外の例外が発生しました");
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(response, jsonOptions);
    }
}
