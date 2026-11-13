# Documentación de la API

Los puestos y las herramientas externas usan un solo punto de entrada: el
**API Gateway** (`http://localhost:5100`). El Gateway reenvía cada ruta al
servicio que le corresponde (Documento 3, contenedor API Gateway).

| Recurso | Ubicación |
| --- | --- |
| Swagger UI (Identity, Business y Sync en un selector) | <http://localhost:5100/swagger> |
| Documentos OpenAPI | `/docs/identity/swagger.json`, `/docs/business/swagger.json`, `/docs/sync/swagger.json` |
| Colección Postman | [`farmacia.postman_collection.json`](farmacia.postman_collection.json) |

## Uso rápido

1. Levante el backend: `docker compose -p farmacia up -d --build`.
2. **Swagger:** abra <http://localhost:5100/swagger>, ejecute `POST /auth/login`
   en *Identity Service*, copie el `token` y péguelo en **Authorize**.
3. **Postman:** importe la colección y ejecute *Login como admin*. El token
   queda guardado en la variable `token` y lo usan las demás solicitudes.

Los endpoints `/internal/*` (Sync → Business y Notification → Business) no se
documentan ni pasan por el Gateway: se protegen con la cabecera `X-Internal-Key`.

## Endpoints del Sprint 1

| HU · Jira | Método y ruta | Requisito | Rol |
| --- | --- | --- | --- |
| HU-01 · SCRUM-2 | `POST /auth/login` | RF-001 | Todos |
| HU-02 · SCRUM-10 | `GET /api/users` | RF-002 | Administrador |
| HU-02 · SCRUM-10 | `POST /api/users` | RF-003 | Administrador |
| HU-02 · SCRUM-10 | `PUT /api/users/{id}` | RF-004 | Administrador |
| HU-02 · SCRUM-10 | `POST /api/users/{id}/activate` · `/deactivate` | RF-005 | Administrador |
| HU-02 · SCRUM-10 | `GET /api/users/audit` | RF-075 | Administrador |
| HU-05 · SCRUM-15 | `GET /api/catalog?search=` | RF-012 | Permiso Catálogo |
| HU-05 · SCRUM-15 | `POST /api/catalog` | RF-009 | Permiso Catálogo |
| HU-05 · SCRUM-15 | `PUT /api/catalog/{id}` | RF-010 | Permiso Catálogo |
| HU-05 · SCRUM-15 | `POST /api/catalog/{id}/activate` · `/deactivate` | RF-011 | Permiso Catálogo |

El permiso Catálogo lo tienen el Administrador, el Farmacéutico y el Gerente de
Operaciones (matriz `RolePermissions` en `Pharmacy.Domain`).

## Endpoints del Sprint 2

En Swagger, el resumen de cada uno empieza con su historia: `[SCRUM-n · HU-x]`.

| HU · Jira | Método y ruta | Requisito | Rol o permiso |
| --- | --- | --- | --- |
| HU-08 · SCRUM-18 | `GET /api/suppliers` | RF-026 | Proveedores o Compras |
| HU-08 · SCRUM-18 | `POST /api/suppliers` · `PUT /api/suppliers/{id}` · `POST /api/suppliers/{id}/activate` · `/deactivate` | RF-026, RF-027 | Proveedores (Administrador) |
| HU-08 · SCRUM-18 | `GET /api/suppliers/{id}/products` · `/purchases` | RF-028, RF-029 | Proveedores o Compras |
| HU-09 · SCRUM-19 | `GET /api/purchases` · `GET /api/purchases/{id}` · `POST /api/purchases` | RF-030, RF-031 | Compras |
| HU-09 · SCRUM-19 | `POST /api/purchases/{id}/receive` | RF-032, RF-033, RF-107, RF-108 | Compras |
| HU-09 · SCRUM-19 | `GET /sync/pull-lots?since=&snapshot=` | RF-033 | Cualquier puesto autenticado |
| HU-10.1 · SCRUM-124 | `POST /api/suppliers/{id}/prices` · `GET /api/suppliers/{id}/prices` · `/prices/history` | RF-034, RF-035 | Compras o Proveedores |
| HU-10.2 · SCRUM-125 | `GET /api/products/{id}/supplier-prices` | RF-036 | Compras o Proveedores |
| HU-06.4 · SCRUM-123 | `GET /api/inventory/movements` | RF-019 | Inventario |
| HU-20.1 · SCRUM-128 | `GET /api/audit?from=&to=&user=&action=` | RF-075 | Administrador, Gerente General, Contadora |
| HU-18.1 · SCRUM-129 | `GET /api/reports/export` | RF-105 | Reportes |
| HU-28.1 · SCRUM-130 | `GET /api/settings` · `PUT /api/settings` | RF-099 | Lectura: autenticado · Cambio: Administrador |
| HU-03.2 · SCRUM-127 | `POST /auth/login` (403 desde un equipo que no admite al Administrador) | RF-007 | — |

Los ajustes, retiros y entregas de caja (SCRUM-101, SCRUM-121 y SCRUM-126) no tienen
endpoint propio: se guardan en el puesto y llegan al servidor por `POST /sync/push`.

## Producción

El mismo Gateway está publicado en <https://farmacia.bell7738.com> (Swagger en
`/swagger`). En Postman, cambie la variable `baseUrl` a esa dirección.
