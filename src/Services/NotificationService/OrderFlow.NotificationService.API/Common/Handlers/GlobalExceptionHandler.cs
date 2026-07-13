using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using OrderFlow.NotificationService.Domain.Exceptions;

namespace OrderFlow.NotificationService.API.Common.Handlers;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var response = exception switch
        {
            DomainException => Results.BadRequest(new
            {
                error = exception.Message
            }),

            ValidationException validationException => Results.BadRequest(new
            {
                errors = validationException.Errors
                    .Select(x => x.ErrorMessage)
            }),

            _ => Results.Problem(
                title: "Internal Server Error",
                statusCode: StatusCodes.Status500InternalServerError)
        };

        await response.ExecuteAsync(httpContext);

        return true;
    }
}
