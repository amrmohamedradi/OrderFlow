using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace OrderFlow.BuildingBlocks.Messaging.Correlation;

public class CorrelationIdMiddleware : IMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public CorrelationIdMiddleware(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        Guid correlationId;

        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out StringValues headerValue)
            && Guid.TryParse(headerValue.FirstOrDefault(), out Guid parsedGuid)
            && parsedGuid != Guid.Empty)
        {
            correlationId = parsedGuid;
        }
        else
        {
            correlationId = Guid.NewGuid();
        }

        _correlationIdAccessor.SetCorrelationId(correlationId);

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers.Append(CorrelationIdHeaderName, correlationId.ToString());
            }
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
