using System.Net;
using System.Text.Json;
using Glyloop.Domain.Common;
using Glyloop.Infrastructure.Services.Dexcom;
using Glyloop.Infrastructure.Services.Dexcom.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Glyloop.Infrastructure.Tests.Services.Dexcom;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class DexcomApiClientTests
{
    private static IConfiguration BuildConfig()
    {
        var dict = new Dictionary<string, string?>
        {
            ["Dexcom:BaseUrl"] = "https://example.dexcom.test",
            ["Dexcom:ClientId"] = "client-id",
            ["Dexcom:ClientSecret"] = "client-secret",
            ["Dexcom:RedirectUri"] = "https://app/callback"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    private static DexcomApiClient CreateClient(HttpMessageHandler handler, IConfiguration config, out CapturingHandler cap)
    {
        cap = (CapturingHandler)handler;
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.dexcom.test") };
        var logger = Substitute.For<ILogger<DexcomApiClient>>();
        return new DexcomApiClient(http, config, logger);
    }

    [Test]
    public async Task ExchangeCodeForTokensAsync_ShouldFail_OnEmptyCode()
    {
        var handler = new CapturingHandler(_ => HttpResponses.Status(HttpStatusCode.BadRequest));
        var client = CreateClient(handler, BuildConfig(), out _);

        var result = await client.ExchangeCodeForTokensAsync("");
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task ExchangeCodeForTokensAsync_ShouldPostForm_AndReturnTokens()
    {
        var tokens = new OAuthTokenResponse("acc", "ref", 3600, "Bearer");
        var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var handler = new CapturingHandler(_ => HttpResponses.Json(HttpStatusCode.OK, json));
        var client = CreateClient(handler, BuildConfig(), out var cap);

        var result = await client.ExchangeCodeForTokensAsync("abc");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.AccessToken, Is.EqualTo("acc"));

        // Verify request details
        Assert.That(cap.LastRequest, Is.Not.Null);
        Assert.That(cap.LastRequest!.RequestUri!.AbsolutePath, Is.EqualTo("/v2/oauth2/token"));
        var body = await cap.LastRequest.Content!.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("client_id=client-id"));
            Assert.That(body, Does.Contain("client_secret=client-secret"));
            Assert.That(body, Does.Contain("code=abc"));
            Assert.That(body, Does.Contain("grant_type=authorization_code"));
            Assert.That(body, Does.Contain("redirect_uri=https%3A%2F%2Fapp%2Fcallback"));
        });
    }

    [Test]
    public async Task RefreshTokenAsync_ShouldPostForm_AndReturnTokens()
    {
        var tokens = new OAuthTokenResponse("acc2", "ref2", 7200, "Bearer");
        var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var handler = new CapturingHandler(_ => HttpResponses.Json(HttpStatusCode.OK, json));
        var client = CreateClient(handler, BuildConfig(), out var cap);

        var result = await client.RefreshTokenAsync("r1");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.RefreshToken, Is.EqualTo("ref2"));

        var body = await cap.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("refresh_token=r1"));
            Assert.That(body, Does.Contain("grant_type=refresh_token"));
        });
    }

    [Test]
    public async Task GetGlucoseReadingsAsync_ShouldFail_OnBadInputs()
    {
        var handler = new CapturingHandler(_ => HttpResponses.Status(HttpStatusCode.OK));
        var client = CreateClient(handler, BuildConfig(), out _);

        var r1 = await client.GetGlucoseReadingsAsync("", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5));
        var r2 = await client.GetGlucoseReadingsAsync("tok", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(r1.IsFailure, Is.True);
            Assert.That(r2.IsFailure, Is.True);
        });
    }

    [Test]
    public async Task GetGlucoseReadingsAsync_ShouldReturnFailure_OnSpecialStatuses()
    {
        // 429
        var handler429 = new CapturingHandler(_ => HttpResponses.Status(HttpStatusCode.TooManyRequests));
        var client429 = CreateClient(handler429, BuildConfig(), out _);
        var r429 = await client429.GetGlucoseReadingsAsync("tok", DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);
        Assert.That(r429.IsFailure, Is.True);
        Assert.That(r429.Error.Code, Is.EqualTo("Dexcom.RateLimited"));

        // 401
        var handler401 = new CapturingHandler(_ => HttpResponses.Status(HttpStatusCode.Unauthorized));
        var client401 = CreateClient(handler401, BuildConfig(), out _);
        var r401 = await client401.GetGlucoseReadingsAsync("tok", DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);
        Assert.That(r401.IsFailure, Is.True);
        Assert.That(r401.Error.Code, Is.EqualTo("Dexcom.Unauthorized"));

        // 500
        var handler500 = new CapturingHandler(_ => HttpResponses.Status(HttpStatusCode.InternalServerError));
        var client500 = CreateClient(handler500, BuildConfig(), out _);
        var r500 = await client500.GetGlucoseReadingsAsync("tok", DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);
        Assert.That(r500.IsFailure, Is.True);
        Assert.That(r500.Error.Code, Is.EqualTo("Dexcom.ApiError"));
    }

    [Test]
    public async Task GetGlucoseReadingsAsync_ShouldAttachAuthHeader_AndParseResponse()
    {
        var payload = new GlucoseReadingsResponse(
            new List<GlucoseReading>
            {
                new(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(-5), 120, "mg/dL", "flat"),
                new(DateTimeOffset.UtcNow.AddMinutes(-3), DateTimeOffset.UtcNow.AddMinutes(-3), 125, "mg/dL", "flat"),
            });
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var handler = new CapturingHandler(_ => HttpResponses.Json(HttpStatusCode.OK, json));
        var client = CreateClient(handler, BuildConfig(), out var cap);

        var result = await client.GetGlucoseReadingsAsync("tok123", DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Records.Count, Is.EqualTo(2));

        Assert.That(cap.LastRequest, Is.Not.Null);
        var auth = cap.LastRequest!.Headers.Authorization;
        Assert.That(auth!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(auth.Parameter, Is.EqualTo("tok123"));
        Assert.That(cap.LastRequest.RequestUri!.Query, Does.Contain("startDate="));
        Assert.That(cap.LastRequest.RequestUri!.Query, Does.Contain("endDate="));
    }
}



