# Prototipo inicial: Sistema de Gestión para Farmacéutica

Primer incremento ejecutable de la arquitectura del Documento 3: un puesto
*local-first* que vende, cobra y cierra caja sin Internet, y un backend que
consolida lo sincronizado. No es el producto final: cubre de punta a punta los
flujos críticos para validar la arquitectura antes de completar los módulos.

## Estructura

```text
Pharmacy.sln
src/
├── shared/
│   ├── Pharmacy.Domain/         Reglas de negocio: FEFO, lotes vencidos, venta, caja, alertas, roles (ADR-004)
│   └── Pharmacy.Application/    Casos de uso, validaciones (FluentValidation), interfaces y contratos
├── frontend/
│   └── Pharmacy.Desktop/        Avalonia MVVM + SQLite + Outbox + Sync Worker (ADR-002, 003, 007)
└── backend/
    ├── Gateway/                 API Gateway con YARP: rutas, límite de solicitudes, correlación
    ├── Identity.Api/            Login JWT, roles, dispositivos autorizados, asistencia, bloqueo
    ├── Sync.Api/                Fachada de sincronización: idempotencia y versiones (ADR-008)
    ├── Business.Api/            Ventas, inventario, caja, reportes y auditoría consolidados
    ├── Notification.Worker/     Alertas de vencimiento y stock mínimo por correo (SMTP)
    └── Pharmacy.ServiceDefaults/ Serilog, ProblemDetails, JWT y acceso a PostgreSQL
tests/
├── unit/Pharmacy.UnitTests/                 Dominio y casos de uso
├── integration/Pharmacy.IntegrationTests/   Servicios con WebApplicationFactory
└── acceptance/Pharmacy.Desktop.UiTests/     Interfaz Avalonia en modo headless
deploy/                          Dockerfile de los servicios y schemas de PostgreSQL
docker-compose.yml               Backend completo para desarrollo
```

## Ejecución

Requisitos: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) y
Docker Desktop.

```sh
# 0. (Opcional) variables propias; sin .env se usan los valores de desarrollo
cp .env.example .env

# 1. Backend: PostgreSQL con schemas por dominio + servicios + Mailpit.
#    Cada servicio aplica sus migraciones EF Core al arrancar.
docker compose -p farmacia up -d --build

# 2. Aplicación de escritorio (puesto PUESTO-01)
dotnet run --project src/frontend/Pharmacy.Desktop

# Segundo puesto en la misma computadora
dotnet run --project src/frontend/Pharmacy.Desktop -- --Desktop:DeviceId=PUESTO-02

# 3. Pruebas (unitarias, integración y aceptación de la interfaz)
dotnet test Pharmacy.sln
```

| Servicio | Dirección |
| --- | --- |
| API Gateway (lo usa el Desktop) | <http://localhost:5100> (`/health`) |
| PostgreSQL | `localhost:5433`, base `farmacia` |
| Documentación de la API (Swagger UI) | <http://localhost:5100/swagger> |
| Correos de alerta (Mailpit) | <http://localhost:8025> |

**Usuarios de demostración** (uno por rol del SRS): `admin`, `gerente`,
`contadora`, `operaciones`, `cajero1`, `cajero2`, `farmaceutico`, `almacen`.
La contraseña de demostración está en `Seed:DemoPassword` de
`src/backend/Identity.Api/appsettings.json`. Las claves de `appsettings.json`,
`.env.example` y `docker-compose.yml` son solo para desarrollo.

| Variable de `.env` | Uso | Valor por defecto |
| --- | --- | --- |
| `POSTGRES_PASSWORD` | Superusuario de PostgreSQL | `postgres_dev` |
| `POSTGRES_PORT` | Puerto publicado de PostgreSQL | `5433` |
| `JWT_KEY` | Firma del JWT (mín. 32 caracteres), igual en todos los servicios | clave de desarrollo |
| `INTERNAL_KEY` | Clave de los endpoints `/internal` entre servicios | clave de desarrollo |
| `GATEWAY_PORT` | Puerto del API Gateway (el Desktop usa `--Desktop:GatewayUrl` si cambia) | `5100` |
| `MAILPIT_PORT` | Bandeja de correos de alerta | `8025` |
| `ALERT_INTERVAL_MINUTES` | Frecuencia del Notification Worker | `5` |

Si otro proyecto ya usa el puerto 5433 o 5100, cambie `POSTGRES_PORT` o
`GATEWAY_PORT` en `.env`. El volumen de datos se llama `postgres-data`; si
quedó el volumen anterior `pgdata` (creado antes de las migraciones), puede
borrarse con `docker volume rm farmacia_pgdata`.

### Demostración sugerida

1. Ingresar como `cajero1`, abrir caja con Bs 200 y vender 130 tabletas de
   paracetamol: el recibo muestra que FEFO tomó primero el lote que vence en
   25 días y completó con el siguiente.
2. Intentar vender amoxicilina sin receta (se bloquea) y con receta (se vende).
3. Detener el backend (`docker compose -p farmacia stop`), seguir vendiendo:
   la barra superior muestra «Sin conexión · N pendientes».
4. Volver a levantarlo (`docker compose -p farmacia start`): el Sync Worker
   envía la cola en orden y la pantalla **Sincronización** la marca como sincronizada.
5. Cerrar caja contando por denominación: con diferencia exige justificación.
6. Ingresar como `gerente` y abrir **Reportes**: ventas consolidadas por cajero.
7. Ingresar como `admin`: registrar un usuario, cambiarle el rol con **Editar** y
   desactivarlo; cada cambio queda en auditoría.
8. Ingresar como `farmaceutico` y abrir **Catálogo**: registrar un medicamento,
   buscarlo por principio activo y desactivarlo. Tras la sincronización ya no
   aparece en el punto de venta del cajero.

## Trazabilidad del prototipo

Cada prueba xUnit lleva la clave de Jira y el requisito que verifica:

```csharp
[Trait("JiraIssue", "SCRUM-15")]
[Trait("Requirement", "RF-009")]
```

Así se pueden correr las pruebas de una historia o de un requisito:

```sh
dotnet test Pharmacy.sln --filter "JiraIssue=SCRUM-15"
dotnet test Pharmacy.sln --filter "Requirement=RF-001"
```

| HU / requisito | Implementación | Prueba |
| --- | --- | --- |
| HU-01 · RF-001, RNF-004 | `Identity.Api/AuthEndpoints.cs`, `LoginViewModel` | `IdentityApiTests` (login, mensaje genérico, bloqueo) |
| HU-02 · RF-002 a RF-005, RF-075 | `UserEndpoints.cs`, `RolePermissions`, `UsersViewModel` | `IdentityApiTests` (403, alta, modificación de rol, desactivación, auditoría) |
| HU-05 · RF-009 a RF-012 | `CatalogEndpoints.cs`, `Product.UpdateDetails`, `CatalogViewModel` | `ProductTests`, `CatalogApiTests` |
| RF-006, RF-008 | Dispositivos autorizados y asistencia en `AuthEndpoints.cs` | `Cajero_en_equipo_no_autorizado_es_rechazado` |
| RF-037 a RF-047, RF-049 | `Sale`, `RegisterSaleHandler`, `PosViewModel` | `SaleTests`, `RegisterSaleHandlerTests` |
| RF-024, RF-095 | `FefoAllocator`, `Lot.Withdraw` | `FefoAllocatorTests` |
| RF-048 | `Sale.Void`, `VoidSaleHandler` | `Anular_devuelve_el_stock_a_su_lote` |
| RF-091 | `Sale.Complete` | `Exige_receta_para_medicamentos_que_la_requieren` |
| RF-052 a RF-060, RF-106 | `CashSession`, `CashHandlers`, `CashViewModel` | `CashSessionTests`, `CashierFlowTests` |
| RF-061, RF-062 | `CashSession.RequestHandover` / `ConfirmHandover` (solo dominio) | `La_entrega_queda_pendiente_...` |
| RF-013, RF-018, RF-022, RF-023 | `InventoryAlerts`, `InventoryQueries`, `Notification.Worker` | `AlertsAndPermissionsTests` |
| RF-080, RF-081, RF-102 | Outbox en `LocalDb`, `SyncWorker`, `Sync.Api` | `SyncApiTests` (orden, duplicados, reintento) |
| RF-104, RF-073, RF-074 | `Business.Api/EventApplier.cs`, `/api/dashboard` | `BusinessApiTests` |

## Límites conocidos del prototipo

- La base local SQLite del puesto se crea con `EnsureCreated`; las migraciones
  EF Core solo cubren la base central.
- El catálogo se administra en línea: sin conexión, el puesto vende con el
  catálogo descargado pero no puede registrar ni modificar medicamentos.
- La descarga desde el servidor trae catálogo y precios, **no lotes nuevos**; la
  recepción de compras (RF-030 a RF-033) solo existe como `POST /api/lots`.
- La entrega de caja entre cajeros está en el dominio, sin pantalla.
- Facturación simulada, clientes, recetas como entidad, proveedores, compras,
  exportación a Excel/PDF y respaldos (RF-082, RF-083) quedan para los próximos sprints.
- Sin refresh tokens ni TLS (en producción lo termina el Gateway, RNF-003); el
  administrador puede entrar desde cualquier equipo (RF-007 pendiente).
- La contraseña del usuario se conserva **solo en memoria** durante la sesión
  para reconectar tras un inicio sin conexión, y las credenciales de demostración
  se precargan en el puesto para probar el modo sin conexión desde el primer arranque.
