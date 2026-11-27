# Matriz de trazabilidad: implementación de los sprints 2 y 3

> Complementa [`matriz-requisitos.md`](matriz-requisitos.md) con lo implementado en el
> prototipo: cada requisito con la historia del SRS, la historia de Jira que lo entregó,
> el componente que lo implementa, el caso de prueba y las pruebas automatizadas que lo
> verifican. La implementación del Sprint 1 está en [`src/README.md`](../../src/README.md#trazabilidad-del-prototipo).

Las pruebas xUnit llevan `[Trait("JiraIssue", …)]` y `[Trait("Requirement", …)]`, así que
cada fila se puede ejecutar por separado:

```sh
dotnet test Pharmacy.sln --filter "JiraIssue=SCRUM-19"
dotnet test Pharmacy.sln --filter "Requirement=RF-107"
```

## Requisito → historia → componente → prueba

| Requisito | HU del SRS | Historia Jira | Componente (C4) e implementación | Caso de prueba | Pruebas automatizadas |
| --- | --- | --- | --- | --- | --- |
| RNF-003 | HU-T02 | SCRUM-100 · HU-T02.2 | API Gateway (Kestrel TLS 1.2+), Desktop (`GatewayUrlPolicy`), nginx en el VPS (`deploy/vps/nginx-farmacia.conf`) | CP-RNF-003 | `GatewayUrlPolicyTests`; inspección con `openssl s_client` |
| RF-014, RF-015, RF-016 | HU-06 | SCRUM-101 · HU-06.3 | InventoryLocalService (`AdjustStockHandler`), Outbox `StockMovementCreated`, Business Service (`EventApplier`) | CP-RF-014 a CP-RF-016 | `AdjustStockHandlerTests`, `PurchasingApiTests`, `CashierFlowTests` |
| RF-019 | HU-06 | SCRUM-123 · HU-06.4 | Business Service (`GET /api/inventory/movements`), Desktop (Movimientos) | CP-RF-019 | `IncorporatedStoriesApiTests` |
| RF-021 | HU-07 | SCRUM-122 · HU-07.3 | InventoryLocalService (`InventoryQueries.GetLotDetailsAsync`) | CP-RF-021 | `IncorporatedStoriesTests` |
| RF-025 | HU-07 | SCRUM-121 · HU-07.2 | InventoryLocalService (movimiento `Retiro`) | CP-RF-025 | `IncorporatedStoriesTests` |
| RF-026, RF-027 | HU-08 | SCRUM-18 | Business Service (compras y proveedores: `Supplier`, `PurchasingEndpoints`), Desktop (Proveedores) | CP-RF-026, CP-RF-027 | `PurchaseTests`, `PurchasingApiTests` |
| RF-028, RF-029 | HU-08 | SCRUM-18 (entregado con SCRUM-19) | Business Service (productos e historial del proveedor) | CP-RF-028, CP-RF-029 | `PurchasingApiTests` |
| RF-030, RF-031 | HU-09 | SCRUM-19 | Business Service (`Purchase`, `POST /api/purchases`), Desktop (Compras) | CP-RF-030, CP-RF-031 | `PurchaseTests`, `PurchasingApiTests` |
| RF-020, RF-032, RF-033 | HU-07, HU-09 | SCRUM-19 | Business Service (`Purchase.Receive`, lotes y `lot_feed`), Sync Service (`/sync/pull-lots`), Sync Worker | CP-RF-020, CP-RF-032, CP-RF-033 | `PurchaseTests`, `PurchasingApiTests`, `SyncApiTests`, `CashierFlowTests` |
| RF-107, RF-108 | HU-09 | SCRUM-19 | Business Service (recepción con dañadas y faltantes) | CP-RF-107, CP-RF-108 | `PurchaseTests`, `PurchasingApiTests` |
| RF-034, RF-035 | HU-10 | SCRUM-124 · HU-10.1 | Business Service (`SupplierPrice`, precios por proveedor), Desktop (Compras) | CP-RF-034, CP-RF-035 | `IncorporatedStoriesTests`, `IncorporatedStoriesApiTests` |
| RF-036 | HU-10 | SCRUM-125 · HU-10.2 | Business Service (`SupplierPrice.Compare`) | CP-RF-036 | `IncorporatedStoriesTests`, `IncorporatedStoriesApiTests` |
| RF-061, RF-062 | HU-16 | SCRUM-126 · HU-16.2 | CashLocalService (`CashHandlers`), Business Service (`EventApplier`) | CP-RF-061, CP-RF-062 | `IncorporatedStoriesTests`, `IncorporatedStoriesApiTests`, `CashierFlowTests` |
| RF-007 | HU-03 | SCRUM-127 · HU-03.2 | Identity Service (`AuthEndpoints`, `devices.allows_administrator`) | CP-RF-007 | `AdministratorDeviceTests` |
| RF-075 | HU-20 | SCRUM-128 · HU-20.1 | Business Service (`GET /api/audit` con filtros), Desktop (Reportes) | CP-RF-075 | `IncorporatedStoriesApiTests` |
| RF-105 | HU-18 | SCRUM-129 · HU-18.1 | Business Service (`XlsxWriter`, `GET /api/reports/export`) | CP-RF-105 | `IncorporatedStoriesApiTests`, `CashierFlowTests` |
| RF-099 | HU-28 | SCRUM-130 · HU-28.1 | Business Service (`OperationalSettings`), Notification Service, Sync Worker | CP-RF-099 | `IncorporatedStoriesTests`, `IncorporatedStoriesApiTests`, `CashierFlowTests` |
| RNF-007 | HU-T05 | SCRUM-131 · HU-T05.1 | Pipeline de CI (`.github/scripts/cobertura.py`) | CP-RNF-007 | Paso «Cobertura» de `dotnet.yml` |

### Sprint 3

| Requisito | HU del SRS | Historia Jira | Componente (C4) e implementación | Caso de prueba | Pruebas automatizadas |
| --- | --- | --- | --- | --- | --- |
| RF-006 | HU-03 | SCRUM-11 | Identity Service (`AuthEndpoints`: el rechazo del cajero queda en `identity.user_changes`) | CP-RF-006 | `CashierDeviceTests` |
| RF-075 | HU-20 | SCRUM-13 | Business Service (`BusinessDb` solo altas en `audit.entries`; trigger `audit.reject_change` de la migración `Sprint3`), Identity (`IdentityDb`) | CP-RF-075 | `Sprint3ApiTests` |
| RF-044, RF-045 | HU-12 | SCRUM-22 | Pharmacy.Domain (`Sale.MissingFor`), Desktop (Punto de venta: «Faltan Bs…») | CP-RF-044, CP-RF-045 | `Sprint3StoriesTests` |
| RF-050, RF-051 | HU-14 | SCRUM-24 | Pharmacy.Application (`Receipts`, `SimplePdf`), Business Service (`GET /api/reports/receipts`), Desktop (Recibo PDF) | CP-RF-050, CP-RF-051 | `Sprint3StoriesTests`, `Sprint3ApiTests` |
| RF-056 | HU-15 | SCRUM-25 | Pharmacy.Domain (`CashSession.RegisterArqueo`), Outbox `CashArqueoRegistered`, `cash.arqueos`, Desktop (Caja) | CP-RF-056 | `Sprint3StoriesTests` |
| RF-063 a RF-067, RF-101 | HU-17 | SCRUM-27 | Business Service (`POST /api/sales/{id}/invoice`, `InvoicePdf`, `sales.invoices`), Desktop (Factura) | CP-RF-063 a CP-RF-067, CP-RF-101 | `Sprint3ApiTests` |
| RF-084 a RF-087 | HU-24 | SCRUM-28 | Business Service (`/api/customers`, `sales.customers`), Desktop (Clientes, búsqueda en el punto de venta) | CP-RF-084 a CP-RF-087 | `Sprint3ApiTests` |
| RF-088 a RF-091 | HU-25 | SCRUM-29 | Pharmacy.Domain (`Prescription`, `Sale.Complete`), `sales.sales.prescription_*`, Desktop (Punto de venta) | CP-RF-088 a CP-RF-091 | `Sprint3StoriesTests`, `SaleTests` |
| RF-096 | HU-27 | SCRUM-30 | Pharmacy.Domain (`DiscountPolicy`), `RegisterSaleHandler` (permiso `Descuentos`), auditoría «Descuento aplicado» | CP-RF-096 | `Sprint3StoriesTests`, `Sprint3ApiTests` |
| RF-068 a RF-072, RF-103, RF-104 | HU-18 | SCRUM-31 | Business Service (`OperationsReports`, `GET /api/reports/operations/{tipo}` en JSON, Excel o PDF), Desktop (Reportes) | CP-RF-068 a CP-RF-072, CP-RF-103 | `Sprint3ApiTests`, `Sprint3StoriesTests` |
| RF-074 | HU-19 | SCRUM-32 | Business Service (`SalesGrouping`, `GET /api/reports/sales-by-cashier`, `GET /api/dashboard?groupBy=`) | CP-RF-074 | `Sprint3StoriesTests`, `Sprint3ApiTests` |
| RF-097, RF-098 | HU-28 | SCRUM-33 | Business Service (`CompanyProfile`, `/api/settings/company`), Sync Worker, recibos | CP-RF-097, CP-RF-098 | `Sprint3ApiTests`, `Sprint3StoriesTests` |
| RF-100 | HU-29 | SCRUM-34 | Business Service (`Branches`, columna `branch` en ventas, caja y stock; `/api/branches`) | CP-RF-100 | `Sprint3ApiTests` |
| RF-080, RF-081, RF-102 | HU-22 | SCRUM-35 | Business Service (`LotFeed.Publish` en cada cambio de stock), Pharmacy.Application (`LotSync`, `PendingLotDeltas`), Sync Worker (envía y luego baja) | CP-RF-080, CP-RF-081, CP-RF-102 | `LotSyncTests`, `Sprint3StoriesTests`, `Sprint3ApiTests` |
| RF-082, RF-083 | HU-23 | SCRUM-36 | Business Service (`PostgresBackupStore` con COPY, `/api/backups`), Desktop (Parámetros) | CP-RF-082, CP-RF-083 | `Sprint3ApiTests`; recuperación verificada sobre PostgreSQL |
| RNF-001, RNF-002 | HU-T01 | SCRUM-37 | Gateway (cupo por puesto), prueba de carga `tests/performance/Pharmacy.LoadTests` | CP-RNF-001, CP-RNF-002 | Prueba de carga (manual, ver `tests/performance/README.md`) |
| RNF-005 | HU-T03 | SCRUM-38 | Desktop (Punto de venta) | CP-RNF-005 | Protocolo con usuarios: `docs/calidad/prueba-usabilidad-venta.md` |
| RNF-006 | HU-T04 | SCRUM-39 | Sync Worker (`NextDelay`, máximo 5 min), `ViewModelBase` (aviso sin conexión) | CP-RNF-006 | `ContinuityTests`, `CashierFlowTests` sin backend |
| RNF-007 | HU-T05 | SCRUM-40 | Pipeline de CI (`tools/Complexity`, umbral 10 por método) | CP-RNF-007 | Paso «Complejidad ciclomática» de `dotnet.yml` |
| RNF-008 | HU-T06 | SCRUM-41 | Business Service (`BackupScheduler`, `BackupHealth` en `/internal/alerts`), `deploy/vps/respaldo-completo.sh` | CP-RNF-008 | `Sprint3ApiTests`; simulacro en `docs/calidad/respaldo-y-restauracion.md` |

## Historia Jira → modelo de datos

| Historia | Migración EF Core | Tablas o columnas |
| --- | --- | --- |
| SCRUM-18 | `Proveedores` | `inventory.suppliers` |
| SCRUM-19 | `ComprasYRecepcion` | `inventory.purchases`, `inventory.purchase_lines`, `inventory.lot_feed` |
| SCRUM-130 | `ParametrosOperativos` | `inventory.settings` |
| SCRUM-124 | `PreciosDeProveedores` | `inventory.supplier_prices` |
| SCRUM-126 | `EntregaDeCaja` | `cash.sessions.handover_to`, `handover_received_amount`, `handover_confirmed_at` |
| SCRUM-127 | `AdministradorEnEquiposAutorizados` | `identity.devices.allows_administrator` |
| SCRUM-13, 25, 27, 28, 29, 30, 34 | `Sprint3` | trigger de `audit.entries`; `cash.arqueos`; `sales.invoices`; `sales.customers`; `sales.sales.prescription_*`, `discount_authorized_by`; columna `branch` en `sales.sales`, `cash.sessions`, `cash.movements` e `inventory.stock_movements` |

El detalle de columnas y relaciones está en [`modelos/bd/`](../../modelos/bd/).
