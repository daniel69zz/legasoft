using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace Business.Api;

/// <summary>Hoja de cálculo: encabezados y filas con textos, números o fechas.</summary>
public sealed record XlsxSheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<object?>> Rows);

/// <summary>
/// SCRUM-129 · HU-18.1 (RF-105): genera un .xlsx (Office Open XML) mínimo sin dependencias externas: una hoja
/// por tabla, encabezados en negrita y números como números para que Excel los pueda sumar.
/// </summary>
public static class XlsxWriter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Write(IReadOnlyList<XlsxSheet> sheets)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "[Content_Types].xml", ContentTypes(sheets.Count));
            Add(zip, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Add(zip, "xl/workbook.xml", Workbook(sheets));
            Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Count));
            Add(zip, "xl/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><numFmts count="1"><numFmt numFmtId="164" formatCode="dd/mm/yyyy hh:mm"/></numFmts><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf/></cellStyleXfs><cellXfs count="4"><xf/><xf fontId="1" applyFont="1"/><xf numFmtId="4" applyNumberFormat="1"/><xf numFmtId="164" applyNumberFormat="1"/></cellXfs></styleSheet>
                """);
            for (var i = 0; i < sheets.Count; i++) Add(zip, $"xl/worksheets/sheet{i + 1}.xml", Sheet(sheets[i]));
        }
        return stream.ToArray();
    }

    private static string ContentTypes(int count)
    {
        var sheets = string.Concat(Enumerable.Range(1, count).Select(i =>
            $"""<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>"""));
        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>{sheets}</Types>
            """;
    }

    private static string Workbook(IReadOnlyList<XlsxSheet> sheets)
    {
        var entries = string.Concat(sheets.Select((s, i) =>
            $"""<sheet name="{Escape(s.Name)}" sheetId="{i + 1}" r:id="rId{i + 1}"/>"""));
        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>{entries}</sheets></workbook>
            """;
    }

    private static string WorkbookRels(int count)
    {
        var rels = string.Concat(Enumerable.Range(1, count).Select(i =>
            $"""<Relationship Id="rId{i}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet{i}.xml"/>"""));
        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">{rels}<Relationship Id="rId{count + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
            """;
    }

    private static string Sheet(XlsxSheet sheet)
    {
        var xml = new StringBuilder("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
        AppendRow(xml, 1, sheet.Headers.Cast<object?>().ToList(), header: true);
        for (var i = 0; i < sheet.Rows.Count; i++) AppendRow(xml, i + 2, sheet.Rows[i], header: false);
        xml.Append("</sheetData></worksheet>");
        return xml.ToString();
    }

    private static void AppendRow(StringBuilder xml, int row, IReadOnlyList<object?> cells, bool header)
    {
        xml.Append($"<row r=\"{row}\">");
        for (var c = 0; c < cells.Count; c++)
        {
            var reference = $"{Column(c)}{row}";
            switch (cells[c])
            {
                case null:
                    break;
                case decimal or int or long or double when !header:
                    var number = Convert.ToDecimal(cells[c], CultureInfo.InvariantCulture);
                    var style = cells[c] is decimal ? " s=\"2\"" : "";
                    xml.Append($"<c r=\"{reference}\"{style}><v>{number.ToString(CultureInfo.InvariantCulture)}</v></c>");
                    break;
                case DateTimeOffset date when !header:
                    // Excel guarda las fechas como días desde 1899-12-30.
                    var serial = (date.DateTime - new DateTime(1899, 12, 30)).TotalDays;
                    xml.Append($"<c r=\"{reference}\" s=\"3\"><v>{serial.ToString(CultureInfo.InvariantCulture)}</v></c>");
                    break;
                default:
                    var bold = header ? " s=\"1\"" : "";
                    xml.Append($"<c r=\"{reference}\" t=\"inlineStr\"{bold}><is><t xml:space=\"preserve\">{Escape(Convert.ToString(cells[c], CultureInfo.InvariantCulture) ?? "")}</t></is></c>");
                    break;
            }
        }
        xml.Append("</row>");
    }

    private static string Column(int index)
    {
        var name = "";
        for (index++; index > 0; index = (index - 1) / 26) name = (char)('A' + (index - 1) % 26) + name;
        return name;
    }

    private static string Escape(string text) => SecurityElement.Escape(text) ?? "";

    private static void Add(ZipArchive zip, string path, string content)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path, CompressionLevel.Fastest).Open(), new UTF8Encoding(false));
        writer.Write(content.Trim());
    }
}
