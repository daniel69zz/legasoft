#!/usr/bin/env python3
"""HU-T05.1 (RNF-007): resume la cobertura de líneas de las pruebas xUnit y aplica el umbral.

Une los reportes Cobertura que deja coverlet (uno por proyecto de pruebas) sumando las líneas
cubiertas por cualquiera de ellos, calcula la cobertura por ensamblado y la escribe en el
resumen del workflow. Falla si algún servicio de negocio crítico queda por debajo del umbral.

Uso: python3 .github/scripts/cobertura.py TestResults [umbral]
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

# Servicios de negocio críticos que mide RNF-007. El resto se informa, pero no bloquea.
CRITICOS = ["Pharmacy.Domain", "Pharmacy.Application", "Business.Api"]

# Código generado o de infraestructura que no aporta a la medición.
EXCLUIDOS = ("/Migrations/", "\\Migrations\\", "/obj/", "\\obj\\")


def absoluta(archivo, raices):
    """Cobertura guarda las rutas relativas a una de sus raíces <source>, que cambian por reporte."""
    for raiz in raices:
        candidata = os.path.normpath(os.path.join(raiz, archivo))
        if os.path.exists(candidata):
            return candidata
    return os.path.normpath(archivo)


def cargar(carpeta):
    lineas = {}  # (ensamblado, archivo) -> {número de línea: cubierta}
    for ruta in glob.glob(os.path.join(carpeta, "**", "coverage.cobertura.xml"), recursive=True):
        raiz_xml = ET.parse(ruta).getroot()
        raices = [s.text for s in raiz_xml.iter("source") if s.text]
        for paquete in raiz_xml.iter("package"):
            ensamblado = paquete.get("name")
            for clase in paquete.iter("class"):
                archivo = absoluta(clase.get("filename", ""), raices)
                if any(x in archivo for x in EXCLUIDOS):
                    continue
                destino = lineas.setdefault((ensamblado, archivo), {})
                for linea in clase.iter("line"):
                    numero = int(linea.get("number"))
                    destino[numero] = destino.get(numero, False) or int(linea.get("hits", "0")) > 0
    return lineas


def main():
    carpeta = sys.argv[1] if len(sys.argv) > 1 else "TestResults"
    umbral = float(sys.argv[2]) if len(sys.argv) > 2 else 70.0
    lineas = cargar(carpeta)
    if not lineas:
        print(f"No se encontraron reportes coverage.cobertura.xml en {carpeta}")
        return 1

    por_ensamblado = {}
    for (ensamblado, _), cubiertas in lineas.items():
        total, ok = por_ensamblado.get(ensamblado, (0, 0))
        por_ensamblado[ensamblado] = (total + len(cubiertas), ok + sum(cubiertas.values()))

    filas = ["| Ensamblado | Líneas | Cubiertas | Cobertura | RNF-007 |", "| --- | ---: | ---: | ---: | --- |"]
    fallas = []
    for ensamblado in sorted(por_ensamblado):
        total, ok = por_ensamblado[ensamblado]
        porcentaje = 100.0 * ok / total if total else 100.0
        critico = ensamblado in CRITICOS
        estado = ("✅" if porcentaje >= umbral else "❌") if critico else "—"
        if critico and porcentaje < umbral:
            fallas.append(f"{ensamblado} ({porcentaje:.1f} %)")
        filas.append(f"| {ensamblado} | {total} | {ok} | {porcentaje:.1f} % | {estado} |")

    resumen = "\n".join([f"## Cobertura de pruebas (umbral {umbral:.0f} %)", "", *filas, ""])
    print(resumen)
    destino = os.environ.get("GITHUB_STEP_SUMMARY")
    if destino:
        with open(destino, "a", encoding="utf-8") as f:
            f.write(resumen + "\n")

    if fallas:
        print("Cobertura por debajo del umbral en: " + ", ".join(fallas))
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
