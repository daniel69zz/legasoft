using Avalonia;
using Avalonia.Headless;
using Pharmacy.Desktop;

[assembly: AvaloniaTestApplication(typeof(Pharmacy.Desktop.UiTests.TestApp))]

namespace Pharmacy.Desktop.UiTests;

public static class TestApp
{
    public static readonly string DatabasePath =
        Path.Combine(Path.GetTempPath(), $"farmacia-ui-{Guid.NewGuid():N}.db");

    public static AppBuilder BuildAvaloniaApp()
    {
        // Base SQLite temporal: la prueba no toca la base real del puesto.
        App.Args = [$"--Desktop:DatabasePath={DatabasePath}", "--Desktop:SyncIntervalSeconds=2"];
        return AppBuilder.Configure<App>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
