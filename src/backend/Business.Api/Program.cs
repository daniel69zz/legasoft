using Business.Api;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Demo;
using Pharmacy.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddPharmacyDb<BusinessDb>(builder.Configuration, "Business");
builder.Services.AddScoped<EventApplier>();

var app = builder.Build();
app.UseServiceDefaults();
app.MapBusinessEndpoints();

await app.Services.EnsureSchemaAsync<BusinessDb>("inventory.products", async db =>
{
    if (await db.Products.AnyAsync()) return;
    db.Products.AddRange(DemoCatalog.Products());
    db.Lots.AddRange(DemoCatalog.Lots(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4))));
    await db.SaveChangesAsync();
});
app.Run();

public partial class Program;
