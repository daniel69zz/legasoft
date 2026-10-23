using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharmacy.Desktop.ViewModels;
using Pharmacy.Desktop.Views;

namespace Pharmacy.Desktop;

public partial class App : Avalonia.Application
{
    public static string[] Args { get; set; } = [];
    private static IHost? _host;

    public static IServiceProvider Services => _host!.Services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _host = DesktopHost.Build(Args);
        _host.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _host.Services.GetRequiredService<MainWindowViewModel>(),
            };
            desktop.ShutdownRequested += (_, _) => _host.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
