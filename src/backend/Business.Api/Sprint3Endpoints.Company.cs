using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Application.Sales;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>SCRUM-33 · HU-28 (RF-097, RF-098): datos de la empresa y de la sucursal.</summary>
public static partial class Sprint3Endpoints
{
    private static void MapCompany(RouteGroupBuilder api)
    {
        api.MapGet("/settings/company", async (BusinessDb db) => await CompanyProfile.GetAsync(db))
            .RequireAuthorization()
            .WithTags("Configuración")
            .WithSummary("[SCRUM-33 · HU-28] Datos de la empresa y de la sucursal (RF-097, RF-098)");

        api.MapPut("/settings/company", async (CompanyProfileDto request, BusinessDb db, HttpContext http,
                TimeProvider time) =>
            {
                CompanyProfile.Validate(request);
                var userName = UserName(http);
                await CompanyProfile.SaveAsync(db, request, userName, time.GetUtcNow());
                Audit(db, http, time, "Cambio de datos de la empresa",
                    $"{request.CompanyName} · sucursal {request.BranchName}");
                await db.SaveChangesAsync();
                return Results.Ok(request);
            })
            .RequireAuthorization(p => p.RequireRole(Roles.Administrador))
            .WithTags("Configuración")
            .WithSummary("[SCRUM-33 · HU-28] Modifica los datos de la empresa y de la sucursal (RF-097, RF-098)")
            .Produces<CompanyProfileDto>().ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
