using System.Globalization;
using System.Text.Json.Serialization;

namespace VodjeniTrening.Models;

public sealed class WorkoutHistoryItem
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("trainingDate")]
    public string TrainingDate { get; init; } = string.Empty;

    [JsonPropertyName("trainingName")]
    public string TrainingName { get; init; } = string.Empty;

    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; init; }

    [JsonPropertyName("comment")]
    public string Comment { get; init; } = string.Empty;

    public string DateText => DateOnly.TryParseExact(
        TrainingDate,
        "yyyy-MM-dd",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out var date)
            ? date.ToString("dd.MM.yyyy.", CultureInfo.CurrentCulture)
            : TrainingDate;

    public string DurationText => DurationSeconds <= 0
        ? "Trajanje: —"
        : $"Trajanje: {FormatDuration(DurationSeconds)}";

    public string CommentText => string.IsNullOrWhiteSpace(Comment)
        ? "Bez komentara"
        : Comment;

    private static string FormatDuration(int totalSeconds)
    {
        var duration = TimeSpan.FromSeconds(totalSeconds);
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes:00}:{duration.Seconds:00}";
    }
}
