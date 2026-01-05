using Microsoft.AspNetCore.Http;

namespace DashboardWolverine;

/// <summary>
/// Configuration options untuk Monitoring Dashboard
/// </summary>
public class MonitoringDashboardOptions
{
    /// <summary>
    /// URL path prefix untuk dashboard. Default: "/monitoring"
    /// </summary>
    public string RoutePrefix { get; set; } = "/wolverine-ui";

    /// <summary>
    /// Judul yang ditampilkan di header dashboard. Default: "API Monitoring Dashboard"
    /// </summary>
    public string DashboardTitle { get; set; } = "API Monitoring Dashboard";

    /// <summary>
    /// Default API endpoint untuk fetch monitoring data. Default: "/api/monitoring/stats"
    /// </summary>
    public string DefaultDataEndpoint { get; set; }

    /// <summary>
    /// Function untuk authorization. Return true untuk allow access, false untuk deny.
    /// Default: null (allow all)
    /// </summary>
    public Func<HttpContext, bool>? Authorization { get; set; }

    /// <summary>
    /// Path ke custom HTML file (optional). Jika null, akan menggunakan embedded HTML default.
    /// </summary>
    public string? CustomHtmlPath { get; set; }

    /// <summary>
    /// Enable auto-refresh data di dashboard. Default: true
    /// </summary>
    public bool EnableAutoRefresh { get; set; } = true;

    /// <summary>
    /// Auto-refresh interval dalam detik. Default: 60
    /// Minimum: 5 detik
    /// </summary>
    public int AutoRefreshIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Custom CSS untuk styling dashboard (optional)
    /// </summary>
    public string? CustomCss { get; set; }

    /// <summary>
    /// PostgreSQL connection string untuk Wolverine database.
    /// Required untuk menggunakan Wolverine monitoring features.
    /// Format: "Host=localhost;Port=5432;Database=db_name;Username=user;Password=pass"
    /// </summary>
    public string? WolverineConnectionString { get; set; }

    /// <summary>
    /// Username untuk Basic Authentication.
    /// Jika diset (tidak null/empty), dashboard akan memerlukan Basic Auth.
    /// Username harus dikombinasikan dengan Password.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password untuk Basic Authentication.
    /// Jika diset (tidak null/empty), dashboard akan memerlukan Basic Auth.
    /// Password harus dikombinasikan dengan Username.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Realm name untuk Basic Authentication prompt.
    /// Default: "Monitoring Dashboard"
    /// </summary>
    public string AuthenticationRealm { get; set; } = "Monitoring Dashboard";

    /// <summary>
    /// Cek apakah Basic Authentication diaktifkan (ada username dan password)
    /// </summary>
    internal bool IsBasicAuthEnabled => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(RoutePrefix))
        {
            throw new ArgumentException("RoutePrefix cannot be empty", nameof(RoutePrefix));
        }

        if (!RoutePrefix.StartsWith("/"))
        {
            RoutePrefix = "/" + RoutePrefix;
        }

        if (AutoRefreshIntervalSeconds < 5)
        {
            throw new ArgumentException("AutoRefreshIntervalSeconds must be at least 5 seconds", nameof(AutoRefreshIntervalSeconds));
        }

        // Validate Basic Auth: jika salah satu diset, keduanya harus diset
        var hasUsername = !string.IsNullOrWhiteSpace(Username);
        var hasPassword = !string.IsNullOrWhiteSpace(Password);

        if (hasUsername != hasPassword)
        {
            throw new ArgumentException(
                "Basic Authentication requires both Username and Password to be set. " +
                "Either set both or leave both empty to disable authentication.",
                hasUsername ? nameof(Password) : nameof(Username));
        }
    }
}