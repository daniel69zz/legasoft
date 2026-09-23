# SRS — Especificación de requisitos

Documentación de los requisitos del sistema.

| Archivo | Código | Estado |
| --- | --- | --- |
| `srs.tex` | `LGS-REQ-001` | Plantilla |

La especificación sigue la estructura de **ISO/IEC/IEEE 29148**, norma que
Legasoft adopta como referencia. Incluye:

- Contexto, usuarios y restricciones del producto.
- Requisitos funcionales (`RF-NNN`), cada uno con prioridad, criterio de
  aceptación y origen.
- Requisitos no funcionales (`RNF-NNN`) organizados por las características de
  calidad de ISO/IEC 25010.
- Historias de usuario (`HU-NNN`) con sus criterios de aceptación.
- Matriz de trazabilidad entre necesidad, requisito, diseño y caso de prueba.

El documento define dos comandos reutilizables, `\reqfun` y `\historia`, para
añadir requisitos e historias sin repetir la maquetación de la ficha.

Un requisito que no puede acompañarse de un criterio de aceptación verificable
todavía no está bien definido.

Compilar con `make srs` desde la raíz del repositorio.
