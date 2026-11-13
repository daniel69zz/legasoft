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
9. Sprint 2 · Ingresar como `admin` y abrir **Proveedores**: registrar uno (sin
   teléfono se rechaza indicando el dato) y ver su historial de compras.
10. Ingresar como `operaciones` y abrir **Compras**: la compra `OC-00001` de la
    semilla está pendiente. Recibirla con lote y vencimiento, por ejemplo 90
    tabletas en buen estado y 10 dañadas: la diferencia se resalta y solo
    ingresan 90. Tras la sincronización, el lote nuevo aparece en el
    **Inventario** del puesto y se vende con FEFO.
11. En **Inventario**, elegir un producto y registrar un **Ajuste** de −3 con el
    motivo «Producto dañado» (también sin conexión). Sin motivo se rechaza. El
    movimiento queda en el historial y llega al servidor por el Outbox. Con
    **Retiro** se da de baja un lote vencido, y la tarjeta **Stock por lote**
    muestra cada lote con su estado.
12. Como `cajero1`, después de cerrar caja, **Entregar** a `cajero2`. Al entrar
    `cajero2`, la caja no se abre hasta que confirme el monto contado; al
    confirmarlo, su turno arranca con ese monto.
13. Como `operaciones`, en **Compras** elegir proveedor y producto: registrar el
    precio ofrecido, ver el historial y la comparación entre proveedores; al
    agregar el producto a la compra, el costo se propone con el precio vigente.
14. Como `admin`: **Parámetros** (umbral de vencimiento y frecuencia de alertas),
    **Movimientos** (historial consolidado con filtros), **Reportes** (auditoría
    filtrada y exportación a Excel). El administrador no inicia sesión en
    `PUESTO-02` (RF-007).

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
| HU-08 · RF-026 a RF-029 | `Supplier`, `PurchasingEndpoints.cs`, `SuppliersViewModel` | `PurchaseTests`, `PurchasingApiTests` |
| HU-09 · RF-030 a RF-033, RF-020, RF-107, RF-108 | `Purchase.Receive`, `PurchasingEndpoints.cs`, `PurchasesViewModel`, `/internal/lots` + `SyncWorker.PullLotsAsync` | `PurchaseTests`, `PurchasingApiTests`, `SyncApiTests` |
| HU-06.3 · RF-014 a RF-016 | `AdjustStockHandler`, evento `StockMovementCreated`, `InventoryViewModel` | `AdjustStockHandlerTests`, `PurchasingApiTests`, `CashierFlowTests` |
| HU-T02.2 · RNF-003 | `GatewayUrlPolicy`, Kestrel con TLS 1.2+, `deploy/vps/Caddyfile` | `GatewayUrlPolicyTests` |
| HU-07.2 · RF-025 | Tipo `Retiro` en `AdjustStockHandler` e Inventario | `IncorporatedStoriesTests` |
| HU-07.3 · RF-021 | `InventoryQueries.GetLotDetailsAsync`, tarjeta «Stock por lote» | `IncorporatedStoriesTests` |
| HU-06.4 · RF-019 | `GET /api/inventory/movements`, `MovementsViewModel` | `IncorporatedStoriesApiTests` |
| HU-10.1, HU-10.2 · RF-034 a RF-036 | `SupplierPrice`, endpoints de precios, tarjeta de precios en Compras | `IncorporatedStoriesTests`, `IncorporatedStoriesApiTests` |
| HU-16.2 · RF-061, RF-062 | `CashHandlers.RequestHandoverAsync` / `ConfirmHandoverAsync`, panel de entrega en Caja | `IncorporatedStoriesTests`, `IncorporatedStoriesApiTests`, `CashierFlowTests` |
| HU-03.2 · RF-007 | `AuthorizedDevice.AllowsAdministrator` en `AuthEndpoints.cs` | `AdministratorDeviceTests` |
| HU-20.1 · RF-075 | `GET /api/audit` con filtros, Reportes | `IncorporatedStoriesApiTests` |
| HU-18.1 · RF-105 | `XlsxWriter`, `GET /api/reports/export`, Reportes | `IncorporatedStoriesApiTests`, `CashierFlowTests` |
| HU-28.1 · RF-099 | `OperationalSettings`, `PUT /api/settings`, `SettingsViewModel`, Notification Worker | `IncorporatedStoriesTests`, `IncorporatedStoriesApiTests` |
| HU-T05.1 · RNF-007 | `.github/scripts/cobertura.py` en `dotnet.yml` (umbral 70 %) | Paso «Cobertura» de la CI |

## Límites conocidos del prototipo

- La base local SQLite del puesto se crea con `EnsureCreated`; las migraciones
  EF Core solo cubren la base central.
- El catálogo se administra en línea: sin conexión, el puesto vende con el
  catálogo descargado pero no puede registrar ni modificar medicamentos.
- La descarga desde el servidor trae catálogo, precios y los **lotes nuevos**
  creados al recibir compras. El stock de los lotes existentes lo mueve cada
  puesto; las ventas y ajustes de un puesto no llegan a los demás puestos.
- Proveedores y compras se administran en línea, como el catálogo. Una compra se
  recibe una sola vez: lo que no llegó queda registrado como faltante.
- La entrega de caja entre cajeros está en el dominio, sin pantalla.
- Facturación simulada, clientes, recetas como entidad, exportación a PDF y
  respaldos (RF-082, RF-083) quedan para los próximos sprints.
- La restricción de equipos del administrador (RF-007) se aplica al iniciar sesión
  en línea; sin conexión, el puesto valida solo las credenciales guardadas.
- TLS (RNF-003): en el VPS lo termina Caddy con `PUBLIC_DOMAIN` en `.env`
  (`deploy/vps/Caddyfile`), y el Desktop solo acepta `http://` contra localhost.
  Sin refresh tokens.
- La contraseña del usuario se conserva **solo en memoria** durante la sesión
  para reconectar tras un inicio sin conexión, y las credenciales de demostración
  se precargan en el puesto para probar el modo sin conexión desde el primer arranque.
