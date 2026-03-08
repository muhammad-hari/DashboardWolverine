using Microsoft.AspNetCore.Http;

namespace DashboardWolverine;

/// <summary>
/// Configuration options for the Monitoring Dashboard
/// </summary>
public class MonitoringDashboardOptions
{
    /// <summary>
    /// URL path prefix for the dashboard. Default: "/wolverine-ui"
    /// </summary>
    public string RoutePrefix { get; set; } = "/wolverine-ui";

    /// <summary>
    /// Title displayed in the dashboard header. Default: "API Monitoring Dashboard"
    /// </summary>
    public string DashboardTitle { get; set; } = "API Monitoring Dashboard";

    /// <summary>
    /// Default API endpoint used to fetch monitoring data. Default: "/api/monitoring/stats"
    /// </summary>
    public string DefaultDataEndpoint { get; set; }

    /// <summary>
    /// Authorization function. Return true to allow access, false to deny.
    /// Default: null (allow all requests).
    /// </summary>
    public Func<HttpContext, bool>? Authorization { get; set; }

    /// <summary>
    /// Path to a custom HTML file (optional). If null, the embedded default HTML will be used.
    /// </summary>
    public string? CustomHtmlPath { get; set; }

    /// <summary>
    /// Enable automatic data refresh on the dashboard. Default: true
    /// </summary>
    public bool EnableAutoRefresh { get; set; } = true;

    /// <summary>
    /// Auto-refresh interval in seconds. Default: 60. Minimum: 5 seconds.
    /// </summary>
    public int AutoRefreshIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Custom CSS for dashboard styling (optional).
    /// </summary>
    public string? CustomCss { get; set; }

    /// <summary>
    /// PostgreSQL connection string for the Wolverine database.
    /// Required to use Wolverine monitoring features.
    /// Format: "Host=localhost;Port=5432;Database=db_name;Username=user;Password=pass"
    /// </summary>
    public string? WolverineConnectionString { get; set; }

    /// <summary>
    /// Database schema for Wolverine tables.
    /// If set, queries will use the schema prefix (e.g., "myschema.wolverine_dead_letters").
    /// If null or empty, the default schema will be used.
    /// Default: null (use default schema).
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Optional additional server path prefix to try when calling API endpoints.
    /// Example: if your app is hosted under "/xyz", set this to "/xyz" so frontend
    /// will try "/xyz/api/..." in addition to "/api/...".
    /// </summary>
    public string? AddServerPath { get; set; }

    /// <summary>
    /// Username for Basic Authentication.
    /// If set (not null/empty), the dashboard will require Basic Auth.
    /// Username must be provided together with a password.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for Basic Authentication.
    /// If set (not null/empty), the dashboard will require Basic Auth.
    /// Password must be provided together with a username.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Realm name for the Basic Authentication prompt.
    /// Default: "Monitoring Dashboard"
    /// </summary>
    public string AuthenticationRealm { get; set; } = "Monitoring Dashboard";

    /// <summary>
    /// Checks whether Basic Authentication is enabled (both username and password are set).
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

        // Normalize AddServerPath: ensure leading slash and no trailing slash
        if (!string.IsNullOrWhiteSpace(AddServerPath))
        {
            AddServerPath = AddServerPath.Trim();
            if (!AddServerPath.StartsWith("/"))
                AddServerPath = "/" + AddServerPath;
            AddServerPath = AddServerPath.TrimEnd('/');
        }

        if (AutoRefreshIntervalSeconds < 5)
        {
            throw new ArgumentException("AutoRefreshIntervalSeconds must be at least 5 seconds", nameof(AutoRefreshIntervalSeconds));
        }

        // Validate Basic Auth: if one is set, both must be set
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