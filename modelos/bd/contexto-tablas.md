# Contexto de las tablas por agrupación

Este documento explica de qué trata cada grupo de tablas de la base de datos del
**Sistema de Gestión para Farmacéutica**: qué parte del negocio representa, qué
guarda cada tabla y cómo se relacionan. Los diagramas están en esta misma carpeta
(`er-identity.svg`, `er-business.svg`, `er-sync-audit.svg`, `er-completo.svg`) y la
estructura detallada de columnas, en [`README.md`](README.md).

## Idea general

La farmacia funciona en modo **local-first** (ADR-002): cada computadora de caja
(«puesto») vende con su propia base SQLite aunque no haya Internet, y después
envía lo ocurrido a la **base central PostgreSQL**. La base central se divide en
**schemas**, uno por dominio de negocio, y cada servicio del backend solo escribe
en los suyos (ADR-005):

| Agrupación | Schemas | Servicio dueño | Pregunta que responde |
| --- | --- | --- | --- |
| Acceso y seguridad | `identity` | Identity Service | ¿Quién es el usuario, qué puede hacer y desde dónde entra? |
| Catálogo e inventario | `inventory` | Business Service | ¿Qué productos hay, en qué lotes y cuánto queda? |
| Ventas | `sales` | Business Service | ¿Qué se vendió, a quién, por quién y de qué lote? |
| Caja | `cash` | Business Service | ¿Cuánto efectivo entró y salió en cada turno y cuadró? |
| Auditoría | `audit` | Business Service | ¿Quién hizo una operación sensible y cuándo? |
| Sincronización | `sync` | Sync Service | ¿Qué envió cada puesto y qué ya se procesó? |

Como cada servicio es dueño de sus tablas, **no hay claves foráneas entre
schemas**: por ejemplo, `sales.sale_lines.product_id` apunta a un producto de
`inventory.products`, pero esa relación la garantiza la aplicación y no la base.
En los diagramas estas relaciones aparecen con línea punteada.

---

## 1. Acceso y seguridad (`identity`)

Controla quién entra al sistema y con qué permisos. Lo usa el inicio de sesión de
todas las personas (HU-01, HU-02, HU-03, HU-04).

| Tabla | De qué trata |
| --- | --- |
| `users` | Una fila por persona que usa el sistema: usuario, nombre, contraseña guardada como hash (nunca en texto plano), **rol** (uno de los 7 del SRS: Administrador, Gerente General, Contadora, Gerente de Operaciones, Cajero, Farmacéutico, Almacén) y si está activo. También lleva el contador de intentos fallidos y la fecha hasta la que la cuenta queda bloqueada (RNF-004). |
| `devices` | Las computadoras de caja autorizadas (`PUESTO-01`, `PUESTO-02`). Un cajero solo puede iniciar sesión desde uno de estos equipos (RF-006). |
| `attendance` | La asistencia de los cajeros: se crea una fila con la hora del **primer** inicio de sesión de cada día en cada equipo (RF-008). |
| `user_changes` | Bitácora de la administración de usuarios: altas, cambios de rol, activaciones y desactivaciones, con quién lo hizo y cuándo (RF-075). |

**Ejemplo:** cuando el administrador cambia a `cajero3` al rol Farmacéutico, se
actualiza `users.role` y se agrega una fila en `user_changes`. El nuevo rol se
aplica en el siguiente inicio de sesión, porque el rol viaja dentro del token JWT.

---

## 2. Catálogo e inventario (`inventory`)

Representa los medicamentos que vende la farmacia y las existencias físicas.
La clave del modelo es que **el stock no está en el producto, sino en sus lotes**,
porque cada lote tiene su propia fecha de vencimiento.

| Tabla | De qué trata |
| --- | --- |
| `products` | El **catálogo único** de medicamentos (HU-05): código, nombre comercial, principio activo, concentración, presentación, laboratorio, categoría, si requiere receta, precio, stock mínimo, ubicación en el estante y si está activo. `updated_at` sirve para que los puestos descarguen solo lo que cambió. |
| `lots` | Cada lote recibido de un producto: número de lote, fecha de vencimiento y unidades que quedan. Un producto tiene varios lotes. |
| `stock_movements` | El historial de cada entrada o salida de unidades: venta, compra, ajuste o retiro, con lote, cantidad, motivo, usuario y fecha. Sirve para explicar por qué el stock tiene el valor que tiene. |

**Cómo se calculan las cosas que ve el usuario:**
- **Stock vendible** de un producto = suma de las unidades de sus lotes **no vencidos**.
- **Alerta de vencimiento:** un lote que vence en 60 días o menos aparece «por vencer»; uno con fecha pasada, «vencido».
- **Alerta de stock mínimo:** cuando el stock vendible es menor o igual a `products.minimum_stock`.
- **FEFO** (*First Expired, First Out*): al vender se descuenta primero del lote que vence antes (HU-07).

---

## 3. Ventas (`sales`)

Registra cada venta realizada en los puestos, ya consolidada en el servidor
(HU-11 a HU-14, HU-25).

| Tabla | De qué trata |
| --- | --- |
| `sales` | La cabecera de la venta: número de recibo, puesto, cajero, sesión de caja, fecha, subtotal, descuento, total, efectivo recibido y cambio. Guarda el número de receta cuando algún medicamento la exige, los datos del cliente si los dio, el estado (`Completada` o `Anulada`) y el motivo de anulación. |
| `sale_lines` | El detalle de la venta: **una línea por producto y por lote**. Si una venta de 130 tabletas tomó 120 de un lote y 10 de otro (FEFO), quedan dos líneas. Así se sabe exactamente de qué lote salió cada unidad. |

`sale_lines.sale_id` → `sales.id` es la **única clave foránea física** del modelo,
porque línea y venta pertenecen al mismo dominio y se guardan juntas.

**Ejemplo de anulación:** al anular, la venta pasa a `Anulada` con su motivo, cada
unidad vuelve al mismo lote del que salió y se registra un movimiento de stock
de devolución.

---

## 4. Caja (`cash`)

Controla el efectivo de cada turno de cajero, desde la apertura hasta el cierre
(HU-15, HU-16). Todos los cobros de la farmacia son en efectivo.

| Tabla | De qué trata |
| --- | --- |
| `sessions` | Un turno de caja en un puesto: cajero, hora de apertura, monto inicial y estado (`Abierta` o `Cerrada`). Al cerrar se completan los totales de ventas y egresos, el **monto esperado**, el **monto contado** por denominación, la **diferencia** y la justificación, obligatoria si la diferencia no es cero (RF-106). |
| `movements` | Cada movimiento de efectivo del turno: ingreso por venta, egreso (con motivo y categoría) o devolución por anulación. |

**Fórmula del cierre:** esperado = monto inicial + ventas − egresos.
diferencia = contado − esperado.

---

## 5. Auditoría (`audit`)

| Tabla | De qué trata |
| --- | --- |
| `entries` | La bitácora de **operaciones críticas** del negocio (RF-075): cambios de precio, altas y cambios del catálogo, egresos de caja, descuentos, anulaciones y cierres de caja. Cada fila guarda quién lo hizo, desde qué equipo, cuándo y el detalle. Si la operación llegó desde un puesto, `event_id` indica qué evento de sincronización la originó. |

Los cambios de usuarios se auditan aparte, en `identity.user_changes`, porque
pertenecen al Identity Service.

---

## 6. Sincronización (`sync`)

Es lo que permite vender sin Internet sin perder ni duplicar información
(HU-22, ADR-003, ADR-008).

| Tabla | De qué trata |
| --- | --- |
| `received_events` | Cada evento que envió un puesto (venta creada, venta anulada, caja abierta, egreso, caja cerrada): puesto, módulo, tipo, versión, fecha, fecha de recepción y el contenido en JSON. Su clave es `event_id`: si un puesto reenvía el mismo evento, el servidor lo reconoce y lo descarta (**idempotencia**). |
| `module_versions` | La última versión procesada de cada módulo (ventas, inventario, caja) para cada puesto. Le permite al servidor saber hasta dónde llegó cada computadora. |

**Recorrido de una venta hecha sin conexión:**
1. El cajero vende; el puesto guarda la venta en su SQLite y agrega un evento a su cola local (`sync_outbox`).
2. Cuando vuelve la conexión, el Sync Worker del puesto envía los eventos en orden.
3. El Sync Service guarda cada uno en `received_events`, descarta los repetidos y avanza `module_versions`.
4. El Business Service aplica el evento: crea la fila en `sales` y `sale_lines`, descuenta los `lots`, registra el `stock_movement` y el movimiento de caja.

---

## 7. Base local del puesto (SQLite)

Cada computadora de caja tiene una copia reducida, con los mismos conceptos y un
prefijo de dominio en el nombre de la tabla:

| Tablas | De qué tratan |
| --- | --- |
| `inventory_products`, `inventory_lots`, `inventory_movements` | Catálogo y stock por lote descargados del servidor, para vender sin conexión. |
| `sales_sales`, `sales_lines` | Ventas hechas en ese puesto. |
| `cash_sessions`, `cash_movements` | Caja del turno en ese puesto. |
| `sync_outbox` | Cola de eventos pendientes de enviar al servidor (patrón *Outbox*). |
| `sync_state` | Hasta qué versión del catálogo se descargó. |
| `identity_cached_users` | Credenciales en caché para poder iniciar sesión sin conexión. |
