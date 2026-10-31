using System.Text.Json.Serialization;

namespace VodjeniTrening.Models;

public sealed class Exercise
{
    [JsonPropertyName("naziv")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("tip")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("brojPonavljanja")]
    public int RepetitionCount { get; init; }

    [JsonPropertyName("trajanjePonavljanjaSekundi")]
    public int RepetitionDurationSeconds { get; init; }

    [JsonPropertyName("trajanjeSekundi")]
    public int DurationSeconds { get; init; }

    [JsonPropertyName("pauzaNakonVezbeSekundi")]
    public int RestAfterExerciseSeconds { get; init; }

    [JsonIgnore]
    public bool IsTimed => Type.Equals("vreme", StringComparison.OrdinalIgnoreCase);
}
