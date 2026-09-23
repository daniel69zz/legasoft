# Legasoft

Legasoft es una consultora académica de transformación digital e ingeniería de
software. Su trabajo consiste en acompañar a una organización cliente en el
análisis de sus procesos, la definición de sus requisitos y el diseño y
construcción de una solución de software que responda a esas necesidades.

## Propósito del repositorio

Este repositorio concentra en un único lugar todo el trabajo del equipo:
la documentación institucional y de proyecto, los modelos y diagramas, el
código fuente y las pruebas. La estructura de carpetas está pensada para que
cada entregable tenga una ubicación clara y para que el avance del proyecto
sea trazable a lo largo del curso.

La documentación se escribe en LaTeX y se compila a PDF con `make`.

## Estructura de carpetas

```text
.
├── README.md
├── .gitignore
├── Makefile             Compila toda la documentación a PDF
├── docs/
│   ├── entrevistas/     Minutas y transcripciones del relevamiento (ENT / VAL)
│   ├── srs/             Especificación de requisitos (SRS), historias de usuario
│   ├── trazabilidad/    Matrices de trazabilidad NEC → RF → HU → C4 → CP
│   ├── arquitectura/    Documento de arquitectura (C4) y decisiones (ADR)
│   ├── latex/           Estilo compartido de todos los documentos (.sty)
│   ├── institucional/   Constitución de la empresa, actas y anexos
│   ├── cv/              Hojas de vida del equipo
│   ├── entrega/         Documentos de entrega consolidados (PDF versionado)
│   ├── minutas/         Minutas y acuerdos de reuniones
│   └── calidad/         Estrategia de QA, plan de pruebas y casos de prueba
├── modelos/
│   ├── c4/              Diagramas del modelo C4 (contexto, contenedores, componentes)
│   ├── uml/             Diagramas UML (casos de uso, clases, secuencia)
│   └── bpmn/            Modelos de procesos de negocio en BPMN (AS-IS / TO-BE)
├── src/
│   ├── frontend/        Código fuente de la interfaz de usuario
│   └── backend/         Código fuente del servidor y la lógica de negocio
├── tests/
│   ├── unit/            Pruebas unitarias
│   ├── integration/     Pruebas de integración
│   └── acceptance/      Pruebas de aceptación
└── .github/
    └── workflows/       Flujos de integración y entrega continua (CI/CD)
```

Cada carpeta incluye su propio `README.md` con una descripción de lo que
contiene.

## Documentos

| Código | Documento | Archivo |
| --- | --- | --- |
| `LGS-INST-001` | Constitución y estructura organizacional | `docs/institucional/constitucion-legasoft.tex` |
| `LGS-INST-002` | Acta de constitución de la consultora | `docs/institucional/anexo-acta-constitucion.tex` |
| `LGS-INST-004` | Infraestructura de trabajo y gestión de configuración | `docs/institucional/anexo-infraestructura.tex` |
| `LGS-PRY-001` | Acta de constitución del proyecto (plantilla) | `docs/institucional/acta-constitucion-proyecto.tex` |
| `LGS-ENT-001` | Entrega fase 1: constitución de Legasoft | `docs/entrega/entrega-fase1.tex` |
| `LGS-REQ-001` | Especificación de requisitos (SRS) | `docs/srs/srs.tex` |
| `LGS-CAL-001` | Estrategia de calidad y plan de pruebas | `docs/calidad/estrategia-qa.tex` |
| `LGS-ARQ-001` | Documento de arquitectura | `docs/arquitectura/documento-arquitectura.tex` |
| `LGS-ADR-NNN` | Plantilla de decisión de arquitectura | `docs/arquitectura/adr-plantilla.tex` |
| `LGS-MIN-NNN` | Plantilla de minuta de reunión | `docs/minutas/plantilla-minuta.tex` |
| `LGS-MOD-*` | Diagramas C4, UML y BPMN | `modelos/` |

Los documentos `.tex` son plantillas institucionales de Legasoft, con los campos
variables marcados en ámbar (`[así]`). Los entregables del proyecto
(`DOCn-*.pdf`) se describen en **Estado actual**.

## Compilación de la documentación

Requiere [Tectonic](https://tectonic-typesetting.github.io), que descarga por sí
solo los paquetes LaTeX que necesite:

```sh
brew install tectonic
```

```sh
make                 # compila todos los documentos a PDF
make list            # lista los documentos detectados
make institucional   # compila solo una carpeta
make clean           # borra los PDF generados
```

Un documento suelto también se compila por su cuenta:

```sh
cd docs/institucional && tectonic -X compile constitucion-legasoft.tex
```

Los PDF son artefactos de compilación y están excluidos por `.gitignore`. Si el
curso exige entregarlos versionados, comente las dos líneas correspondientes.

## Flujo de trabajo con Git

- **`main`** se mantiene siempre en un estado presentable; no se hacen commits
  directos sobre ella.
- **Una rama por entregable o funcionalidad**, con el prefijo de su ámbito:
  `feat/`, `docs/`, `fix/`, `test/`, `refactor/` o `chore/`
  (por ejemplo `feat/modelos-c4`).
- **Commits semánticos** según [Conventional Commits](https://www.conventionalcommits.org/es/):
  `tipo: descripción en imperativo` (`feat: agregar diagrama C4 de contexto`).
- **Integración mediante pull request** hacia `main`, con merge commit para
  conservar la historia de la rama.

## Roles del equipo

| Rol | Integrante | Responsabilidades principales |
| --- | --- | --- |
| Director de Proyecto y Analista de Sistemas | Oziel Rodman Ramos Torrez | Planificación y seguimiento del proyecto, coordinación del equipo, relación con la parte interesada, levantamiento y análisis de requisitos. |
| Arquitecto de Software y Desarrollador Backend | Luis Daniel Rojas Cáceres | Diseño de la arquitectura de la solución, decisiones técnicas y desarrollo del backend. |
| Especialista en QA y Desarrollador Frontend | Pedro Andrez Condorena Apaza | Estrategia y ejecución de pruebas, aseguramiento de la calidad y desarrollo de la interfaz de usuario. |

## Estado actual

Proyecto en curso: **Sistema de Gestión para Farmacéutica** (farmacia en
apertura en Sopocachi, La Paz), aplicación de escritorio *local-first* con
servicios cloud ASP.NET Core y PostgreSQL. Todos los cobros son en efectivo y
la facturación es simulada.

| Documento | Contenido | Ubicación |
| --- | --- | --- |
| Documento 1 | Minuta de entrevistas y relevamiento (ENT-001, ENT-002, VAL-001; 38 necesidades) | [`docs/entrevistas/`](docs/entrevistas/) |
| Documento 2 | SRS ISO 29148: 101 RF, 8 RNF, historias y criterios de aceptación | [`docs/srs/`](docs/srs/) |
| Documento 3 | Arquitectura con modelo C4 (niveles 1 a 3) y ADR-001 a ADR-008 | [`docs/arquitectura/`](docs/arquitectura/) |
| Diagramas C4 | Contexto, contenedores y componentes (Desktop y Backend) | [`modelos/c4/`](modelos/c4/) |
| Trazabilidad | NEC → RF/RNF → HU → componente C4 → caso de prueba | [`docs/trazabilidad/`](docs/trazabilidad/) |

Las tecnologías seleccionadas (C# .NET 8, Avalonia UI, ASP.NET Core, SQLite,
PostgreSQL, Docker) están registradas como ADR en
[`docs/arquitectura/adr/`](docs/arquitectura/adr/).
