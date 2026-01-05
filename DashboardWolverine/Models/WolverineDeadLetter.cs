namespace DashboardWolverine.Models;

public class WolverineDeadLetter
{
    public Guid Id { get; set; }
    public DateTime? ExecutionTime { get; set; }
    public byte[] Body { get; set; } = Array.Empty<byte>();
    public string MessageType { get; set; } = string.Empty;
    public string ReceivedAt { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public bool? Replayable { get; set; }
    
    /// <summary>
    /// JSON body extracted from byte array using PostgreSQL encode/substring
    /// </summary>
    public string? JsonBody { get; set; }
}
