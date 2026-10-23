using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Data;

namespace Pharmacy.Desktop.Services;

/// <summary>Inicio de sesión en línea contra el Identity Service, con respaldo sin conexión.</summary>
public sealed class AuthService(ApiClient api, Session session, SyncStatus status, IServiceScopeFactory scopes)
{
    /// <summary>Devuelve null si el inicio de sesión fue correcto, o el mensaje de error.</summary>
    public async Task<string?> LoginAsync(string userName, string password)
    {
        userName = userName.Trim().ToLowerInvariant();
        try
        {
            var response = await api.LoginAsync(new LoginRequest(userName, password, session.DeviceId));
            session.SignIn(response.UserName, response.FullName, response.Role, response.Token, password);
            status.Update(online: true);
            await CacheCredentialAsync(response, password);
            return null;
        }
        catch (ApiException e)
        {
            return e.Message;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            status.Update(online: false);
            return await OfflineLoginAsync(userName, password);
        }
    }

    /// <summary>Al volver la conexión obtiene un token para la sesión iniciada sin conexión.</summary>
    public async Task<bool> TryReconnectAsync()
    {
        if (session.Password is null) return false;
        var response = await api.LoginAsync(new LoginRequest(session.UserName, session.Password, session.DeviceId));
        session.SetToken(response.Token);
        return true;
    }

    private async Task<string?> OfflineLoginAsync(string userName, string password)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalDb>();
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserName == userName);
        if (user is null || new PasswordHasher<LocalUser>().VerifyHashedPassword(user, user.PasswordHash, password)
            == PasswordVerificationResult.Failed)
            return "Sin conexión con el servidor. Solo pueden entrar usuarios que ya iniciaron sesión en este puesto.";

        session.SignIn(user.UserName, user.FullName, user.Role, token: null, password);
        return null;
    }

    private async Task CacheCredentialAsync(LoginResponse response, string password)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalDb>();
        var user = await db.Users.FindAsync(response.UserName);
        if (user is null) db.Users.Add(user = new LocalUser { UserName = response.UserName });
        user.FullName = response.FullName;
        user.Role = response.Role;
        user.PasswordHash = new PasswordHasher<LocalUser>().HashPassword(user, password);
        user.CachedAt = DateTimeOffset.Now;
        await db.SaveChangesAsync();
    }
}
