# Calidad

Documentación relacionada con el aseguramiento de la calidad del producto.

| Archivo | Código | Estado |
| --- | --- | --- |
| `estrategia-qa.tex` | `LGS-CAL-001` | Plantilla |

El documento reúne la estrategia de calidad y el plan de pruebas:

- Modelo de calidad según **ISO/IEC 25010**, con la prioridad de cada
  característica ajustada al proyecto.
- Niveles de prueba y su correspondencia con `tests/unit`,
  `tests/integration` y `tests/acceptance`.
- Criterios de entrada, suspensión, reanudación y salida.
- Casos de prueba (`CP-NNN`) vinculados al requisito que verifican, mediante el
  comando reutilizable `\caso`.
- Gestión de defectos: escala de severidad e información mínima de un reporte.
- Métricas de seguimiento e informe de validación previo a la entrega.

Sin criterios de aceptación aprobados en `docs/srs/` no es posible
diseñar casos de prueba verificables.

Compilar con `make calidad` desde la raíz del repositorio.
