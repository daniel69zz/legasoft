# Plan de contingencia de la demo — Sprint 1

Objetivo: que la demostración del incremento no dependa de Internet, del
proyector ni de una sola computadora.

## Preparación (el día anterior)

| # | Acción | Responsable |
| --- | --- | --- |
| 1 | Clonar `main` en **dos** computadoras (principal y respaldo) y ejecutar `dotnet test Pharmacy.sln`: todo en verde | Pedro |
| 2 | Construir las imágenes con Internet: `docker compose -p farmacia build` y `docker compose -p farmacia pull`. Así el día de la demo no se descarga nada | Luis |
| 3 | Levantar el backend una vez (`docker compose -p farmacia up -d`) y comprobar <http://localhost:5100/health> y <http://localhost:5100/swagger> | Luis |
| 4 | Abrir el Desktop una vez por puesto (`PUESTO-01`) con el backend arriba, para que descargue el catálogo y guarde las credenciales en caché | Pedro |
| 5 | Grabar un **video de respaldo** (3–4 min) con el guion de abajo | Pedro |
| 6 | Capturas de evidencia en una carpeta local: tablero y burndown de Jira, PR con la clave Jira, ejecución verde de GitHub Actions, Swagger | Oziel |
| 7 | Exportar las diapositivas a PDF y copiarlas, junto con el video, en una memoria USB | Oziel |

## Guion de la demo (HU del Sprint 1)

1. **HU-01:** iniciar sesión como `cajero1` con una contraseña errónea (mensaje
   genérico) y luego con la correcta: el menú muestra solo Punto de venta, Caja y
   Sincronización.
2. **HU-02:** iniciar sesión como `admin`, registrar `cajero3`, cambiarle el rol
   con **Editar** y desactivarlo. `cajero3` ya no puede iniciar sesión. Mostrar la
   auditoría.
3. **HU-05:** iniciar sesión como `farmaceutico`, abrir **Catálogo**, registrar
   `MED-001 Ibuprofeno 400 mg`, intentar repetir el código (rechazado), buscarlo
   por principio activo y desactivarlo. Como cajero, el producto ya no aparece en
   el punto de venta.
4. **Evidencia técnica:** Swagger (`/swagger`), pipeline en verde y
   `dotnet test --filter "JiraIssue=SCRUM-15"`.

## Si algo falla

| Problema | Respuesta |
| --- | --- |
| No hay Internet | El backend y el Desktop corren en local con Docker; las imágenes ya están construidas (paso 2). Jira y GitHub se muestran con las capturas (paso 6) |
| El puerto 5433 o 5100 está ocupado | Copiar `.env.example` a `.env`, cambiar `POSTGRES_PORT` o `GATEWAY_PORT` y abrir el Desktop con `--Desktop:GatewayUrl=http://localhost:<puerto>` |
| El backend no arranca | Mostrar el modo sin conexión: el puesto inicia sesión con las credenciales en caché y sigue vendiendo (barra «Sin conexión · N pendientes»). Luego, `docker compose -p farmacia down -v && docker compose -p farmacia up -d` recrea la base desde las migraciones con los datos de demostración |
| Falla la computadora principal | Cambiar a la de respaldo, ya preparada con los pasos 1 a 4 |
| Falla el proyector o la demo en vivo | Reproducir el video de respaldo y seguir con las diapositivas en PDF |

## Datos de demostración

Los cargan las migraciones y las semillas al arrancar: un usuario por rol
(`admin`, `gerente`, `contadora`, `operaciones`, `cajero1`, `cajero2`,
`farmaceutico`, `almacen`) y un catálogo con lotes por vencer, vencidos y con
stock mínimo. La contraseña está en `Seed:DemoPassword` de
`src/backend/Identity.Api/appsettings.json`.
