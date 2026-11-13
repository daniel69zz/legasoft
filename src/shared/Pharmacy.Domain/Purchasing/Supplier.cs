using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Purchasing;

/// <summary>Proveedor de medicamentos (SCRUM-18 · HU-08, RF-026 y RF-027).</summary>
public class Supplier
{
    private Supplier() { }

    public Supplier(Guid id, string taxId, string name, string contactName, string phone, string email,
        string address, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(taxId)) throw new DomainException("El NIT del proveedor es obligatorio.");
        Validate(name, phone, email);

        Id = id;
        TaxId = taxId.Trim();
        Name = name.Trim();
        ContactName = contactName.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        Address = address.Trim();
        IsActive = true;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public string TaxId { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string ContactName { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Address { get; private set; } = "";
    public bool IsActive { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Los datos obligatorios se validan por separado para que el mensaje indique cuál falta
    /// (escenario "Datos obligatorios incompletos" de SCRUM-18 · HU-08).
    /// </summary>
    private static void Validate(string name, string phone, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("La razón social del proveedor es obligatoria.");
        if (string.IsNullOrWhiteSpace(phone)) throw new DomainException("El teléfono del proveedor es obligatorio.");
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            throw new DomainException("El correo del proveedor no es válido.");
    }

    /// <summary>RF-027: modifica los datos del proveedor. El NIT no cambia porque lo identifica.</summary>
    public void Update(string name, string contactName, string phone, string email, string address,
        DateTimeOffset now)
    {
        Validate(name, phone, email);
        Name = name.Trim();
        ContactName = contactName.Trim();
        Phone = phone.Trim();
        Email = email.Trim();
        Address = address.Trim();
        UpdatedAt = now;
    }

    public void SetActive(bool active, DateTimeOffset now)
    {
        IsActive = active;
        UpdatedAt = now;
    }

    public bool Matches(string term) =>
        string.IsNullOrWhiteSpace(term)
        || Name.Contains(term, StringComparison.OrdinalIgnoreCase)
        || TaxId.Contains(term, StringComparison.OrdinalIgnoreCase)
        || ContactName.Contains(term, StringComparison.OrdinalIgnoreCase);
}
