using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.UnitTests;

internal static class TestData
{
    public static readonly DateOnly Today = new(2026, 10, 14);
    public static readonly DateTimeOffset Now = new(2026, 10, 14, 10, 30, 0, TimeSpan.FromHours(-4));

    public static Product Product(decimal price = 2.50m, bool prescription = false, int minimum = 10) =>
        new(Guid.NewGuid(), "P-0001", "Amoxicilina", "Amoxicilina", "500 mg", "Cápsula", "Lab", "Antibióticos",
            prescription, price, minimum);

    public static Lot Lot(Product product, string number, int daysToExpire, int quantity) =>
        new(Guid.NewGuid(), product.Id, number, Today.AddDays(daysToExpire), quantity);
}
