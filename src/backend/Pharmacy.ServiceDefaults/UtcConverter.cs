using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Pharmacy.ServiceDefaults;

/// <summary>Los puestos envían la hora local de La Paz (−04:00); PostgreSQL timestamptz exige UTC.</summary>
public sealed class UtcConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(
    v => v.ToUniversalTime(), v => v);
