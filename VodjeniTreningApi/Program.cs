using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AuthenticationOptions>(
    builder.Configuration.GetSection(AuthenticationOptions.SectionName));
builder.Services.AddSingleton<SessionTokenStore>();

var app = builder.Build();

app.MapPost("/login", (
    LoginRequest request,
    IOptions<AuthenticationOptions> authentication,
    SessionTokenStore tokenStore) =>
{
    var configuredUser = authentication.Value.UserName;
    var configuredPassword = authentication.Value.UserPassword;

    if (!SecureEquals(request.UserName, configuredUser) ||
        !SecureEquals(request.UserPassword, configuredPassword))
    {
        return Results.Unauthorized();
    }

    var session = tokenStore.Create();
    return Results.Ok(new LoginResponse(session.Token, session.ExpiresAtUtc));
});

app.MapGet("/GetTraining", (
    HttpRequest request,
    IWebHostEnvironment environment,
    SessionTokenStore tokenStore) =>
{
    if (!TryReadBearerToken(request, out var token) || !tokenStore.IsValid(token))
        return Results.Unauthorized();

    var filePath = Path.Combine(environment.ContentRootPath, "Data", "vezbe.json");
    return File.Exists(filePath)
        ? Results.File(filePath, "application/json; charset=utf-8")
        : Results.Problem("Fajl Data/vezbe.json nije pronađen na serveru.", statusCode: 500);
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run("http://0.0.0.0:8081");

static bool TryReadBearerToken(HttpRequest request, out string token)
{
    const string prefix = "Bearer ";
    var authorization = request.Headers.Authorization.ToString();
    if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        token = string.Empty;
        return false;
    }

    token = authorization[prefix.Length..].Trim();
    return token.Length > 0;
}

static bool SecureEquals(string? supplied, string configured)
{
    var suppliedBytes = Encoding.UTF8.GetBytes(supplied ?? string.Empty);
    var configuredBytes = Encoding.UTF8.GetBytes(configured);
    return suppliedBytes.Length == configuredBytes.Length &&
           CryptographicOperations.FixedTimeEquals(suppliedBytes, configuredBytes);
}

public sealed record LoginRequest(string UserName, string UserPassword);
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAtUtc);

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string UserName { get; init; } = string.Empty;
    public string UserPassword { get; init; } = string.Empty;
}

public sealed class SessionTokenStore
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _tokens = new();

    public (string Token, DateTimeOffset ExpiresAtUtc) Create()
    {
        RemoveExpired();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(TokenLifetime);
        _tokens[token] = expiresAtUtc;
        return (token, expiresAtUtc);
    }

    public bool IsValid(string token)
    {
        if (!_tokens.TryGetValue(token, out var expiresAtUtc))
            return false;

        if (expiresAtUtc > DateTimeOffset.UtcNow)
            return true;

        _tokens.TryRemove(token, out _);
        return false;
    }

    private void RemoveExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (token, expiresAtUtc) in _tokens)
        {
            if (expiresAtUtc <= now)
                _tokens.TryRemove(token, out _);
        }
    }
}
