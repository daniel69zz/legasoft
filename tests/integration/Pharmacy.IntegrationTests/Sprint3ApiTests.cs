using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Business.Api;
using Identity.Api;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Demo;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Identity;

namespace Pharmacy.IntegrationTests;

/// <summary>Historias del Sprint 3 sobre el Business Service, con la API real y EF Core InMemory.</summary>
public class Sprint3ApiTests : IDisposable
{
    private static readonly Guid Paracetamol = DemoCatalog.ProductId(1);
    private static readonly Guid ParacetamolLot = new("00000000-0000-0000-0002-000000000001");

    private readonly ServiceFactory<BusinessDb, BusinessDb> _factory = new();
    private readonly HttpClient _admin;

    public Sprint3ApiTests() => _admin = _factory.ClientFor(Roles.Administrador, "admin");

    public void Dispose() => _factory.Dispose();

    // ---------- SCRUM-13 · HU-20 ----------

    [Fact]
    [Trait("JiraIssue", "SCRUM-13")]
    [Trait("Requirement", "RF-075")]
    public async Task La_bitacora_no_se_puede_modificar_ni_borrar()
    {
        await SaleAsync("PUESTO-01-000001", 4, discount: 1m, authorizedBy: "farmaceutico");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusinessDb>();
        var entry = db.Audit.Single(a => a.Action == "Descuento aplicado");
        Assert.Equal("farmaceutico", entry.UserName);   // el descuento queda con quién lo autorizó

        entry.Detail = "alterado";
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        db.ChangeTracker.Clear();
        db.Audit.Remove(db.Audit.First());
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    // ---------- SCRUM-35 · HU-22 ----------

    [Fact]
    [Trait("JiraIssue", "SCRUM-35")]
    [Trait("Requirement", "RF-081")]
    [Trait("Requirement", "RF-102")]
    public async Task La_venta_de_un_puesto_llega_a_los_demas_con_el_stock_nuevo_del_lote()
    {
        var before = await LotsSinceAsync(0);
        await SaleAsync("PUESTO-02-000001", 20, device: "PUESTO-02");
        await StockMovementAsync(-3);

        var changes = (await LotsSinceAsync(before.Version)).Lots.Where(l => l.LotId == ParacetamolLot).ToList();

        Assert.Equal(2, changes.Count);                       // una entrada por cada cambio de stock
        Assert.Equal(120 - 20 - 3, changes.MaxBy(c => c.Sequence)!.Quantity);
    }

    // ---------- SCRUM-31 · HU-18 y SCRUM-24 · HU-14 ----------

    [Fact]
    [Trait("JiraIssue", "SCRUM-31")]
    [Trait("Requirement", "RF-068")]
    [Trait("Requirement", "RF-070")]
    [Trait("Requirement", "RF-071")]
    [Trait("Requirement", "RF-072")]
    public async Task Los_reportes_del_periodo_se_ven_y_se_exportan_a_excel_y_pdf()
    {
        await SaleAsync("PUESTO-01-000001", 4);
        await SaleAsync("PUESTO-01-000002", 6);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4));
        var range = $"from={today.AddDays(-1):yyyy-MM-dd}&to={today:yyyy-MM-dd}";

        var sales = await _admin.GetFromJsonAsync<ReportTableDto>($"/api/reports/operations/ventas?{range}");
        Assert.Equal(2, sales!.Rows.Count);
        Assert.Equal("5,00", sales.Totals[^1]);               // 10 tabletas a Bs 0,50
        foreach (var kind in new[] { "compras", "inventario", "stock-bajo", "vencimientos" })
            Assert.NotNull(await _admin.GetFromJsonAsync<ReportTableDto>($"/api/reports/operations/{kind}?{range}"));

        var pdf = await _admin.GetByteArrayAsync($"/api/reports/operations/ventas?{range}&format=pdf");
        var xlsx = await _admin.GetByteArrayAsync($"/api/reports/operations/ventas?{range}&format=xlsx");
        Assert.StartsWith("%PDF", Encoding.Latin1.GetString(pdf, 0, 4));
        Assert.Equal((byte)'P', xlsx[0]);                     // zip de Office Open XML

        var receipts = await _admin.GetByteArrayAsync($"/api/reports/receipts?{range}");
        Assert.Contains("PUESTO-01-000002", Encoding.Latin1.GetString(receipts));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-31")]
    [Trait("JiraIssue", "SCRUM-24")]
    [Trait("Requirement", "RF-068")]
    [Trait("Requirement", "RF-051")]
    public async Task Un_rango_invertido_se_rechaza_y_sin_ventas_no_hay_recibos()
    {
        var inverted = await _admin.GetAsync("/api/reports/operations/ventas?from=2026-11-15&to=2026-11-01");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, inverted.StatusCode);
        Assert.Contains("posterior", await inverted.Content.ReadAsStringAsync());

        var none = await _admin.GetAsync("/api/reports/receipts?from=2020-01-01&to=2020-01-02");
        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
        Assert.Contains("No hay recibos", await none.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Forbidden,
            (await _factory.ClientFor(Roles.Cajero).GetAsync("/api/reports/operations/ventas")).StatusCode);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-31")]
    [Trait("Requirement", "RF-103")]
    public async Task El_cierre_diario_consolida_los_turnos_del_dia()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4));
        foreach (var (cashier, counted) in new[] { ("cajero1", 230m), ("cajero2", 220m) })
        {
            var session = Guid.NewGuid();
            await SendAsync(EventTypes.CashSessionOpened, session,
                new CashSessionOpenedPayload(session, "PUESTO-01", cashier, 200m, DateTimeOffset.UtcNow));
            await SendAsync(EventTypes.CashSessionClosed, session, new CashSessionClosedPayload(session, "PUESTO-01",
                cashier, 200m, 50m, 20m, 230m, counted, counted - 230m, counted == 230m ? "" : "Faltante",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new Dictionary<decimal, int>()));
        }

        var report = await _admin.GetFromJsonAsync<ReportTableDto>(
            $"/api/reports/operations/cierre-diario?to={today:yyyy-MM-dd}");

        Assert.Equal(2, report!.Rows.Count);
        Assert.Equal(["100,00", "40,00"], report.Totals.Skip(6).Take(2));   // ventas y egresos del día
        Assert.Equal("Saldo 450,00", report.Totals[^1]);
    }

    // ---------- SCRUM-32 · HU-19 ----------

    [Fact]
    [Trait("JiraIssue", "SCRUM-32")]
    [Trait("Requirement", "RF-074")]
    public async Task El_comparativo_por_cajero_se_agrupa_por_semana()
    {
        await SaleAsync("PUESTO-01-000001", 4);
        await SaleAsync("PUESTO-01-000002", 2);

        var week = await _admin.GetFromJsonAsync<List<SalesByCashierRow>>(
            "/api/reports/sales-by-cashier?groupBy=semana");
        var dashboard = await _admin.GetFromJsonAsync<DashboardSummary>("/api/dashboard?groupBy=mes");

        var row = Assert.Single(week!);
        Assert.Equal((DayOfWeek.Monday, 2, 3m), (row.Day.DayOfWeek, row.Sales, row.Total));
        Assert.Equal(1, Assert.Single(dashboard!.SalesByCashier).Day.Day);
        Assert.Equal(HttpStatusCode.Forbidden, (await _factory.ClientFor(Roles.Cajero).GetAsync("/api/dashboard")).StatusCode);
    }

    // ---------- SCRUM-33 · HU-28 y SCRUM-34 · HU-29 ----------

    [Fact]
    [Trait("JiraIssue", "SCRUM-33")]
    [Trait("Requirement", "RF-097")]
    [Trait("Requirement", "RF-098")]
    public async Task Los_datos_de_la_empresa_se_configuran_y_llegan_a_los_puestos()
    {
        var profile = new CompanyProfileDto("Farmacia Pacheco S.R.L.", "1023456789", "Av. 20 de Octubre 2400",
            "2-2411111", "Sopocachi", "Av. 6 de Agosto 2100");
        Assert.Equal(HttpStatusCode.OK, (await _admin.PutAsJsonAsync("/api/settings/company", profile)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await _admin.PutAsJsonAsync("/api/settings/company", profile with { CompanyName = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _factory.ClientFor(Roles.Cajero)
            .PutAsJsonAsync("/api/settings/company", profile)).StatusCode);

        var pull = await Internal().GetFromJsonAsync<PullResponse>("/internal/catalog?since=0");

        Assert.Equal(profile, pull!.Company);
        Assert.Equal(20, pull.MaxDiscountPercent);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-34")]
    [Trait("Requirement", "RF-100")]
    public async Task La_venta_queda_asociada_a_la_sucursal_del_puesto()
    {
        Assert.Equal(HttpStatusCode.OK, (await _admin.PutAsJsonAsync("/api/branches/devices",
            new BranchDeviceDto("PUESTO-02", "Miraflores"))).StatusCode);

        await SaleAsync("PUESTO-02-000001", 4, device: "PUESTO-02");
        await SaleAsync("PUESTO-01-000001", 2);

        var miraflores = await _admin.GetFromJsonAsync<BranchSummaryDto>("/api/branches/Miraflores/summary");
        var sopocachi = await _admin.GetFromJsonAsync<BranchSummaryDto>("/api/branches/Sopocachi/summary");
        var report = await _admin.GetFromJsonAsync<ReportTableDto>("/api/reports/operations/ventas?branch=Miraflores");

        Assert.Equal(["PUESTO-02"], miraflores!.Devices);
        Assert.Equal((1, 2m), (miraflores.Sales, miraflores.SalesTotal));
        Assert.Equal(-4, Assert.Single(miraflores.StockMovements).NetQuantity);
        Assert.Equal(1, sopocachi!.Sales);
        Assert.Equal("Miraflores", Assert.Single(report!.Rows)[4]);
    }

    // ---------- SCRUM-28 · HU-24 ----------

    [Fact]
    [Trait("JiraIssue", "SCRUM-28")]
    [Trait("Requirement", "RF-084")]
    [Trait("Requirement", "RF-085")]
    [Trait("Requirement", "RF-086")]
    [Trait("Requirement", "RF-087")]
    public async Task Los_clientes_se_registran_buscan_y_muestran_su_historial()
    {
        var cashier = _factory.ClientFor(Roles.Cajero, "cajero1");
        var created = await cashier.PostAsJsonAsync("/api/customers", new CustomerRequest("María Quispe", "1234567"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var maria = (await created.Content.ReadFromJsonAsync<CustomerDto>())!;

        var duplicate = await cashier.PostAsJsonAsync("/api/customers", new CustomerRequest("Otra persona", "1234567"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(maria.Id, (await duplicate.Content.ReadFromJsonAsync<CustomerDto>())!.Id);   // muestra el existente
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await cashier.PostAsJsonAsync("/api/customers", new CustomerRequest("Sin NIT", "12"))).StatusCode);

        await cashier.PutAsJsonAsync($"/api/customers/{maria.Id}", new CustomerRequest("María Quispe Mamani", "1234567", "71234567"));
        Assert.Single((await cashier.GetFromJsonAsync<List<CustomerDto>>("/api/customers?search=quispe"))!);
        Assert.Single((await cashier.GetFromJsonAsync<List<CustomerDto>>("/api/customers?search=1234567"))!);

        await SaleAsync("PUESTO-01-000001", 4, customerTaxId: "1234567");
        var history = await cashier.GetFromJsonAsync<List<CustomerPurchaseDto>>($"/api/customers/{maria.Id}/purchases");
        var purchase = Assert.Single(history!);
        Assert.Equal(("PUESTO-01-000001", 2m), (purchase.ReceiptNumber, purchase.Total));
        Assert.Contains("× 4", Assert.Single(purchase.Products));

        Assert.Equal(HttpStatusCode.Forbidden,
            (await _factory.ClientFor(Roles.GerenteGeneral).GetAsync("/api/customers")).StatusCode);
    }

    // ---------- SCRUM-27 · HU-17 ----------

    [Fact]
    [Trait("JiraIssue", "SCRUM-27")]
    [Trait("Requirement", "RF-063")]
    [Trait("Requirement", "RF-064")]
    [Trait("Requirement", "RF-101")]
    public async Task La_factura_simulada_tiene_numero_correlativo_y_no_se_emite_para_anuladas()
    {
        var cashier = _factory.ClientFor(Roles.Cajero, "cajero1");
        var first = await SaleAsync("PUESTO-01-000001", 4);
        var second = await SaleAsync("PUESTO-01-000002", 2);
        var voided = await SaleAsync("PUESTO-01-000003", 2);
        await SendAsync(EventTypes.SaleVoided, voided, new SaleVoidedPayload(voided, "Error", "cajero1", DateTimeOffset.UtcNow));

        var invoice1 = await IssueAsync(cashier, first);
        var invoice2 = await IssueAsync(cashier, second);
        var again = await IssueAsync(cashier, first);

        Assert.Equal((1, 2, 1), (invoice1.Number, invoice2.Number, again.Number));
        Assert.Equal(("María Quispe", "1234567", 2m), (invoice1.CustomerName, invoice1.CustomerTaxId, invoice1.Total));
        Assert.Equal(4, Assert.Single(invoice1.Lines).Quantity);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cashier.PostAsJsonAsync($"/api/sales/{voided}/invoice",
            new InvoiceRequest("X", "1234567"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cashier.PostAsJsonAsync($"/api/sales/{Guid.NewGuid()}/invoice",
            new InvoiceRequest("X", "1234567"))).StatusCode);

        var pdf = Encoding.Latin1.GetString(await cashier.GetByteArrayAsync("/api/invoices/1/pdf"));
        Assert.Contains("FACTURA SIMULADA N.º 000001", pdf);
        Assert.Contains("SIN VALIDEZ FISCAL", pdf);
    }

    // ---------- SCRUM-36 · HU-23 y SCRUM-41 · HU-T06 ----------

    [Fact]
    [Trait("JiraIssue", "SCRUM-36")]
    [Trait("Requirement", "RF-082")]
    [Trait("Requirement", "RF-083")]
    public async Task Solo_el_administrador_respalda_y_recupera_con_confirmacion()
    {
        var created = await _admin.PostAsync("/api/backups", null);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var backup = (await created.Content.ReadFromJsonAsync<BackupDto>())!;
        Assert.Equal(DateTimeOffset.UtcNow.Date, backup.CreatedAt.UtcDateTime.Date);

        var cashier = _factory.ClientFor(Roles.Cajero);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/api/backups")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.PostAsJsonAsync($"/api/backups/{backup.Name}/restore",
            new RestoreRequest(true))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _admin.PostAsJsonAsync(
            $"/api/backups/{backup.Name}/restore", new RestoreRequest(false))).StatusCode);

        var restored = await _admin.PostAsJsonAsync($"/api/backups/{backup.Name}/restore", new RestoreRequest(true));

        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Contains(backup.Name, _factory.Services.GetRequiredService<InMemoryBackupStore>().Restored);
        var audit = await _admin.GetFromJsonAsync<List<AuditEntryDto>>("/api/audit");
        Assert.Contains(audit!, a => a.Action == "Recuperación de respaldo" && a.UserName == "admin");
        Assert.Contains(audit!, a => a.Action == "Respaldo generado");
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-41")]
    [Trait("Requirement", "RNF-008")]
    public async Task El_respaldo_diario_se_genera_y_si_falla_se_avisa_por_correo()
    {
        var scheduler = _factory.Services.GetRequiredService<BackupScheduler>();
        await scheduler.RunOnceAsync();
        await scheduler.RunOnceAsync();   // dentro de las 24 h no genera otro

        var store = _factory.Services.GetRequiredService<InMemoryBackupStore>();
        Assert.Single(await store.ListAsync(), b => b.Kind == BackupKinds.Automatic);
        var alerts = await Internal().GetFromJsonAsync<List<AlertDto>>("/internal/alerts");
        Assert.DoesNotContain(alerts!, a => a.Kind.StartsWith("Respaldo"));

        using (var scope = _factory.Services.CreateScope())
            await BackupHealth.RecordAsync(scope.ServiceProvider.GetRequiredService<BusinessDb>(), false,
                "El respaldo diario falló: disco lleno", DateTimeOffset.UtcNow);

        alerts = await Internal().GetFromJsonAsync<List<AlertDto>>("/internal/alerts");
        Assert.Contains(alerts!, a => a.Kind == "Respaldo fallido" && a.Detail.Contains("disco lleno"));
    }

    // ---------- utilidades ----------

    private async Task<InvoiceDto> IssueAsync(HttpClient client, Guid saleId)
    {
        var response = await client.PostAsJsonAsync($"/api/sales/{saleId}/invoice",
            new InvoiceRequest("María Quispe", "1234567"));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<InvoiceDto>())!;
    }

    private async Task<Guid> SaleAsync(string receipt, int quantity, string device = "PUESTO-01",
        decimal discount = 0, string authorizedBy = "", string customerTaxId = "")
    {
        var id = Guid.NewGuid();
        var subtotal = quantity * 0.50m;
        await SendAsync(EventTypes.SaleCreated, id, new SaleCreatedPayload(id, receipt, device, "cajero1",
                Guid.NewGuid(), DateTimeOffset.UtcNow, subtotal, discount, subtotal - discount, 20m, 20m - subtotal + discount,
                "", customerTaxId.Length > 0 ? "María Quispe" : "", customerTaxId,
                [new(Paracetamol, "Paracetamol 500 mg", ParacetamolLot, "PCM-2401", quantity, 0.50m)],
                DiscountAuthorizedBy: authorizedBy),
            device);
        return id;
    }

    private Task StockMovementAsync(int quantity) => SendAsync(EventTypes.StockMovementCreated, Guid.NewGuid(),
        new StockMovementPayload(Guid.NewGuid(), Paracetamol, ParacetamolLot, "PCM-2401", "Ajuste", quantity,
            "Producto dañado", "almacen", DateTimeOffset.UtcNow));

    private async Task<LotPullResponse> LotsSinceAsync(long since) =>
        (await Internal().GetFromJsonAsync<LotPullResponse>($"/internal/lots?since={since}"))!;

    private async Task SendAsync<T>(string eventType, Guid entityId, T payload, string device = "PUESTO-01")
    {
        var module = eventType.StartsWith("Cash") ? SyncModules.Cash
            : eventType.StartsWith("Sale") ? SyncModules.Sales : SyncModules.Inventory;
        var message = new SyncEnvelope(Guid.NewGuid(), device, module, eventType, entityId, 1,
            DateTimeOffset.UtcNow, JsonSerializer.Serialize(payload, OutboxMessage.Json));
        var response = await Internal().PostAsJsonAsync("/internal/events", message);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private HttpClient Internal()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", "clave-interna-solo-desarrollo");
        return client;
    }
}

/// <summary>SCRUM-11 · HU-03 (RF-006): el intento de un cajero en un equipo no autorizado queda registrado.</summary>
public class CashierDeviceTests : IDisposable
{
    private readonly ServiceFactory<IdentityDb, IdentityDb> _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    [Trait("JiraIssue", "SCRUM-11")]
    [Trait("Requirement", "RF-006")]
    public async Task El_cajero_en_un_equipo_no_autorizado_es_rechazado_y_queda_en_auditoria()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/auth/login",
            new LoginRequest("cajero1", "Farmacia2026!", "PUESTO-09"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var audit = await _factory.ClientFor(Roles.Administrador, "admin")
            .GetFromJsonAsync<List<AuditEntryDto>>("/api/users/audit");
        var entry = Assert.Single(audit!, a => a.Action == "Acceso rechazado");
        Assert.Equal("cajero1", entry.UserName);
        Assert.Contains("PUESTO-09", entry.Detail);
    }
}
