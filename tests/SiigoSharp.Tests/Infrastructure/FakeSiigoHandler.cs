using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SiigoSharp.Tests.Infrastructure;

/// <summary>
/// An in-memory stand-in for the Siigo API. It records every request and answers with the responder
/// registered for its method and path. By default <c>POST /auth</c> issues tokens named
/// <c>token-1</c>, <c>token-2</c>… valid for 24 hours.
/// </summary>
internal sealed class FakeSiigoHandler : HttpMessageHandler
{
    private readonly object _gate = new();
    private readonly List<RecordedRequest> _requests = [];
    private readonly Dictionary<string, Func<RecordedRequest, Task<HttpResponseMessage>>> _routes = [];
    private int _tokensIssued;

    public FakeSiigoHandler()
    {
        On(HttpMethod.Post, "/auth", _ => Json(HttpStatusCode.OK, new
        {
            access_token = $"token-{Interlocked.Increment(ref _tokensIssued)}",
            expires_in = 86400,
            token_type = "Bearer",
            scope = "SiigoAPI",
        }));
    }

    public int TokensIssued => Volatile.Read(ref _tokensIssued);

    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_gate)
            {
                return [.. _requests];
            }
        }
    }

    public IReadOnlyList<RecordedRequest> RequestsTo(string path) =>
        Requests.Where(request => request.Path == path).ToList();

    public FakeSiigoHandler On(HttpMethod method, string path, Func<RecordedRequest, HttpResponseMessage> respond) =>
        OnAsync(method, path, request => Task.FromResult(respond(request)));

    public FakeSiigoHandler OnAsync(HttpMethod method, string path, Func<RecordedRequest, Task<HttpResponseMessage>> respond)
    {
        lock (_gate)
        {
            _routes[Key(method, path)] = respond;
        }

        return this;
    }

    /// <summary>Answers with each response in turn; the last one repeats.</summary>
    public FakeSiigoHandler OnSequence(HttpMethod method, string path, params Func<HttpResponseMessage>[] responses)
    {
        var next = 0;
        return On(method, path, _ => responses[Math.Min(Interlocked.Increment(ref next) - 1, responses.Length - 1)]());
    }

    public static HttpResponseMessage Json(HttpStatusCode status, object body) =>
        Json(status, JsonSerializer.Serialize(body));

    public static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    public static HttpResponseMessage SiigoError(HttpStatusCode status, string code, string message, params string[] parameters) =>
        Json(status, new { Errors = new[] { new { Code = code, Message = message, Params = parameters } } });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var recorded = new RecordedRequest(
            request.Method,
            request.RequestUri!,
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
            request.Headers.ToDictionary(header => header.Key, header => string.Join(", ", header.Value), StringComparer.OrdinalIgnoreCase));

        Func<RecordedRequest, Task<HttpResponseMessage>>? respond;
        lock (_gate)
        {
            _requests.Add(recorded);
            _routes.TryGetValue(Key(request.Method, recorded.Path), out respond);
        }

        var response = respond is null
            ? SiigoError(HttpStatusCode.NotFound, "not_found", $"No fake route for {request.Method} {recorded.Path}")
            : await respond(recorded);
        response.RequestMessage = request;
        return response;
    }

    private static string Key(HttpMethod method, string path) => $"{method.Method} {path}";
}

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Body, IReadOnlyDictionary<string, string> Headers)
{
    public string? Authorization => Header("Authorization");

    public string? PartnerId => Header("Partner-Id");

    public string? IdempotencyKey => Header("Idempotency-Key");

    public string Path => Uri.AbsolutePath;

    public string Query => Uri.Query;

    public JsonNode BodyJson => JsonNode.Parse(Body ?? throw new InvalidOperationException("The request had no body."))!;

    public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
}
