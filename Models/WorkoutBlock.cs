using System.Text.Json.Serialization;

namespace VodjeniTrening.Models;

public sealed class WorkoutBlock
{
    [JsonPropertyName("naziv")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("brojKrugova")]
    public int RoundCount { get; init; }

    [JsonPropertyName("pauzaIzmedjuKrugovaSekundi")]
    public int RestBetweenRoundsSeconds { get; init; }

    [JsonPropertyName("pauzaNakonBlokaSekundi")]
    public int RestAfterBlockSeconds { get; init; }

    [JsonPropertyName("vezbe")]
    public List<Exercise> Exercises { get; init; } = [];
}
