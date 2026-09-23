# Entrevistas

Minutas, actas y transcripciones del relevamiento de requerimientos realizado
con los stakeholders de la organización cliente.

| Archivo | Documento | Versión |
| --- | --- | --- |
| `DOC1-minuta-entrevistas-relevamiento-v2.pdf` | Documento 1 — Minuta de entrevista y relevamiento de requerimientos | v2 |
| `DOC1-minuta-entrevistas-relevamiento-v2.docx` | Fuente editable del Documento 1 | v2 |

## Cliente

Farmacéutica en proceso de apertura ubicada en Presbítero Medina 2488,
Sopocachi, La Paz. No cuenta con un sistema informático previo: el proyecto
parte desde cero.

| Stakeholder | Cargo |
| --- | --- |
| Rodrigo | Gerente General |
| Lic. Carla Mamani Quispe | Contadora |
| Ing. Marcos Choque Limachi | Gerente de Operaciones |

## Sesiones

| Código | Fecha | Tipo | Objetivo |
| --- | --- | --- | --- |
| `ENT-001` | 19/08/2026 | Relevamiento inicial | Identificar las necesidades operativas y administrativas del sistema |
| `ENT-002` | 02/09/2026 | Validación y refinamiento | Validar un mockup inicial y precisar nuevos requerimientos |
| `VAL-001` | 09/09/2026 | Validación complementaria | Revisar el mockup ajustado e incorporar funcionalidades de crecimiento |

Entrevistadores: Oziel Rodman Ramos Torrez y Pedro Andrez Condorena Apaza.

## Dolores de negocio (AS-IS)

- **Retrasos de 24 a 48 horas** para consolidar compras, gastos e inventario
  desde hojas Excel separadas (Contadora, `ENT-001`).
- **Falta de trazabilidad**: no se sabe quién modificó un dato, cuándo ni por
  qué (Gerente de Operaciones, `ENT-001`).
- **Silos de información** en hojas Excel de Gerencia, Contabilidad y
  Operaciones con versiones distintas de precios, stock y proveedores.

## Catálogo preliminar de necesidades

38 necesidades (`NEC-13` y `NEC-36` fueron eliminadas). Son el origen de los
requisitos de [`docs/srs/`](../srs/).

| ID | Necesidad | Fuente |
| --- | --- | --- |
| NEC-01 | Centralizar las principales operaciones en un único sistema | Gerente General — ENT-001 |
| NEC-02 | Gestionar usuarios mediante roles y permisos diferenciados | Gerente General — ENT-001 |
| NEC-03 | Mantener un catálogo estructurado de medicamentos y productos | Gerente General — ENT-001 |
| NEC-04 | Controlar el stock disponible y los movimientos de inventario | Gerente General — ENT-001 |
| NEC-05 | Controlar lotes, fechas de vencimiento y productos vencidos | Gerente General — ENT-001 |
| NEC-06 | Registrar proveedores y las compras realizadas a cada uno | Gerente General — ENT-001 |
| NEC-07 | Sistematizar el proceso completo de ventas | Gerente General — ENT-001 |
| NEC-08 | Actualizar el inventario después de ventas y recepciones | Gerente General — ENT-001 |
| NEC-09 | Gestionar apertura, movimientos, arqueo y cierre de caja | Gerente General — ENT-001 |
| NEC-10 | Generar facturas simuladas, recibos y trazabilidad de operaciones | Gerente General — ENT-001 / ENT-002 |
| NEC-11 | Reportes de ventas, compras, inventario, stock bajo y vencimientos | Gerente General — ENT-001 |
| NEC-12 | Información resumida para decisiones gerenciales | Gerente General — ENT-001 |
| NEC-14 | Operaciones críticas disponibles sin Internet | Gerente General — ENT-001 |
| NEC-15 | Restringir el acceso de cajeros a computadoras autorizadas | Gerente General — ENT-002 |
| NEC-16 | Registrar la asistencia de los cajeros al iniciar sesión | Gerente General — ENT-002 |
| NEC-17 | Registrar el cobro de todas las ventas en efectivo (único medio de pago) | Gerente General — ENT-002 |
| NEC-18 | Controlar anulaciones de ventas con su motivo | Gerente General — ENT-002 |
| NEC-19 | Registrar el efectivo por denominación de billetes y monedas | Gerente General — ENT-002 |
| NEC-20 | Registrar egresos de caja con motivo y categoría | Gerente General — ENT-002 |
| NEC-21 | Formalizar la entrega y recepción de caja entre turnos | Gerente General — ENT-002 |
| NEC-22 | Comparar las ventas de cada cajero por período | Gerente General — ENT-002 |
| NEC-23 | Historial de precios ofrecidos por los proveedores | Gerente General — ENT-002 |
| NEC-24 | Comparar precios de proveedores para decidir compras | Gerente General — ENT-002 |
| NEC-25 | Consolidar automáticamente compras, gastos, ventas e inventario | Contadora — ENT-001 |
| NEC-26 | Reporte de cierre diario con ventas, egresos y saldo de caja | Contadora — ENT-001 |
| NEC-27 | Registrar en cada venta factura/recibo, monto en efectivo y usuario | Contadora — ENT-001 |
| NEC-28 | Bitácora de ajustes de inventario con usuario, fecha, hora y motivo | Gerente de Operaciones — ENT-001 |
| NEC-29 | Registrar la ubicación física (estante o sector) de cada producto | Gerente de Operaciones — ENT-001 |
| NEC-30 | Exportar reportes contables por período en Excel o PDF | Contadora — ENT-002 |
| NEC-31 | Diferencia de caja esperada vs. contada con justificación obligatoria | Contadora — ENT-002 |
| NEC-32 | Verificar recepciones contra el pedido (faltantes o dañados) | Gerente de Operaciones — ENT-002 |
| NEC-33 | Alertar productos próximos a vencer con 60 días de anticipación | Gerente de Operaciones — ENT-002 |
| NEC-34 | Registrar clientes y consultar su historial de compras | Gerente General — VAL-001 |
| NEC-35 | Controlar la venta de medicamentos con receta médica | Gerente General — VAL-001 |
| NEC-37 | Priorizar la salida de lotes con vencimiento más próximo (FEFO) | Gerente General — VAL-001 |
| NEC-38 | Aplicar descuentos autorizados registrando al usuario | Gerente General — VAL-001 |
| NEC-39 | Configurar datos de la empresa, sucursal y parámetros operativos | Gerente General — VAL-001 |
| NEC-40 | Permitir la ampliación futura a múltiples sucursales | Gerente General — VAL-001 |
