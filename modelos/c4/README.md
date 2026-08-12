# Modelos C4

Diagramas del sistema siguiendo el modelo C4, elaborados en TikZ.

| Archivo | Nivel | Código |
| --- | --- | --- |
| `c4-contexto.tex` | 1 — Contexto | `LGS-MOD-C4-1` |
| `c4-contenedores.tex` | 2 — Contenedores | `LGS-MOD-C4-2` |
| `c4-componentes.tex` | 3 — Componentes | `LGS-MOD-C4-3` |

- **Nivel 1 (contexto):** el sistema como caja negra, con las personas y los
  sistemas externos con los que se relaciona. Sin tecnología interna.
- **Nivel 2 (contenedores):** las unidades desplegables —aplicación web, API,
  base de datos— y la tecnología de cada una.
- **Nivel 3 (componentes):** la estructura interna de *un* contenedor. Solo se
  elabora para aquellos cuya organización interna necesite explicarse.

Los tres son plantillas: los rótulos en ámbar se sustituyen por los elementos
reales del proyecto. Las tecnologías quedan pendientes hasta que se registren
como ADR en `docs/arquitectura/`.

Los estilos (`c4persona`, `c4foco`, `c4externo`, `c4datos`, `c4rel`…) están en
`docs/latex/legasoft-diagramas.sty`.

Compilar con `make modelos` desde la raíz del repositorio.
