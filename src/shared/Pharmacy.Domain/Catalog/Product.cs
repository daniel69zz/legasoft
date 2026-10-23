using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Catalog;

/// <summary>Medicamento o producto comercializado (RF-009 a RF-012, RF-017, RF-109).</summary>
public class Product
{
    private Product() { }

    public Product(Guid id, string code, string name, string activeIngredient, string concentration,
        string presentation, string laboratory, string category, bool requiresPrescription,
        decimal salePrice, int minimumStock, string location = "")
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("El código del producto es obligatorio.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre comercial es obligatorio.");
        if (salePrice <= 0) throw new DomainException("El precio de venta debe ser mayor a cero.");
        if (minimumStock < 0) throw new DomainException("El stock mínimo no puede ser negativo.");

        Id = id;
        Code = code.Trim();
        Name = name.Trim();
        ActiveIngredient = activeIngredient;
        Concentration = concentration;
        Presentation = presentation;
        Laboratory = laboratory;
        Category = category;
        RequiresPrescription = requiresPrescription;
        SalePrice = MoneyMath.Round(salePrice);
        MinimumStock = minimumStock;
        Location = location;
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string ActiveIngredient { get; private set; } = "";
    public string Concentration { get; private set; } = "";
    public string Presentation { get; private set; } = "";
    public string Laboratory { get; private set; } = "";
    public string Category { get; private set; } = "";
    public bool RequiresPrescription { get; private set; }
    public decimal SalePrice { get; private set; }
    public int MinimumStock { get; private set; }
    public string Location { get; private set; } = "";
    public bool IsActive { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public string DisplayName => $"{Name} {Concentration} · {Presentation}".Trim();

    public void ChangePrice(decimal newPrice, DateTimeOffset now)
    {
        if (newPrice <= 0) throw new DomainException("El precio de venta debe ser mayor a cero.");
        SalePrice = MoneyMath.Round(newPrice);
        UpdatedAt = now;
    }

    public void SetActive(bool active, DateTimeOffset now)
    {
        IsActive = active;
        UpdatedAt = now;
    }

    /// <summary>Aplica en el puesto un cambio de catálogo descargado del servidor.</summary>
    public void ApplyCatalogChange(string name, decimal salePrice, int minimumStock, bool requiresPrescription,
        bool isActive, string location, DateTimeOffset updatedAt)
    {
        Name = name;
        SalePrice = salePrice;
        MinimumStock = minimumStock;
        RequiresPrescription = requiresPrescription;
        IsActive = isActive;
        Location = location;
        UpdatedAt = updatedAt;
    }

    public bool Matches(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return true;
        return Code.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || ActiveIngredient.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Category.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
