namespace Business.Api;

/// <summary>SCRUM-35 · HU-22: publica en el feed el stock vigente de un lote para que lo reciban todos los puestos.</summary>
public static class LotFeed
{
    public static void Publish(BusinessDb db, Pharmacy.Domain.Inventory.Lot lot, DateTimeOffset now) =>
        db.LotFeed.Add(new LotFeedEntry
        {
            LotId = lot.Id, ProductId = lot.ProductId, LotNumber = lot.LotNumber,
            ExpirationDate = lot.ExpirationDate, Quantity = lot.Quantity, CreatedAt = now,
        });
}
