# ADR-007: Una única base SQLite local por computadora

| Campo | Detalle |
| --- | --- |
| Estado | Aceptado |
| Fecha | 22 de septiembre de 2026 |
| Documento | Documento 3 v3, sección 8 |

## Contexto

Cada puesto necesita almacenar la información operativa de varios módulos y ejecutar transacciones que abarcan ventas, stock, caja y Outbox al mismo tiempo.

## Alternativas evaluadas

- **A1.** Una base SQLite por módulo: aislamiento aparente, pero impide una transacción atómica entre venta, stock, caja y Outbox.
- **A2.** Una única base SQLite con tablas separadas lógicamente por módulo: transacción local atómica y administración simple.

## Decisión

Se adopta la alternativa A2. La base contiene solo la información necesaria para operar sin conexión; los reportes consolidados, la auditoría global y las estadísticas permanecen en el servidor.

## Consecuencias positivas

Atomicidad local de las operaciones, un solo archivo que respaldar y migrar, y menor volumen de datos replicados.

## Trade-offs y riesgos

La base del puesto debe protegerse ante pérdida del equipo, y su esquema debe evolucionar mediante migraciones coordinadas con las actualizaciones de la aplicación.
