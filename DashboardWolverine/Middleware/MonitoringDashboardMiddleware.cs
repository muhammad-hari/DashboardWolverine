using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace DashboardWolverine;

public class MonitoringDashboardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MonitoringDashboardOptions _options;
    private readonly IWebHostEnvironment _environment;
    private static string? _cachedHtml;
    private static readonly object _cacheLock = new();

    public MonitoringDashboardMiddleware(
        RequestDelegate next,
        MonitoringDashboardOptions options,
        IWebHostEnvironment environment)
    {
        _next = next;
        _options = options;
        _environment = environment;
        _options.Validate();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var routePrefix = _options.RoutePrefix.ToLower();

        // Handle dashboard root path
        if (path == routePrefix || path == $"{routePrefix}/")
        {
            await ServeDashboardAsync(context);
            return;
        }

        // Handle static assets (CSS, JS, images)
        if (path.StartsWith($"{routePrefix}/assets/"))
        {
            await ServeAssetAsync(context, path, routePrefix);
            return;
        }

        // Continue to next middleware
        await _next(context);
    }

    private bool IsBasicAuthValid(HttpContext context)
    {
        try
        {
            // Get Authorization header
            var authHeader = context.Request.Headers.Authorization.ToString();
            
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Extract and decode credentials
            var encodedCredentials = authHeader.Substring(6).Trim();
            var decodedBytes = Convert.FromBase64String(encodedCredentials);
            var decodedCredentials = System.Text.Encoding.UTF8.GetString(decodedBytes);
            
            var colonIndex = decodedCredentials.IndexOf(':');
            if (colonIndex == -1)
            {
                return false;
            }

            var username = decodedCredentials.Substring(0, colonIndex);
            var password = decodedCredentials.Substring(colonIndex + 1);

            // Validate credentials (constant-time comparison for security)
            var usernameMatch = CryptographicEquals(username, _options.Username!);
            var passwordMatch = CryptographicEquals(password, _options.Password!);

            return usernameMatch && passwordMatch;
        }
        catch
        {
            return false;
        }
    }

    // Constant-time string comparison to prevent timing attacks
    private static bool CryptographicEquals(string a, string b)
    {
        if (a == null || b == null)
            return false;

        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    private async Task ServeDashboardAsync(HttpContext context)
    {
        // Check Basic Authentication first (if enabled)
        if (_options.IsBasicAuthEnabled)
        {
            if (!IsBasicAuthValid(context))
            {
                // Request Basic Auth
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.Append("WWW-Authenticate", $"Basic realm=\"{_options.AuthenticationRealm}\"");
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("401 Unauthorized - Authentication required");
                return;
            }
        }

        // Check custom authorization (if set)
        if (_options.Authorization != null && !_options.Authorization.Invoke(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(GetUnauthorizedHtml());
            return;
        }

        // Get HTML content (from embedded resource or custom path)
        string htmlContent;

        // Priority 1: Custom HTML Path (jika di-set oleh user)
        if (!string.IsNullOrEmpty(_options.CustomHtmlPath) && File.Exists(_options.CustomHtmlPath))
        {
            htmlContent = await File.ReadAllTextAsync(_options.CustomHtmlPath);
        }
        // Priority 2: Development - baca langsung dari file system (wwwroot/monitoring/dashboard.html)
        else if (_environment.IsDevelopment())
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", "monitoring", "dashboard.html");
            if (File.Exists(filePath))
            {
                // Baca langsung dari file - tidak pakai cache agar perubahan langsung terlihat
                htmlContent = await File.ReadAllTextAsync(filePath);
            }
            else
            {
                // Fallback jika file tidak ada
                htmlContent = await GetEmbeddedHtmlAsync();
            }
        }
        // Priority 3: Production - gunakan embedded resource (untuk performa)
        else
        {
            htmlContent = await GetEmbeddedHtmlAsync();
        }

        // Replace placeholders
        htmlContent = ReplacePlaceholders(htmlContent);

        // Serve HTML
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(htmlContent);
    }

    private async Task ServeAssetAsync(HttpContext context, string path, string routePrefix)
    {
        // Convert URL path to embedded resource name
        var resourcePath = path
            .Replace(routePrefix, "wwwroot.monitoring")
            .Replace("/", ".")
            .TrimStart('.');

        var assembly = Assembly.GetExecutingAssembly();
        var fullResourceName = $"{assembly.GetName().Name}.{resourcePath}";

        // Try to find resource
        var resourceStream = assembly.GetManifestResourceStream(fullResourceName);

        if (resourceStream == null)
        {
            // Try alternative naming
            var alternativeNames = assembly.GetManifestResourceNames()
                .Where(r => r.Contains(Path.GetFileName(path)))
                .ToList();

            if (alternativeNames.Any())
            {
                resourceStream = assembly.GetManifestResourceStream(alternativeNames.First());
            }
        }

        if (resourceStream == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Set content type
        var extension = Path.GetExtension(path).ToLowerInvariant();
        context.Response.ContentType = GetContentType(extension);

        // Serve resource
        await resourceStream.CopyToAsync(context.Response.Body);
        await resourceStream.DisposeAsync();
    }

    private async Task<string> GetEmbeddedHtmlAsync()
    {
        // Use cached HTML if available
        if (_cachedHtml != null)
        {
            return _cachedHtml;
        }

        lock (_cacheLock)
        {
            if (_cachedHtml != null)
            {
                return _cachedHtml;
            }

            var assembly = Assembly.GetExecutingAssembly();

            // Try to find dashboard.html in embedded resources
            var possibleNames = new[]
            {
                $"{assembly.GetName().Name}.wwwroot.monitoring.dashboard.html",
                $"{assembly.GetName().Name}.dashboard.html",
                "dashboard.html"
            };

            Stream? stream = null;
            foreach (var name in possibleNames)
            {
                stream = assembly.GetManifestResourceStream(name);
                if (stream != null) break;
            }

            if (stream == null)
            {
                // If no embedded resource found, use fallback HTML
                _cachedHtml = GetFallbackHtml();
                return _cachedHtml;
            }

            using var reader = new StreamReader(stream);
            _cachedHtml = reader.ReadToEnd();
            return _cachedHtml;
        }
    }

    private string ReplacePlaceholders(string html)
    {
        var result = html
            .Replace("{{DASHBOARD_TITLE}}", _options.DashboardTitle)
            .Replace("{{DEFAULT_ENDPOINT}}", _options.DefaultDataEndpoint)
            .Replace("{{BASE_PATH}}", _options.RoutePrefix)
            .Replace("{{AUTO_REFRESH}}", _options.EnableAutoRefresh.ToString().ToLower())
            .Replace("{{REFRESH_INTERVAL}}", (_options.AutoRefreshIntervalSeconds * 1000).ToString());

        // Add custom CSS if provided
        if (!string.IsNullOrEmpty(_options.CustomCss))
        {
            result = result.Replace("</head>", $"<style>{_options.CustomCss}</style></head>");
        }


        return result;
    }

    private static string GetContentType(string extension) => extension switch
    {
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".json" => "application/json",
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".ico" => "image/x-icon",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        ".ttf" => "font/ttf",
        ".eot" => "application/vnd.ms-fontobject",
        _ => "application/octet-stream"
    };

    private string GetUnauthorizedHtml()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Unauthorized</title>
    <style>
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }
        .container {
            background: white;
            padding: 40px;
            border-radius: 15px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 500px;
        }
        h1 { color: #e74c3c; margin-bottom: 10px; }
        p { color: #666; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔒 401 - Unauthorized</h1>
        <p>You don't have permission to access this dashboard.</p>
    </div>
</body>
</html>";
    }

    private string GetFallbackHtml()
    {
        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{{{{DASHBOARD_TITLE}}}}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }}
        .container {{ max-width: 1400px; margin: 0 auto; }}
        .header {{
            background: white;
            padding: 30px;
            border-radius: 15px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
            margin-bottom: 30px;
        }}
        .header h1 {{ color: #333; font-size: 32px; margin-bottom: 10px; }}
        .header p {{ color: #666; font-size: 14px; }}
        .stats-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }}
        .stat-card {{
            background: white;
            padding: 25px;
            border-radius: 15px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
            transition: transform 0.3s ease;
        }}
        .stat-card:hover {{ transform: translateY(-5px); }}
        .stat-card h3 {{
            color: #666;
            font-size: 14px;
            text-transform: uppercase;
            margin-bottom: 10px;
            font-weight: 500;
        }}
        .stat-value {{ color: #333; font-size: 36px; font-weight: bold; }}
        .stat-label {{ color: #999; font-size: 12px; margin-top: 5px; }}
        .content-section {{
            background: white;
            padding: 30px;
            border-radius: 15px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
            margin-bottom: 20px;
        }}
        .content-section h2 {{
            color: #333;
            font-size: 24px;
            margin-bottom: 20px;
            padding-bottom: 15px;
            border-bottom: 2px solid #f0f0f0;
        }}
        .loading {{
            text-align: center;
            padding: 40px;
            color: #999;
        }}
        .spinner {{
            border: 4px solid #f3f3f3;
            border-top: 4px solid #667eea;
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: spin 1s linear infinite;
            margin: 0 auto 20px;
        }}
        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}
        .error {{
            background: #fee;
            color: #c33;
            padding: 15px;
            border-radius: 8px;
            margin: 10px 0;
        }}
        .btn {{
            background: #667eea;
            color: white;
            border: none;
            padding: 12px 24px;
            border-radius: 8px;
            cursor: pointer;
            font-size: 14px;
            font-weight: 500;
            transition: background 0.3s ease;
        }}
        .btn:hover {{ background: #5568d3; }}
        .endpoint-config {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
        }}
        .endpoint-config input {{
            width: 100%;
            padding: 10px;
            border: 1px solid #ddd;
            border-radius: 6px;
            font-size: 14px;
            margin-top: 5px;
        }}
        .endpoint-config label {{
            display: block;
            color: #666;
            font-size: 13px;
            font-weight: 500;
            margin-bottom: 5px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{{{DASHBOARD_TITLE}}}}</h1>
            <p>Real-time monitoring dashboard • Last updated: <span id='lastUpdate'>-</span></p>
        </div>

        <div class='stats-grid'>
            <div class='stat-card'>
                <h3>Total Requests</h3>
                <div class='stat-value' id='totalRequests'>-</div>
                <div class='stat-label'>All time</div>
            </div>
            <div class='stat-card'>
                <h3>Active Users</h3>
                <div class='stat-value' id='activeUsers'>-</div>
                <div class='stat-label'>Currently online</div>
            </div>
            <div class='stat-card'>
                <h3>Response Time</h3>
                <div class='stat-value' id='responseTime'>-</div>
                <div class='stat-label'>Average (ms)</div>
            </div>
            <div class='stat-card'>
                <h3>Error Rate</h3>
                <div class='stat-value' id='errorRate'>-</div>
                <div class='stat-label'>Last 24 hours</div>
            </div>
        </div>

        <div class='content-section'>
            <h2>API Configuration</h2>
            <div class='endpoint-config'>
                <label for='apiEndpoint'>Data API Endpoint URL:</label>
                <input type='text' id='apiEndpoint' placeholder='e.g., /api/monitoring/stats' value='{{{{DEFAULT_ENDPOINT}}}}'>
            </div>
            <button class='btn' onclick='fetchData()'>Refresh Data</button>
        </div>

        <div class='content-section'>
            <h2>Monitoring Data</h2>
            <div id='dataContent'>
                <div class='loading'>
                    <div class='spinner'></div>
                    <p>Loading data...</p>
                </div>
            </div>
        </div>
    </div>

    <script>
        let autoRefreshInterval;

        async function fetchData() {{
            const endpoint = document.getElementById('apiEndpoint').value;
            const dataContent = document.getElementById('dataContent');

            if (!endpoint) {{
                dataContent.innerHTML = '<div class=""error"">Please enter an API endpoint URL</div>';
                return;
            }}

            dataContent.innerHTML = '<div class=""loading""><div class=""spinner""></div><p>Loading data...</p></div>';

            try {{
                const response = await fetch(endpoint);
                
                if (!response.ok) {{
                    throw new Error(`HTTP error! status: ${{response.status}}`);
                }}

                const data = await response.json();
                
                updateStats(data);
                displayData(data);
                
                document.getElementById('lastUpdate').textContent = new Date().toLocaleString();
            }} catch (error) {{
                dataContent.innerHTML = `<div class=""error"">Error fetching data: ${{error.message}}</div>`;
                console.error('Fetch error:', error);
            }}
        }}

        function updateStats(data) {{
            if (data.totalRequests !== undefined) {{
                document.getElementById('totalRequests').textContent = data.totalRequests.toLocaleString();
            }}
            if (data.activeUsers !== undefined) {{
                document.getElementById('activeUsers').textContent = data.activeUsers.toLocaleString();
            }}
            if (data.responseTime !== undefined) {{
                document.getElementById('responseTime').textContent = data.responseTime + 'ms';
            }}
            if (data.errorRate !== undefined) {{
                document.getElementById('errorRate').textContent = data.errorRate + '%';
            }}
        }}

        function displayData(data) {{
            const dataContent = document.getElementById('dataContent');
            dataContent.innerHTML = `
                <pre style=""background: #f8f9fa; padding: 20px; border-radius: 8px; overflow-x: auto; font-size: 13px;"">
${{JSON.stringify(data, null, 2)}}
                </pre>
            `;
        }}

        function startAutoRefresh() {{
            if ({{{{AUTO_REFRESH}}}}) {{
                autoRefreshInterval = setInterval(fetchData, {{{{REFRESH_INTERVAL}}}});
            }}
        }}

        window.addEventListener('load', () => {{
            fetchData();
            startAutoRefresh();
        }});
    </script>
</body>
</html>";
    }
}