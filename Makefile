# Compilación de la documentación LaTeX de Legasoft
#
#   make            compila todos los documentos a PDF
#   make entrega    compila solo el PDF único que se presenta
#   make <ruta>.pdf compila solo uno
#   make clean      borra los PDF generados
#   make list       lista los documentos detectados
#
# Requiere Tectonic (https://tectonic-typesetting.github.io):
#   brew install tectonic
# Tectonic descarga por sí solo los paquetes LaTeX que falten, así que no hace
# falta una distribución TeX completa.

TECTONIC ?= tectonic
TECTONIC_FLAGS ?= -X compile

# Documentos compilables: todo .tex que no sea un paquete de estilo ni un
# fragmento. Los .sty viven en docs/latex/. Los fragmentos —el cuerpo de la
# constitución y los anexos— no tienen preámbulo: se incluyen desde el documento
# de entrega y no se compilan por su cuenta.
FRAGMENTOS := -not -name '*-cuerpo.tex' -not -name 'anexo-*.tex'
SOURCES := $(shell find docs modelos -name '*.tex' -not -path 'docs/latex/*' $(FRAGMENTOS) | sort)
PDFS    := $(SOURCES:.tex=.pdf)

# Si cambia un fragmento, hay que recompilar los documentos que lo incluyen.
FUENTES_INC := $(shell find docs -name '*-cuerpo.tex' -o -name 'anexo-*.tex' | sort)

# Los estilos compartidos: si cambian, se recompila todo.
STYLES := docs/latex/legasoft.sty docs/latex/legasoft-diagramas.sty

.PHONY: all entrega clean list institucional srs entrevistas trazabilidad minutas calidad arquitectura modelos

all: $(PDFS)
	@echo "Listo: $(words $(PDFS)) documento(s) compilado(s)."

# Tectonic resuelve las rutas relativas (\usepackage{../latex/legasoft})
# respecto al directorio del archivo, así que se compila dentro de su carpeta.
%.pdf: %.tex $(STYLES) $(FUENTES_INC)
	@echo "==> $<"
	@cd $(dir $<) && $(TECTONIC) $(TECTONIC_FLAGS) $(notdir $<)

# El PDF único que se presenta al docente.
entrega: docs/entrega/entrega-fase1.pdf
	@echo "Entrega lista: docs/entrega/entrega-fase1.pdf"

# Atajos por carpeta
institucional: $(filter docs/institucional/%,$(PDFS))
srs:           $(filter docs/srs/%,$(PDFS))
entrevistas:   $(filter docs/entrevistas/%,$(PDFS))
trazabilidad:  $(filter docs/trazabilidad/%,$(PDFS))
minutas:       $(filter docs/minutas/%,$(PDFS))
calidad:       $(filter docs/calidad/%,$(PDFS))
arquitectura:  $(filter docs/arquitectura/%,$(PDFS))
modelos:       $(filter modelos/%,$(PDFS))

list:
	@echo "Documentos detectados:"
	@printf '  %s\n' $(SOURCES)

clean:
	@rm -f $(PDFS)
	@find docs modelos -name '*.aux' -o -name '*.log' -o -name '*.toc' \
	   -o -name '*.out' -o -name '*.synctex.gz' | xargs -r rm -f
	@echo "PDF y archivos auxiliares eliminados."
