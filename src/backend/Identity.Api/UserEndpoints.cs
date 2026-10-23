using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;

namespace Identity.Api;

/// <summary>Gestión de usuarios, solo para el Administrador (RF-002 a RF-005) con auditoría (RF-075).</summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api").RequireAuthorization(p => p.RequireRole(Roles.Administrador));

        admin.MapGet("/users", async (IdentityDb db) =>
            await db.Users.OrderBy(u => u.UserName)
                .Select(u => new UserDto(u.Id, u.UserName, u.FullName, u.Role, u.IsActive)).ToListAsync());

        admin.MapPost("/users", async (CreateUserRequest request, IdentityDb db, HttpContext http, TimeProvider time) =>
        {
            var userName = request.UserName.Trim().ToLowerInvariant();
            var errors = new List<string>();
            if (userName.Length < 3) errors.Add("El usuario debe tener al menos 3 caracteres.");
            if (string.IsNullOrWhiteSpace(request.FullName)) errors.Add("El nombre es obligatorio.");
            if (request.Password.Length < 8) errors.Add("La contraseña inicial debe tener al menos 8 caracteres.");
            if (!Roles.IsValid(request.Role)) errors.Add("El rol no existe.");
            if (errors.Count > 0) return Results.Problem(string.Join(" ", errors), statusCode: 400);
            if (await db.Users.AnyAsync(u => u.UserName == userName))
                return Results.Problem("Ya existe un usuario con ese nombre.", statusCode: 409);

            var user = new User
            {
                Id = Guid.NewGuid(), UserName = userName, FullName = request.FullName.Trim(), Role = request.Role,
                CreatedAt = time.GetUtcNow(),
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
            db.Users.Add(user);
            Audit(db, http, time, "Alta de usuario", $"{user.UserName} con rol {user.Role}");
            await db.SaveChangesAsync();
            return Results.Created($"/api/users/{user.Id}",
                new UserDto(user.Id, user.UserName, user.FullName, user.Role, user.IsActive));
        });

        admin.MapPut("/users/{id:guid}", async (Guid id, UpdateUserRequest request, IdentityDb db, HttpContext http,
            TimeProvider time) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();
            if (!Roles.IsValid(request.Role)) return Results.Problem("El rol no existe.", statusCode: 400);
            if (string.IsNullOrWhiteSpace(request.FullName))
                return Results.Problem("El nombre es obligatorio.", statusCode: 400);
            Audit(db, http, time, "Modificación de usuario",
                $"{user.UserName}: rol {user.Role} → {request.Role}, nombre «{request.FullName}»");
            user.FullName = request.FullName.Trim();
            user.Role = request.Role;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        admin.MapPost("/users/{id:guid}/{action:regex(^(activate|deactivate)$)}", async (Guid id, string action,
            IdentityDb db, HttpContext http, TimeProvider time) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();
            if (user.UserName == http.User.FindFirst("name")?.Value)
                return Results.Problem("No puede desactivar su propio usuario.", statusCode: 400);
            user.IsActive = action == "activate";
            Audit(db, http, time, user.IsActive ? "Activación de usuario" : "Desactivación de usuario", user.UserName);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        admin.MapGet("/users/audit", async (IdentityDb db) =>
            await db.UserChanges.OrderByDescending(c => c.OccurredAt).Take(200)
                .Select(c => new AuditEntryDto(c.OccurredAt, c.ChangedBy, c.Action, c.Detail)).ToListAsync());

        admin.MapGet("/devices", async (IdentityDb db) => await db.Devices.OrderBy(d => d.DeviceId).ToListAsync());

        app.MapGet("/api/attendance", async (DateOnly? day, IdentityDb db, TimeProvider time) =>
            {
                var date = day ?? DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
                return await db.Attendance.Where(a => a.Day == date).OrderBy(a => a.FirstLoginAt).ToListAsync();
            })
            .RequireAuthorization(p => p.RequireRole(Roles.Administrador, Roles.GerenteGeneral));
    }

    private static void Audit(IdentityDb db, HttpContext http, TimeProvider time, string action, string detail) =>
        db.UserChanges.Add(new UserChange
        {
            Id = Guid.NewGuid(), OccurredAt = time.GetUtcNow(), ChangedBy = http.User.FindFirst("name")?.Value ?? "?",
            Action = action, Detail = detail,
        });
}
