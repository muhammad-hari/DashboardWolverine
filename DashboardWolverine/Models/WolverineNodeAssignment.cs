namespace DashboardWolverine.Models;

public class WolverineNodeAssignment
{
    public string Id { get; set; } = string.Empty;
    public Guid? NodeId { get; set; }
    public DateTime Started { get; set; }
}
