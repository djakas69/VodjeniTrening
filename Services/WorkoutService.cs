using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VodjeniTrening.Models;

namespace VodjeniTrening.Services;

public interface IWorkoutService
{
    Task<Workout> LoadAsync(CancellationToken cancellationToken = default);
    Task SubmitFeedbackAsync(string comment, CancellationToken cancellationToken = default);
}

public sealed class WorkoutDataException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class WorkoutService(
    HttpClient httpClient,
    IAuthenticationSession authenticationSession) : IWorkoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Workout> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = authenticationSession.Token;
            if (string.IsNullOrWhiteSpace(token))
                throw new WorkoutDataException("Prvo se ulogujte da biste učitali trening.");

            var trainingDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
            using var trainingRequest = new HttpRequestMessage(
                HttpMethod.Get, $"GetTraining?date={trainingDate}");
            trainingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var trainingResponse = await httpClient.SendAsync(trainingRequest, cancellationToken);
            if (trainingResponse.StatusCode == HttpStatusCode.Unauthorized)
                throw new WorkoutDataException("Sesija je odbijena. Pokušajte ponovo.");

            trainingResponse.EnsureSuccessStatusCode();

            await using var stream = await trainingResponse.Content.ReadAsStreamAsync(cancellationToken);
            var document = await JsonSerializer.DeserializeAsync<WorkoutDocument>(
                stream, JsonOptions, cancellationToken);

            var workout = document?.Workout
                ?? throw new WorkoutDataException("Server nije vratio podatke o treningu.");

            Validate(workout);
            return workout;
        }
        catch (WorkoutDataException)
        {
            throw;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WorkoutDataException("Server za trening nije odgovorio na vreme.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new WorkoutDataException(
                "Nije moguće povezivanje sa serverom za trening. Proverite internet konekciju.", exception);
        }
        catch (JsonException exception)
        {
            throw new WorkoutDataException("Server je vratio neispravan JSON dokument.", exception);
        }
        catch (Exception exception)
        {
            throw new WorkoutDataException("Trening nije moguće učitati sa servera.", exception);
        }
    }

    public async Task SubmitFeedbackAsync(
        string comment,
        CancellationToken cancellationToken = default)
    {
        var token = authenticationSession.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new WorkoutDataException("Sesija je istekla. Ulogujte se ponovo.");

        try
        {
            var trainingDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
            using var request = new HttpRequestMessage(HttpMethod.Post, "feedback")
            {
                Content = JsonContent.Create(new FeedbackRequest(comment.Trim(), trainingDate))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new WorkoutDataException("Sesija je istekla. Ulogujte se ponovo.");
            if (response.StatusCode == HttpStatusCode.BadRequest)
                throw new WorkoutDataException("Komentar nije moguće sačuvati. Proverite njegovu dužinu.");

            response.EnsureSuccessStatusCode();
        }
        catch (WorkoutDataException)
        {
            throw;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WorkoutDataException("Server nije odgovorio na vreme.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new WorkoutDataException(
                "Komentar nije poslat. Proverite internet konekciju.", exception);
        }
        catch (Exception exception)
        {
            throw new WorkoutDataException("Komentar trenutno nije moguće poslati.", exception);
        }
    }

    private static void Validate(Workout workout)
    {
        if (string.IsNullOrWhiteSpace(workout.Name))
            throw new WorkoutDataException("Naziv treninga nije unet.");

        if (workout.Blocks.Count == 0)
            throw new WorkoutDataException("Trening mora da sadrži najmanje jedan blok.");

        for (var blockIndex = 0; blockIndex < workout.Blocks.Count; blockIndex++)
        {
            var block = workout.Blocks[blockIndex];
            var blockDescription = $"Blok {blockIndex + 1}";

            if (string.IsNullOrWhiteSpace(block.Name))
                throw new WorkoutDataException($"{blockDescription} nema naziv.");
            if (block.RoundCount < 1)
                throw new WorkoutDataException($"Blok „{block.Name}” mora da ima najmanje jedan krug.");
            if (block.RestBetweenRoundsSeconds < 0 || block.RestAfterBlockSeconds < 0)
                throw new WorkoutDataException($"Pauze u bloku „{block.Name}” ne mogu biti negativne.");
            if (block.Exercises.Count == 0)
                throw new WorkoutDataException($"Blok „{block.Name}” mora da sadrži najmanje jednu vežbu.");

            for (var exerciseIndex = 0; exerciseIndex < block.Exercises.Count; exerciseIndex++)
            {
                var exercise = block.Exercises[exerciseIndex];
                var exerciseDescription = $"Vežba {exerciseIndex + 1} u bloku „{block.Name}”";

                if (string.IsNullOrWhiteSpace(exercise.Name))
                    throw new WorkoutDataException($"{exerciseDescription} nema naziv.");
                if (exercise.RestAfterExerciseSeconds < 0)
                    throw new WorkoutDataException($"Pauza nakon vežbe „{exercise.Name}” ne može biti negativna.");

                if (exercise.Type.Equals("vreme", StringComparison.OrdinalIgnoreCase))
                {
                    if (exercise.DurationSeconds <= 0)
                        throw new WorkoutDataException($"Vežba „{exercise.Name}” mora da ima trajanje veće od nule.");
                }
                else if (exercise.Type.Equals("ponavljanja", StringComparison.OrdinalIgnoreCase))
                {
                    if (exercise.RepetitionCount <= 0)
                        throw new WorkoutDataException($"Vežba „{exercise.Name}” mora da ima broj ponavljanja veći od nule.");
                    if (exercise.RepetitionDurationSeconds <= 0)
                        throw new WorkoutDataException(
                            $"Vežba „{exercise.Name}” mora da ima trajanje jednog ponavljanja veće od nule.");
                }
                else
                {
                    throw new WorkoutDataException(
                        $"Vežba „{exercise.Name}” ima nepodržan tip „{exercise.Type}”. Koristite „vreme” ili „ponavljanja”.");
                }
            }
        }
    }

    private sealed record FeedbackRequest(string Comment, string TrainingDate);

}

public static class ApiSettings
{
    public const string BaseUrl = "https://api.darkojakas.de/";
}
