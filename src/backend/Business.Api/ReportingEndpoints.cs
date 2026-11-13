using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;
using Pharmacy.Domain.Inventory;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>
/// Consultas y configuración incorporadas al Sprint 2: historial general de movimientos (HU-06.4),
/// auditoría con filtros (HU-20.1), exportación a Excel (HU-18.1) y parámetros operativos (HU-28.1).
/// </summary>
public static class ReportingEndpoints
{
    public const int MaxRows = 1000;

    public static void MapReportingEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/settings", async (BusinessDb db) => await OperationalSettings.GetAsync(db))
            .RequireAuthorization()
            .WithTags("Configuración")
            .WithSummary("Parámetros operativos vigentes (RF-099)");

        api.MapPut("/settings", async (OperationalSettingsDto request, BusinessDb db, HttpContext http,
                TimeProvider time) =>
            {
                OperationalSettings.Validate(request);
                var before = await OperationalSettings.GetAsync(db);
                var userName = http.User.FindFirst("name")?.Value ?? "?";
                await OperationalSettings.SaveAsync(db, request, userName, time.GetUtcNow());
                db.Audit.Add(new AuditEntry
                {
                    Id = Guid.NewGuid(), OccurredAt = time.GetUtcNow(), UserName = userName,
                    DeviceId = http.User.FindFirst("device")?.Value ?? "", Action = "Cambio de parámetros",
                    Detail = $"Umbral de vencimiento {before.ExpiryThresholdDays} → {request.ExpiryThresholdDays} días; " +
                             $"alertas cada {before.AlertIntervalMinutes} → {request.AlertIntervalMinutes} min",
                });
                await db.SaveChangesAsync();
                return Results.Ok(request);
            })
            .RequireAuthorization(p => p.RequireRole(Roles.Administrador))
            .WithTags("Configuración")
            .WithSummary("Modifica los parámetros operativos de inventario (RF-099)");
    }
}
