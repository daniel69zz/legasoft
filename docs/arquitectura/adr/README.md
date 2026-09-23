# Registros de decisiones de arquitectura (ADR)

Cada ADR documenta una decisión que condiciona la evolución del sistema: su
contexto, las alternativas evaluadas, la decisión y sus consecuencias. Un ADR
aceptado no se edita; si la decisión cambia, se registra uno nuevo que lo
reemplaza.

| ADR | Decisión | Estado |
| --- | --- | --- |
| [ADR-001](adr-001-framework-backend.md) | ASP.NET Core (.NET 8 LTS) para los servicios cloud | Aceptado |
| [ADR-002](adr-002-estrategia-persistencia.md) | SQLite local por puesto y PostgreSQL administrado como fuente central | Aceptado |
| [ADR-003](adr-003-mecanismo-sincronizacion.md) | Sincronización mediante Outbox Pattern y Sync Worker | Aceptado |
| [ADR-004](adr-004-logica-dominio-compartida.md) | Bibliotecas `Pharmacy.Domain` y `Pharmacy.Application` compartidas | Aceptado |
| [ADR-005](adr-005-postgresql-schemas-por-dominio.md) | Una instancia PostgreSQL con schemas por dominio | Aceptado |
| [ADR-006](adr-006-servicios-cloud-separados.md) | Backend separado en servicios con fronteras claras | Aceptado |
| [ADR-007](adr-007-sqlite-unica-por-puesto.md) | Una única base SQLite local por computadora | Aceptado |
| [ADR-008](adr-008-sync-service-fachada.md) | Sync Service como punto único de sincronización | Aceptado |

Fuente: Documento 3 v3, sección 8
([`../DOC3-arquitectura-modelo-c4-v3.pdf`](../DOC3-arquitectura-modelo-c4-v3.pdf)).
Para nuevos ADR se puede usar la plantilla [`../adr-plantilla.tex`](../adr-plantilla.tex).
