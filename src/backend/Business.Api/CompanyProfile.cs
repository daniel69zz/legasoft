using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Common;

namespace Business.Api;

/// <summary>
/// SCRUM-33 · HU-28 (RF-097, RF-098): datos de la empresa y de la sucursal, guardados en inventory.settings.
/// Llegan a los puestos con la sincronización para imprimirlos en los recibos.
/// </summary>
public static class CompanyProfile
{
    private static readonly string[] Keys =
        ["CompanyName", "CompanyTaxId", "CompanyAddress", "CompanyPhone", "BranchName", "BranchAddress"];

    public static async Task<CompanyProfileDto> GetAsync(BusinessDb db)
    {
        var values = await db.Settings.Where(s => Keys.Contains(s.Key)).ToDictionaryAsync(s => s.Key, s => s.Value);
        var d = CompanyProfileDto.Default;
        string Read(string key, string fallback) => values.TryGetValue(key, out var v) && v.Length > 0 ? v : fallback;
        return new CompanyProfileDto(Read("CompanyName", d.CompanyName), Read("CompanyTaxId", d.CompanyTaxId),
            Read("CompanyAddress", d.CompanyAddress), Read("CompanyPhone", d.CompanyPhone),
            Read("BranchName", d.BranchName), Read("BranchAddress", d.BranchAddress));
    }

    public static void Validate(CompanyProfileDto p)
    {
        if (string.IsNullOrWhiteSpace(p.CompanyName)) throw new DomainException("Indique el nombre de la empresa (RF-097).");
        if (string.IsNullOrWhiteSpace(p.BranchName)) throw new DomainException("Indique el nombre de la sucursal (RF-098).");
        if (p.CompanyTaxId.Length > 0 && !p.CompanyTaxId.All(char.IsDigit))
            throw new DomainException("El NIT de la empresa solo lleva números.");
    }

    public static async Task SaveAsync(BusinessDb db, CompanyProfileDto p, string userName, DateTimeOffset now)
    {
        var values = new Dictionary<string, string>
        {
            ["CompanyName"] = p.CompanyName, ["CompanyTaxId"] = p.CompanyTaxId, ["CompanyAddress"] = p.CompanyAddress,
            ["CompanyPhone"] = p.CompanyPhone, ["BranchName"] = p.BranchName, ["BranchAddress"] = p.BranchAddress,
        };
        foreach (var (key, value) in values)
        {
            var entry = await db.Settings.FindAsync(key);
            if (entry is null) db.Settings.Add(entry = new SettingEntry { Key = key });
            entry.Value = value.Trim();
            entry.UpdatedAt = now;
            entry.UpdatedBy = userName;
        }
    }
}
