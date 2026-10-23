using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pharmacy.ServiceDefaults;

namespace Pharmacy.IntegrationTests;

/// <summary>Levanta un servicio real en memoria, con su DbContext sobre EF Core InMemory.</summary>
public class ServiceFactory<TEntry, TContext>(Action<IServiceCollection>? configure = null)
    : WebApplicationFactory<TEntry> where TEntry : class where TContext : DbContext
{
    private readonly string _database = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<TContext>>();
            services.AddDbContext<TContext>(o => o.UseInMemoryDatabase(_database));
            configure?.Invoke(services);
        });
    }

    public HttpClient ClientFor(string role, string userName = "tester", string device = "PUESTO-01")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(role, userName, device));
        return client;
    }

    /// <summary>Token firmado con la misma clave de desarrollo que validan los servicios.</summary>
    public string Token(string role, string userName, string device)
    {
        var jwt = Services.GetRequiredService<JwtOptions>();
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience,
            [new Claim("name", userName), new Claim("role", role), new Claim("device", device)],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList()) services.Remove(d);
    }
}
