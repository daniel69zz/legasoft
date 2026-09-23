# ADR-006: Separación del backend en servicios con fronteras claras

| Campo | Detalle |
| --- | --- |
| Estado | Aceptado |
| Fecha | 22 de septiembre de 2026 |
| Documento | Documento 3 v3, sección 8 |

## Contexto

La API única concentraba seguridad, sincronización, notificaciones y negocio en un mismo despliegue. Algunos de estos dominios tienen ciclos de cambio y requisitos de disponibilidad diferentes.

## Alternativas evaluadas

- **A1.** Monolito único: simple de desplegar, pero cualquier cambio obliga a redesplegar todo el backend.
- **A2.** Microservicio por entidad o CRUD: fronteras artificiales, transacciones distribuidas y exceso de infraestructura.
- **A3.** Servicios por frontera clara: Identity, Sync y Notifications separados, y un Business Service que agrupa ventas, inventario, caja y compras.

## Decisión

Se adopta la alternativa A3, con un API Gateway como punto de entrada y una imagen Docker por servicio.

## Consecuencias positivas

Despliegue y escalamiento independientes de los servicios con fronteras claras, sin romper la consistencia transaccional entre venta, stock y caja; queda preparada la extracción futura de otros dominios.

## Trade-offs y riesgos

Mayor número de despliegues, necesidad de trazas correlacionadas con Serilog y de comunicación interna entre servicios.
