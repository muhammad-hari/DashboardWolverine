namespace DashboardWolverine.Models;

public class WolverineNode
{
    public Guid Id { get; set; }
    public int NodeNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public DateTime Started { get; set; }
    public DateTime HealthCheck { get; set; }
    public string[]? Capabilities { get; set; }
}
