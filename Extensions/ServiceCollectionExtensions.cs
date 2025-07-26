using Blazored.LocalStorage;
using FSH.OfflineSync.Handlers;
using FSH.OfflineSync.Models;
using FSH.OfflineSync.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.OfflineSync.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOfflineSyncHttpClient(
     this IServiceCollection services,
     IConfiguration configuration,
     Action<OfflineSyncOptions>? configureOptions = null)
        {
            var options = new OfflineSyncOptions();
            configureOptions?.Invoke(options);

            services.AddSingleton(options);
            services.AddBlazoredLocalStorage();

            services.AddScoped<AuthHeaderHandler>();
            services.AddScoped<OfflineSyncService>();
            services.AddScoped<OfflineDelegatingHandler>();

            var apiDomain = configuration["APIDomain"]
                ?? throw new InvalidOperationException("Missing APIDomain in appsettings.json");

            services.AddHttpClient("FSHClient", client =>
            {
                client.BaseAddress = new Uri(apiDomain);
            })
            .AddHttpMessageHandler<OfflineDelegatingHandler>()
            .AddHttpMessageHandler<AuthHeaderHandler>();

            services.AddScoped(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return factory.CreateClient("FSHClient");
            });

            return services;
        }
    }

}
