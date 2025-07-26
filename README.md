# FSH.OfflineSync

> 🔄 A plug-and-play offline sync library for **Blazor WebAssembly + FullStackHero** apps using `HttpClient`, `DelegatingHandler`, and `Blazor.Localstorage`.

---

## ✨ Features

- 📡 Caches GET API responses locally
- 📴 Queues POST/PUT/DELETE requests offline
- 🔁 Automatically syncs when connection is restored
- 🔐 Attaches `Authorization: Bearer <token>` using `Blazored.LocalStorage`
- 🔌 One-liner integration via `AddOfflineSyncHttpClient()`
- ✅ Designed for FullStackHero + Blazor WebAssembly
- 💾 Uses Blazor.Localstorage for durable offline support

---

## 📦 Installation

```bash
dotnet add package FSH.OfflineSync
```

---

## 🚀 Getting Started

### 1. Add the following to your `Program.cs`:

```csharp
builder.Services.AddOfflineSyncHttpClient(builder.Configuration, options =>
{
    options.AuthTokenKey = StorageConstants.Local.AuthToken; // or "authToken" Or StorageConstants.Local.AuthToken if using FullStackHero
});
```

### 2. Add your API domain to `wwwroot/appsettings.json`:

```json
{
  "APIDomain": "https://your-api-domain.com"
}
```

---

## 🧩 What’s Registered Automatically

| Service | Purpose |
|--------|---------|
| `OfflineDelegatingHandler` | Intercepts API calls and manages caching |
| `AuthHeaderHandler`        | Adds `Authorization` header from local storage |
| `OfflineSyncService`       | Manages offline queue and sync process |
| `ILocalStorageService`     | Handles token and local cache storage |
| `HttpClient` ("FSHClient") | Pre-configured with handlers and base URL |

---

## ⚙️ Configuration

```csharp
public class OfflineSyncOptions
{
    public string AuthTokenKey { get; set; } = "authToken";
}
```

If your app uses a different key for the auth token in `localStorage`, pass it via `OfflineSyncOptions`.

---

## 🧠 Sample Usage

You don't need to change how you use `HttpClient`. All GET/POST/PUT/DELETE calls will work as usual:

```csharp
@inject HttpClient Http

var result = await Http.GetFromJsonAsync<List<Product>>("api/products");
```

If the app is offline:

- `GET` will return from local cache (if available)
- `POST/PUT/DELETE` will be queued in IndexedDB and synced when online

---

## 🗃 Project Structure

```
FSH.OfflineSync/
│
├── Extensions/
│   └── ServiceCollectionExtensions.cs
│
├── Handlers/
│   ├── OfflineDelegatingHandler.cs
│   └── AuthHeaderHandler.cs
│
├── Services/
│   ├── OfflineSyncService.cs
│   └── IOfflineSyncService.cs
│
├── Models/
│   ├── QueuedRequest.cs
│   └── SyncResult.cs
│
├── JSInterop/
│   └── OfflineSyncJsInterop.cs
│
├── Options/
│   └── OfflineSyncOptions.cs
│
├── wwwroot/
│   └── manifest.sync.json (optional)
│
└── FSH.OfflineSync.csproj
```

---

## 📈 Roadmap / Future Enhancements

- [ ] 🔄 Background sync using Service Workers
- [ ] ⚙️ Configurable retry policy (e.g., exponential backoff)
- [ ] 🪵 Logging support via `ILogger`
- [ ] 📶 `<OfflineStatus>` UI component
- [ ] 🔐 Encrypted offline storage
- [ ] 🧪 Unit test project + integration test sample
- [ ] 🧩 Typed API client (`IApiClient`)
- [ ] 📁 Sample app (`ConsumerApp`) in repo
- [ ] 📦 NuGet & CI/CD auto-publish

---

## ✅ GitHub Actions: CI/CD Workflow

## 📄 License

MIT © 2025 — Developed by [KyrilosAdel](https://github.com/cstkyrilos)

---

## 💬 Support & Contributions

Feel free to open [issues](https://github.com/cstkyrilos/FSH.OfflineSync/issues) or submit pull requests. Contributions are welcome!
