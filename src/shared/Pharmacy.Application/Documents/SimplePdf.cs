using System.Globalization;
using System.Text;

namespace Pharmacy.Application.Documents;

/// <summary>
/// Generador mínimo de PDF (texto y líneas) sin dependencias externas, para recibos (SCRUM-24 · HU-14),
/// reportes (SCRUM-31 · HU-18) y facturas simuladas (SCRUM-27 · HU-17). Usa las fuentes estándar de PDF
/// (Helvetica y Courier) con codificación WinAnsi, que cubre los acentos y la eñe del español.
/// </summary>
public sealed class SimplePdf
{
    public const string ContentType = "application/pdf";

    private readonly double _width;
    private readonly double _height;
    private const double Margin = 40;
    private readonly List<StringBuilder> _pages = [];
    private StringBuilder _page = null!;
    private double _y;

    public SimplePdf(bool landscape = false)
    {
        (_width, _height) = landscape ? (842d, 595d) : (595d, 842d);
        NewPage();
    }

    /// <summary>Ancho útil en caracteres de Courier para el tamaño dado (para alinear tablas).</summary>
    public int MonoColumns(double size) => (int)((_width - 2 * Margin) / (size * 0.6));

    public SimplePdf Text(string text, double size = 10, bool bold = false, bool mono = false)
    {
        foreach (var line in Wrap(text, size, mono))
        {
            EnsureSpace(size * 1.35);
            var font = mono ? "F3" : bold ? "F2" : "F1";
            _page.Append(CultureInfo.InvariantCulture,
                $"BT /{font} {size:0.##} Tf {Margin:0.##} {_y:0.##} Td ({Escape(line)}) Tj ET\n");
            _y -= size * 1.35;
        }
        return this;
    }

    public SimplePdf Space(double points = 8)
    {
        _y -= points;
        return this;
    }

    public SimplePdf Rule()
    {
        EnsureSpace(8);
        _page.Append(CultureInfo.InvariantCulture,
            $"0.6 w {Margin:0.##} {_y + 4:0.##} m {_width - Margin:0.##} {_y + 4:0.##} l S\n");
        _y -= 8;
        return this;
    }

    public SimplePdf PageBreak()
    {
        NewPage();
        return this;
    }

    public byte[] Build()
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "", // páginas, se completa al final
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>",
        };
        var kids = new List<int>();
        foreach (var page in _pages)
        {
            var content = Latin(page.ToString());
            objects.Add($"<< /Length {content.Length} >>\nstream\n{page}endstream");
            var contentId = objects.Count;
            objects.Add(string.Create(CultureInfo.InvariantCulture,
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {_width:0} {_height:0}] /Contents {contentId} 0 R /Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R >> >> >>"));
            kids.Add(objects.Count);
        }
        objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', kids.Select(k => $"{k} 0 R"))}] /Count {kids.Count} >>";

        using var output = new MemoryStream();
        void Write(string s) => output.Write(Latin(s));
        Write("%PDF-1.4\n%âãÏÓ\n");
        var offsets = new List<long>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = output.Position;
        Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets) Write($"{offset:D10} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    private void NewPage()
    {
        _page = new StringBuilder();
        _pages.Add(_page);
        _y = _height - Margin;
    }

    private void EnsureSpace(double needed)
    {
        if (_y - needed < Margin) NewPage();
    }

    private IEnumerable<string> Wrap(string text, double size, bool mono)
    {
        var limit = mono ? MonoColumns(size) : (int)((_width - 2 * Margin) / (size * 0.5));
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            var line = raw;
            while (line.Length > limit)
            {
                var cut = line.LastIndexOf(' ', limit);
                if (cut <= 0) cut = limit;
                yield return line[..cut];
                line = line[cut..].TrimStart();
            }
            yield return line;
        }
    }

    private static string Escape(string text) =>
        Normalize(text).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    /// <summary>Lleva el texto a WinAnsi: reemplaza los signos que esa codificación no tiene.</summary>
    private static string Normalize(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(c switch
            {
                '—' => '\u0097', '–' => '\u0096', '…' => '\u0085', '•' => '\u0095', '‘' => '\u0091', '’' => '\u0092',
                '“' => '\u0093', '”' => '\u0094', '→' => '>', '−' => '-', '─' => '-', '€' => '\u0080',
                _ when c > 0xFF => '?',
                _ => c,
            });
        }
        return sb.ToString();
    }

    private static byte[] Latin(string s) => Encoding.Latin1.GetBytes(s);
}
