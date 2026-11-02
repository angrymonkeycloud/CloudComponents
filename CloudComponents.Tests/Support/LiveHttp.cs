using System.Net;
using System.Text;
using System.Text.Json;

namespace CloudComponents.Tests.Support;

/// <summary>What a live service answered. The URL and body are only ever shown with the keys taken out.</summary>
public sealed record LiveResponse(HttpStatusCode Status, string Body, string? ContentType, string Url)
{
    public bool IsSuccess => (int)Status is >= 200 and < 300;

    /// <summary>The body as JSON; the test fails, naming the service, when it is not.</summary>
    public JsonElement Json
    {
        get
        {
            try
            {
                return JsonDocument.Parse(Body).RootElement.Clone();
            }
            catch (JsonException)
            {
                throw new Xunit.Sdk.XunitException($"{Describe()} did not answer with JSON: {TestSecrets.Redact(Body.Length > 300 ? Body[..300] : Body)}");
            }
        }
    }

    public string Describe() => $"{(int)Status} from {TestSecrets.Redact(Url)}";

    /// <summary>Fails with the service's own explanation, so a disabled API or a wrong key reads as that and not as a missing field.</summary>
    public LiveResponse EnsureSuccess()
    {
        if (!IsSuccess)
            throw new Xunit.Sdk.XunitException($"{Describe()}: {TestSecrets.Redact(Body.Length > 600 ? Body[..600] : Body)}");

        return this;
    }
}

public static class LiveHttp
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static Task<LiveResponse> GetAsync(string url, IReadOnlyDictionary<string, string>? headers = null) =>
        SendAsync(() => Prepare(new HttpRequestMessage(HttpMethod.Get, url), headers), url);

    public static Task<LiveResponse> PostJsonAsync(string url, object body, IReadOnlyDictionary<string, string>? headers = null) =>
        SendAsync(() => Prepare(new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        }, headers), url);

    private static HttpRequestMessage Prepare(HttpRequestMessage request, IReadOnlyDictionary<string, string>? headers)
    {
        foreach ((string name, string value) in headers ?? new Dictionary<string, string>())
            request.Headers.TryAddWithoutValidation(name, value);

        return request;
    }

    /// <summary>One retry for a service that is briefly rate limiting or unavailable, which a probe should not read as a broken key.</summary>
    private static async Task<LiveResponse> SendAsync(Func<HttpRequestMessage> create, string url)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                using HttpRequestMessage request = create();
                using HttpResponseMessage response = await Client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (attempt == 0 && response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                {
                    await Task.Delay(1500);
                    continue;
                }

                return new LiveResponse(response.StatusCode, body, response.Content.Headers.ContentType?.MediaType, url);
            }
            catch (HttpRequestException exception)
            {
                throw new Xunit.Sdk.XunitException($"No answer from {TestSecrets.Redact(url)}: {TestSecrets.Redact(exception.Message)}");
            }
        }
    }
}
