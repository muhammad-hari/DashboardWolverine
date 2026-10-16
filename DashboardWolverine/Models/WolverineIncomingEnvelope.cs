using System.Text.Json.Serialization;

namespace DashboardWolverine.Models;

public class WolverineIncomingEnvelope
{
    [JsonPropertyName("Id")] public Guid Id { get; set; }
    [JsonPropertyName("Status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("OwnerId")] public int OwnerId { get; set; }
    [JsonPropertyName("ExecutionTime")] public DateTime? ExecutionTime { get; set; }
    [JsonPropertyName("Attempts")] public int Attempts { get; set; }
    [JsonPropertyName("Body")] public byte[] Body { get; set; } = Array.Empty<byte>();
    [JsonPropertyName("MessageType")] public string MessageType { get; set; } = string.Empty;
    [JsonPropertyName("ReceivedAt")] public string ReceivedAt { get; set; } = string.Empty;
    [JsonPropertyName("KeepUntil")] public DateTime? KeepUntil { get; set; }
}
