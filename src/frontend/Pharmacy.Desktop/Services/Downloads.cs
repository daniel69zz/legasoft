namespace Pharmacy.Desktop.Services;

/// <summary>Guarda en la carpeta de descargas los archivos que genera el puesto (recibos, reportes, facturas).</summary>
public static class Downloads
{
    /// <summary>Carpeta destino; las pruebas la cambian por una temporal.</summary>
    public static string Folder { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    public static async Task<string> SaveAsync(byte[] content, string fileName)
    {
        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, string.Concat(fileName.Split(Path.GetInvalidFileNameChars())));
        await File.WriteAllBytesAsync(path, content);
        return path;
    }
}
