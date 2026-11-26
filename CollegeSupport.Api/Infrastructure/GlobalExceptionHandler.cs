using CollegeSupport.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeSupport.Api.Infrastructure;

/// <summary>Преобразует исключения в ответы ProblemDetails с правильным HTTP-кодом.</summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Объект не найден"),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Нарушено бизнес-правило"),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Ошибка входа"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфликт данных"),
            DbUpdateException => (StatusCodes.Status409Conflict, "Конфликт данных"),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Необработанное исключение при запросе {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            _logger.LogWarning("{Title}: {Message}", title, exception.Message);

        string detail = status switch
        {
            StatusCodes.Status500InternalServerError => "Произошла ошибка на сервере. Обратитесь к администратору.",
            StatusCodes.Status409Conflict => "Данные противоречат уже сохранённым (например, повтор уникального значения).",
            _ => exception.Message
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}
