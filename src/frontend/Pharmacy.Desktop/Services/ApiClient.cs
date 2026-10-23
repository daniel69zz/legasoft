using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Pharmacy.Application.Contracts;

namespace Pharmacy.Desktop.Services;

public sealed class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

/// <summary>Cliente HTTPS/REST hacia el API Gateway (Identity, Sync y Business).</summary>
public sealed partial class ApiClient(IHttpClientFactory factory, Session session)
{
    public Task<LoginResponse> LoginAsync(LoginRequest request) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "/auth/login", request, authenticated: false);

    public Task<PushResponse> PushAsync(PushRequest request, CancellationToken ct) =>
        SendAsync<PushResponse>(HttpMethod.Post, "/sync/push", request, ct: ct);

    public Task<PullResponse> PullAsync(long since, CancellationToken ct) =>
        SendAsync<PullResponse>(HttpMethod.Get, $"/sync/pull?since={since}", ct: ct);

    public Task<DashboardSummary> GetDashboardAsync() => SendAsync<DashboardSummary>(HttpMethod.Get, "/api/dashboard");

    public Task<List<AuditEntryDto>> GetAuditAsync() => SendAsync<List<AuditEntryDto>>(HttpMethod.Get, "/api/audit");

    public Task<List<UserDto>> GetUsersAsync() => SendAsync<List<UserDto>>(HttpMethod.Get, "/api/users");

    public Task<List<AuditEntryDto>> GetUserAuditAsync() =>
        SendAsync<List<AuditEntryDto>>(HttpMethod.Get, "/api/users/audit");

    public Task<UserDto> CreateUserAsync(CreateUserRequest request) =>
        SendAsync<UserDto>(HttpMethod.Post, "/api/users", request);

    public Task UpdateUserAsync(Guid id, UpdateUserRequest request) =>
        SendAsync<object>(HttpMethod.Put, $"/api/users/{id}", request);

    public Task SetUserActiveAsync(Guid id, bool active) =>
        SendAsync<object>(HttpMethod.Post, $"/api/users/{id}/{(active ? "activate" : "deactivate")}");

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null, bool authenticated = true,
        CancellationToken ct = default)
    {
        using var client = factory.CreateClient("gateway");
        using var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType());
        if (authenticated)
        {
            if (session.Token is null) throw new ApiException(401, "La sesión no tiene conexión con el servidor.");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        }

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new ApiException((int)response.StatusCode, await ReadProblemAsync(response, ct));
        if (response.Content.Headers.ContentLength == 0 || typeof(T) == typeof(object)) return default!;
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    private static async Task<string> ReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            foreach (var field in new[] { "detail", "title" })
                if (doc.RootElement.TryGetProperty(field, out var value) && value.GetString() is { Length: > 0 } text)
                    return text;
        }
        catch (JsonException) { }
        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Forbidden => "No tiene permiso para esta operación.",
            System.Net.HttpStatusCode.Unauthorized => "La sesión expiró. Vuelva a iniciar sesión.",
            _ => $"El servidor respondió {(int)response.StatusCode}.",
        };
    }
}
