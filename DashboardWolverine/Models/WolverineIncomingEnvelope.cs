namespace DashboardWolverine.Models;

public class WolverineIncomingEnvelope
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public DateTime? ExecutionTime { get; set; }
    public int Attempts { get; set; }
    public byte[] Body { get; set; } = Array.Empty<byte>();
    public string MessageType { get; set; } = string.Empty;
    public string ReceivedAt { get; set; } = string.Empty;
    public DateTime? KeepUntil { get; set; }
}
