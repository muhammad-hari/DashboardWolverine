using DashboardWolverine.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardWolverine;

/// <summary>
/// Extension methods to set up the Monitoring Dashboard.
/// </summary>
public static class MonitoringDashboardExtensions
{
    /// <summary>
    /// Adds the dashboard with programmatic options.
    /// </summary>
    public static IServiceCollection AddMonitoringDashboard(
        this IServiceCollection services,
        Action<MonitoringDashboardOptions>? configure = null)
    {
        var options = new MonitoringDashboardOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        RegisterRepository(services, options);

        return services;
    }

    /// <summary>
    /// Adds the dashboard from the WolverineDashboard configuration section.
    /// ConnectionStrings:WolverineDb is used when no explicit connection string
    /// is present in that section. Explicit options take precedence.
    /// </summary>
    public static IServiceCollection AddMonitoringDashboard(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<MonitoringDashboardOptions>? configure = null)
    {
        var options = new MonitoringDashboardOptions();
        configuration.GetSection("WolverineDashboard").Bind(options);

        if (string.IsNullOrWhiteSpace(options.WolverineConnectionString))
        {
            options.WolverineConnectionString = configuration.GetConnectionString("WolverineDb");
        }

        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        RegisterRepository(services, options);

        return services;
    }

    /// <summary>
    /// Adds the dashboard middleware to the ASP.NET Core pipeline.
    /// </summary>
    public static IApplicationBuilder UseMonitoringDashboard(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<MonitoringDashboardOptions>();
        app.UseMiddleware<MonitoringDashboardMiddleware>(options);

        return app;
    }

    private static void RegisterRepository(
        IServiceCollection services,
        MonitoringDashboardOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.WolverineConnectionString))
        {
            services.AddControllers()
                .AddApplicationPart(typeof(Controllers.WolverineController).Assembly);

            services.AddSingleton(new WolverineRepository(
                options.WolverineConnectionString,
                options.Schema));
        }
    }
}
