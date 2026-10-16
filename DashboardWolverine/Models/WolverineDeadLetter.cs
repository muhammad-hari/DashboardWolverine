using System.Text.Json.Serialization;

namespace DashboardWolverine.Models;

public class WolverineDeadLetter
{
    [JsonPropertyName("Id")] public Guid Id { get; set; }
    [JsonPropertyName("ExecutionTime")] public DateTime? ExecutionTime { get; set; }
    [JsonPropertyName("Body")] public byte[] Body { get; set; } = Array.Empty<byte>();
    [JsonPropertyName("MessageType")] public string MessageType { get; set; } = string.Empty;
    [JsonPropertyName("ReceivedAt")] public string ReceivedAt { get; set; } = string.Empty;
    [JsonPropertyName("Source")] public string? Source { get; set; }
    [JsonPropertyName("ExceptionType")] public string? ExceptionType { get; set; }
    [JsonPropertyName("ExceptionMessage")] public string? ExceptionMessage { get; set; }
    [JsonPropertyName("SentAt")] public DateTime? SentAt { get; set; }
    [JsonPropertyName("Replayable")] public bool? Replayable { get; set; }
    [JsonPropertyName("JsonBody")] public string? JsonBody { get; set; }
}
