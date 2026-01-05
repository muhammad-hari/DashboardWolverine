# Dashboard Wolverine - Library Usage Guide

## 📚 Instalasi dan Setup

### 1. Install Package

Install package ke project Anda:

```bash
dotnet add reference DashboardWolverine
```

### 2. Tambahkan Connection String

Edit `appsettings.json` di aplikasi Anda:

```json
{
  "ConnectionStrings": {
    "WolverineDb": "Host=localhost;Port=5432;Database=your_db;Username=user;Password=pass"
  }
}
```

### 3. Register Dashboard di Program.cs

#### Cara 1: Menggunakan `AddMonitoringDashboard` + `UseMonitoringDashboard`

```csharp
using DashboardWolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register dashboard services (Optional - untuk dependency injection)
builder.Services.AddMonitoringDashboard(options =>
{
    options.RoutePrefix = "/monitoring";
    options.DashboardTitle = "My App - Wolverine Dashboard";
    options.WolverineConnectionString = builder.Configuration.GetConnectionString("WolverineDb");
});

var app = builder.Build();

// Use dashboard middleware (Required)
app.UseMonitoringDashboard(options =>
{
    options.RoutePrefix = "/monitoring";
    options.DashboardTitle = "My App - Wolverine Dashboard";
    options.EnableAutoRefresh = true;
    options.AutoRefreshIntervalSeconds = 30;
    options.WolverineConnectionString = builder.Configuration.GetConnectionString("WolverineDb");
});

app.MapControllers();
app.Run();
```

#### Cara 2: Hanya menggunakan `UseMonitoringDashboard` (Simplified)

```csharp
using DashboardWolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

// Use dashboard middleware dengan WolverineConnectionString
app.UseMonitoringDashboard(options =>
{
    options.RoutePrefix = "/monitoring";
    options.DashboardTitle = "My App - Wolverine Dashboard";
    options.EnableAutoRefresh = true;
    options.AutoRefreshIntervalSeconds = 30;
    // PENTING: Set connection string dari configuration
    options.WolverineConnectionString = builder.Configuration.GetConnectionString("WolverineDb");
});

app.MapControllers();
app.Run();
```

## 🔧 Configuration Options

```csharp
options.RoutePrefix = "/monitoring";                        // Dashboard URL prefix
options.DashboardTitle = "My Dashboard";                    // Judul dashboard
options.EnableAutoRefresh = true;                           // Enable auto-refresh
options.AutoRefreshIntervalSeconds = 30;                    // Refresh interval (seconds)
options.WolverineConnectionString = "connection_string";    // PostgreSQL connection string
options.DefaultDataEndpoint = "/api/wolverine/stats";       // Default API endpoint
options.CustomHtmlPath = "/path/to/custom.html";           // Custom HTML (optional)
options.Authorization = (context) => {                      // Authorization (optional)
    return context.User.IsInRole("Admin");
};
```

## 🌐 Akses Dashboard

Setelah aplikasi berjalan, akses dashboard melalui browser:

### Dashboard Umum
```
https://localhost:5001/monitoring
```

### Dashboard Wolverine (Dead Letters, Nodes, dll)
```
https://localhost:5001/monitoring/wolverine-dashboard.html
```

## 📡 API Endpoints

Setelah dashboard teregistrasi, endpoints berikut otomatis tersedia:

### Stats
- `GET /api/wolverine/stats` - Dashboard statistics

### Dead Letters
- `GET /api/wolverine/dead-letters` - List semua dead letters
- `GET /api/wolverine/dead-letters/{id}?receivedAt=xxx` - Detail dead letter
- `PUT /api/wolverine/dead-letters/{id}/replay?receivedAt=xxx` - Replay single
- `PUT /api/wolverine/dead-letters/replay-multiple` - Bulk replay
- `DELETE /api/wolverine/dead-letters/{id}?receivedAt=xxx` - Delete

### Incoming Envelopes
- `GET /api/wolverine/incoming-envelopes` - List incoming envelopes
- `GET /api/wolverine/incoming-envelopes/{id}?receivedAt=xxx` - Detail
- `DELETE /api/wolverine/incoming-envelopes/{id}?receivedAt=xxx` - Delete

### Nodes
- `GET /api/wolverine/nodes` - List nodes
- `GET /api/wolverine/nodes/{id}` - Detail node
- `DELETE /api/wolverine/nodes/{id}` - Delete node

### Node Assignments
- `GET /api/wolverine/node-assignments` - List assignments
- `GET /api/wolverine/node-assignments/{id}` - Detail assignment
- `DELETE /api/wolverine/node-assignments/{id}` - Delete assignment

## 💡 Contoh Penggunaan

### Bulk Replay Dead Letters dari Code

```csharp
public class MyService
{
    private readonly HttpClient _httpClient;
    
    public async Task ReplayDeadLetters(List<Guid> ids)
    {
        var request = new
        {
            deadLetters = ids.Select(id => new { id, receivedAt = "node1" }).ToList(),
            replayable = true
        };
        
        var response = await _httpClient.PutAsJsonAsync(
            "/api/wolverine/dead-letters/replay-multiple", 
            request
        );
        
        response.EnsureSuccessStatusCode();
    }
}
```

### Cek Stats dari Code

```csharp
public class MonitoringService
{
    private readonly HttpClient _httpClient;
    
    public async Task<DashboardStats> GetStatsAsync()
    {
        var stats = await _httpClient.GetFromJsonAsync<DashboardStats>(
            "/api/wolverine/stats"
        );
        
        return stats;
    }
}

public class DashboardStats
{
    public int TotalDeadLetters { get; set; }
    public int ReplayableDeadLetters { get; set; }
    public int TotalIncomingEnvelopes { get; set; }
    public int ActiveNodes { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 🔐 Security

Dashboard ini **tidak memiliki autentikasi built-in**. Untuk production:

### Menambahkan Authorization

```csharp
app.UseMonitoringDashboard(options =>
{
    options.RoutePrefix = "/monitoring";
    options.WolverineConnectionString = builder.Configuration.GetConnectionString("WolverineDb");
    
    // Hanya allow user dengan role "Admin"
    options.Authorization = (context) =>
    {
        return context.User.IsInRole("Admin");
    };
});
```

### Menggunakan IP Whitelist

```csharp
options.Authorization = (context) =>
{
    var remoteIp = context.Connection.RemoteIpAddress?.ToString();
    var allowedIps = new[] { "127.0.0.1", "::1", "192.168.1.100" };
    return allowedIps.Contains(remoteIp);
};
```

## 🧪 Testing

Untuk testing, Anda bisa mock WolverineRepository:

```csharp
services.AddSingleton<WolverineRepository>(sp => 
{
    return new WolverineRepository("test_connection_string");
});
```

## ❗ Troubleshooting

### Error: "Connection string 'WolverineDb' not found"

**Solusi**: Pastikan Anda sudah menambahkan connection string di `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "WolverineDb": "Host=localhost;..."
  }
}
```

### Error: "WolverineRepository not registered"

**Solusi**: Pastikan Anda set `WolverineConnectionString` di options:

```csharp
options.WolverineConnectionString = builder.Configuration.GetConnectionString("WolverineDb");
```

### Dashboard tidak muncul

**Solusi**: 
1. Cek bahwa `app.UseMonitoringDashboard()` dipanggil setelah `app.Build()`
2. Pastikan `RoutePrefix` benar (default: `/monitoring`)
3. Akses dengan URL lengkap: `https://localhost:5001/monitoring`

### API endpoints return 404

**Solusi**: Pastikan `app.MapControllers()` sudah dipanggil di `Program.cs`

## 📝 Best Practices

1. **Always use appsettings.json untuk connection strings** - Jangan hardcode
2. **Enable authorization di production** - Dashboard berisi data sensitif
3. **Set auto-refresh interval yang reasonable** - Jangan terlalu cepat (min 5 detik)
4. **Monitor your PostgreSQL connection pool** - Library ini menggunakan Npgsql
5. **Use HTTPS di production** - Protect data in transit

## 🆘 Support

Untuk issue atau pertanyaan, silakan buka GitHub issue di repository project.
