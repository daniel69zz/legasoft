# Sprint Review — Sprint 1

**Sprint:** 2 al 16 de octubre de 2026 · **Revisión:** en la presentación del Sprint 1
**Objetivo:** que cada usuario ingrese con sus credenciales y vea solo las funciones de su rol, y que el farmacéutico mantenga el catálogo único de medicamentos, sobre una base técnica con CI y trazabilidad Jira–GitHub.

## Incremento

### Compromiso del planning (16 SP)

| Clave | Elemento | SP | Evidencia |
| --- | --- | --- | --- |
| SCRUM-69 | Base técnica: solución .NET, Gateway, CI/CD, migraciones y API documentada (reemplaza a SCRUM-1) | 3 | PR #14, #15, #18, #22 y #23 · GitHub Actions en verde |
| SCRUM-2 | HU-01 Inicio de sesión | 3 | `IdentityApiTests` (RF-001) |
| SCRUM-10 | HU-02 Usuarios, roles y permisos | 5 | `IdentityApiTests` (RF-002 a RF-005) · PR #16 |
| SCRUM-15 | HU-05 Catálogo de medicamentos | 5 | `ProductTests`, `CatalogApiTests` (RF-009 a RF-012) · PR #17 |

### Historias incorporadas al cierre (48 SP)

| Clave | Historia | SP | Evidencia |
| --- | --- | --- | --- |
| SCRUM-57 | HU-03.1 Cajeros solo en equipos autorizados, con asistencia | 3 | `IdentityApiTests` (RF-006, RF-008) |
| SCRUM-58 | HU-T02.1 Bloqueo tras 5 intentos fallidos | 2 | `IdentityApiTests` (RNF-004) |
| SCRUM-59 | HU-06.1 Stock vendible con alertas de vencimiento y stock mínimo | 5 | `AlertsAndPermissionsTests`, `FefoAllocatorTests` |
| SCRUM-60 | HU-07.1 Salida FEFO y bloqueo de vencidos | 3 | `FefoAllocatorTests`, `CashierFlowTests` |
| SCRUM-61 | HU-06.2 Alertas de inventario por correo | 3 | Demostración en Mailpit |
| SCRUM-62 | HU-11.1 Venta en efectivo con cambio y recibo | 8 | `SaleTests`, `RegisterSaleHandlerTests`, `CashierFlowTests` |
| SCRUM-63 | HU-25.1 Receta obligatoria | 2 | `SaleTests` (RF-091) |
| SCRUM-64 | HU-13.1 Anulación con motivo | 3 | `SaleTests`, `RegisterSaleHandlerTests` (RF-048) |
| SCRUM-65 | HU-15.1 Apertura de caja y egresos | 3 | `CashSessionTests` |
| SCRUM-66 | HU-16.1 Cierre de caja por denominación | 5 | `CashSessionTests`, `CashierFlowTests` |
| SCRUM-67 | HU-19.1 Dashboard gerencial | 3 | `BusinessApiTests` (RF-073) |
| SCRUM-68 | HU-22.1 Venta sin conexión y sincronización | 8 | `SyncApiTests`, `BusinessApiTests` |

SCRUM-3 (integración GitHub–Jira, 2 SP) se retiró del sprint y sigue en el
backlog.

**Pruebas automatizadas:** 62 en verde (40 unitarias, 21 de integración y 1 de
interfaz). Cada prueba lleva `[Trait("JiraIssue")]` con la historia que verifica
y `[Trait("Requirement")]` con su requisito; se filtran con
`dotnet test --filter "JiraIssue=SCRUM-62"`.

## Métricas

| Métrica | Valor |
| --- | --- |
| Capacidad | 20 SP · 96 h |
| Compromiso inicial | 16 SP (80 %) |
| Incorporado al cierre | 48 SP (SCRUM-57 a 68) |
| Finalizado en Jira | 64 SP |
| Burndown | Captura del informe *Burndown Chart* del Sprint 1 en Jira (muestra el aumento de alcance al incorporar las historias) |

## Demo

Seguir el guion de [`plan-contingencia.md`](plan-contingencia.md#guion-de-la-demo-hu-del-sprint-1).

## Retroalimentación del cliente

| Comentario | Acción en el Product Backlog |
| --- | --- |
| _a registrar durante la revisión_ | |

## Pendiente para el Sprint 2

Lo que las historias incorporadas dejaron fuera de alcance está anotado en
cada HU del SRS en Jira: RF-007, TLS, ajustes de inventario, registro de lotes
desde la pantalla, entrega de caja entre cajeros, recetas como entidad,
proveedores y compras, exportación de reportes y facturación simulada.
