# Dashboard Wolverine - Monitoring Library

A monitoring library and dashboard for the Wolverine Framework.

<img width="1858" height="1382" alt="dashboard" src="https://github.com/user-attachments/assets/f2fda76f-1b48-44fe-9bae-e803e6506e8c" />

<img width="1858" height="1382" alt="detail" src="https://github.com/user-attachments/assets/b9965fff-1c5e-4b2e-975c-ef0a81113c4d" />


## 🚀 List of features

- ✅ **Dead Letters Management** - View and retry dead-letter messages.
- ✅ **Bulk Replay** - Replay multiple dead letters at once.
- ✅ **Incoming Envelopes** - Monitor incoming envelopes.
- ✅ **Nodes Management** - Monitor Wolverine nodes.
- ✅ **Node Assignments** - View node assignments.
- ✅ **Real-time Stats** - Real-time statistics dashboard.
- ✅ **Auto Refresh** - Automatically refresh data.
- ✅ **Search & Filter** - Easily search and filter data.

## 📦 Installation Guide

### 1. Install NuGet Package

```bash
dotnet add package WolverineFx.Dashboard
```

### 3. Register the service in Program.cs

```csharp
using DashboardWolverine;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Register Dashboard
builder.Services.AddMonitoringDashboard(config =>
{
    config.WolverineConnectionString = "npgsql connection string";
    config.RoutePrefix = "/wolverine-ui";
});

var app = builder.Build();

// Use Monitoring Dashboard
app.UseMonitoringDashboard();

app.MapControllers();
app.Run();
```

## 🖥️ Dashboard UI

Access the dashboard through your browser:

```
https://localhost:5001/wolverine-ui
```

### Dashboard Features:

1. **Dead Letters Tab**
   - View all dead letters
   - Select multiple items for bulk retry
   - Search & filter
   - View message details
   - Delete individual dead letters

2. **Incoming Envelopes Tab**
   - Monitor incoming envelopes
   - Filter by status and message type
   - View retry attempts and execution time

3. **Nodes Tab**
   - Monitor node status (Healthy / Unhealthy)
   - Health check monitoring
   - Node capabilities

4. **Node Assignments Tab**
   - View node assignments
   - Monitor started time

## ⚙️ Configuration Options

```csharp
builder.Services.AddMonitoringDashboard(config =>
{
    config.WolverineConnectionString = "npgsql connection string"; // Npgsql Connection string

    config.Schema = "schema"; // database schema for wolverine tables (optional)

    config.RoutePrefix = "/wolverine-ui";                  // Dashboard URL prefix
    config.DashboardTitle = "My Dashboard";                // Dashboard title
    config.EnableAutoRefresh = true;                       // Enable auto refresh
    config.AutoRefreshIntervalSeconds = 30;                // Refresh interval
});
```

```csharp
app.UseMonitoringDashboard();
```

## 🔒 Security

**IMPORTANT**: This dashboard includes built-in authentication. For production environments:

1. Add authorization using a username and password
2. Restrict access by IP/Network
3. Use HTTPS

Example of adding authorization:

```csharp
app.AddMonitoringDashboard(config =>
{
    // other options ...
    config.Username = "admin" ;       // Set username
    config.Password = "password";     // Set password
});
```

## 📝 License

MIT

## 🤝 Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## 📞 Support

For questions or issues, please open a GitHub issue.
