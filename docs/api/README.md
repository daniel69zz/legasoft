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
