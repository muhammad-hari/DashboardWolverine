using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DashboardWolverine.Repositories;

namespace DashboardWolverine;

/// <summary>
/// Extension methods to set up the Monitoring Dashboard
/// </summary>
public static class MonitoringDashboardExtensions
{
    /// <summary>
    /// Adds the Monitoring Dashboard middleware to the ASP.NET Core application.
    /// The dashboard will be accessible at the configured route (default: /monitoring).
    /// </summary>
    public static IApplicationBuilder UseMonitoringDashboard(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<MonitoringDashboardOptions>();
        app.UseMiddleware<MonitoringDashboardMiddleware>(options);

        return app;
    }

    /// <summary>
    /// Adds Monitoring Dashboard services to the dependency injection container.
    /// Optional - use this if you want to inject `MonitoringDashboardOptions` into other services.
    /// </summary>
    public static IServiceCollection AddMonitoringDashboard(
        this IServiceCollection services,
        Action<MonitoringDashboardOptions>? configure = null)
    {
        var options = new MonitoringDashboardOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);

        // Register WolverineRepository if connection string is provided
        if (!string.IsNullOrWhiteSpace(options.WolverineConnectionString))
        {
            services.AddSingleton(new WolverineRepository(options.WolverineConnectionString, options.Schema));
        }

        return services;
    }
}