# Matriz de trazabilidad: implementación del Sprint 2

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
| RNF-003 | HU-T02 | SCRUM-100 · HU-T02.2 | API Gateway (Kestrel TLS 1.2+), Desktop (`GatewayUrlPolicy`), Caddy en el VPS (`deploy/vps/Caddyfile`) | CP-RNF-003 | `GatewayUrlPolicyTests`; inspección con `openssl s_client` |
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

## Historia Jira → modelo de datos

| Historia | Migración EF Core | Tablas o columnas |
| --- | --- | --- |
| SCRUM-18 | `Proveedores` | `inventory.suppliers` |
| SCRUM-19 | `ComprasYRecepcion` | `inventory.purchases`, `inventory.purchase_lines`, `inventory.lot_feed` |
| SCRUM-130 | `ParametrosOperativos` | `inventory.settings` |
| SCRUM-124 | `PreciosDeProveedores` | `inventory.supplier_prices` |
| SCRUM-126 | `EntregaDeCaja` | `cash.sessions.handover_to`, `handover_received_amount`, `handover_confirmed_at` |
| SCRUM-127 | `AdministradorEnEquiposAutorizados` | `identity.devices.allows_administrator` |

El detalle de columnas y relaciones está en [`modelos/bd/`](../../modelos/bd/).
