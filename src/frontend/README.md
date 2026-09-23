# Frontend

Espacio reservado para el código fuente de la interfaz de usuario. Todavía no
contiene código: el desarrollo comienza en la siguiente etapa del proyecto.

## Tecnología definida

La interfaz es una **aplicación de escritorio local-first** (Pharmacy.Desktop),
descrita en el Documento 3 y en
[ADR-002](../../docs/arquitectura/adr/adr-002-estrategia-persistencia.md),
[ADR-003](../../docs/arquitectura/adr/adr-003-mecanismo-sincronizacion.md) y
[ADR-007](../../docs/arquitectura/adr/adr-007-sqlite-unica-por-puesto.md).

| Elemento | Tecnología |
| --- | --- |
| Aplicación | C# .NET 8 + Avalonia UI con patrón MVVM |
| Base local | SQLite (una por puesto) con EF Core / Dapper |
| Sincronización | Outbox + Sync Worker (BackgroundService) hacia el Sync Service |
| Actualizaciones | Velopack |
