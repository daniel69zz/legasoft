using System.Net;
using System.Net.Http.Json;
using Pharmacy.Application.Contracts;
using Pharmacy.ServiceDefaults;

namespace Sync.Api;

public sealed record DispatchResult(bool Success, bool Permanent = false, string? Error = null);

/// <summary>Despacha cada evento al servicio de dominio según su módulo (REST interno).</summary>
public class EventDispatcher(HttpClient http, IConfiguration configuration)
{
    public virtual async Task<DispatchResult> DispatchAsync(SyncEnvelope message, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/internal/events")
            {
                Content = JsonContent.Create(message),
            };
            request.Headers.Add(ServiceDefaults.InternalKeyHeader, configuration["Internal:Key"]);
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return new(true);

            var detail = await response.Content.ReadAsStringAsync(ct);
            // 4xx: el evento no es válido y reenviarlo no lo arregla. 5xx: se reintenta.
            var permanent = (int)response.StatusCode is >= 400 and < 500 && response.StatusCode != HttpStatusCode.TooManyRequests;
            return new(false, permanent, $"{(int)response.StatusCode}: {detail}");
        }
        catch (HttpRequestException e)
        {
            return new(false, false, $"Business Service no disponible: {e.Message}");
        }
    }

    public virtual async Task<PullResponse> GetCatalogChangesAsync(long since, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/catalog?since={since}");
        request.Headers.Add(ServiceDefaults.InternalKeyHeader, configuration["Internal:Key"]);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PullResponse>(ct))!;
    }
}
