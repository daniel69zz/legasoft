# Prueba de usabilidad del flujo de venta (SCRUM-38 · HU-T03 · RNF-005)

**Criterio:** un cajero con 30 minutos de capacitación como máximo completa una venta estándar sin ayuda en
3 minutos o menos. Si tarda más o pide ayuda, el requisito no se acepta y se registran los puntos de fricción.

Esta prueba la hace una persona; el software ya la soporta: la venta estándar son **cuatro acciones**
(buscar, «Agregar», escribir el efectivo recibido, «Cobrar y emitir recibo»), recorridas de forma
automática en `CashierFlowTests`.

## Preparación

1. Puesto con la versión del Sprint 3, caja abierta y el catálogo de demostración.
2. Participantes: al menos 3 personas que no hayan usado el sistema (idealmente cajeros de la farmacia).
3. Capacitación de 30 minutos como máximo, con este guion: inicio de sesión, apertura de caja, buscar un
   producto, agregarlo, cambiar la cantidad, cobrar y leer el cambio. No se muestra la venta de la prueba.

## Tarea

> «Un cliente pide 2 tabletas de paracetamol 500 mg y 1 de loratadina 10 mg. Paga con Bs 20.
> Registre la venta y dígale cuánto es el cambio.»

El observador toma el tiempo desde que el cajero toca la búsqueda hasta que aparece el recibo, sin ayudar.

## Registro

| Participante | Capacitación (min) | Tiempo de la venta | ¿Pidió ayuda? | ¿Cambio correcto? | Puntos de fricción | Resultado |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | | | | | | |
| 2 | | | | | | |
| 3 | | | | | | |

**Se acepta** si todos los participantes terminan en 3 minutos o menos, sin ayuda y con el cambio correcto.
Los puntos de fricción se convierten en historias para el siguiente sprint.
