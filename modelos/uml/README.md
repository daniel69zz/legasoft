# Modelos UML

Diagramas UML que describen la estructura y el comportamiento del sistema,
elaborados en TikZ.

| Archivo | Diagrama | Código |
| --- | --- | --- |
| `uml-casos-uso.tex` | Casos de uso | `LGS-MOD-UML-1` |
| `uml-clases.tex` | Clases | `LGS-MOD-UML-2` |
| `uml-secuencia.tex` | Secuencia | `LGS-MOD-UML-3` |

- **Casos de uso:** actores, casos (`CU-NN`) y relaciones `«include»`. Cada caso
  se vincula a un requisito funcional de `docs/requisitos/`.
- **Clases:** entidades del dominio con atributos, operaciones, multiplicidades
  y relaciones (herencia, composición, agregación, asociación).
- **Secuencia:** un escenario concreto, normalmente el flujo principal de un
  caso de uso, con líneas de vida, barras de activación, mensajes y retornos.

Las clases y los mensajes que aparecen son un ejemplo de la notación, no un
modelo del dominio del cliente.

El diagrama de clases describe el dominio en objetos; el modelo de datos
relacional que lo persiste se documenta aparte, en `LGS-ARQ-001`.

Compilar con `make modelos` desde la raíz del repositorio.
