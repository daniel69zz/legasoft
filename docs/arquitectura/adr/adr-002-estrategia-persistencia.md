# ADR-002: SQLite local por puesto y PostgreSQL administrado como fuente central

| Campo | Detalle |
| --- | --- |
| Estado | Aceptado |
| Fecha | 22 de septiembre de 2026 |
| Documento | Documento 3 v3, sección 8 |

## Contexto

Las funciones críticas de venta y caja deben seguir operando sin Internet, mientras que la gerencia necesita información consolidada, auditoría y reportes de todos los puestos y futuras sucursales, con costos de infraestructura moderados.

## Alternativas evaluadas

- **A1.** Solo base central en la nube: simple, pero impide operar sin conexión.
- **A2.** SQL Server local y central: robusto, pero con costo de licencias e instalación pesada en cada puesto.
- **A3.** Base documental central (MongoDB): flexible, pero sin transacciones relacionales adecuadas para ventas, caja e inventario.
- **A4.** SQLite local + PostgreSQL administrado central: operación local embebida y una base central relacional robusta y de código abierto.

## Decisión

Se adopta la alternativa A4. SQLite es la fuente operativa local del puesto y PostgreSQL la fuente consolidada central del negocio; la organización interna de cada una se detalla en los ADR-005 y ADR-007.

## Consecuencias positivas

Continuidad de la operación sin conexión, bajo costo de licencias, transacciones ACID en ambos extremos y respaldos automáticos en el servicio administrado.

## Trade-offs y riesgos

Existen diferencias de tipos y funciones entre SQLite y PostgreSQL que deben cubrirse con pruebas de integración. El stock local de un puesto sin conexión puede diferir del stock central hasta la siguiente sincronización.
