# ADR-005: Una instancia PostgreSQL con schemas separados por dominio

| Campo | Detalle |
| --- | --- |
| Estado | Aceptado |
| Fecha | 22 de septiembre de 2026 |
| Documento | Documento 3 v3, sección 8 |

## Contexto

Los servicios cloud necesitan responsabilidades claras sobre sus datos, pero el proyecto debe mantener una infraestructura acotada y un costo moderado.

## Alternativas evaluadas

- **A1.** Una base física por servicio: máximo aislamiento, pero multiplica la infraestructura, los respaldos y el costo.
- **A2.** Una sola base sin separación: simple, pero cualquier servicio puede modificar cualquier tabla.
- **A3.** Una instancia PostgreSQL con schemas por dominio y usuarios por servicio: separación lógica y de permisos sobre una misma infraestructura.

## Decisión

Se adopta la alternativa A3 con los schemas sales, inventory, cash, identity, audit y sync. Cada servicio utiliza un usuario con permisos de escritura solo sobre su schema.

## Consecuencias positivas

Responsabilidad clara sobre los datos, un único servicio administrado que respaldar y operar, y posibilidad de migrar un schema a otra base si en el futuro se extrae el dominio.

## Trade-offs y riesgos

Todos los servicios comparten la disponibilidad y la capacidad de la misma instancia. Los reportes que cruzan dominios requieren vistas o permisos de lectura explícitos.
