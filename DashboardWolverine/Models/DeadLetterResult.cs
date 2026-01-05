namespace DashboardWolverine.Models;

public class DeadLetterResult
{
    public List<WolverineDeadLetter> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public List<string> AvailableMessageTypes { get; set; } = new();
    public List<string> AvailableExceptionTypes { get; set; } = new();
}
