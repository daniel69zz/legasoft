using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Application.Sales;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>SCRUM-27 · HU-17 (RF-063 a RF-067, RF-101): factura simulada.</summary>
public static partial class Sprint3Endpoints
{
    private static void MapInvoices(RouteGroupBuilder api)
    {
        api.MapPost("/sales/{saleId:guid}/invoice", async (Guid saleId, InvoiceRequest request, BusinessDb db,
                HttpContext http, TimeProvider time) =>
            {
                var sale = await db.Sales.Include(s => s.Lines).SingleOrDefaultAsync(s => s.Id == saleId);
                if (sale is null)
                    return Results.Problem("La venta todavía no llegó al servidor: sincronice el puesto e intente de nuevo.",
                        statusCode: StatusCodes.Status404NotFound);
                if (sale.Status == "Anulada")
                    throw new DomainException($"La venta {sale.ReceiptNumber} está anulada: no se puede facturar (RF-063).");
                if (await db.Invoices.SingleOrDefaultAsync(i => i.SaleId == saleId) is { } issued)
                    return Results.Ok(await ToInvoiceAsync(db, issued, sale));
                if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.CustomerTaxId))
                    throw new DomainException("Indique el nombre y el NIT o CI del cliente para la factura.");

                // RF-101: numeración correlativa; el índice único impide que dos documentos compartan número.
                var invoice = new InvoiceRecord
                {
                    Id = Guid.NewGuid(),
                    Number = (await db.Invoices.MaxAsync(i => (int?)i.Number) ?? 0) + 1,
                    SaleId = sale.Id, ReceiptNumber = sale.ReceiptNumber, IssuedAt = time.GetUtcNow(),
                    IssuedBy = UserName(http), CustomerName = request.CustomerName.Trim(),
                    CustomerTaxId = request.CustomerTaxId.Trim(), Subtotal = sale.Subtotal, Discount = sale.Discount,
                    Total = sale.Total,
                };
                db.Invoices.Add(invoice);
                Audit(db, http, time, "Factura simulada",
                    $"N.º {invoice.Number} · {sale.ReceiptNumber} · {invoice.CustomerName} · Bs {invoice.Total:0.00}");
                await db.SaveChangesAsync();
                return Results.Created($"/api/invoices/{invoice.Number}", await ToInvoiceAsync(db, invoice, sale));
            })
            .RequirePermission(Permission.Ventas)
            .WithTags("Ventas")
            .WithSummary("[SCRUM-27 · HU-17] Emite la factura simulada de una venta sincronizada; rechaza ventas anuladas (RF-063 a RF-067, RF-101)")
            .Produces<InvoiceDto>(StatusCodes.Status201Created).Produces<InvoiceDto>()
            .ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapGet("/invoices/{number:int}/pdf", async (int number, BusinessDb db) =>
            {
                var invoice = await db.Invoices.SingleOrDefaultAsync(i => i.Number == number);
                if (invoice is null) return Results.NotFound();
                var sale = await db.Sales.Include(s => s.Lines).SingleAsync(s => s.Id == invoice.SaleId);
                return Results.File(InvoicePdf.Build(await ToInvoiceAsync(db, invoice, sale)), SimplePdf.ContentType,
                    $"factura-{number:D6}.pdf");
            })
            .RequirePermission(Permission.Ventas)
            .WithTags("Ventas")
            .WithSummary("[SCRUM-27 · HU-17] Factura simulada en PDF (RF-065)")
            .Produces(StatusCodes.Status200OK, typeof(byte[]), SimplePdf.ContentType);
    }

    private static async Task<InvoiceDto> ToInvoiceAsync(BusinessDb db, InvoiceRecord i, SaleRecord sale) =>
        new(i.Number, i.SaleId, i.ReceiptNumber, i.IssuedAt, i.IssuedBy, i.CustomerName, i.CustomerTaxId,
            sale.Lines.Select(l => new InvoiceLineDto(l.ProductName, l.Quantity, l.UnitPrice, l.UnitPrice * l.Quantity))
                .ToList(), i.Subtotal, i.Discount, i.Total, await CompanyProfile.GetAsync(db));
}

/// <summary>SCRUM-27 · HU-17 (RF-065): factura simulada en PDF con la leyenda de que no tiene validez fiscal.</summary>
public static class InvoicePdf
{
    public static byte[] Build(InvoiceDto i)
    {
        var pdf = new SimplePdf();
        pdf.Text(i.Company.CompanyName, 16, bold: true);
        if (i.Company.CompanyTaxId.Length > 0) pdf.Text($"NIT {i.Company.CompanyTaxId}", 10);
        pdf.Text($"{i.Company.CompanyAddress} · Sucursal {i.Company.BranchName}", 10)
            .Space()
            .Text($"FACTURA SIMULADA N.º {i.Number:D6}", 14, bold: true)
            .Text(InvoiceDto.Legend, 10, bold: true)
            .Space(4)
            .Text($"Fecha: {i.IssuedAt.ToOffset(BusinessEndpoints.LaPaz):dd/MM/yyyy HH:mm}   Venta: {i.ReceiptNumber}   Emitida por: {i.IssuedBy}", 10)
            .Text($"Cliente: {i.CustomerName}   NIT/CI: {i.CustomerTaxId}", 10)
            .Rule();
        pdf.Text($"{"Cant.",5}  {"Producto",-50} {"P. unit.",10} {"Subtotal",11}", 9, mono: true);
        foreach (var l in i.Lines)
            pdf.Text($"{l.Quantity,5}  {Fit(l.ProductName, 50),-50} {l.UnitPrice,10:N2} {l.Subtotal,11:N2}", 9, mono: true);
        pdf.Rule();
        if (i.Discount > 0)
        {
            pdf.Text($"{"Subtotal",-67} {i.Subtotal,11:N2}", 9, mono: true);
            pdf.Text($"{"Descuento",-67} {i.Discount,11:N2}", 9, mono: true);
        }
        pdf.Text($"{"TOTAL Bs",-67} {i.Total,11:N2}", 10, mono: true).Space(12)
            .Text("Comprobante interno de la farmacia. No se envía a Impuestos Nacionales.", 8);
        return pdf.Build();
    }

    private static string Fit(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
