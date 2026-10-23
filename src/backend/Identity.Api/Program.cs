using Identity.Api;
using Pharmacy.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddPharmacyDb<IdentityDb>(builder.Configuration, "Identity");

var app = builder.Build();
app.UseServiceDefaults();
app.MapAuthEndpoints();
app.MapUserEndpoints();

await app.Services.EnsureSchemaAsync<IdentityDb>("identity.users",
    db => IdentitySeed.RunAsync(db, app.Configuration["Seed:DemoPassword"] ?? "Farmacia2026!"));
app.Run();

public partial class Program;
