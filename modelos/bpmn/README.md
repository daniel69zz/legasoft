# Modelos BPMN

Modelos de procesos de negocio en notación BPMN, elaborados en TikZ.

| Archivo | Código |
| --- | --- |
| `bpmn-proceso.tex` | `LGS-MOD-BPMN-1` |

El documento contiene los dos modelos que sustentan una propuesta de mejora:

- **AS-IS:** el proceso actual, que hace visibles las tareas manuales, los
  registros dispersos y los puntos sin trazabilidad.
- **Hallazgos:** tabla que conecta cada problema observado con su consecuencia
  medible (tiempo, error, retrabajo).
- **TO-BE:** el proceso propuesto, con la digitalización incorporada.

El diagnóstico de procesos es el punto de partida de toda intervención de
Legasoft: la comparación AS-IS / TO-BE es lo que justifica la inversión
tecnológica ante el cliente.

Elementos disponibles: `bpmninicio`, `bpmntarea`, `bpmngateway`,
`bpmnintermedio`, `bpmnfin`, `bpmnflujo`, `bpmnmensaje` y carriles
(`bpmncarril`), definidos en `docs/latex/legasoft-diagramas.sty`.

Compilar con `make modelos` desde la raíz del repositorio.
