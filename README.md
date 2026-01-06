# Dashboard Wolverine - Monitoring Library

Library monitoring dan dashboard untuk Wolverine Framework.

## 🚀 Fitur

- ✅ **Dead Letters Management** - View, replay, dan delete dead letters
- ✅ **Bulk Replay** - Replay multiple dead letters sekaligus
- ✅ **Incoming Envelopes** - Monitor envelopes yang masuk
- ✅ **Nodes Management** - Monitor Wolverine nodes
- ✅ **Node Assignments** - Lihat assignment nodes
- ✅ **Real-time Stats** - Dashboard statistik real-time
- ✅ **Auto Refresh** - Refresh otomatis data
- ✅ **Search & Filter** - Cari data dengan mudah

## 📦 Instalasi

### 1. Install NuGet Package

```bash
dotnet add package Npgsql
```

### 3. Register Service di Program.cs

```csharp
using DashboardWolverine;

var builder = WebApplication.CreateBuilder(args);

// Add Wolverine Repository
var connectionString = builder.Configuration.GetConnectionString("WolverineDb") 
    ?? throw new InvalidOperationException("Connection string 'WolverineDb' not found.");
builder.Services.AddSingleton(new WolverineRepository(connectionString));

builder.Services.AddControllers();

var app = builder.Build();

// Use Monitoring Dashboard
app.UseMonitoringDashboard(options =>
{
    options.RoutePrefix = "/wolverine-ui";
    options.DashboardTitle = "Dashboard Wolverine - Monitoring";
    options.EnableAutoRefresh = true;
    options.AutoRefreshIntervalSeconds = 30;
});

app.MapControllers();
app.Run();
```

## 🖥️ Dashboard UI

Akses dashboard melalui browser:

```
https://localhost:5001/wolverine-ui
```

atau untuk dashboard Wolverine khusus:

```
https://localhost:5001/monitoring/dashboard.html
```

### Fitur Dashboard:

1. **Dead Letters Tab**
   - List semua dead letters
   - Select multiple items untuk bulk replay/unreplay
   - Search & filter
   - View detail message
   - Delete individual dead letter

2. **Incoming Envelopes Tab**
   - Monitor envelopes yang masuk
   - Filter by status dan message type
   - View attempts dan execution time

3. **Nodes Tab**
   - Monitor status nodes (Healthy/Unhealthy)
   - Health check monitoring
   - Node capabilities

4. **Node Assignments Tab**
   - View node assignments
   - Monitor started time

## 🗄️ Database Tables

Library ini bekerja dengan tabel Wolverine standar:

- `wolverine_dead_letters`
- `wolverine_incoming_envelopes`
- `wolverine_nodes`
- `wolverine_node_assignments`

## ⚙️ Configuration Options

```csharp
app.UseMonitoringDashboard(options =>
{
    options.RoutePrefix = "/wolverine-ui";                    // Dashboard URL prefix
    options.DashboardTitle = "My Dashboard";                // Dashboard title
    options.EnableAutoRefresh = true;                       // Enable auto refresh
    options.AutoRefreshIntervalSeconds = 30;                // Refresh interval
});
```

## 🔒 Security

**PENTING**: Dashboard ini memiliki autentikasi built-in. Untuk production:

1. Tambahkan authorization pada username & password
2. Restrict access by IP/Network
3. Gunakan HTTPS

Contoh menambahkan authorization:

```csharp
app.UseMonitoringDashboard(options =>
{
    // other options ...
    options.Username = "admin" ;                                   // Set username
    options.Password = "password";                                // Set password
});
```

## 📝 License

MIT

## 🤝 Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## 📞 Support

Untuk pertanyaan atau issue, silakan buka GitHub issue.
