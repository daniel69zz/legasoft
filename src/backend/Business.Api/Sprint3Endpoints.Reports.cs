using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Application.Sales;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>SCRUM-31 · HU-18 (reportes), SCRUM-24 · HU-14 (recibos del período) y SCRUM-32 · HU-19 (comparativo).</summary>
public static partial class Sprint3Endpoints
{
    private static void MapReports(RouteGroupBuilder api)
    {
        api.MapGet("/reports/operations/{kind}", async (string kind, DateOnly? from, DateOnly? to, string? branch,
                string? format, BusinessDb db, TimeProvider time) =>
            {
                var table = await OperationsReports.BuildAsync(db, kind, from, to, branch, time);
                var name = $"{kind}-{BusinessEndpoints.Today(time):yyyyMMdd}";
                return format switch
                {
                    "xlsx" => Results.File(XlsxWriter.Write([OperationsReports.ToSheet(table)]), XlsxWriter.ContentType,
                        $"{name}.xlsx"),
                    "pdf" => Results.File(ReportPdf.Build(table, await CompanyProfile.GetAsync(db)), SimplePdf.ContentType,
                        $"{name}.pdf"),
                    _ => Results.Ok(table),
                };
            })
            .RequirePermission(Permission.Reportes)
            .WithTags("Reportes")
            .WithSummary("[SCRUM-31 · HU-18] Reporte del período (ventas, compras, inventario, stock bajo, vencimientos o cierre diario) en JSON, Excel o PDF (RF-068 a RF-072, RF-103)")
            .Produces<ReportTableDto>().ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // SCRUM-24 · HU-14 (RF-051): recibos de un período en un solo PDF.
        api.MapGet("/reports/receipts", async (DateOnly? from, DateOnly? to, string? device, BusinessDb db,
                TimeProvider time) =>
            {
                var r = OperationsReports.Range(from, to, time);
                var sales = (await db.Sales.Include(s => s.Lines)
                        .Where(s => s.CreatedAt >= r.Start && s.CreatedAt < r.End).ToListAsync())
                    .Where(s => string.IsNullOrWhiteSpace(device) || s.DeviceId == device)
                    .OrderBy(s => s.CreatedAt).ToList();
                if (sales.Count == 0)
                    return Results.Problem($"No hay recibos entre el {r.From:dd/MM/yyyy} y el {r.To:dd/MM/yyyy}.",
                        statusCode: StatusCodes.Status404NotFound);
                var company = await CompanyProfile.GetAsync(db);
                var pdf = Receipts.Pdf(company, sales.Select(ToReceipt).ToList(),
                    $"Recibos del {r.From:dd/MM/yyyy} al {r.To:dd/MM/yyyy}");
                return Results.File(pdf, SimplePdf.ContentType, $"recibos-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}.pdf");
            })
            .RequireAnyPermission(Permission.Ventas, Permission.Reportes)
            .WithTags("Ventas")
            .WithSummary("[SCRUM-24 · HU-14] Recibos del período en un solo PDF; 404 si no hay ventas (RF-051)")
            .Produces(StatusCodes.Status200OK, typeof(byte[]), SimplePdf.ContentType)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // SCRUM-32 · HU-19 (RF-074): comparativo de ventas por cajero agrupado por día, semana o mes.
        api.MapGet("/reports/sales-by-cashier", async (DateOnly? from, DateOnly? to, string? groupBy, BusinessDb db,
                TimeProvider time) =>
            {
                var r = OperationsReports.Range(from, to, time);
                return await BusinessEndpoints.SalesByCashierAsync(db, r.From, r.To, groupBy);
            })
            .RequirePermission(Permission.Reportes)
            .WithTags("Reportes")
            .WithSummary("[SCRUM-32 · HU-19] Ventas por cajero agrupadas por día, semana o mes (RF-074)")
            .Produces<List<SalesByCashierRow>>();
    }

    internal static SaleReceipt ToReceipt(SaleRecord s) => new(s.Id, s.ReceiptNumber, s.CreatedAt, s.CashierName,
        s.Lines.Select(l => new SaleLinePayload(l.ProductId, l.ProductName, l.LotId, l.LotNumber, l.Quantity, l.UnitPrice))
            .ToList(), s.Subtotal, s.Discount, s.Total, s.CashReceived, s.Change, s.CustomerName, s.CustomerTaxId,
        s.PrescriptionNumber, s.Status);
}
