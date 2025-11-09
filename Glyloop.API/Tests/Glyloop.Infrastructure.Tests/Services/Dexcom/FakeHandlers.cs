using System.Net;
using System.Text;

namespace Glyloop.Infrastructure.Tests.Services.Dexcom;

internal sealed class CapturingHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public HttpRequestMessage? LastRequest { get; private set; }

    public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_responder(request));
    }
}

internal static class HttpResponses
{
    public static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static HttpResponseMessage Status(HttpStatusCode status)
    {
        return new HttpResponseMessage(status);
    }
}



