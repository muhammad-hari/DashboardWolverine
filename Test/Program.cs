using DashboardWolverine;
using Test;
using Wolverine;
using Wolverine.Http;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Host.AddOutboxMessaging();


        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();


        builder.Services.AddMonitoringDashboard(x =>
        {
            x.RoutePrefix = "monitoring"; // Set the route prefix for the dashboard
            x.DashboardTitle = "Test Application - Wolverine Dashboard";
            x.WolverineConnectionString = "Host=localhost;Port=5432;Database=wv_db;Username=postgres;Password=postgres"; 
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // ===== SETUP MONITORING DASHBOARD (mirip Hangfire) =====
        app.UseMonitoringDashboard(options =>
        {
            options.RoutePrefix = "/wolverine-ui";
            options.DashboardTitle = "Test Application - Wolverine Dashboard";
            options.EnableAutoRefresh = false;

            // Basic Authentication (optional - comment out to disable)
            options.Username = "admin";
            options.Password = "password123";
            options.AuthenticationRealm = "Wolverine Monitoring";

            options.AutoRefreshIntervalSeconds = 30;
            options.WolverineConnectionString = builder.Configuration.GetConnectionString("WolverineDb"); // Ambil dari config
        });
        // =====================================================


        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}