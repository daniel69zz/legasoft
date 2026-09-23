# ADR-008: Sync Service como punto único de sincronización de los puestos

| Campo | Detalle |
| --- | --- |
| Estado | Aceptado |
| Fecha | 22 de septiembre de 2026 |
| Documento | Documento 3 v3, sección 8 |

## Contexto

Con varios servicios cloud, el Desktop podría sincronizar directamente con cada uno, lo que multiplicaría conexiones, credenciales y lógica de reintento en el cliente.

## Alternativas evaluadas

- **A1.** Sincronización directa con cada servicio: acopla el cliente a la estructura interna del backend.
- **A2.** Sync Service como fachada: un único punto de entrada que valida, registra y despacha los eventos al dominio correspondiente.

## Decisión

Se adopta la alternativa A2. El Sync Service gestiona idempotencia, versiones por módulo y dispositivo y resolución de conflictos, y despacha cada evento según su module y eventType.

## Consecuencias positivas

El cliente permanece simple y estable ante cambios internos del backend, y la sincronización se controla y audita en un solo lugar.

## Trade-offs y riesgos

El Sync Service es un componente crítico y debe diseñarse para alta disponibilidad y monitoreo; su indisponibilidad retrasa la consolidación, aunque no detiene la operación local.
