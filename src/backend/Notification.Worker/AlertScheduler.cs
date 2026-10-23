using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using Pharmacy.Application.Contracts;

namespace Notification.Worker;

public sealed class NotificationOptions
{
    public string BusinessUrl { get; set; } = "http://localhost:5103";
    public string InternalKey { get; set; } = "";
    public int IntervalMinutes { get; set; } = 60;
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public string From { get; set; } = "alertas@farmacia.local";
    public string[] Recipients { get; set; } = [];
}

/// <summary>Programador de alertas (Documento 3, C3 Backend): consulta vencimientos y stock mínimo y los envía por correo.</summary>
public sealed class AlertScheduler(HttpClient http, NotificationOptions options, ILogger<AlertScheduler> logger)
    : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromMinutes(options.IntervalMinutes);
            try
            {
                await SendAlertsAsync(stoppingToken);
            }
            catch (Exception e) when (e is HttpRequestException or SmtpException)
            {
                // Al arrancar junto con el Business Service, reintenta pronto en vez de esperar el intervalo completo.
                logger.LogWarning("No se pudieron enviar las alertas: {Error}. Reintento en 30 s", e.Message);
                delay = RetryDelay;
            }
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task SendAlertsAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{options.BusinessUrl}/internal/alerts");
        request.Headers.Add("X-Internal-Key", options.InternalKey);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>(ct) ?? [];
        if (alerts.Count == 0) return;

        var body = new StringBuilder("Alertas de inventario de la farmacia\n\n");
        foreach (var group in alerts.GroupBy(a => a.Kind))
        {
            body.AppendLine($"{group.Key} ({group.Count()})");
            foreach (var a in group) body.AppendLine($"  - {a.Product}: {a.Detail}");
            body.AppendLine();
        }

        using var smtp = new SmtpClient(options.SmtpHost, options.SmtpPort);
        using var mail = new MailMessage { From = new MailAddress(options.From), Subject = $"Farmacia: {alerts.Count} alertas de inventario", Body = body.ToString() };
        foreach (var r in options.Recipients) mail.To.Add(r);
        await smtp.SendMailAsync(mail, ct);
        logger.LogInformation("Enviadas {Count} alertas a {Recipients}", alerts.Count, string.Join(", ", options.Recipients));
    }
}
