# Guía rápida de contribución

Este documento complementa el flujo de trabajo descrito en el [README principal](../README.md#flujo-de-trabajo-con-git).

## Antes de comenzar

1. Sincronizar las referencias remotas con `git fetch origin`.
2. Crear la rama de trabajo desde la versión actual de `develop`.
3. Mantener los cambios relacionados con un mismo objetivo en un commit pequeño.

```bash
git fetch origin
git switch develop
git pull --ff-only origin develop
git switch -c docs/SCRUM-n-descripcion
```

Sustituir `SCRUM-n` por la clave real del ticket de Jira.

## Antes de registrar los cambios

Revisar qué archivos se modificaron y comprobar los espacios en blanco:

```bash
git status --short
git diff --check
git diff
```

Agregar únicamente los archivos correspondientes al cambio. Evitar incluir archivos temporales, credenciales, artefactos de compilación o modificaciones ajenas.

## Mensajes de commit

Usar un verbo que describa lo que realmente cambia y, cuando corresponda, la clave de Jira:

```text
docs(SCRUM-n): aclarar instrucciones de configuración
test(SCRUM-n): añadir verificación de un caso existente
fix(SCRUM-n): corregir una validación puntual
```

## Integración

Subir la rama de trabajo y abrir un pull request hacia `develop`. Comprobar las pruebas y solicitar la revisión de otro integrante antes de integrar, de acuerdo con el flujo del equipo.

Esta guía no sustituye las instrucciones de configuración y pruebas de cada componente.
