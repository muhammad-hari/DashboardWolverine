using DashboardWolverine;
using Microsoft.Extensions.Options;
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


        builder.Services.AddMonitoringDashboard(options =>
        {
            options.RoutePrefix = "monitoring"; // Set the route prefix for the dashboard
            options.DashboardTitle = "Test Application - Wolverine Dashboard";
            options.WolverineConnectionString = "Host=172.17.200.9;Port=5432;Database=wv_db;Username=postgres;Password=postgres";
            options.Schema = "fbi_outbox";

            options.RoutePrefix = "/wolverine-ui";
            options.DashboardTitle = "Test Application - Wolverine Dashboard";
            options.EnableAutoRefresh = false;

            // Basic Authentication (optional - comment out to disable)
            options.Username = "admin";
            options.Password = "password123";
            options.AuthenticationRealm = "Wolverine Monitoring";

            options.AutoRefreshIntervalSeconds = 30;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMonitoringDashboard();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}