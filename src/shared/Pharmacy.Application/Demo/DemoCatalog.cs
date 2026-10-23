using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Application.Demo;

/// <summary>
/// Catálogo de demostración con identificadores fijos, para que el puesto y el
/// servidor partan de los mismos productos y lotes. Los laboratorios son ficticios.
/// </summary>
public static class DemoCatalog
{
    public static IReadOnlyList<Product> Products() =>
    [
        P(1, "Paracetamol", "Paracetamol", "500 mg", "Tableta", "Lab. Illimani", "Analgésicos", false, 0.50m, 100, "Estante A1"),
        P(2, "Ibuprofeno", "Ibuprofeno", "400 mg", "Tableta", "Lab. Andino", "Analgésicos", false, 1.00m, 80, "Estante A1"),
        P(3, "Amoxicilina", "Amoxicilina", "500 mg", "Cápsula", "Lab. Sajama", "Antibióticos", true, 2.50m, 60, "Estante B2"),
        P(4, "Loratadina", "Loratadina", "10 mg", "Tableta", "Lab. Illimani", "Antialérgicos", false, 1.50m, 50, "Estante A3"),
        P(5, "Omeprazol", "Omeprazol", "20 mg", "Cápsula", "Lab. Andino", "Gastrointestinales", false, 1.20m, 60, "Estante C1"),
        P(6, "Suero oral", "Sales de rehidratación oral", "27,9 g", "Sobre", "Lab. Tunari", "Hidratación", false, 4.00m, 20, "Estante C2"),
        P(7, "Salbutamol", "Salbutamol", "100 mcg", "Inhalador", "Lab. Sajama", "Respiratorios", true, 45.00m, 5, "Vitrina 1"),
        P(8, "Alcohol en gel", "Etanol 70 %", "250 ml", "Frasco", "Lab. Tunari", "Higiene", false, 18.00m, 10, "Góndola 2"),
        P(9, "Diclofenaco", "Diclofenaco sódico", "50 mg", "Tableta", "Lab. Andino", "Analgésicos", false, 0.80m, 80, "Estante A2"),
        P(10, "Metformina", "Metformina", "850 mg", "Tableta", "Lab. Illimani", "Antidiabéticos", true, 1.00m, 90, "Estante B1"),
    ];

    /// <summary>Lotes relativos a <paramref name="today"/>: incluye uno vencido, varios por vencer y stock bajo.</summary>
    public static IReadOnlyList<Lot> Lots(DateOnly today) =>
    [
        L(1, 1, "PCM-2401", today.AddDays(25), 120),
        L(2, 1, "PCM-2409", today.AddDays(400), 500),
        L(3, 2, "IBU-2312", today.AddDays(-10), 40),
        L(4, 2, "IBU-2410", today.AddDays(300), 200),
        L(5, 3, "AMX-2405", today.AddDays(45), 30),
        L(6, 3, "AMX-2411", today.AddDays(500), 150),
        L(7, 4, "LOR-2402", today.AddDays(200), 35),
        L(8, 5, "OME-2406", today.AddDays(365), 180),
        L(9, 6, "SRO-2403", today.AddDays(90), 60),
        L(10, 7, "SAL-2407", today.AddDays(540), 12),
        L(11, 8, "GEL-2404", today.AddDays(15), 8),
        L(12, 9, "DCF-2408", today.AddDays(250), 240),
        L(13, 10, "MET-2409", today.AddDays(330), 300),
    ];

    private static Product P(int n, string name, string ingredient, string concentration, string presentation,
        string laboratory, string category, bool prescription, decimal price, int minimum, string location) =>
        new(ProductId(n), $"P-{n:D4}", name, ingredient, concentration, presentation, laboratory, category,
            prescription, price, minimum, location);

    private static Lot L(int n, int product, string number, DateOnly expiration, int quantity) =>
        new(new Guid($"00000000-0000-0000-0002-{n:D12}"), ProductId(product), number, expiration, quantity);

    public static Guid ProductId(int n) => new($"00000000-0000-0000-0001-{n:D12}");
}
