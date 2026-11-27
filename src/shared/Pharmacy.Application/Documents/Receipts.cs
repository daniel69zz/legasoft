using System.Text;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sales;

namespace Pharmacy.Application.Documents;

/// <summary>
/// Recibo interno sin validez fiscal (RF-049) con los datos de la empresa y la sucursal (SCRUM-33 · HU-28).
/// El mismo texto se muestra en pantalla, se descarga suelto (RF-050) y se consolida por período (RF-051).
/// </summary>
public static class Receipts
{
    public static string Text(CompanyProfileDto company, SaleReceipt r)
    {
        var sb = new StringBuilder();
        sb.AppendLine(company.CompanyName.ToUpperInvariant());
        if (company.CompanyTaxId.Length > 0) sb.AppendLine($"NIT {company.CompanyTaxId}");
        sb.AppendLine($"Sucursal {company.BranchName} · {company.BranchAddress}");
        if (company.CompanyPhone.Length > 0) sb.AppendLine($"Tel. {company.CompanyPhone}");
        sb.AppendLine("RECIBO INTERNO SIN VALOR FISCAL");
        sb.AppendLine($"N.º {r.ReceiptNumber}   {r.CreatedAt.LocalDateTime:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Cajero: {r.CashierName}");
        if (r.CustomerName.Length > 0 || r.CustomerTaxId.Length > 0)
            sb.AppendLine($"Cliente: {r.CustomerName} {(r.CustomerTaxId.Length > 0 ? $"· NIT/CI {r.CustomerTaxId}" : "")}".TrimEnd());
        if (r.PrescriptionNumber.Length > 0) sb.AppendLine($"Receta: {r.PrescriptionNumber}");
        if (r.Status == "Anulada") sb.AppendLine("*** VENTA ANULADA ***");
        sb.AppendLine(new string('-', 40));
        foreach (var l in r.Lines)
            sb.AppendLine($"{l.Quantity,3} x {l.ProductName}\n      lote {l.LotNumber}   Bs {l.UnitPrice * l.Quantity,8:N2}");
        sb.AppendLine(new string('-', 40));
        if (r.Discount > 0)
        {
            sb.AppendLine($"Subtotal         Bs {r.Subtotal,10:N2}");
            sb.AppendLine($"Descuento        Bs {r.Discount,10:N2}");
        }
        sb.AppendLine($"TOTAL            Bs {r.Total,10:N2}");
        sb.AppendLine($"Efectivo         Bs {r.CashReceived,10:N2}");
        sb.AppendLine($"Cambio           Bs {r.Change,10:N2}");
        return sb.ToString();
    }

    /// <summary>RF-050: un recibo en PDF.</summary>
    public static byte[] Pdf(CompanyProfileDto company, SaleReceipt receipt) => Pdf(company, [receipt], null);

    /// <summary>RF-051: los recibos de un período en un solo PDF, uno por página.</summary>
    public static byte[] Pdf(CompanyProfileDto company, IReadOnlyList<SaleReceipt> receipts, string? title)
    {
        var pdf = new SimplePdf();
        if (title is not null)
        {
            pdf.Text(title, 14, bold: true).Text($"{receipts.Count} recibos · {company.CompanyName}", 10).Space();
        }
        for (var i = 0; i < receipts.Count; i++)
        {
            if (i > 0) pdf.Rule().Space(4);
            pdf.Text(Text(company, receipts[i]), 9, mono: true);
        }
        return pdf.Build();
    }
}

/// <summary>SCRUM-31 · HU-18: reporte tabular en PDF (Courier, para que las columnas queden alineadas).</summary>
public static class ReportPdf
{
    public static byte[] Build(ReportTableDto table, CompanyProfileDto company)
    {
        var pdf = new SimplePdf(landscape: true);
        pdf.Text(table.Title, 14, bold: true)
            .Text($"{company.CompanyName} · Sucursal {company.BranchName} · {table.Subtitle}", 9)
            .Space(6);
        if (table.Notice.Length > 0) pdf.Text(table.Notice, 10, bold: true).Space(4);
        foreach (var line in Lines(table, pdf.MonoColumns(7.5))) pdf.Text(line, 7.5, mono: true);
        return pdf.Build();
    }

    /// <summary>Tabla en texto de ancho fijo; también la usa la vista previa del Desktop.</summary>
    public static IEnumerable<string> Lines(ReportTableDto table, int maxWidth = 160)
    {
        var rows = table.Rows.Append(table.Totals).Where(r => r.Count > 0).ToList();
        var widths = table.Columns.Select((c, i) =>
            Math.Min(32, Math.Max(c.Length, rows.Select(r => i < r.Count ? r[i].Length : 0).DefaultIfEmpty(0).Max())))
            .ToArray();
        string Format(IReadOnlyList<string> cells) => string.Join("  ", widths.Select((w, i) =>
        {
            var value = i < cells.Count ? cells[i] : "";
            if (value.Length > w) value = value[..(w - 1)] + "…";
            return LooksNumeric(value) ? value.PadLeft(w) : value.PadRight(w);
        })).TrimEnd();

        var header = Format(table.Columns);
        yield return Cut(header, maxWidth);
        yield return Cut(new string('-', header.Length), maxWidth);
        foreach (var row in table.Rows) yield return Cut(Format(row), maxWidth);
        if (table.Totals.Count > 0)
        {
            yield return Cut(new string('-', header.Length), maxWidth);
            yield return Cut(Format(table.Totals), maxWidth);
        }
    }

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..max];

    private static bool LooksNumeric(string s) =>
        s.Length > 0 && s.All(c => char.IsDigit(c) || c is '.' or ',' or '-' or '+' or ' ' or '%') && s.Any(char.IsDigit);
}
