# Sprint Planning — Sprint 2

**Proyecto:** Sistema de Gestión para Farmacéutica (Legasoft)
**Tablero:** Jira `SCRUM` (PROYECTO-FARMACIA) · Sprint 2 (id 1)
**Sprint:** del viernes 13 al viernes 27 de noviembre de 2026 (2 semanas, 10 días hábiles)
**Documentos de entrada:** Product Backlog en Jira, revisión y retrospectiva del Sprint 1, SRS v2 (`docs/srs/requisitos.md`), Documento 3 (C4 y ADR-001 a ADR-008)

## Asistentes

| Integrante | Rol Scrum | Rol técnico |
| --- | --- | --- |
| Oziel Rodman Ramos Torrez | Scrum Master / Product Owner delegado | Analista de sistemas |
| Luis Daniel Rojas Cáceres | Developer | Arquitecto y backend |
| Pedro Andrez Condorena Apaza | Developer | Frontend y QA |

---

## 0. Antes del planning

| # | Acción | Responsable |
| --- | --- | --- |
| 1 | ✅ Sprint 1 cerrado en Jira (13/11) | Oziel |
| 2 | ✅ Historias divididas creadas: **SCRUM-101 HU-06.3** (épica SCRUM-6, desde SCRUM-16) y **SCRUM-100 HU-T02.2** (épica SCRUM-5, desde SCRUM-14) | Oziel |
| 3 | Volver a estimar los SP de SCRUM-16 y SCRUM-14 según el alcance que les queda (la división está comentada en cada historia) | Equipo |

## 1. ¿Por qué? — Objetivo del sprint

> **Que la farmacia se abastezca dentro del sistema: el administrador registra proveedores, el gerente de operaciones registra compras y su recepción con lote y vencimiento, y el inventario sube solo con lo que llegó en buen estado; las correcciones manuales quedan registradas con su motivo y las comunicaciones con la nube van cifradas.**

En el Sprint 1 la venta, la caja y el FEFO ya funcionan, pero los lotes solo
existen en los datos de demostración. Sin compras ni recepción el stock no se
puede reponer de verdad, así que el sistema todavía no sirve para el día a día de
la farmacia. Este sprint completa el ciclo **compra → lote → venta FEFO**. Además
cierra un pendiente que viene del Sprint 1: TLS (RNF-003).

## 2. ¿Qué? — Sprint Backlog

Estimación con Planning Poker (escala Fibonacci). Capacidad: **20 SP**. El Sprint
1 comprometió 16 SP y terminó todos, así que esa es la velocidad de referencia;
los 48 SP incorporados al cierre ya estaban hechos en el prototipo y no cuentan
para la velocidad.

| Clave | Tipo | Elemento | RF / RNF | MoSCoW | Responsable | SP |
| --- | --- | --- | --- | --- | --- | --- |
| SCRUM-100 | Historia | HU-T02.2 TLS 1.2+ entre los puestos y la nube | RNF-003 | Must | Luis | 2 |
| SCRUM-18 | Historia | HU-08 Gestión de proveedores | RF-026 a RF-029 | Must (RF-028, RF-029: Should) | Luis | 3 |
| SCRUM-19 | Historia | HU-09 Registro de compras y recepción de productos | RF-030 a RF-033, RF-020, RF-107, RF-108 | Must | Luis | 8 |
| SCRUM-101 | Historia | HU-06.3 Entradas, salidas y ajustes manuales de inventario con motivo | RF-014 a RF-016 | Must | Pedro | 3 |
| | | | | | **Total** | **16** |

**Ocupación:** 16 SP de 20 = **80 %**. Se deja una holgura del 20 % porque
HU-09 es la historia más grande que ha tomado el equipo y toca dominio, base de
datos, sincronización y pantalla.

**Fuera de este sprint:** RF-007 (equipos autorizados también para el
administrador), entrega de caja entre cajeros (resto de HU-16), recetas como
entidad (resto de HU-25), descuentos (HU-27), factura simulada (HU-17),
exportación de reportes (HU-18) y precios por proveedor (HU-10). HU-10 es la
candidata natural para el Sprint 3, porque depende de los proveedores y compras de
este sprint.

## 3. ¿Cómo? — Tareas (subtareas en Jira)

Capacidad por integrante: 4 h/día × 10 días − 8 h de eventos Scrum = **32 h**.
Cada tarea es una subtarea en Jira (SCRUM-103 a SCRUM-120). Como el proyecto no
tiene seguimiento de tiempo, las horas están en la descripción de cada subtarea.

### SCRUM-100 · HU-T02.2 TLS en las comunicaciones (2 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-103 | Certificado y HTTPS en el VPS con TLS 1.2 como mínimo; HTTP sin cifrar rechazado o redirigido | Luis | 4 |
| SCRUM-104 | El Desktop usa `https://` para el Gateway y rechaza certificados no válidos fuera de desarrollo | Pedro | 2 |
| SCRUM-105 | Ejecutar CP-RNF-003 por inspección (`openssl s_client -tls1_1` rechazado y `-tls1_2` aceptado) y guardar la evidencia | Oziel | 2 |

### SCRUM-18 · HU-08 Proveedores (3 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-106 | Entidad `Supplier`, migración EF Core y API: registrar, modificar, listar y productos suministrados (RF-026 a RF-028) | Luis | 5 |
| SCRUM-107 | Pantalla de proveedores en el Desktop: listado, formulario y productos suministrados (solo Administrador) | Pedro | 6 |
| SCRUM-108 | Consulta del historial de compras por proveedor (RF-029; se hace después de SCRUM-110) | Luis | 2 |
| SCRUM-109 | Pruebas xUnit con `[Trait]` y ejecución de CP-RF-026 a CP-RF-029 | Oziel | 3 |

### SCRUM-19 · HU-09 Compras y recepción (8 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-110 | Dominio en `Pharmacy.Domain`: `Purchase` con líneas y total calculado (RF-031) y `Reception` con cantidades recibidas, faltantes y dañadas (RF-107, RF-108), más la migración | Luis | 6 |
| SCRUM-111 | API de compras y de confirmación de la recepción: crea los lotes con su número y vencimiento (RF-020) y registra un movimiento de entrada solo por las unidades en buen estado (RF-033) | Luis | 6 |
| SCRUM-112 | Bajada de los lotes nuevos hacia los puestos en la sincronización, para que lo recibido se pueda vender con FEFO (pendiente de HU-22.1) | Luis | 5 |
| SCRUM-113 | Pantalla de registro de compra: proveedor, productos, cantidades, costo y total | Pedro | 6 |
| SCRUM-114 | Pantalla de recepción: lote, vencimiento, recibidas y dañadas por línea, con la diferencia frente al pedido resaltada | Pedro | 7 |
| SCRUM-115 | Pruebas xUnit de dominio e integración y ejecución de CP-RF-030 a CP-RF-033, CP-RF-107 y CP-RF-108 | Oziel | 6 |

### SCRUM-101 · HU-06.3 Movimientos y ajustes de inventario (3 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-116 | API de entrada, salida y ajuste manual por lote, con motivo obligatorio y registro de usuario, fecha y hora (RF-014 a RF-016) | Oziel | 4 |
| SCRUM-117 | Acción «Ajustar» en la pantalla de Inventario, con el motivo obligatorio y el historial de movimientos del producto | Pedro | 5 |
| SCRUM-118 | Pruebas xUnit y ejecución de CP-RF-014 a CP-RF-016 | Oziel | 3 |

### Transversal

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-119 | Semilla de demostración: dos proveedores y una compra pendiente de recibir | Pedro | 2 |
| SCRUM-120 | Actualizar la matriz de trazabilidad (RF → HU → componente C4 → CP → prueba) y el modelo entidad-relación | Oziel | 2 |

### 3.1 Carga por integrante

| Integrante | Tareas | Horas | Capacidad | Uso |
| --- | --- | --- | --- | --- |
| Luis | SCRUM-103, 106, 108, 110, 111, 112 | 28 | 32 | 88 % |
| Pedro | SCRUM-104, 107, 113, 114, 117, 119 | 28 | 32 | 88 % |
| Oziel | SCRUM-105, 109, 115, 116, 118, 120 | 20 | 32 | 63 % |
| **Equipo** | 18 tareas | **76** | **96** | **79 %** |

Oziel tiene menos horas asignadas porque facilita los eventos Scrum, coordina con
el cliente y revisa los PR del resto del equipo.

### 3.2 Orden de trabajo

| Semana | Foco |
| --- | --- |
| 1 (13–20 nov) | TLS (SCRUM-103 a SCRUM-105), proveedores de punta a punta (SCRUM-106, SCRUM-107, SCRUM-109), dominio de compras (SCRUM-110) y API de ajustes (SCRUM-116) |
| 2 (23–27 nov) | API y pantallas de compra y recepción (SCRUM-111, SCRUM-113, SCRUM-114), bajada de lotes (SCRUM-112), historial del proveedor (SCRUM-108), pantalla de ajustes (SCRUM-117), pruebas y trazabilidad (SCRUM-115, SCRUM-118 a SCRUM-120) |

**Camino crítico:** SCRUM-110 → SCRUM-111 → SCRUM-112 → SCRUM-114. Si SCRUM-110 se retrasa más de dos días,
se corta SCRUM-112 y la mercadería recibida solo se vende en el puesto que tiene
conexión. Así se protege el objetivo del sprint.

## 4. Criterios de aceptación

Los criterios Gherkin completos están en cada historia de Jira. Resumen:

| HU | Escenario principal | Escenarios alternos |
| --- | --- | --- |
| HU-T02.2 | El puesto se conecta al Gateway con TLS 1.2 o superior | Una conexión sin TLS o con TLS 1.1 es rechazada |
| HU-08 | El administrador registra un proveedor y este queda disponible para las compras | Falta un dato obligatorio → se rechaza e indica cuál; el historial lista fecha, productos y total de cada compra |
| HU-09 | Compra de 100 unidades y recepción de 100 con lote y vencimiento → el stock sube 100 y el total se calcula solo | 90 en buen estado y 10 dañadas → se muestra la diferencia y solo ingresan 90 |
| HU-06.3 | Stock de 50 y ajuste de −3 con motivo «Producto dañado» → queda en 47, con usuario, fecha, hora y motivo | Ajuste sin motivo → rechazado y el stock no cambia |

## 5. Definition of Done

La misma del Sprint 1:

1. Criterios de aceptación Gherkin cumplidos y CP ejecutados en verde.
2. Pruebas xUnit con `[Trait("JiraIssue")]` y `[Trait("Requirement")]`.
3. Rama `feature/SCRUM-n-…`, commits `tipo(SCRUM-n): …` y PR con la clave en el título, revisado por otro integrante.
4. CI de GitHub Actions en verde.
5. Documentación actualizada (README, API, trazabilidad).
6. Ticket en *Finalizado* al fusionar el PR.

## 6. Mejoras tomadas de la retrospectiva del Sprint 1

| Mejora | Cómo se aplica en este sprint |
| --- | --- |
| Commits desde una sola cuenta | Cada integrante trabaja sus tareas en su propia rama y abre su propio PR |
| Un solo PR grande al final (#14) | Un PR por historia o tarea técnica, fusionado a `develop` apenas pasa la revisión, para que el burndown baje de forma gradual |
| Alcance incorporado al cierre | Todo lo que entra a mitad del sprint se acuerda en la daily y se registra en la sección 9 |

## 7. Riesgos

| Riesgo | Mitigación | Responsable |
| --- | --- | --- |
| HU-09 (8 SP) no se termina | Camino crítico identificado; SCRUM-112 es lo primero que se corta | Luis |
| Certificado TLS en el VPS (dominio, DNS, renovación) | Let's Encrypt con renovación automática; si no hay dominio a tiempo, certificado propio con la CA instalada en los puestos de demo | Luis |
| Conflictos entre lotes creados en la nube y el stock local de los puestos | La nube es la dueña de los lotes; los puestos los reciben en la sincronización (ADR-002, ADR-003) | Luis |
| Reglas de recepción poco claras (¿quién la confirma?, ¿se puede recibir en partes?) | Validar con el Gerente de Operaciones en la semana 1. Mientras tanto: una sola recepción por compra | Oziel |

## 8. Compromiso

El equipo se compromete con el **objetivo del sprint** y con **16 SP** (SCRUM-100,
18, 19 y 101).

| Integrante | Conformidad |
| --- | --- |
| Oziel Rodman Ramos Torrez | ☐ |
| Luis Daniel Rojas Cáceres | ☐ |
| Pedro Andrez Condorena Apaza | ☐ |

## 9. Cambios posteriores al planning

| Fecha | Cambio | Efecto en el compromiso |
| --- | --- | --- |
| 13/11/2026 | SCRUM-3 (integración GitHub–Jira) se descartó por innecesaria y salió del sprint junto con su subtarea SCRUM-102 | 18 → **16 SP** (80 %) |
| 13/11/2026 | Se incorporaron 11 historias del alcance pendiente (SCRUM-121 a 131, detalle abajo) | +24 SP · **40 SP** en el sprint |

### 9.1 Historias incorporadas después del planning

| Clave | Historia | RF / RNF | MoSCoW | Épica | Responsable | SP |
| --- | --- | --- | --- | --- | --- | --- |
| SCRUM-121 | HU-07.2 Retiro de medicamentos vencidos o dañados | RF-025 | Must | 2 | Pedro | 2 |
| SCRUM-122 | HU-07.3 Consulta de stock disponible por lote | RF-021 | Should | 2 | Pedro | 1 |
| SCRUM-123 | HU-06.4 Historial general de movimientos con filtros | RF-019 | Should | 2 | Luis | 2 |
| SCRUM-124 | HU-10.1 Precio de compra por proveedor e historial | RF-034, RF-035 | Must | 2 | Luis | 3 |
| SCRUM-125 | HU-10.2 Comparación de precios de proveedores | RF-036 | Should | 2 | Pedro | 2 |
| SCRUM-126 | HU-16.2 Entrega de caja entre cajeros con confirmación | RF-061, RF-062 | Must | 3 | Pedro | 3 |
| SCRUM-127 | HU-03.2 Administrador solo en equipos autorizados | RF-007 | Should | 1 | Luis | 2 |
| SCRUM-128 | HU-20.1 Consulta de auditoría con filtros | RF-075 | Must | 1 | Oziel | 2 |
| SCRUM-129 | HU-18.1 Exportación a Excel de ventas, egresos y cierres | RF-105 | Should | 4 | Oziel | 3 |
| SCRUM-130 | HU-28.1 Parámetros operativos de inventario configurables | RF-099 | Should | 4 | Oziel | 2 |
| SCRUM-131 | HU-T05.1 Cobertura de pruebas en la CI con umbral del 70 % | RNF-007 | Should | 5 | Oziel | 2 |
| | | | | | **Total** | **24** |

Cada historia tiene sus subtareas en Jira (**SCRUM-132 a SCRUM-162**: implementación,
pantalla y pruebas, con las horas en la descripción).

**Efecto en la capacidad:** el sprint queda con 40 SP para una capacidad de 20 SP
(200 %). El compromiso del planning sigue siendo de 16 SP; las historias
incorporadas se trabajan después de él, en el orden de la tabla. Las que no se
terminen vuelven al backlog en la Sprint Review y no cuentan para la velocidad.
