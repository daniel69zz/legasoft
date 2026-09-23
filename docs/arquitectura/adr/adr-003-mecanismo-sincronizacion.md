# ADR-003: Sincronización mediante Outbox Pattern y Sync Worker

| Campo | Detalle |
| --- | --- |
| Estado | Aceptado |
| Fecha | 22 de septiembre de 2026 |
| Documento | Documento 3 v3, sección 8 |

## Contexto

Las operaciones registradas localmente deben llegar al servidor sin pérdidas ni duplicados, y el puesto debe recibir los cambios centrales necesarios para seguir operando.

## Alternativas evaluadas

- **A1.** Llamadas directas a la API en cada operación: simple, pero pierde datos cuando no hay conexión.
- **A2.** Cola de mensajería externa (p. ej. RabbitMQ): robusta, pero agrega infraestructura y no resuelve el almacenamiento local sin conexión.
- **A3.** Replicación nativa de base de datos: no es compatible entre SQLite y PostgreSQL y no aplica reglas de negocio.
- **A4.** Outbox local + Sync Worker + Sync Service: mensajes persistidos en la misma transacción y enviados cuando hay conectividad.

## Decisión

Se adopta la alternativa A4, con mensajes identificados por eventId, deviceId, module, eventType, entityId, version, timestamp y payload, sincronización bidireccional y versiones por módulo.

## Consecuencias positivas

Atomicidad local, idempotencia, envío por lotes con reintentos, descarga incremental y trazabilidad completa de cada evento.

## Trade-offs y riesgos

Se requiere diseñar reglas de resolución de conflictos por entidad y monitorear los mensajes pendientes. Las operaciones realizadas sin conexión se consolidan con retraso.
