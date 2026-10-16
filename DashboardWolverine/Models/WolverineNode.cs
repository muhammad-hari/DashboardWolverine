using System.Text.Json.Serialization;

namespace DashboardWolverine.Models;

public class WolverineNode
{
    [JsonPropertyName("Id")] public Guid Id { get; set; }
    [JsonPropertyName("NodeNumber")] public int NodeNumber { get; set; }
    [JsonPropertyName("Description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("Uri")] public string Uri { get; set; } = string.Empty;
    [JsonPropertyName("Started")] public DateTime Started { get; set; }
    [JsonPropertyName("HealthCheck")] public DateTime HealthCheck { get; set; }
    [JsonPropertyName("Capabilities")] public string[]? Capabilities { get; set; }
}
