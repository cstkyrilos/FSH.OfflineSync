using Blazored.LocalStorage;
using FSH.OfflineSync.Models;
using FSH.OfflineSync.Services;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace FSH.OfflineSync.Handlers
{
    public class OfflineDelegatingHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly OfflineSyncService _syncService;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public OfflineDelegatingHandler(
            ILocalStorageService localStorage,
            OfflineSyncService syncService)
        {
            _localStorage = localStorage;
            _syncService = syncService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cacheKey = $"offline-cache::{request.RequestUri}";
            var handler = InnerHandler ?? throw new InvalidOperationException("InnerHandler is not set.");
            var client = new HttpClient(handler)
            {
                BaseAddress = request.RequestUri?.GetLeftPart(UriPartial.Authority) is { } baseUri
                    ? new Uri(baseUri)
                    : null
            };
            request.RequestUri = request.RequestUri?.PathAndQuery != null
                                ? new Uri(request.RequestUri.PathAndQuery, UriKind.Relative)
                                : request.RequestUri;

            try
            {
                var clonedRequest = CloneRequest(request);
                var response = await client.SendAsync(clonedRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    var cacheModel = new StoredHttpResponse
                    {
                        StatusCode = (int)response.StatusCode,
                        ReasonPhrase = response.ReasonPhrase ?? "OK",
                        Version = response.Version.ToString(),
                        Body = body,
                        Headers = response.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()),
                        ContentHeaders = response.Content.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray())
                    };
                    await _localStorage.SetItemAsync(cacheKey, cacheModel, cancellationToken);
                }

                return response;
            }
            catch (Exception ex)
            {
                var d = ex.Message;
                if (await _localStorage.ContainKeyAsync(cacheKey, cancellationToken))
                {
                    var cached = await _localStorage.GetItemAsync<StoredHttpResponse>(cacheKey, cancellationToken);
                    var response = new HttpResponseMessage((HttpStatusCode)cached.StatusCode)
                    {
                        ReasonPhrase = cached.ReasonPhrase,
                        Version = Version.Parse(cached.Version),
                        Content = new StringContent(cached.Body, Encoding.UTF8)
                    };
                    foreach (var h in cached.Headers)
                        response.Headers.TryAddWithoutValidation(h.Key, h.Value);
                    foreach (var ch in cached.ContentHeaders)
                        response.Content.Headers.TryAddWithoutValidation(ch.Key, ch.Value);

                    return response;
                }

                if (request.Method != HttpMethod.Get)
                {
                    string? body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
                    var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
                    var queue = await _localStorage.GetItemAsync<List<OfflineRequest>>("offline-queue") ?? new();
                    queue.Add(new OfflineRequest
                    {
                        Url = request.RequestUri?.ToString(),
                        Method = request.Method.Method,
                        Body = body,
                        Headers = headers,
                        Timestamp = DateTime.UtcNow
                    });
                    await _localStorage.SetItemAsync("offline-queue", queue);

                    var fallback = new StoredHttpResponse
                    {
                        StatusCode = 200,
                        ReasonPhrase = "OK",
                        Version = "1.1",
                        Body = JsonSerializer.Serialize(new { message = "Offline queued", isDirty = true }),
                        Headers = new(),
                        ContentHeaders = new()
                    };
                    var fake = new HttpResponseMessage((HttpStatusCode)fallback.StatusCode)
                    {
                        ReasonPhrase = fallback.ReasonPhrase,
                        Version = Version.Parse(fallback.Version),
                        Content = new StringContent(fallback.Body, Encoding.UTF8, "application/json")
                    };
                    return fake;
                }

                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("{\"message\":\"Offline and no cache available.\"}", Encoding.UTF8, "application/json")
                };
            }
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri)
            {
                Version = original.Version
            };
            foreach (var header in original.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            if (original.Content != null)
            {
                var body = original.Content.ReadAsStringAsync().Result;
                clone.Content = new StringContent(body, Encoding.UTF8, "application/json");
                foreach (var header in original.Content.Headers)
                {
                    if (!clone.Content.Headers.Contains(header.Key))
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            return clone;
        }
    }
}
