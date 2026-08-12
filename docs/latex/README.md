# Estilo LaTeX compartido

Esta carpeta no contiene documentos, sino los paquetes de estilo que usan todos
los demás. No se compila por sí sola.

| Archivo | Contenido |
| --- | --- |
| `legasoft.sty` | Estilo institucional: tipografía, colores, portada, encabezados, tablas, cajas y datos del equipo. |
| `legasoft-diagramas.sty` | Estilos TikZ para los diagramas C4, UML y BPMN de `modelos/`. |

## Cómo se usan

Desde `docs/<carpeta>/`:

```latex
\usepackage{../latex/legasoft}
```

Desde `modelos/<carpeta>/`, que está un nivel más abajo:

```latex
\usepackage{../../docs/latex/legasoft}
\usepackage{../../docs/latex/legasoft-diagramas}
```

La ruta es relativa al archivo, de modo que cada documento compila por su cuenta
sin necesidad de configurar `TEXINPUTS`.

## Datos del equipo

Los nombres y roles están definidos como macros en `legasoft.sty`
(`\lgDirector`, `\lgArquitectoRol`, `\lgQACV`…). **Este es el único sitio donde
editarlos**: al cambiarlos aquí se actualizan todos los documentos y diagramas.

## Metadatos de cada documento

Antes de `\begin{document}`:

```latex
\lgtitulo{Título completo}
\lgsubtitulo{Subtítulo}
\lgtituloCorto{Lo que aparece en el encabezado}
\lgcodigo{LGS-XXX-001}
\lgversion{1.0}
\lgestado{Borrador}
\lgfecha{11 de agosto de 2026}
\lgautor{\lgDirector}
```

Y dentro del documento, `\lgportada` y `\lgcontrolversiones{...}`.

## Utilidades

| Comando | Para qué sirve |
| --- | --- |
| `\campo{dato}` | Marca en ámbar un dato pendiente de rellenar. |
| `\campoclaro{dato}` | Igual, pero legible sobre fondos oscuros (cajas azules de los diagramas). |
| `\ruta{docs/x/y.tex}` | Ruta en monoespaciado que puede partirse al final de línea. Use esto en vez de `\texttt{}` para rutas: `\texttt{}` no se parte y desborda el margen. |
| `\ajustaancho{...}` | Reduce una figura solo si excede el ancho del texto. |
| `nota` (entorno) | Caja azul informativa. |
| `porcompletar` (entorno) | Caja ámbar con lo que falta por decidir o rellenar. |

## Dos detalles que conviene conocer

**Babel en español desactiva atajos.** El estilo carga babel con
`es-noshorthands` a propósito: sin esa opción, `<`, `>` y `"` pasan a ser
caracteres activos y rompen tanto las flechas de TikZ (`\draw[<->]`) como las
opciones de `tabularx`.

**En TikZ, `align` debe ir después de `text width`.** Si un nodo redefine
`text width`, TikZ descarta la alineación que traía el estilo y vuelve a texto
justificado. Por eso las variantes anchas (`c4focoancho`,
`c4contenedorancho`) están definidas en `legasoft-diagramas.sty` con el orden
correcto, en lugar de ajustar el ancho en cada nodo.

## Motor de compilación

Los documentos se compilan con **XeLaTeX** a través de Tectonic. Las fuentes
(TeX Gyre Pagella, Heros y Cursor) se cargan por nombre de archivo `.otf`, que
es como Tectonic las encuentra en su propio paquete; cargarlas por nombre de
familia (`\setmainfont{TeX Gyre Pagella}`) falla porque no están instaladas en
el sistema.
