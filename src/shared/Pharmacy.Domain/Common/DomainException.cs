namespace Pharmacy.Domain.Common;

/// <summary>Violación de una regla de negocio. El mensaje se muestra tal cual al usuario.</summary>
public sealed class DomainException(string message) : Exception(message);
