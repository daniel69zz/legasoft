# Arquitectura

Arquitectura de software del Sistema de Gestión para Farmacéutica, descrita con
el modelo C4 (niveles 1 al 3) y registrada con decisiones de arquitectura (ADR).

| Archivo | Código | Contenido |
| --- | --- | --- |
| `DOC3-arquitectura-modelo-c4-v3.pdf` | `LGS-ARQ-001` | Documento 3 — Arquitectura de software, modelo C4 (versión entregada) |
| `DOC3-arquitectura-modelo-c4-v3.docx` | `LGS-ARQ-001` | Fuente editable del Documento 3 |
| `documento-arquitectura.tex` | — | Plantilla LaTeX institucional de arquitectura |
| `adr-plantilla.tex` | `LGS-ADR-NNN` | Plantilla LaTeX institucional de ADR |

Los diagramas (Figuras 1 a 4) están en [`modelos/c4/`](../../modelos/c4/).

## Visión general

Arquitectura **local-first**: cada computadora de la farmacia ejecuta una
aplicación de escritorio con su propia base SQLite, que le permite seguir
vendiendo, operando la caja y consultando inventario sin Internet. En la nube,
servicios ASP.NET Core concentran la seguridad, la sincronización, las
notificaciones y la información consolidada en PostgreSQL.

| Entorno | Elementos |
| --- | --- |
| Lógica compartida | `Pharmacy.Domain` y `Pharmacy.Application` (C#): ventas, FEFO, inventario, caja y validaciones |
| Local (por puesto) | Aplicación Desktop Avalonia (MVVM), SQLite local, Outbox y Sync Worker |
| Cloud (Docker) | API Gateway (YARP), Sync Service, Identity Service, Business Service, Notification Service, servidor de actualizaciones Velopack |
| Datos centrales | PostgreSQL administrado con schemas `sales`, `inventory`, `cash`, `identity`, `audit` y `sync` |
| Externo | Servicio de correo (SMTP con TLS). Todos los pagos son en efectivo: no hay pasarela de pagos ni conexión bancaria |

## Stack tecnológico

| Capa | Tecnología |
| --- | --- |
| Desktop | C# .NET 8 + Avalonia UI, MVVM |
| Acceso a datos local | SQLite + Entity Framework Core / Dapper |
| Backend | ASP.NET Core Web API (.NET 8) en Docker |
| Base central | PostgreSQL administrado |
| Sincronización | Outbox Pattern + Sync Worker + Sync Service |
| Autenticación | ASP.NET Core Identity + JWT / refresh tokens |
| Actualizaciones | Velopack |
| Logging / validación / pruebas | Serilog, FluentValidation, xUnit |

## Contenido del Documento 3

1. Introducción y enfoque del modelo C4.
2. Visión general: entornos, lógica compartida, stack y drivers.
3. C1 — Contexto: frontera, personas y sistemas externos.
4. C2 — Contenedores: locales, cloud, almacenamiento, protocolos y operación con y sin conexión.
5. C3 — Componentes de la Aplicación Desktop y flujo de una venta sin conexión.
6. C3 — Componentes del Backend Cloud, granularidad de servicios y acceso por schema.
7. Sincronización local–cloud: flujo, mensaje Outbox, versiones, idempotencia y conflictos.
8. ADR-001 a ADR-008 (ver [`adr/`](adr/)).
9. Relación con los requisitos y atributos de calidad (ver [`docs/trazabilidad/`](../trazabilidad/)).
10. Conclusiones.
