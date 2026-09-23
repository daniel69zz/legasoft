# Catálogo de requisitos

Extraído de las secciones 3 y 4 del Documento 2 (SRS v2). La fuente de verdad es
[`DOC2-especificacion-requisitos-srs-v2.pdf`](DOC2-especificacion-requisitos-srs-v2.pdf).

Prioridad según MoSCoW. Estado: *Validado* (confirmado con el cliente) o *Propuesto* (derivación técnica del equipo).

## Requisitos funcionales

| ID | Módulo | Requisito | Fuente | Prioridad | Estado |
| --- | --- | --- | --- | --- | --- |
| RF-001 | Usuarios | El sistema deberá permitir que los usuarios registrados inicien sesión mediante credenciales válidas. | NEC-02 \| ENT-001 y ENT-002 | Must | Validado |
| RF-002 | Usuarios | El sistema deberá gestionar roles y permisos diferenciados para restringir el acceso a las funciones según el perfil del usuario. | NEC-02 \| ENT-001 | Must | Validado |
| RF-003 | Usuarios | El sistema deberá permitir al administrador registrar nuevos usuarios. | NEC-02 \| ENT-001 | Must | Validado |
| RF-004 | Usuarios | El sistema deberá permitir al administrador modificar los datos de un usuario registrado. | NEC-02 \| ENT-001 | Must | Validado |
| RF-005 | Usuarios | El sistema deberá permitir al administrador activar o desactivar usuarios. | NEC-02 \| ENT-001 | Must | Validado |
| RF-006 | Usuarios | El sistema deberá restringir el inicio de sesión de los cajeros a computadoras previamente autorizadas. | NEC-15 \| ENT-002 | Must | Validado |
| RF-007 | Usuarios | El sistema deberá permitir al administrador iniciar sesión únicamente desde dispositivos autorizados para su perfil. | ENT-002 | Should | Validado |
| RF-008 | Asistencia | El sistema deberá registrar automáticamente la asistencia del cajero cuando inicie sesión correctamente en un equipo autorizado. | NEC-16 \| ENT-002 | Must | Validado |
| RF-009 | Medicamentos | El sistema deberá permitir registrar medicamentos con nombre comercial, principio activo, concentración, presentación, laboratorio, código de producto, categoría y condición de venta (con o sin receta médica). | NEC-03 y NEC-35 \| ENT-001 y VAL-001 | Must | Validado |
| RF-010 | Medicamentos | El sistema deberá permitir modificar la información de medicamentos registrados. | NEC-03 \| ENT-001 | Should | Validado |
| RF-011 | Medicamentos | El sistema deberá permitir activar o desactivar medicamentos registrados. | NEC-03 \| ENT-001 | Should | Validado |
| RF-012 | Medicamentos | El sistema deberá permitir buscar medicamentos por nombre, código, principio activo o categoría. | NEC-03 y NEC-07 \| ENT-001 | Must | Validado |
| RF-013 | Inventario | El sistema deberá permitir consultar el stock disponible de cada producto. | NEC-04 \| ENT-001 | Must | Validado |
| RF-014 | Inventario | El sistema deberá registrar las entradas de productos al inventario. | NEC-04 \| ENT-001 | Must | Validado |
| RF-015 | Inventario | El sistema deberá registrar salidas de productos del inventario. | NEC-04 \| ENT-001 | Must | Validado |
| RF-016 | Inventario | El sistema deberá permitir realizar ajustes manuales de inventario indicando el motivo correspondiente y registrando el usuario, la fecha y la hora del ajuste. | NEC-04 y NEC-28 \| ENT-001 | Must | Validado |
| RF-017 | Inventario | El sistema deberá permitir definir un nivel mínimo de stock para cada producto. | NEC-04 \| ENT-001 | Must | Validado |
| RF-018 | Inventario | El sistema deberá generar una alerta cuando un producto alcance su nivel mínimo de stock configurado. | NEC-04 \| ENT-001 | Must | Validado |
| RF-019 | Inventario | El sistema deberá permitir consultar el historial de movimientos de inventario. | NEC-04 \| ENT-001 | Should | Validado |
| RF-020 | Lotes | El sistema deberá registrar el número de lote y la fecha de vencimiento de los medicamentos recibidos. | NEC-05 \| ENT-001 | Must | Validado |
| RF-021 | Lotes | El sistema deberá permitir consultar el stock disponible por lote. | NEC-05 \| ENT-001 | Should | Validado |
| RF-022 | Lotes | El sistema deberá generar una alerta para cada lote cuya fecha de vencimiento se encuentre dentro de un umbral configurable de días (valor por defecto: 60 días). | NEC-05 y NEC-33 \| ENT-001 y ENT-002 | Must | Validado |
| RF-023 | Lotes | El sistema deberá identificar los medicamentos vencidos. | NEC-05 \| ENT-001 | Must | Validado |
| RF-024 | Lotes | El sistema deberá impedir la venta de medicamentos vencidos. | NEC-05 \| ENT-001 | Must | Validado |
| RF-025 | Lotes | El sistema deberá permitir registrar el retiro de medicamentos vencidos o dañados del inventario. | NEC-05 \| ENT-001 | Must | Validado |
| RF-026 | Proveedores | El sistema deberá permitir registrar proveedores. | NEC-06 \| ENT-001 | Must | Validado |
| RF-027 | Proveedores | El sistema deberá permitir modificar la información de proveedores registrados. | NEC-06 \| ENT-001 | Must | Validado |
| RF-028 | Proveedores | El sistema deberá permitir consultar los productos suministrados por cada proveedor. | NEC-06 \| ENT-001 | Should | Validado |
| RF-029 | Proveedores | El sistema deberá permitir consultar el historial de compras realizadas a cada proveedor. | NEC-06 \| ENT-001 | Should | Validado |
| RF-030 | Compras | El sistema deberá permitir registrar una compra asociada a un proveedor, incluyendo los productos y cantidades adquiridas. | NEC-06 \| ENT-001 | Must | Validado |
| RF-031 | Compras | El sistema deberá calcular automáticamente el total de una compra registrada. | NEC-06 \| ENT-001 | Must | Validado |
| RF-032 | Compras | El sistema deberá permitir registrar la recepción de productos incluyendo lotes y fechas de vencimiento. | NEC-06 y NEC-05 \| ENT-001 | Must | Validado |
| RF-033 | Compras | El sistema deberá actualizar automáticamente el inventario después de confirmar la recepción de una compra. | NEC-08 \| ENT-001 | Must | Validado |
| RF-034 | Proveedores | El sistema deberá registrar el precio de compra ofrecido por cada proveedor para cada producto. | NEC-23 y NEC-24 \| ENT-002 | Must | Validado |
| RF-035 | Proveedores | El sistema deberá mantener un historial de precios ofrecidos por cada proveedor para cada producto. | NEC-23 \| ENT-002 | Should | Validado |
| RF-036 | Proveedores | El sistema deberá mostrar, para un producto seleccionado, los precios vigentes de todos sus proveedores ordenados de menor a mayor costo. | NEC-24 \| ENT-002 | Should | Validado |
| RF-037 | Ventas | El sistema deberá permitir iniciar una nueva venta. | NEC-07 \| ENT-001 y ENT-002 | Must | Validado |
| RF-038 | Ventas | El sistema deberá permitir seleccionar o buscar los productos que serán incluidos en una venta. | NEC-07 \| ENT-001 y ENT-002 | Must | Validado |
| RF-039 | Ventas | El sistema deberá verificar la disponibilidad de stock antes de agregar un producto a una venta. | NEC-07 \| ENT-001 | Must | Validado |
| RF-040 | Ventas | El sistema deberá permitir modificar las cantidades de los productos antes de finalizar una venta. | NEC-07 \| ENT-001 | Must | Validado |
| RF-041 | Ventas | El sistema deberá calcular automáticamente los subtotales y el total de la venta. | NEC-07 \| ENT-001 | Must | Validado |
| RF-042 | Ventas | El sistema deberá registrar el pago en efectivo de cada venta, que es el único medio de pago admitido. | NEC-17 y NEC-27 \| ENT-001 y ENT-002 | Must | Validado |
| RF-044 | Ventas | Para pagos en efectivo, el sistema deberá permitir registrar el monto recibido del cliente. | NEC-17 \| ENT-002 | Must | Validado |
| RF-045 | Ventas | Para pagos en efectivo, el sistema deberá calcular automáticamente el cambio que debe entregarse al cliente. | NEC-17 \| ENT-002 | Must | Validado |
| RF-046 | Ventas | Al completar una venta, el sistema deberá descontar automáticamente del inventario los productos vendidos. | NEC-08 \| ENT-001 | Must | Validado |
| RF-047 | Ventas | El sistema deberá registrar el usuario responsable de cada venta realizada. | NEC-07, NEC-22 y NEC-27 \| ENT-001 | Must | Validado |
| RF-048 | Ventas | El sistema deberá permitir anular una venta registrada únicamente cuando el usuario ingrese el motivo de la anulación. | NEC-18 \| ENT-002 | Must | Validado |
| RF-049 | Recibos | El sistema deberá generar un recibo por cada venta realizada. | NEC-10 y NEC-27 \| ENT-001 y ENT-002 | Must | Validado |
| RF-050 | Recibos | El sistema deberá permitir descargar individualmente un recibo generado. | NEC-10 \| ENT-002 | Should | Validado |
| RF-051 | Recibos | El sistema deberá permitir descargar de forma consolidada los recibos correspondientes a un período seleccionado. | NEC-10 \| ENT-002 | Should | Validado |
| RF-052 | Caja | El sistema deberá permitir realizar la apertura de caja y registrar el monto inicial de efectivo. | NEC-09 \| ENT-001 | Must | Validado |
| RF-053 | Caja | El sistema deberá registrar automáticamente en caja los ingresos provenientes de las ventas. | NEC-09 \| ENT-001 | Must | Validado |
| RF-054 | Caja | El sistema deberá permitir registrar el efectivo físico por denominación de billetes y monedas. | NEC-19 \| ENT-002 | Must | Validado |
| RF-055 | Caja | El sistema deberá permitir registrar egresos de caja indicando el motivo y la categoría correspondiente. | NEC-20 \| ENT-002 | Must | Validado |
| RF-056 | Caja | El sistema deberá permitir realizar arqueos de caja durante el turno. | NEC-09 \| ENT-001 | Must | Validado |
| RF-057 | Caja | Al cerrar la caja, el sistema deberá calcular el monto esperado considerando ventas, ingresos y egresos registrados. | NEC-09 \| ENT-001 y ENT-002 | Must | Validado |
| RF-058 | Caja | El sistema deberá permitir registrar el conteo físico del dinero existente al momento del cierre de caja. | NEC-09 y NEC-19 \| ENT-001 y ENT-002 | Must | Validado |
| RF-059 | Caja | El sistema deberá mostrar la diferencia entre el monto esperado y el monto físico declarado durante el cierre de caja. | NEC-09, NEC-19 y NEC-31 \| ENT-001 y ENT-002 | Must | Validado |
| RF-060 | Caja | El sistema deberá generar un reporte de cierre de caja al finalizar el turno. | NEC-09 \| ENT-001 y ENT-002 | Should | Validado |
| RF-061 | Caja | El sistema deberá permitir registrar la entrega de caja del cajero saliente al cajero entrante. | NEC-21 \| ENT-002 | Must | Validado |
| RF-062 | Caja | El sistema deberá mantener pendiente la entrega de caja hasta que el cajero entrante confirme el monto recibido y su conformidad. | NEC-21 \| ENT-002 | Must | Validado |
| RF-063 | Facturación | El sistema deberá permitir generar una factura simulada, sin validez fiscal ni envío a Impuestos Nacionales, para una venta completada cuando el cliente la solicite. | NEC-10 \| ENT-001 | Must | Validado |
| RF-064 | Facturación | El sistema deberá completar automáticamente la factura simulada con los datos de la venta asociada: cliente, NIT o CI, productos, cantidades, precios unitarios y total. | NEC-10 \| ENT-001 | Must | Propuesto |
| RF-065 | Facturación | El sistema deberá permitir consultar las facturas simuladas emitidas. | NEC-10 \| ENT-001 | Should | Propuesto |
| RF-066 | Facturación | El sistema deberá permitir descargar la representación digital de una factura simulada emitida. | NEC-10 \| ENT-001 | Should | Propuesto |
| RF-067 | Facturación | El sistema deberá permitir imprimir una factura simulada emitida. | NEC-10 \| ENT-001 | Should | Propuesto |
| RF-068 | Reportes | El sistema deberá generar reportes de ventas para un rango de fechas seleccionado por el usuario. | NEC-11 y NEC-22 \| ENT-001 y ENT-002 | Must | Validado |
| RF-069 | Reportes | El sistema deberá generar reportes de compras para un rango de fechas seleccionado, indicando proveedor, productos, cantidades y monto total. | NEC-11 \| ENT-001 | Must | Validado |
| RF-070 | Reportes | El sistema deberá generar reportes de inventario que indiquen, por producto, el stock actual, el stock por lote y el nivel mínimo configurado. | NEC-11 \| ENT-001 | Must | Validado |
| RF-071 | Reportes | El sistema deberá generar reportes de productos con stock bajo. | NEC-11 \| ENT-001 | Must | Validado |
| RF-072 | Reportes | El sistema deberá generar reportes de medicamentos próximos a vencer. | NEC-11 y NEC-33 \| ENT-001 y ENT-002 | Must | Validado |
| RF-073 | Dashboard | El sistema deberá mostrar un dashboard gerencial con información resumida de ventas, inventario y caja. | NEC-12 \| ENT-001 y ENT-002 | Should | Validado |
| RF-074 | Dashboard | El sistema deberá mostrar un comparativo del total de ventas por cajero, agrupado por día, semana o mes según la selección del usuario. | NEC-22 \| ENT-002 | Should | Validado |
| RF-075 | Auditoría | El sistema deberá registrar el usuario responsable, la fecha y la hora de las siguientes operaciones críticas: anulación de ventas, ajustes de inventario, egresos de caja, cierre y entrega de caja, aplicación de descuentos y cambios de usuarios o permisos. | NEC-10 y NEC-28 \| ENT-001 | Must | Validado |
| RF-080 | Sincronización | El sistema deberá almacenar en la base SQLite local del puesto las ventas, los movimientos de inventario y los movimientos de caja realizados cuando no exista conexión a Internet. | NEC-14 \| ENT-001 | Must | Validado |
| RF-081 | Sincronización | Al restablecerse la conexión, el sistema deberá sincronizar las operaciones pendientes en el orden en que fueron registradas. | NEC-14 \| ENT-001 | Must | Validado |
| RF-082 | Respaldo | El sistema deberá permitir realizar respaldos de la información. | ENT-001 \| AS-IS 4.3 | Should | Validado |
| RF-083 | Respaldo | El sistema deberá permitir recuperar la información desde un respaldo autorizado. | ENT-001 \| AS-IS 4.3 | Should | Validado |
| RF-084 | Clientes | El sistema deberá permitir registrar clientes. | NEC-34 \| VAL-001 | Should | Validado |
| RF-085 | Clientes | El sistema deberá permitir modificar la información de clientes registrados. | NEC-34 \| VAL-001 | Should | Validado |
| RF-086 | Clientes | El sistema deberá permitir buscar clientes registrados. | NEC-34 \| VAL-001 | Should | Validado |
| RF-087 | Clientes | El sistema deberá permitir consultar el historial de compras de un cliente. | NEC-34 \| VAL-001 | Should | Validado |
| RF-088 | Recetas | El sistema deberá permitir registrar recetas médicas. | NEC-35 \| VAL-001 | Should | Validado |
| RF-089 | Recetas | El sistema deberá permitir asociar una receta médica con la venta correspondiente. | NEC-35 \| VAL-001 | Should | Validado |
| RF-090 | Recetas | El sistema deberá verificar si un medicamento requiere receta médica. | NEC-35 \| VAL-001 | Must | Validado |
| RF-091 | Recetas | El sistema deberá exigir el registro de una receta válida antes de completar la venta de un medicamento que la requiera. | NEC-35 \| VAL-001 | Must | Validado |
| RF-095 | Inventario | El sistema deberá aplicar la regla FEFO para priorizar la salida de productos con fecha de vencimiento más próxima. | NEC-37 \| VAL-001 | Should | Validado |
| RF-096 | Ventas | El sistema deberá permitir aplicar descuentos autorizados en las ventas, registrando el usuario responsable y el valor aplicado. | NEC-38 \| VAL-001 | Should | Validado |
| RF-097 | Configuración | El sistema deberá permitir configurar los datos generales de la empresa. | NEC-39 \| VAL-001 | Should | Validado |
| RF-098 | Configuración | El sistema deberá permitir configurar los datos de la sucursal. | NEC-39 \| VAL-001 | Should | Validado |
| RF-099 | Configuración | El sistema deberá permitir configurar parámetros operativos relacionados con inventario, ventas y facturación. | NEC-39 \| VAL-001 | Should | Validado |
| RF-100 | Escalabilidad | El sistema deberá soportar la gestión futura de múltiples sucursales, permitiendo distinguir inventario, caja y ventas por sucursal. | NEC-40 \| VAL-001 | Could | Validado |
| RF-101 | Facturación | El sistema deberá asignar a cada factura simulada emitida un número correlativo interno único. | NEC-10 y NEC-27 \| ENT-001 | Must | Propuesto |
| RF-102 | Sincronización | Durante la sincronización, el sistema deberá descartar las operaciones cuyo identificador único ya exista en el servidor, evitando registros duplicados. | NEC-14 \| ENT-001 | Must | Validado |
| RF-103 | Reportes | El sistema deberá generar automáticamente un reporte de cierre diario que consolide las ventas, los egresos y el saldo de caja de todos los turnos del día. | NEC-26 \| ENT-001 | Must | Validado |
| RF-104 | Reportes | El sistema deberá consolidar automáticamente en la base de datos central las compras, gastos, ventas y movimientos de inventario registrados por todas las áreas, de modo que los saldos y reportes estén disponibles el mismo día en que se registran las operaciones. | NEC-01 y NEC-25 \| ENT-001 | Must | Validado |
| RF-105 | Reportes | El sistema deberá permitir exportar en formato Excel o PDF las ventas, los egresos y los cierres de caja de un período seleccionado. | NEC-30 \| ENT-002 | Should | Validado |
| RF-106 | Caja | El sistema deberá exigir al cajero el registro de una justificación cuando exista diferencia entre el efectivo esperado y el efectivo contado, antes de permitir el cierre del turno. | NEC-31 \| ENT-002 | Must | Validado |
| RF-107 | Compras | Al registrar la recepción de una compra, el sistema deberá comparar las cantidades recibidas con las cantidades solicitadas en el pedido. | NEC-32 \| ENT-002 | Must | Validado |
| RF-108 | Compras | El sistema deberá permitir registrar los productos faltantes o dañados de una recepción, excluyéndolos del ingreso al stock. | NEC-32 \| ENT-002 | Must | Validado |
| RF-109 | Inventario | El sistema deberá permitir registrar y consultar la ubicación física (estante o sector) de cada producto. | NEC-29 \| ENT-001 | Should | Validado |

## Requisitos no funcionales

| ID | Atributo de calidad | Requisito | Fuente | Prioridad | Estado |
| --- | --- | --- | --- | --- | --- |
| RNF-001 | Rendimiento | El 95 % de las operaciones habituales de consulta, búsqueda, registro de venta y consulta de inventario deberá responder en un tiempo máximo de 2 segundos con hasta 20 usuarios concurrentes. | Derivado técnico de NEC-07, NEC-11 y NEC-12 | Must | Propuesto |
| RNF-002 | Rendimiento | Los reportes operativos de hasta 12 meses de información deberán generarse en un tiempo máximo de 5 segundos bajo carga normal. | Derivado técnico de NEC-11 y NEC-12 | Should | Propuesto |
| RNF-003 | Seguridad | El 100 % de las comunicaciones entre componentes locales y servicios en nube deberá utilizar TLS 1.2 o superior. | Derivado técnico de NEC-14 | Must | Propuesto |
| RNF-004 | Seguridad | Después de 5 intentos fallidos de inicio de sesión dentro de un período de 15 minutos, el sistema deberá bloquear temporalmente el acceso de la cuenta durante 15 minutos. | Derivado técnico de NEC-02 y NEC-15 | Must | Propuesto |
| RNF-005 | Usabilidad | Un cajero capacitado durante un máximo de 30 minutos deberá poder completar una venta estándar sin asistencia en un tiempo máximo de 3 minutos. | Derivado técnico de NEC-07 y NEC-17 | Should | Propuesto |
| RNF-006 | Disponibilidad | El sistema deberá mantener una disponibilidad mínima del 99,5 % mensual durante el horario de atención de la farmacia. Las funciones críticas de ventas, caja e inventario deberán continuar operativas localmente durante interrupciones de Internet de hasta 8 horas; al restablecerse la conexión, la sincronización de operaciones pendientes deberá iniciar en un máximo de 5 minutos. | NEC-14 \| ENT-001 | Must | Validado |
| RNF-007 | Mantenibilidad | Los servicios de negocio críticos del backend deberán alcanzar al menos 70 % de cobertura mediante pruebas automatizadas y una complejidad ciclomática promedio no mayor a 10 por método, medida con una herramienta de análisis estático. | Derivado técnico del alcance del proyecto | Should | Propuesto |
| RNF-008 | Respaldo / Recuperación | El sistema deberá ejecutar al menos un respaldo diario de la información crítica, con un RPO máximo de 24 horas y un RTO máximo de 4 horas. | ENT-001 \| AS-IS 4.3 | Should | Propuesto |
