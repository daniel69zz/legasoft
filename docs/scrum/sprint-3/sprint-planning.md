# Sprint Planning — Sprint 3

**Proyecto:** Sistema de Gestión para Farmacéutica (Legasoft)
**Tablero:** Jira `SCRUM` (PROYECTO-FARMACIA) · Sprint 3 (id 3)
**Sprint:** del sábado 28 de noviembre al viernes 11 de diciembre de 2026 (2 semanas, 10 días hábiles)
**Documentos de entrada:** Product Backlog en Jira, revisión del Sprint 2 (`docs/scrum/sprint-2/`), SRS v2 (`docs/srs/requisitos.md`), Documento 3 (C4 y ADR-001 a ADR-008)

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
| 1 | ✅ Sprint 2 cerrado en Jira (27/11) con las 15 historias en *Finalizado* | Oziel |
| 2 | ✅ Sprint 3 creado e iniciado con las 28 historias del SRS que seguían en *Por hacer* | Oziel |
| 3 | ✅ Responsable asignado a cada historia en Jira | Oziel |
| 4 | ✅ Revisión de cada historia contra el código: qué requisitos ya cubren las historias divididas de los sprints 1 y 2 y qué falta (sección 2) | Equipo |
| 5 | Verificar y pasar a *Finalizado* las 7 historias ya cubiertas (sección 2.1) | Responsable de cada una |
| 6 | Validar con el cliente las reglas pendientes de HU-25 (qué es una receta válida) y HU-27 (quién autoriza los descuentos y con qué límite) | Oziel |

## 1. ¿Por qué? — Objetivo del sprint

> **Que la farmacia pueda operar con más de una caja y sin miedo a perder datos: el stock coincide entre los puestos, la información se respalda y se puede recuperar, la contadora obtiene sus reportes del período y del cierre diario, y se cierran los huecos de seguridad y auditoría que quedaron de los sprints anteriores.**

Al cierre del Sprint 2 el sistema ya cubre el ciclo compra → lote → venta FEFO →
caja, y está desplegado en producción (`https://farmacia.bell7738.com`). La
revisión del Sprint 2 dejó tres problemas que impiden usarlo de verdad en la
farmacia:

1. **Dos cajas no comparten el stock.** Cada puesto solo recibe del servidor la
   cantidad de un lote en su primera descarga y cuando entra un lote nuevo; las
   ventas, ajustes y retiros de un puesto no llegan al otro, que puede vender
   unidades que ya no existen.
2. **No hay respaldos.** La base central del VPS no tiene copia diaria ni
   procedimiento de recuperación.
3. **Reportes incompletos.** Solo existe la exportación a Excel de ventas, egresos
   y cierres; faltan los reportes por rango, el cierre diario consolidado y el PDF.

## 2. ¿Qué? — Sprint Backlog

Las 28 historias del sprint son las historias originales del SRS. Varias ya se
avanzaron con historias divididas en los sprints 1 y 2 (por ejemplo HU-03 con
HU-03.1 y HU-03.2), así que se revisó cada una contra el código y quedaron en tres
grupos: **cubiertas** (solo falta cerrarlas), **comprometidas** (lo que el equipo
se compromete a terminar) y **adicionales** (se trabajan si sobra capacidad).

Estimación con Planning Poker (Fibonacci) **solo del alcance que falta**.

**Capacidad: 30 SP.** El Sprint 1 terminó 16 SP y el Sprint 2 terminó 40 SP (16
comprometidos y 24 incorporados). Se toma un punto intermedio porque el Sprint 3
tiene historias de infraestructura (respaldos, sincronización entre puestos) con
más incertidumbre que las pantallas del Sprint 2.

### 2.1 Historias ya cubiertas (0 SP)

Todos sus criterios se cumplen con historias de sprints anteriores. Se verifican
con las pruebas existentes y se pasan a *Finalizado* en la primera semana.

| Clave | Historia | Cubierta por | Responsable |
| --- | --- | --- | --- |
| SCRUM-12 | HU-04 Asistencia automática del cajero | HU-03.1 (SCRUM-57) | Oziel |
| SCRUM-16 | HU-06 Inventario, stock mínimo y ajustes | HU-06.1 a HU-06.4 (SCRUM-59, 61, 101, 123) y el catálogo (stock mínimo y ubicación, HU-05) | Oziel |
| SCRUM-17 | HU-07 Lotes, vencimientos y FEFO | HU-07.1 a HU-07.3 (SCRUM-60, 121, 122) y HU-06.2 (SCRUM-61) | Oziel |
| SCRUM-20 | HU-10 Precios por proveedor | HU-10.1 y HU-10.2 (SCRUM-124, 125) | Luis |
| SCRUM-21 | HU-11 Registro de venta | HU-11.1 (SCRUM-62) | Pedro |
| SCRUM-23 | HU-13 Anulación de venta con motivo | HU-13.1 (SCRUM-64) | Pedro |
| SCRUM-26 | HU-16 Cierre y entrega de caja | HU-16.1 y HU-16.2 (SCRUM-66, 126) | Pedro |

### 2.2 Compromiso del sprint (30 SP)

| Clave | Historia | Lo que falta | RF / RNF | MoSCoW | Responsable | SP |
| --- | --- | --- | --- | --- | --- | --- |
| SCRUM-35 | HU-22 Operación sin conexión | **Stock compartido entre puestos:** el servidor publica cada cambio de stock (venta, ajuste, retiro) y el puesto toma la cantidad del servidor menos sus movimientos aún no enviados. Prueba con dos puestos que venden el mismo lote | RF-080, RF-081, RF-102 | Must | Luis | 5 |
| SCRUM-36 | HU-23 Respaldo y recuperación | Respaldo a pedido del administrador con fecha y hora, y recuperación desde un respaldo autorizado; otros roles reciben 403 | RF-082, RF-083 | Should | Luis | 5 |
| SCRUM-13 | HU-20 Auditoría de operaciones críticas | La bitácora no se puede modificar ni borrar (permisos de la base y sin endpoints de edición); auditar los descuentos cuando existan | RF-075 | Must | Luis | 2 |
| SCRUM-14 | HU-T02 Seguridad: TLS y bloqueo | Cloudflare acepta TLS 1.1 en el borde: fijar *Minimum TLS Version 1.2* y repetir CP-RNF-003 con evidencia | RNF-003, RNF-004 | Must | Luis | 1 |
| SCRUM-31 | HU-18 Reportes y cierre diario | Reportes por rango de ventas, compras, inventario, stock bajo y vencimientos; cierre diario consolidado de todos los turnos; exportación a PDF; rango inválido rechazado; aviso de que requiere conexión | RF-068 a RF-072, RF-103, RF-104 | Must | Pedro | 8 |
| SCRUM-25 | HU-15 Apertura, movimientos y arqueo | Arqueo durante el turno (RF-056): conteo parcial sin cerrar la caja, con la diferencia del momento | RF-056 | Must | Pedro | 2 |
| SCRUM-22 | HU-12 Cobro en efectivo | Con monto insuficiente, mostrar cuánto falta («faltan Bs 7,50») además de impedir el cobro | RF-044, RF-045 | Must | Pedro | 1 |
| SCRUM-41 | HU-T06 Respaldo diario (RPO 24 h, RTO 4 h) | `pg_dump` diario programado en el VPS con retención, aviso por correo si falla y simulacro de restauración cronometrado | RNF-008 | Should | Oziel | 3 |
| SCRUM-39 | HU-T04 Continuidad operativa local | Prueba de corte prolongado (ventas, caja e inventario sin conexión), sincronización en menos de 5 minutos al volver y aviso en funciones que requieren conexión, con evidencia | RNF-006 | Must | Oziel | 2 |
| SCRUM-11 | HU-03 Dispositivos autorizados | Registrar en auditoría el intento de un cajero desde un equipo no autorizado (hoy solo se audita el del administrador) | RF-006 | Must | Oziel | 1 |
| | | | | | **Total** | **30** |

**Ocupación:** 30 SP de 30 = **100 %**. Si la historia de stock compartido
(SCRUM-35) se complica, lo primero que se corta es la exportación a PDF de HU-18.

### 2.3 Adicionales si sobra capacidad (44 SP)

En este orden. Las que no se terminen vuelven al backlog en la Sprint Review.

| # | Clave | Historia | Lo que falta | MoSCoW | Responsable | SP |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | SCRUM-32 | HU-19 Dashboard y comparativo | Agrupación del comparativo por semana y por mes (hoy solo por día) | Should | Oziel | 2 |
| 2 | SCRUM-33 | HU-28 Empresa, sucursal y parámetros | Datos de la empresa y de la sucursal configurables y visibles en los recibos | Should | Oziel | 3 |
| 3 | SCRUM-24 | HU-14 Recibos | Descarga individual del recibo y descarga consolidada por período, con aviso si no hay ventas | Must (RF-050, RF-051: Should) | Pedro | 5 |
| 4 | SCRUM-29 | HU-25 Recetas médicas | Receta como registro propio (médico, paciente, fecha) asociada a la venta. **Depende de la validación con el cliente** | Must | Pedro | 3 |
| 5 | SCRUM-30 | HU-27 Descuentos autorizados | Permiso de descuento, pantalla en el punto de venta y auditoría. **Depende de la validación con el cliente** | Should | Pedro | 3 |
| 6 | SCRUM-28 | HU-24 Clientes | Registro, búsqueda, NIT/CI único e historial de compras | Should | Pedro | 5 |
| 7 | SCRUM-27 | HU-17 Factura simulada | Factura con correlativo propio, datos del cliente y leyenda sin validez fiscal; rechazo para ventas anuladas | Must | Luis | 8 |
| 8 | SCRUM-37 | HU-T01 Rendimiento | Prueba de carga con 20 usuarios (p95 ≤ 2 s) y reporte de 12 meses (≤ 5 s) | Must | Luis | 3 |
| 9 | SCRUM-38 | HU-T03 Usabilidad de la venta | Prueba con un cajero capacitado 30 min: venta estándar en ≤ 3 min, con registro de puntos de fricción | Should | Pedro | 2 |
| 10 | SCRUM-40 | HU-T05 Mantenibilidad | Medir la complejidad ciclomática promedio (≤ 10 por método) en la CI; la cobertura ya está (HU-T05.1) | Should | Oziel | 2 |
| 11 | SCRUM-34 | HU-29 Múltiples sucursales | Sucursal en inventario, caja y ventas. Es *Could Have*: lo más probable es que vuelva al backlog | Could | Luis | 8 |
| | | | | | | **Total** | **44** |

## 3. ¿Cómo? — Carga por integrante

Capacidad por integrante: 4 h/día × 10 días − 8 h de eventos Scrum = **32 h**.
Las subtareas (implementación, pantalla y pruebas, con sus horas) se crean en
Jira al empezar cada historia, como en el Sprint 2.

| Integrante | Compromiso | SP | Horas estimadas | Uso |
| --- | --- | --- | --- | --- |
| Luis | SCRUM-35, 36, 13, 14 | 13 | 28 | 88 % |
| Pedro | SCRUM-31, 25, 22 | 11 | 26 | 81 % |
| Oziel | SCRUM-41, 39, 11 | 6 | 16 | 50 % |
| **Equipo** | 10 historias | **30** | **70** | **73 %** |

Oziel tiene menos carga porque facilita los eventos Scrum, valida con el cliente
las reglas de HU-25 y HU-27, verifica las 7 historias cubiertas y revisa los PR.
Es el primero en tomar las adicionales (SCRUM-32 y SCRUM-33).

### 3.1 Orden de trabajo

| Semana | Foco |
| --- | --- |
| 1 (30 nov – 4 dic) | Cierre de las 7 historias cubiertas; stock compartido entre puestos (SCRUM-35); TLS en Cloudflare (SCRUM-14); respaldo diario (SCRUM-41); auditoría del cajero rechazado (SCRUM-11); reportes por rango (SCRUM-31); faltante en el cobro (SCRUM-22) |
| 2 (7 – 11 dic) | Respaldo y recuperación desde la app (SCRUM-36); bitácora inalterable (SCRUM-13); cierre diario y PDF (SCRUM-31); arqueo (SCRUM-25); prueba de corte (SCRUM-39); adicionales en orden |

**Camino crítico:** SCRUM-41 (respaldo diario en el VPS) → SCRUM-36 (recuperación
desde la app). La recuperación usa los respaldos que genera el script diario. Si
SCRUM-41 se retrasa más de dos días, SCRUM-36 queda solo con el respaldo a pedido
y la recuperación se documenta como procedimiento manual.

## 4. Criterios de aceptación

Los criterios Gherkin completos están en cada historia de Jira. Resumen de lo
comprometido:

| HU | Escenario principal | Escenarios alternos |
| --- | --- | --- |
| HU-22 | Dos puestos venden del mismo lote; al sincronizar, los dos muestran el mismo stock que el servidor | Una venta sin conexión no se pierde ni se duplica; la idempotencia por `eventId` se mantiene |
| HU-23 | El administrador genera un respaldo y queda registrado con fecha y hora; la recuperación deja los datos como estaban | Un usuario sin rol de administrador recibe 403 |
| HU-20 | Un ajuste de inventario queda en la bitácora con usuario, dispositivo, fecha, hora y datos | Intentar modificar o borrar un registro de auditoría se rechaza |
| HU-T02 | `curl --tls-max 1.2` contra producción responde | `curl --tls-max 1.1` es rechazado; HTTP redirige a HTTPS |
| HU-18 | Reporte de ventas del 1 al 15 con su total, exportable a Excel y PDF; cierre diario con dos turnos | Fecha inicial posterior a la final → error; sin conexión → aviso de que requiere conexión |
| HU-15 | Arqueo durante el turno con el conteo del momento y su diferencia, sin cerrar la caja | Venta sin caja abierta → se pide abrir la caja |
| HU-12 | Total Bs 37,50 y recibido Bs 50,00 → cambio Bs 12,50 | Recibido Bs 30,00 → no se cobra y se muestra «faltan Bs 7,50» |
| HU-T06 | Existe un respaldo de las últimas 24 h; la restauración tarda 4 h o menos | Si el respaldo falla, llega un correo al administrador |
| HU-T04 | Ventas, caja e inventario funcionan sin conexión; al volver, la sincronización empieza en menos de 5 min | Usuarios sin conexión → aviso sin bloquear el resto |
| HU-03 | Cajero en PUESTO-01 entra | Cajero en un equipo no autorizado → rechazado y el intento queda en auditoría |

## 5. Definition of Done

La misma de los sprints anteriores:

1. Criterios de aceptación Gherkin cumplidos y CP ejecutados en verde.
2. Pruebas xUnit con `[Trait("JiraIssue")]` y `[Trait("Requirement")]`.
3. Rama `feature/SCRUM-n-…`, commits `tipo(SCRUM-n): …` y PR con la clave en el título, revisado por otro integrante.
4. CI de GitHub Actions en verde, con la cobertura de los servicios de negocio en 70 % o más.
5. Documentación actualizada (README, Swagger y Postman, trazabilidad).
6. Desplegado en el VPS y probado contra producción cuando la historia toca el servidor.
7. Ticket en *Finalizado* al fusionar el PR.

## 6. Mejoras tomadas de la revisión del Sprint 2

| Hallazgo | Cómo se aplica en este sprint |
| --- | --- |
| SCRUM-18 se dio por terminada mostrando la cantidad de productos en lugar de los productos | Antes de pasar una historia a *Finalizado* se revisa escenario por escenario contra la pantalla real, no solo contra las pruebas |
| SCRUM-100 pasaba en el VPS pero no en Cloudflare | Las historias que tocan el servidor se prueban contra `https://farmacia.bell7738.com`, no solo en local (punto 6 de la DoD) |
| La prueba de interfaz en línea depende de datos recién sembrados | Cada corrida en línea usa una base nueva o datos propios, para poder repetirla |

## 7. Riesgos

| Riesgo | Mitigación | Responsable |
| --- | --- | --- |
| El stock compartido entre puestos (SCRUM-35) cambia la sincronización, que es la parte más delicada | Prueba de integración con dos puestos antes de tocar el código; se publica el cambio de stock como un evento más del feed de lotes, sin cambiar el Outbox | Luis |
| Restaurar un respaldo en producción por error | La recuperación pide confirmación, solo la hace el administrador y antes guarda un respaldo del estado actual | Luis |
| El cliente no define a tiempo las reglas de recetas y descuentos | Quedaron entre las adicionales; si no hay respuesta en la semana 1 vuelven al backlog | Oziel |
| Sprint con 28 historias en el tablero | El compromiso es de 10; las 7 cubiertas se cierran la primera semana y las 11 adicionales se ordenan por prioridad | Oziel |

## 8. Compromiso

El equipo se compromete con el **objetivo del sprint** y con **30 SP** (SCRUM-35,
36, 13, 14, 31, 25, 22, 41, 39 y 11).

| Integrante | Conformidad |
| --- | --- |
| Oziel Rodman Ramos Torrez | ☐ |
| Luis Daniel Rojas Cáceres | ☐ |
| Pedro Andrez Condorena Apaza | ☐ |

## 9. Cambios posteriores al planning

| Fecha | Cambio | Efecto en el compromiso |
| --- | --- | --- |
| | | |
