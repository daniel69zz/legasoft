# Backend

Código fuente de los servicios cloud y de la lógica de negocio compartida.

## Tecnología definida

Registrada en [ADR-001](../../docs/arquitectura/adr/adr-001-framework-backend.md),
[ADR-004](../../docs/arquitectura/adr/adr-004-logica-dominio-compartida.md) y
[ADR-006](../../docs/arquitectura/adr/adr-006-servicios-cloud-separados.md).

| Elemento | Tecnología |
| --- | --- |
| Servicios cloud | ASP.NET Core Web API (.NET 8), una imagen Docker por servicio |
| Punto de entrada | API Gateway con YARP |
| Servicios | Sync Service, Identity Service, Business Service y Notification Service |
| Lógica compartida con el Desktop | Bibliotecas `Pharmacy.Domain` y `Pharmacy.Application` |
| Base de datos | PostgreSQL administrado con schemas por dominio (EF Core / Npgsql) |
| Autenticación | ASP.NET Core Identity + JWT / refresh tokens |
| Logging y validación | Serilog y FluentValidation |
