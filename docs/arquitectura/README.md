# Arquitectura

Documentación técnica y decisiones de arquitectura del sistema.

| Archivo | Código | Estado |
| --- | --- | --- |
| `documento-arquitectura.tex` | `LGS-ARQ-001` | Plantilla |
| `adr-plantilla.tex` | `LGS-ADR-NNN` | Plantilla |

**`documento-arquitectura.tex`** describe la solución siguiendo el modelo C4:
restricciones y atributos de calidad, vistas de contexto, contenedores y
componentes, modelo de datos, contratos de integración, seguridad y despliegue.
Los diagramas se elaboran en `modelos/c4/` y se incorporan aquí como figuras.

**`adr-plantilla.tex`** registra una decisión de arquitectura. Para crear una:

```sh
mkdir -p adr
cp adr-plantilla.tex adr/adr-001-eleccion-backend.tex
```

Un ADR se escribe cuando la decisión condiciona la evolución del producto y
revertirla sería costoso. Los ADR **no se editan una vez aceptados**: si la
decisión cambia, se crea uno nuevo que reemplaza al anterior.

Aquí se registrarán las tecnologías de frontend, backend y pruebas cuando se
seleccionen.

Compilar con `make arquitectura` desde la raíz del repositorio.
