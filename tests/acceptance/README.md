# Pruebas de aceptación

Pruebas que validan el sistema frente a los criterios de aceptación acordados
en `docs/srs/`.

Cada requisito tiene su caso de prueba (`CP-RF-NNN` / `CP-RNF-NNN`), definido
en la matriz de trazabilidad
([`docs/trazabilidad/matriz-requisitos.md`](../../docs/trazabilidad/matriz-requisitos.md)).
Un requisito se considera aceptado cuando su caso de prueba se ejecuta con el
resultado esperado.

Proyecto: [`Pharmacy.Desktop.UiTests`](Pharmacy.Desktop.UiTests/) recorre la
jornada de un cajero en la interfaz Avalonia sin pantalla (headless). Con la
variable `SCREENSHOTS_DIR` guarda una captura por pantalla.
