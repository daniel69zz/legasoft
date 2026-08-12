# Minutas de reunión

Registro de las reuniones del equipo y de los acuerdos alcanzados.

| Archivo | Código | Estado |
| --- | --- | --- |
| `plantilla-minuta.tex` | `LGS-MIN-NNN` | Plantilla |

Para levantar una minuta, copie la plantilla:

```sh
cp plantilla-minuta.tex minuta-001-levantamiento.tex
```

Complete los campos, elimine la caja «Cómo usar esta plantilla» y compile.
La numeración es correlativa (`minuta-001`, `minuta-002`…).

La plantilla cubre datos de la reunión, participantes, agenda, desarrollo,
acuerdos (`A-NN`), compromisos con responsable y fecha (`C-NN`), temas
pendientes y próxima reunión.

La preparación y conservación de las minutas corresponde al Director de
Proyecto y Analista de Sistemas; el seguimiento de los compromisos se lleva en
Jira.

Compilar con `make minutas` desde la raíz del repositorio.
