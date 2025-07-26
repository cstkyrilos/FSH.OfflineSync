using Blazored.LocalStorage;
using FSH.OfflineSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.OfflineSync.Handlers
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly OfflineSyncOptions _options;

        public AuthHeaderHandler(ILocalStorageService localStorage, OfflineSyncOptions options)
        {
            _localStorage = localStorage;
            _options = options;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenKey = _options.AuthTokenKey ?? "authToken"; // fallback if null
            var token = await _localStorage.GetItemAsync<string>(tokenKey);

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
