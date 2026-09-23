# ADR-004: Bibliotecas Pharmacy.Domain y Pharmacy.Application compartidas

| Campo | Detalle |
| --- | --- |
| Estado | Aceptado |
| Fecha | 22 de septiembre de 2026 |
| Documento | Documento 3 v3, sección 8 |

## Contexto

Las reglas de ventas, FEFO, stock y caja deben ejecutarse en el puesto durante la operación sin conexión y en el servidor al consolidar los eventos. Si se implementan por separado, pueden divergir y producir resultados distintos para una misma operación.

## Alternativas evaluadas

- **A1.** Reglas solo en el servidor: el puesto no podría operar sin conexión.
- **A2.** Reglas duplicadas en el cliente y el servidor: permite operar sin conexión, pero con alto riesgo de inconsistencias y mayor mantenimiento.
- **A3.** Bibliotecas C# compartidas sin dependencias de infraestructura: una sola implementación reutilizada en ambos entornos.

## Decisión

Se adopta la alternativa A3. Pharmacy.Domain y Pharmacy.Application no dependen de SQLite, PostgreSQL, HTTP, SMTP ni proveedores externos; cada entorno implementa sus interfaces.

## Consecuencias positivas

Reglas únicas y consistentes, pruebas unitarias del dominio independientes de la infraestructura y menor esfuerzo de mantenimiento.

## Trade-offs y riesgos

Toda versión del Desktop y del servidor debe ser compatible con la versión de las bibliotecas; un cambio de regla exige actualizar ambos entornos de forma coordinada mediante Velopack y el despliegue de los servicios.
