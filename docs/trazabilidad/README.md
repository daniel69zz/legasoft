# Trazabilidad

Matrices que relacionan los artefactos del proyecto entre sí:

```text
Entrevista (ENT/VAL) → Necesidad (NEC) → Requisito (RF/RNF) → Historia (HU)
                                        → Contenedor / componente C4 → Prueba (CP)
```

La trazabilidad permite recorrer el camino en ambos sentidos: desde una
necesidad del cliente hasta el componente que la implementa, y desde un
componente hasta la necesidad que lo justifica.

| Archivo | Sentido | Contenido |
| --- | --- | --- |
| [`matriz-requisitos.md`](matriz-requisitos.md) | Requisito → artefactos | 109 requisitos (101 RF + 8 RNF) con fuente, historia Jira, componente C4 y caso de prueba |
| [`matriz-inversa.md`](matriz-inversa.md) | Artefacto → requisitos | 33 historias de usuario con los requisitos que cubren, y cobertura de las 38 necesidades `NEC` |
| [`matriz-arquitectura.md`](matriz-arquitectura.md) | Requisito ↔ arquitectura | Áreas funcionales y RNF relacionados con contenedores, componentes C4 y ADR |

## Cobertura

- Las **38 necesidades** del catálogo (`NEC-01` a `NEC-40`, sin `NEC-13` ni
  `NEC-36`) tienen al menos un requisito asociado.
- Los **109 requisitos** tienen historia de usuario, componente C4 y caso de
  prueba asignados.

## Fuentes

- Documento 1 — [`docs/entrevistas/`](../entrevistas/): sesiones y catálogo `NEC`.
- Documento 2 — [`docs/srs/`](../srs/): sección 6, matriz de trazabilidad bidireccional.
- Documento 3 — [`docs/arquitectura/`](../arquitectura/): sección 9, relación con los requisitos.
