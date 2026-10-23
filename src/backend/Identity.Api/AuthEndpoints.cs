using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;
using Pharmacy.ServiceDefaults;

namespace Identity.Api;

/// <summary>AuthController del Documento 3: inicio de sesión (RF-001, RF-006, RF-008, RNF-004).</summary>
public static class AuthEndpoints
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Mismo mensaje para usuario inexistente, contraseña errónea o usuario desactivado.
    private const string InvalidCredentials = "Usuario o contraseña incorrectos.";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", LoginAsync).AllowAnonymous();
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, IdentityDb db, JwtOptions jwt,
        TimeProvider time, ILogger<User> logger)
    {
        var now = time.GetUtcNow();
        var userName = request.UserName.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserName == userName);
        if (user is null) return Unauthorized();

        if (user.LockedUntil > now)
        {
            var minutes = (int)Math.Ceiling((user.LockedUntil.Value - now).TotalMinutes);
            return Results.Problem($"Cuenta bloqueada temporalmente por intentos fallidos. Intente en {minutes} min.",
                statusCode: StatusCodes.Status423Locked);
        }

        var hasher = new PasswordHasher<User>();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            RegisterFailure(user, now);
            await db.SaveChangesAsync();
            logger.LogWarning("Intento fallido de inicio de sesión para {UserName}", userName);
            return Unauthorized();
        }

        if (!user.IsActive) return Unauthorized();

        // RF-006: los cajeros solo inician sesión en computadoras autorizadas.
        var deviceAuthorized = await db.Devices.AnyAsync(d => d.DeviceId == request.DeviceId && d.IsActive);
        if (user.Role == Roles.Cajero && !deviceAuthorized)
            return Results.Problem("Este equipo no está autorizado para el inicio de sesión de cajeros.",
                statusCode: StatusCodes.Status403Forbidden);

        user.FailedAttempts = 0;
        user.FirstFailedAt = null;
        user.LockedUntil = null;

        // RF-008: la asistencia del cajero se registra con el primer inicio de sesión del día.
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        if (user.Role == Roles.Cajero && !await db.Attendance.AnyAsync(a =>
                a.UserName == user.UserName && a.Day == today && a.DeviceId == request.DeviceId))
        {
            db.Attendance.Add(new Attendance
            {
                Id = Guid.NewGuid(), UserName = user.UserName, DeviceId = request.DeviceId, Day = today,
                FirstLoginAt = now,
            });
        }
        await db.SaveChangesAsync();

        var expires = now.AddMinutes(jwt.ExpirationMinutes);
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("name", user.UserName),
                new Claim("full_name", user.FullName),
                new Claim("role", user.Role),
                new Claim("device", request.DeviceId),
            ],
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256));

        return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.UserName,
            user.FullName, user.Role));
    }

    /// <summary>RNF-004: 5 intentos fallidos en 15 minutos bloquean la cuenta durante 15 minutos.</summary>
    private static void RegisterFailure(User user, DateTimeOffset now)
    {
        if (user.FirstFailedAt is null || now - user.FirstFailedAt > FailureWindow)
        {
            user.FirstFailedAt = now;
            user.FailedAttempts = 0;
        }
        user.FailedAttempts++;
        if (user.FailedAttempts >= MaxFailedAttempts)
        {
            user.LockedUntil = now + LockoutDuration;
            user.FailedAttempts = 0;
            user.FirstFailedAt = null;
        }
    }

    private static IResult Unauthorized() =>
        Results.Problem(InvalidCredentials, statusCode: StatusCodes.Status401Unauthorized);
}
