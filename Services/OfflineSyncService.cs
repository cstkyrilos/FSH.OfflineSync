using System.Net.Http;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using FSH.OfflineSync.Handlers;
using FSH.OfflineSync.Models;

namespace FSH.OfflineSync.Services
{
    public class OfflineSyncService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILocalStorageService _localStorage;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public OfflineSyncService(
            IHttpClientFactory clientFactory,
            ILocalStorageService localStorage)
        {
            _clientFactory = clientFactory;
            _localStorage = localStorage;
        }

        public async Task SyncAsync()
        {
            var queue = await _localStorage.GetItemAsync<List<OfflineRequest>>("offline-queue") ?? new();
            if (queue.Count == 0) return;

            bool changed = false;
            var client = _clientFactory.CreateClient();

            foreach (var request in queue.ToList())
            {
                try
                {
                    if (request.Url?.Contains("/api/tokens", StringComparison.OrdinalIgnoreCase) == true)
                    {


                        Console.WriteLine($"[OfflineSync] Skipping replay of /api/tokens.");
                        queue.Remove(request); 
                        continue;
                    }

                    var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method!), request.Url);

                    if (!string.IsNullOrWhiteSpace(request.Body))
                    {
                        httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
                    }

                    var response = await client.SendAsync(httpRequest);
                    if (response.IsSuccessStatusCode)
                    {
                        await MarkSyncedDto(request);
                        queue.Remove(request);
                        changed = true;
                    }
                }
                catch
                {
                    break;
                }
            }

            if (changed)
            {
                await _localStorage.SetItemAsync("offline-queue", queue);
            }
        }

        private async Task MarkSyncedDto(OfflineRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Body))
                return;

            foreach (var dtoType in GetFSHOfflineDtoTypes())
            {
                try
                {
                    var dto = JsonSerializer.Deserialize(request.Body, dtoType, _jsonOptions);
                    if (dto is FSHApiOffline offlineDto)
                    {
                        offlineDto.IsDirty = false;
                        offlineDto.LastModified = DateTime.UtcNow;

                        string cacheKey = $"offline-cache::{request.Url}";
                        var updatedBody = JsonSerializer.Serialize(offlineDto, dtoType, _jsonOptions);
                        await _localStorage.SetItemAsync(cacheKey, updatedBody);
                    }
                }
                catch
                {
                }
            }
        }

        private static IEnumerable<Type> GetFSHOfflineDtoTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => typeof(FSHApiOffline).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
        }
    }
}
