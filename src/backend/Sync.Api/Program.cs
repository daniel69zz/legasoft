using Pharmacy.ServiceDefaults;
using Sync.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddPharmacyDb<SyncDb>(builder.Configuration, "Sync");
builder.Services.AddHttpClient<EventDispatcher>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Business"] ?? "http://localhost:5103"));

var app = builder.Build();
app.UseServiceDefaults();
app.MapSyncEndpoints();

await app.Services.EnsureSchemaAsync<SyncDb>("sync.received_events");
app.Run();

public partial class Program;
