# Pruebas de integración

Pruebas que verifican la interacción entre varios componentes o servicios del
sistema.

**Herramienta:** xUnit. Cubren los servicios cloud, el acceso a SQLite y
PostgreSQL (incluidas las diferencias entre ambos motores) y la sincronización
local–cloud: envío del Outbox, idempotencia por `eventId`, versiones por módulo
y resolución de conflictos.
