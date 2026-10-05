using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Common.Builders;

namespace Umbuntu.Automate.ManualTriggerInput.Configuration;

/// <summary>
/// Caps the request body before model binding reads it. <c>[RequestSizeLimit]</c> needs a
/// compile-time constant, so this applies the configurable limit instead: requests that
/// declare an oversized Content-Length are rejected up front, and the server's own body limit
/// is lowered so chunked uploads can't stream past it either.
/// </summary>
public sealed class TriggerInputRequestSizeFilter(IOptions<ManualTriggerInputOptions> options) : IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var limit = options.Value.MaxRequestBodyBytes;

        if (context.HttpContext.Request.ContentLength > limit)
        {
            context.Result = TooLarge(options.Value.MaxInputBytes);
            return;
        }

        var bodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is { IsReadOnly: false })
            bodySizeFeature.MaxRequestBodySize = limit;
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }

    internal static ObjectResult TooLarge(long maxInputBytes) =>
        new(new ProblemDetailsBuilder()
            .WithTitle("Input too large")
            .WithDetail($"The trigger input can be at most {maxInputBytes:N0} bytes.")
            .Build())
        {
            StatusCode = StatusCodes.Status413PayloadTooLarge,
        };
}
