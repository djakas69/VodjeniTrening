using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace VodjeniTrening.Services;

public interface IAuthenticationService
{
    Task LoginAsync(string userName, string password, CancellationToken cancellationToken = default);
    Task RegisterAsync(string userName, string password, CancellationToken cancellationToken = default);
}

public interface IAuthenticationSession
{
    string? Token { get; }
}

public sealed class AuthenticationException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class AuthenticationService(HttpClient httpClient)
    : IAuthenticationService, IAuthenticationSession
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string? Token { get; private set; }

    public async Task LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "login", new CredentialsRequest(userName.Trim(), password), cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new AuthenticationException("Korisničko ime ili lozinka nisu ispravni.");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new AuthenticationException("Previše pokušaja. Sačekajte minut i pokušajte ponovo.");

            response.EnsureSuccessStatusCode();
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>(
                JsonOptions, cancellationToken)
                ?? throw new AuthenticationException("Server nije vratio podatke za prijavu.");

            Token = login.Token;
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AuthenticationException("Server nije odgovorio na vreme.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AuthenticationException(
                "Nije moguće povezivanje sa serverom. Proverite internet konekciju.", exception);
        }
        catch (Exception exception)
        {
            throw new AuthenticationException("Prijava trenutno nije moguća.", exception);
        }
    }

    public async Task RegisterAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "register", new CredentialsRequest(userName.Trim(), password), cancellationToken);

            if (response.StatusCode == HttpStatusCode.Conflict)
                throw new AuthenticationException("Korisničko ime je već zauzeto.");
            if (response.StatusCode == HttpStatusCode.BadRequest)
                throw new AuthenticationException(await ReadServerMessageAsync(response, cancellationToken));
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new AuthenticationException("Previše pokušaja. Sačekajte minut i pokušajte ponovo.");

            response.EnsureSuccessStatusCode();
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AuthenticationException("Server nije odgovorio na vreme.", exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AuthenticationException(
                "Nije moguće povezivanje sa serverom. Proverite internet konekciju.", exception);
        }
        catch (Exception exception)
        {
            throw new AuthenticationException("Kreiranje naloga trenutno nije moguće.", exception);
        }
    }

    private static async Task<string> ReadServerMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(
                JsonOptions, cancellationToken);
            return problem?.Detail ?? "Proverite unete podatke.";
        }
        catch
        {
            return "Proverite unete podatke.";
        }
    }

    private sealed record CredentialsRequest(string UserName, string UserPassword);
    private sealed record LoginResponse(string Token, DateTimeOffset ExpiresAtUtc);
    private sealed record ProblemResponse(string? Detail);
}

