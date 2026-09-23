# SRS — Especificación de requisitos

Requisitos del Sistema de Gestión para Farmacéutica, especificados según
**ISO/IEC/IEEE 29148**.

| Archivo | Código | Contenido |
| --- | --- | --- |
| `DOC2-especificacion-requisitos-srs-v2.pdf` | `LGS-REQ-001` | Documento 2 — SRS v2 (versión entregada) |
| `DOC2-especificacion-requisitos-srs-v2.docx` | `LGS-REQ-001` | Fuente editable del Documento 2 |
| [`requisitos.md`](requisitos.md) | — | Catálogo de RF y RNF en Markdown, para consulta rápida |
| `srs.tex` | — | Plantilla LaTeX institucional de SRS |

## Resumen

| Elemento | Cantidad | Detalle |
| --- | --- | --- |
| Requisitos funcionales | 101 | `RF-001` a `RF-109` — 68 *Must*, 32 *Should*, 1 *Could* |
| Requisitos no funcionales | 8 | `RNF-001` a `RNF-008` — 4 *Must*, 4 *Should* |
| Historias de usuario (Jira) | 33 | `HU-01` a `HU-29` y técnicas `HU-T01` a `HU-T06` |
| Casos de prueba | 109 | Un `CP-RF-NNN` / `CP-RNF-NNN` por requisito |

Fuentes: entrevistas `ENT-001`, `ENT-002`, sesión `VAL-001` y el catálogo de
necesidades `NEC-01` a `NEC-40` de [`docs/entrevistas/`](../entrevistas/).

## Contenido del Documento 2

1. Introducción: propósito, alcance, fuentes y definiciones.
2. Descripción general: contexto, usuarios, 19 módulos, restricciones.
3. Requisitos funcionales con ID, fuente, prioridad MoSCoW y estado.
4. Requisitos no funcionales medibles (rendimiento, seguridad, usabilidad,
   disponibilidad, mantenibilidad y respaldo).
5. Matriz de atributos: riesgo, estabilidad y método de verificación.
6. Matriz de trazabilidad bidireccional (ver [`docs/trazabilidad/`](../trazabilidad/)).
7. Criterios de aceptación y verificación.
8. Estándares y acreditaciones proyectadas.

## Restricciones principales

- **Pago únicamente en efectivo** y **facturación simulada**: sin conexión con
  Impuestos Nacionales ni con entidades bancarias.
- **Operación sin conexión** de ventas, caja e inventario sobre SQLite local,
  con sincronización posterior.
- **Cajeros solo desde computadoras autorizadas.**
- **Prohibida la venta de medicamentos vencidos.**
- Preparado para **múltiples sucursales**.
