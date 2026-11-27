using System.Net.Http.Headers;
using Pharmacy.Application.Contracts;

namespace Pharmacy.Desktop.Services;

/// <summary>Llamadas del Sprint 3: reportes, recibos, clientes, facturas, empresa, sucursales y respaldos.</summary>
public sealed partial class ApiClient
{
    // SCRUM-31 · HU-18
    public Task<ReportTableDto> GetReportAsync(string kind, DateOnly from, DateOnly to, string? branch) =>
        SendAsync<ReportTableDto>(HttpMethod.Get, ReportUrl(kind, from, to, branch, null));

    public Task<ExportFile> DownloadReportAsync(string kind, DateOnly from, DateOnly to, string? branch, string format) =>
        DownloadAsync(ReportUrl(kind, from, to, branch, format), $"{kind}.{format}");

    private static string ReportUrl(string kind, DateOnly from, DateOnly to, string? branch, string? format) =>
        $"/api/reports/operations/{kind}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}" +
        (string.IsNullOrWhiteSpace(branch) ? "" : $"&branch={Uri.EscapeDataString(branch)}") +
        (format is null ? "" : $"&format={format}");

    // SCRUM-24 · HU-14 (RF-051)
    public Task<ExportFile> DownloadReceiptsAsync(DateOnly from, DateOnly to, string? device) =>
        DownloadAsync($"/api/reports/receipts?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}" +
            (string.IsNullOrWhiteSpace(device) ? "" : $"&device={Uri.EscapeDataString(device)}"), "recibos.pdf");

    // SCRUM-32 · HU-19
    public Task<DashboardSummary> GetDashboardAsync(string groupBy) =>
        SendAsync<DashboardSummary>(HttpMethod.Get, $"/api/dashboard?groupBy={groupBy}");

    // SCRUM-33 · HU-28
    public Task<CompanyProfileDto> GetCompanyAsync() =>
        SendAsync<CompanyProfileDto>(HttpMethod.Get, "/api/settings/company");

    public Task<CompanyProfileDto> SaveCompanyAsync(CompanyProfileDto profile) =>
        SendAsync<CompanyProfileDto>(HttpMethod.Put, "/api/settings/company", profile);

    // SCRUM-34 · HU-29
    public Task<List<BranchDeviceDto>> GetBranchDevicesAsync() =>
        SendAsync<List<BranchDeviceDto>>(HttpMethod.Get, "/api/branches/devices");

    public Task<BranchDeviceDto> AssignBranchAsync(BranchDeviceDto request) =>
        SendAsync<BranchDeviceDto>(HttpMethod.Put, "/api/branches/devices", request);

    public Task<BranchSummaryDto> GetBranchSummaryAsync(string branch, DateOnly from, DateOnly to) =>
        SendAsync<BranchSummaryDto>(HttpMethod.Get,
            $"/api/branches/{Uri.EscapeDataString(branch)}/summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    // SCRUM-28 · HU-24
    public Task<List<CustomerDto>> SearchCustomersAsync(string search) =>
        SendAsync<List<CustomerDto>>(HttpMethod.Get, $"/api/customers?search={Uri.EscapeDataString(search)}");

    public Task<CustomerDto> CreateCustomerAsync(CustomerRequest request) =>
        SendAsync<CustomerDto>(HttpMethod.Post, "/api/customers", request);

    public Task<CustomerDto> UpdateCustomerAsync(Guid id, CustomerRequest request) =>
        SendAsync<CustomerDto>(HttpMethod.Put, $"/api/customers/{id}", request);

    public Task<List<CustomerPurchaseDto>> GetCustomerPurchasesAsync(Guid id) =>
        SendAsync<List<CustomerPurchaseDto>>(HttpMethod.Get, $"/api/customers/{id}/purchases");

    // SCRUM-27 · HU-17
    public Task<InvoiceDto> IssueInvoiceAsync(Guid saleId, InvoiceRequest request) =>
        SendAsync<InvoiceDto>(HttpMethod.Post, $"/api/sales/{saleId}/invoice", request);

    public Task<ExportFile> DownloadInvoiceAsync(int number) =>
        DownloadAsync($"/api/invoices/{number}/pdf", $"factura-{number:D6}.pdf");

    // SCRUM-36 · HU-23
    public Task<List<BackupDto>> GetBackupsAsync() => SendAsync<List<BackupDto>>(HttpMethod.Get, "/api/backups");

    public Task<BackupDto> CreateBackupAsync() => SendAsync<BackupDto>(HttpMethod.Post, "/api/backups");

    public Task<BackupDto> RestoreBackupAsync(string name) =>
        SendAsync<BackupDto>(HttpMethod.Post, $"/api/backups/{Uri.EscapeDataString(name)}/restore",
            new RestoreRequest(true));

    private async Task<ExportFile> DownloadAsync(string url, string fallbackName)
    {
        using var client = factory.CreateClient("gateway");
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (session.Token is null) throw new ApiException(401, "La sesión no tiene conexión con el servidor.");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new ApiException((int)response.StatusCode, await ReadProblemAsync(response, default));
        var name = response.Content.Headers.ContentDisposition?.FileNameStar
                   ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                   ?? fallbackName;
        return new ExportFile(await response.Content.ReadAsByteArrayAsync(), name, false);
    }
}
