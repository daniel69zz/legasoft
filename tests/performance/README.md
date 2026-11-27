# Prueba de carga (SCRUM-37 · HU-T01 · RNF-001 y RNF-002)

Mide con un backend levantado:

- **RNF-001:** 20 usuarios concurrentes (cada uno con su puesto `CARGA-01`…`CARGA-20` y una pausa de 1 a 3 s
  entre operaciones) consultan productos, movimientos y cambios de catálogo y lotes, y registran ventas por
  la sincronización. Se acepta si el 95 % responde bien en 2 s o menos.
- **RNF-002:** el reporte de ventas de 12 meses se obtiene en 5 s o menos.

No corre en la CI. **`--escribir` registra ventas de verdad:** usarlo solo contra un backend de prueba.

```sh
docker compose up -d --build
dotnet run --project tests/performance/Pharmacy.LoadTests -c Release -- \
  --gateway http://localhost:5100 --usuarios 20 --segundos 60 --sembrar-meses 12 --ventas-por-dia 10 --escribir
```

## Resultado del 27/11/2026 (backend local con Docker, 3857 ventas en 12 meses)

| Operación | Cantidad | Errores | p50 (ms) | p95 (ms) | Máx (ms) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Consulta de movimientos de inventario | 118 | 0 | 11 | 152 | 448 |
| Consulta de productos con stock | 122 | 0 | 7 | 24 | 403 |
| Descarga de cambios de lotes | 118 | 0 | 13 | 94 | 475 |
| Descarga de cambios del catálogo | 114 | 0 | 9 | 96 | 415 |
| Registro de venta (sincronización) | 128 | 0 | 21 | 116 | 481 |

**RNF-001:** 100 % de las operaciones en 2 s o menos. **RNF-002:** reporte de 12 meses en 0,11 s. **Cumple.**

La primera corrida detectó que el Gateway limitaba a 300 solicitudes por minuto **por IP**; detrás de nginx
todas las cajas llegan desde la misma IP y compartían ese cupo. Ahora el cupo es por puesto y usuario.
