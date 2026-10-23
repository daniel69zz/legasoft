namespace Pharmacy.Domain.Identity;

/// <summary>Actores del SRS (Documento 2, sección 2.2) que inician sesión en el sistema.</summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string GerenteGeneral = "GerenteGeneral";
    public const string Contadora = "Contadora";
    public const string GerenteOperaciones = "GerenteOperaciones";
    public const string Cajero = "Cajero";
    public const string Farmaceutico = "Farmaceutico";
    public const string Almacen = "Almacen";

    public static readonly IReadOnlyList<string> All =
        [Administrador, GerenteGeneral, Contadora, GerenteOperaciones, Cajero, Farmaceutico, Almacen];

    public static bool IsValid(string role) => All.Contains(role);
}
