using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VodjeniTrening.Models;

namespace VodjeniTrening.Services;

public interface IWorkoutService
{
    Task<Workout> LoadAsync(CancellationToken cancellationToken = default);
}

public sealed class WorkoutDataException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class WorkoutService(HttpClient httpClient) : IWorkoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Workout> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var loginResponse = await httpClient.PostAsJsonAsync(
                "login",
                new LoginRequest(ApiSettings.UserName, ApiSettings.UserPassword),
                cancellationToken);

            if (loginResponse.StatusCode == HttpStatusCode.Unauthorized)
                throw new WorkoutDataException("Prijava na server nije uspela. Proverite korisničko ime i lozinku.");

            loginResponse.EnsureSuccessStatusCode();
            var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(
                JsonOptions, cancellationToken)
                ?? throw new WorkoutDataException("Server nije vratio podatke nakon prijave.");

            using var trainingRequest = new HttpRequestMessage(HttpMethod.Get, "GetTraining");
            trainingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

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

    private sealed record LoginRequest(string UserName, string UserPassword);
    private sealed record LoginResponse(string Token, DateTimeOffset ExpiresAtUtc);
}

public static class ApiSettings
{
    public const string BaseUrl = "https://api.darkojakas.de/";
    public const string UserName = "vodjenitrening";
    public const string UserPassword = "PromeniMe-8081!";
}
