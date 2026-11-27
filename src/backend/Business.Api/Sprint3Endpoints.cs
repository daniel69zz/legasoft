using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Application.Sales;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>
/// Endpoints del Sprint 3, en un archivo por historia: reportes y recibos (Sprint3Endpoints.Reports.cs),
/// empresa (…Company.cs), sucursales (…Branches.cs), clientes (…Customers.cs) y factura simulada
/// (…Invoices.cs).
/// </summary>
public static partial class Sprint3Endpoints
{
    public static void MapSprint3Endpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");
        MapReports(api);
        MapCompany(api);
        MapBranches(api);
        MapCustomers(api);
        MapInvoices(api);
    }

    internal static string UserName(HttpContext http) => http.User.FindFirst("name")?.Value ?? "?";

    internal static void Audit(BusinessDb db, HttpContext http, TimeProvider time, string action, string detail) =>
        db.Audit.Add(new AuditEntry
        {
            Id = Guid.NewGuid(), OccurredAt = time.GetUtcNow(), UserName = UserName(http),
            DeviceId = http.User.FindFirst("device")?.Value ?? "", Action = action, Detail = detail,
        });
}
