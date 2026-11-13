using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Cash;
using Pharmacy.Application.Inventory;
using Pharmacy.Application.Sales;
using Pharmacy.Desktop.Data;
using Pharmacy.Desktop.Services;
using Pharmacy.Desktop.ViewModels;

namespace Pharmacy.Desktop;

/// <summary>Composición del puesto: servicios locales, SQLite, Sync Worker y ViewModels.</summary>
public static class DesktopHost
{
    public static IHost Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });
        var options = builder.Configuration.GetSection("Desktop").Get<DesktopOptions>() ?? new DesktopOptions();
        var s = builder.Services;

        s.AddSingleton(options);
        s.AddSingleton<Session>();
        s.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<Session>());
        s.AddSingleton<IClock, SystemClock>();
        s.AddSingleton<SyncStatus>();
        s.AddDbContext<LocalDb>(o => o.UseSqlite($"Data Source={options.ResolveDatabasePath()}"));

        // Repositorios locales y casos de uso compartidos (Pharmacy.Application)
        s.AddScoped<LocalStore>();
        s.AddScoped<ICatalogRepository>(sp => sp.GetRequiredService<LocalStore>());
        s.AddScoped<ISalesRepository>(sp => sp.GetRequiredService<LocalStore>());
        s.AddScoped<ICashRepository>(sp => sp.GetRequiredService<LocalStore>());
        s.AddScoped<IOutbox>(sp => sp.GetRequiredService<LocalStore>());
        s.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LocalStore>());
        s.AddScoped<IOperationalSettings>(sp => sp.GetRequiredService<LocalStore>());
        s.AddScoped<RegisterSaleHandler>();
        s.AddScoped<VoidSaleHandler>();
        s.AddScoped<CashHandlers>();
        s.AddScoped<InventoryQueries>();
        s.AddScoped<AdjustStockHandler>();   // SCRUM-101 y SCRUM-121

        // SCRUM-100 · HU-T02.2: HTTPS con TLS 1.2+ fuera de localhost.
        var gateway = GatewayUrlPolicy.Validate(options.GatewayUrl);
        s.AddHttpClient("gateway", c =>
        {
            c.BaseAddress = gateway;
            c.Timeout = TimeSpan.FromSeconds(8);
        }).ConfigurePrimaryHttpMessageHandler(GatewayUrlPolicy.CreateHandler);
        s.AddSingleton<ApiClient>();
        s.AddSingleton<AuthService>();
        s.AddSingleton<Operations>();
        s.AddSingleton<SyncWorker>();
        s.AddHostedService(sp => sp.GetRequiredService<SyncWorker>());

        s.AddSingleton<MainWindowViewModel>();
        s.AddTransient<LoginViewModel>();
        s.AddTransient<ShellViewModel>();
        s.AddTransient<PosViewModel>();
        s.AddTransient<InventoryViewModel>();
        s.AddTransient<CashViewModel>();
        s.AddTransient<SyncViewModel>();
        s.AddTransient<ReportsViewModel>();
        s.AddTransient<UsersViewModel>();
        s.AddTransient<CatalogViewModel>();
        s.AddTransient<SuppliersViewModel>();   // SCRUM-18
        s.AddTransient<PurchasesViewModel>();   // SCRUM-19, SCRUM-124 y SCRUM-125
        s.AddTransient<MovementsViewModel>();   // SCRUM-123
        s.AddTransient<SettingsViewModel>();    // SCRUM-130

        var host = builder.Build();
        using var scope = host.Services.CreateScope();
        LocalSeeder.Run(scope.ServiceProvider.GetRequiredService<LocalDb>(), options.DemoPassword);
        return host;
    }
}
