namespace DashboardWolverine.Models;

public class IncomingEnvelopeResult
{
    public List<WolverineIncomingEnvelope> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public List<string> AvailableMessageTypes { get; set; } = new();
    public List<string> AvailableStatuses { get; set; } = new();
}
