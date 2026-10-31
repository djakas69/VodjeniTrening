using System.Text.Json.Serialization;

namespace VodjeniTrening.Models;

public sealed class WorkoutDocument
{
    [JsonPropertyName("trening")]
    public Workout? Workout { get; init; }
}

public sealed class Workout
{
    [JsonPropertyName("naziv")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("blokovi")]
    public List<WorkoutBlock> Blocks { get; init; } = [];
}
