using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Umbuntu.Automate.ManualTriggerInput.Configuration;

namespace Umbuntu.Automate.ManualTriggerInput.Tests;

public class TriggerInputRequestSizeFilterTests
{
    private static readonly ManualTriggerInputOptions Options = new() { MaxInputBytes = 1000 };

    // Body limit = (MaxInputBytes * 2) + 4096 headroom for JSON escaping.
    private const long BodyLimit = (1000 * 2) + 4096;

    [Fact]
    public void Request_declaring_a_body_over_the_limit_is_rejected_with_413()
    {
        var (context, _) = CreateContext(contentLength: BodyLimit + 1);

        CreateFilter().OnResourceExecuting(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, result.StatusCode);
    }

    [Theory]
    [InlineData(BodyLimit)]
    [InlineData(null)] // chunked: no Content-Length
    public void Request_within_the_limit_continues_and_caps_the_server_body_limit(long? contentLength)
    {
        var (context, bodySize) = CreateContext(contentLength);

        CreateFilter().OnResourceExecuting(context);

        Assert.Null(context.Result);
        Assert.Equal(BodyLimit, bodySize.MaxRequestBodySize);
    }

    [Fact]
    public void Read_only_body_size_feature_is_left_alone()
    {
        var (context, bodySize) = CreateContext(contentLength: 10, isReadOnly: true);

        CreateFilter().OnResourceExecuting(context);

        Assert.Null(context.Result);
        Assert.Equal(FakeBodySizeFeature.ServerDefault, bodySize.MaxRequestBodySize);
    }

    private static TriggerInputRequestSizeFilter CreateFilter() =>
        new(Microsoft.Extensions.Options.Options.Create(Options));

    private static (ResourceExecutingContext Context, FakeBodySizeFeature BodySize) CreateContext(
        long? contentLength,
        bool isReadOnly = false)
    {
        var bodySize = new FakeBodySizeFeature { IsReadOnly = isReadOnly };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentLength = contentLength;
        httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(bodySize);

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ResourceExecutingContext(actionContext, [], new List<IValueProviderFactory>());
        return (context, bodySize);
    }

    private sealed class FakeBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        public const long ServerDefault = 30_000_000;

        public bool IsReadOnly { get; init; }

        public long? MaxRequestBodySize { get; set; } = ServerDefault;
    }
}
