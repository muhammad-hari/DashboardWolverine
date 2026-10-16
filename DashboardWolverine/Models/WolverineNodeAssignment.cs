using System.Text.Json.Serialization;

namespace DashboardWolverine.Models;

public class WolverineNodeAssignment
{
    [JsonPropertyName("Id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("NodeId")] public Guid? NodeId { get; set; }
    [JsonPropertyName("Started")] public DateTime Started { get; set; }
}
