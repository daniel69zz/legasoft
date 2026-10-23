# Sprint Review — Sprint 1

**Sprint:** 2 al 16 de octubre de 2026 · **Revisión:** en la presentación del Sprint 1
**Objetivo:** que cada usuario ingrese con sus credenciales y vea solo las funciones de su rol, y que el farmacéutico mantenga el catálogo único de medicamentos, sobre una base técnica con CI y trazabilidad Jira–GitHub.

## Incremento

| Clave | Elemento | SP | Evidencia | Estado en Jira |
| --- | --- | --- | --- | --- |
| SCRUM-2 | HU-01 Inicio de sesión | 3 | PR #14 · `IdentityApiTests` (RF-001) | Finalizado |
| SCRUM-10 | HU-02 Usuarios, roles y permisos | 5 | PR #14 + PR de edición de usuarios · `IdentityApiTests` (RF-002 a RF-005) | En revisión → Finalizado al fusionar |
| SCRUM-15 | HU-05 Catálogo de medicamentos | 5 | PR del catálogo · `ProductTests`, `CatalogApiTests` (RF-009 a RF-012) | En revisión → Finalizado al fusionar |
| SCRUM-1 | Base técnica: solución, Gateway, CI, migraciones, API documentada | 3 | PR #14 + PR de migraciones y Swagger · GitHub Actions en verde | En revisión → Finalizado al fusionar |
| SCRUM-3 | Integración GitHub–Jira | 2 | Panel de desarrollo del ticket y regla de automatización | Pendiente de configurar |

**Pruebas automatizadas:** 62 en verde (40 unitarias, 21 de integración y 1 de
interfaz), todas con `[Trait("JiraIssue")]` y `[Trait("Requirement")]`.

## Métricas

| Métrica | Valor |
| --- | --- |
| Capacidad | 20 SP · 96 h |
| Comprometido | 18 SP (90 %) · 85 h |
| Completado (Finalizado en Jira) | _completar al cerrar el sprint_ |
| Velocidad del Sprint 1 | _SP finalizados_ |
| Burndown | Captura del informe *Burndown Chart* del Sprint 1 en Jira |

## Demo

Seguir el guion de [`plan-contingencia.md`](plan-contingencia.md#guion-de-la-demo-hu-del-sprint-1).

## Retroalimentación del cliente

| Comentario | Acción en el Product Backlog |
| --- | --- |
| _a registrar durante la revisión_ | |

## Avance adelantado para próximos sprints

El prototipo ya incluye parte de HU-03/04 (dispositivos y asistencia), HU-06/07
(inventario, lotes y FEFO), HU-11 a HU-16 (venta, cobro, anulación y caja),
HU-19 (dashboard) y HU-22 (operación sin conexión). Se revisarán y estimarán en
el Sprint 2 Planning; no cuentan como terminadas en este sprint.
