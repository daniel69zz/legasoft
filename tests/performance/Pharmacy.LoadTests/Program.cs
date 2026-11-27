using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Demo;
using Pharmacy.Application.Sync;

// SCRUM-37 · HU-T01: prueba de carga de RNF-001 (95 % de las operaciones habituales en 2 s o menos con 20
// usuarios concurrentes) y RNF-002 (reporte de 12 meses en 5 s o menos).
//
// Uso (solo contra un backend de prueba: --escribir registra ventas de verdad):
//   dotnet run --project tests/performance/Pharmacy.LoadTests -c Release -- \
//     --gateway http://localhost:5100 --usuarios 20 --segundos 60 --sembrar-meses 12 --escribir

var options = Options.Parse(args);
var es = CultureInfo.GetCultureInfo("es-BO");
using var http = new HttpClient { BaseAddress = new Uri(options.Gateway), Timeout = TimeSpan.FromSeconds(30) };

var admin = await LoginAsync(http, "admin", options.Password, "PUESTO-01");
var cashier = await LoginAsync(http, "cajero1", options.Password, "PUESTO-01");

// Cada usuario virtual entra con su propio puesto (CARGA-01, CARGA-02…), como cajas distintas, con roles que
// pueden consultar inventario y que no están restringidos a equipos autorizados.
string[] roles = ["gerente", "operaciones", "almacen", "farmaceutico"];
var tokens = new List<string>();
for (var u = 0; u < options.Users; u++)
    tokens.Add(await LoginAsync(http, roles[u % roles.Length], options.Password, $"CARGA-{u + 1:D2}"));

if (options.SeedMonths > 0)
{
    if (!options.Write) return Fail("Para sembrar ventas use --escribir (solo contra un backend de prueba).");
    var seeded = await SeedAsync(http, cashier, options.SeedMonths, options.SalesPerDay);
    Console.WriteLine($"Sembradas {seeded} ventas en {options.SeedMonths} meses.");
}

// Operaciones habituales del día a día (RNF-001). La venta solo se incluye con --escribir.
var operations = new List<(string Name, Func<HttpClient, string, Task<HttpResponseMessage>> Call)>
{
    ("Consulta de productos con stock", (c, t) => Get(c, t, "/api/products")),
    ("Consulta de movimientos de inventario", (c, t) => Get(c, t, "/api/inventory/movements")),
    ("Descarga de cambios del catálogo", (c, t) => Get(c, t, "/sync/pull?since=0")),
    ("Descarga de cambios de lotes", (c, t) => Get(c, t, "/sync/pull-lots?since=0")),
};
if (options.Write)
    operations.Add(("Registro de venta (sincronización)", (c, t) => PushSaleAsync(c, t, DeviceOf(t), DateTimeOffset.UtcNow)));

var results = new System.Collections.Concurrent.ConcurrentBag<(string Op, double Ms, bool Ok)>();
var until = DateTime.UtcNow.AddSeconds(options.Seconds);
var users = Enumerable.Range(0, options.Users).Select(async user =>
{
    var random = new Random(user);
    using var client = new HttpClient { BaseAddress = http.BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
    while (DateTime.UtcNow < until)
    {
        var (name, call) = operations[random.Next(operations.Count)];
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await call(client, tokens[user]);
            results.Add((name, watch.Elapsed.TotalMilliseconds, response.IsSuccessStatusCode));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            results.Add((name, watch.Elapsed.TotalMilliseconds, false));
        }
        // Tiempo de pausa de una persona entre operaciones (por defecto entre 1 y 3 s).
        await Task.Delay(random.Next(options.PauseMs / 2, options.PauseMs * 3 / 2 + 1));
    }
});
await Task.WhenAll(users);

var all = results.ToList();
var withinTwoSeconds = all.Count(r => r.Ok && r.Ms <= 2000) / (double)all.Count;
Console.WriteLine($"## Prueba de carga · {options.Users} usuarios · {options.Seconds} s · {all.Count} operaciones");
Console.WriteLine();
Console.WriteLine("| Operación | Cantidad | Errores | p50 (ms) | p95 (ms) | Máx (ms) |");
Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: |");
foreach (var group in all.GroupBy(r => r.Op).OrderBy(g => g.Key))
{
    var ms = group.Select(r => r.Ms).Order().ToList();
    Console.WriteLine($"| {group.Key} | {ms.Count} | {group.Count(r => !r.Ok)} | {Percentile(ms, 50):0} | {Percentile(ms, 95):0} | {ms[^1]:0} |");
}
Console.WriteLine();
Console.WriteLine($"**RNF-001:** {withinTwoSeconds.ToString("P1", es)} de las operaciones respondió bien en 2 s o menos (mínimo 95 %).");

// RNF-002: reporte operativo de 12 meses en 5 s o menos.
var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4));
var report = Stopwatch.StartNew();
using var reportResponse = await Get(http, admin, $"/api/reports/operations/ventas?from={today.AddMonths(-12):yyyy-MM-dd}&to={today:yyyy-MM-dd}");
var reportRows = reportResponse.IsSuccessStatusCode
    ? (await reportResponse.Content.ReadFromJsonAsync<ReportTableDto>())?.Rows.Count ?? 0
    : 0;
report.Stop();
if (!reportResponse.IsSuccessStatusCode) Console.WriteLine($"El reporte respondió {(int)reportResponse.StatusCode}.");
Console.WriteLine($"**RNF-002:** reporte de ventas de 12 meses ({reportRows} ventas) en {report.Elapsed.TotalSeconds.ToString("0.00", es)} s (máximo 5 s).");

var ok = withinTwoSeconds >= 0.95 && reportResponse.IsSuccessStatusCode && report.Elapsed <= TimeSpan.FromSeconds(5);
Console.WriteLine();
Console.WriteLine(ok ? "Resultado: **cumple** RNF-001 y RNF-002." : "Resultado: **no cumple**; el requisito no se acepta.");
return ok ? 0 : 1;

static double Percentile(List<double> sorted, int p) =>
    sorted[Math.Clamp((int)Math.Ceiling(p / 100.0 * sorted.Count) - 1, 0, sorted.Count - 1)];

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 2;
}

static async Task<string> LoginAsync(HttpClient http, string user, string password, string device)
{
    var response = await http.PostAsJsonAsync("/auth/login", new LoginRequest(user, password, device));
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
}

static Task<HttpResponseMessage> Get(HttpClient http, string token, string url)
{
    var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return http.SendAsync(request);
}

static Task<HttpResponseMessage> PushSaleAsync(HttpClient http, string token, string device, DateTimeOffset at) =>
    PushAsync(http, token, [SaleMessage(at, device)]);

// El puesto que figura en el token (el Sync Service exige que coincida con el del lote enviado).
static string DeviceOf(string token)
{
    var part = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(part.PadRight(part.Length + (4 - part.Length % 4) % 4, '=')));
    return JsonDocument.Parse(json).RootElement.GetProperty("device").GetString()!;
}

static Task<HttpResponseMessage> PushAsync(HttpClient http, string token, IReadOnlyList<SyncEnvelope> messages)
{
    var request = new HttpRequestMessage(HttpMethod.Post, "/sync/push")
    {
        Content = JsonContent.Create(new PushRequest(messages[0].DeviceId, messages)),
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return http.SendAsync(request);
}

static SyncEnvelope SaleMessage(DateTimeOffset at, string device = "PUESTO-01")
{
    var id = Guid.NewGuid();
    var lot = new Guid("00000000-0000-0000-0002-000000000002");   // PCM-2409, el lote con más stock
    var payload = new SaleCreatedPayload(id, $"{device}-{id.ToString()[..8]}", device, "cajero1", Guid.NewGuid(), at,
        1m, 0, 1m, 1m, 0, "", "", "", [new(DemoCatalog.ProductId(1), "Paracetamol 500 mg · Tableta", lot, "PCM-2409", 2, 0.50m)]);
    return new SyncEnvelope(Guid.NewGuid(), device, SyncModules.Sales, EventTypes.SaleCreated, id, 1, at,
        JsonSerializer.Serialize(payload, OutboxMessage.Json));
}

static async Task<int> SeedAsync(HttpClient http, string token, int months, int perDay)
{
    var messages = new List<SyncEnvelope>();
    var start = DateTimeOffset.UtcNow.AddMonths(-months);
    for (var day = start; day < DateTimeOffset.UtcNow; day = day.AddDays(1))
        for (var i = 0; i < perDay; i++) messages.Add(SaleMessage(day.AddMinutes(13 + i * 23)));
    foreach (var batch in messages.Chunk(100))
        (await PushAsync(http, token, batch)).EnsureSuccessStatusCode();
    return messages.Count;
}

sealed record Options(string Gateway, string Password, int Users, int Seconds, int SeedMonths, int SalesPerDay,
    bool Write, int PauseMs)
{
    public static Options Parse(string[] args)
    {
        string Value(string name, string fallback)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
        return new Options(Value("--gateway", "http://localhost:5100"),
            Value("--clave", Environment.GetEnvironmentVariable("DEMO_PASSWORD") ?? "Farmacia2026!"),
            int.Parse(Value("--usuarios", "20")), int.Parse(Value("--segundos", "60")),
            int.Parse(Value("--sembrar-meses", "0")), int.Parse(Value("--ventas-por-dia", "10")),
            args.Contains("--escribir"), int.Parse(Value("--pausa-ms", "2000")));
    }
}
