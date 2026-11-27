using Business.Api;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Demo;
using Pharmacy.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddPharmacyDb<BusinessDb>(builder.Configuration, "Business", historySchema: "inventory");
builder.Services.AddScoped<EventApplier>();

// SCRUM-36 · HU-23 y SCRUM-41 · HU-T06: respaldos (COPY de PostgreSQL; registro simulado con InMemory).
var backupOptions = builder.Configuration.GetSection("Backup").Get<BackupOptions>() ?? new BackupOptions();
builder.Services.AddSingleton(backupOptions);
builder.Services.AddSingleton<InMemoryBackupStore>();
builder.Services.AddScoped<IBackupStore>(sp => sp.GetRequiredService<BusinessDb>().Database.IsRelational()
    ? ActivatorUtilities.CreateInstance<PostgresBackupStore>(sp)
    : sp.GetRequiredService<InMemoryBackupStore>());
builder.Services.AddSingleton<BackupScheduler>();
if (backupOptions.Scheduled && !builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService(sp => sp.GetRequiredService<BackupScheduler>());

var app = builder.Build();
app.UseServiceDefaults();
app.MapBusinessEndpoints();

await app.Services.MigrateAsync<BusinessDb>(async db =>
{
    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(DemoCatalog.Products());
        db.Lots.AddRange(DemoCatalog.Lots(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4))));
        await db.SaveChangesAsync();
    }
    // SCRUM-18 y SCRUM-19 (Sprint 2): proveedores y una compra pendiente de recibir para la demostración (se agregan
    // también a bases creadas en el Sprint 1, que ya tienen catálogo).
    if (!await db.Suppliers.AnyAsync())
    {
        var products = await db.Products.ToDictionaryAsync(p => p.Id);
        var (suppliers, purchase) = DemoPurchasing.Create(products, DateTimeOffset.UtcNow);
        db.Suppliers.AddRange(suppliers);
        if (purchase is not null) db.Purchases.Add(purchase);
        await db.SaveChangesAsync();
    }
});
app.Run();

public partial class Program;
