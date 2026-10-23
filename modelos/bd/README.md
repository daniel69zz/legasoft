# Modelo de base de datos

Modelo entidad-relación del **Sistema de Gestión para Farmacéutica**, tal como lo
crean las migraciones EF Core del Sprint 1. Sigue el ADR-005: una sola instancia
de PostgreSQL, un *schema* por dominio y un usuario de base de datos por servicio.
Cada puesto de caja guarda además una copia local en SQLite (ADR-002).

| Código | Contenido | Fuente |
| --- | --- | --- |
| `LGS-MOD-BD-1` | Base central PostgreSQL (schemas `identity`, `inventory`, `sales`, `cash`, `audit`, `sync`) | Migraciones `Inicial` de cada servicio |
| `LGS-MOD-BD-2` | Base local SQLite del puesto | `src/frontend/Pharmacy.Desktop/Data/LocalDb.cs` |

## Migraciones

| Servicio | Schemas | Migración | Historial |
| --- | --- | --- | --- |
| Identity Service | `identity` | `src/backend/Identity.Api/Migrations/*_Inicial.cs` | `identity.__ef_migrations_history` |
| Business Service | `inventory`, `sales`, `cash`, `audit` | `src/backend/Business.Api/Migrations/*_Inicial.cs` | `inventory.__ef_migrations_history` |
| Sync Service | `sync` | `src/backend/Sync.Api/Migrations/*_Inicial.cs` | `sync.__ef_migrations_history` |

Cada servicio aplica sus migraciones pendientes al arrancar (`MigrateAsync` en
`Pharmacy.ServiceDefaults`). Para agregar una migración nueva:

```sh
dotnet tool install -g dotnet-ef --version 8.0.11
dotnet ef migrations add <Nombre> -p src/backend/Business.Api -o Migrations
```

Los servicios no comparten tablas: se comunican por API. Por eso las relaciones
entre schemas (por ejemplo, `sales.sale_lines.product_id` → `inventory.products`)
son **referencias lógicas** sin clave foránea, y en el diagrama se dibujan con
línea punteada. La única clave foránea física es `sale_lines.sale_id` → `sales.id`.

## Base central (PostgreSQL)

```mermaid
erDiagram
    %% ---------- identity (Identity Service) ----------
    USERS {
        uuid id PK
        varchar user_name UK "único, máx. 60"
        text full_name
        text password_hash "PBKDF2 (PasswordHasher)"
        text role "uno de los 7 roles del SRS"
        boolean is_active
        int failed_attempts
        timestamptz first_failed_at "nullable"
        timestamptz locked_until "nullable"
        timestamptz created_at
    }
    DEVICES {
        text device_id PK
        text description
        boolean is_active
    }
    ATTENDANCE {
        uuid id PK
        text user_name
        text device_id
        date day
        timestamptz first_login_at
    }
    USER_CHANGES {
        uuid id PK
        timestamptz occurred_at
        text changed_by
        text action
        text detail
    }
    USERS ||..o{ ATTENDANCE : "registra asistencia"
    DEVICES ||..o{ ATTENDANCE : "en el equipo"
    USERS ||..o{ USER_CHANGES : "cambios auditados"

    %% ---------- inventory (Business Service) ----------
    PRODUCTS {
        uuid id PK
        text code UK "código de producto"
        text name "nombre comercial"
        text active_ingredient
        text concentration
        text presentation
        text laboratory
        text category
        boolean requires_prescription
        numeric sale_price "12,2"
        int minimum_stock
        text location
        boolean is_active
        timestamptz updated_at "versión para la descarga incremental"
    }
    LOTS {
        uuid id PK
        uuid product_id
        text lot_number
        date expiration_date
        int quantity
    }
    STOCK_MOVEMENTS {
        uuid id PK
        uuid product_id
        uuid lot_id
        text type "Venta, Compra, Ajuste, Retiro"
        int quantity
        text reason
        text user_name
        timestamptz occurred_at
    }
    PRODUCTS ||..o{ LOTS : "tiene"
    LOTS ||..o{ STOCK_MOVEMENTS : "registra"

    %% ---------- sales (Business Service) ----------
    SALES {
        uuid id PK
        text receipt_number
        text device_id
        text cashier_name
        uuid cash_session_id
        timestamptz created_at
        numeric subtotal
        numeric discount
        numeric total
        numeric cash_received
        numeric change
        text prescription_number
        text customer_name
        text customer_tax_id
        text status "Completada, Anulada"
        text void_reason
    }
    SALE_LINES {
        uuid id PK
        uuid sale_id FK
        uuid product_id
        text product_name
        uuid lot_id
        text lot_number
        int quantity
        numeric unit_price
    }
    SALES ||--|{ SALE_LINES : "contiene"
    PRODUCTS ||..o{ SALE_LINES : "vendido en"
    LOTS ||..o{ SALE_LINES : "sale del lote (FEFO)"

    %% ---------- cash (Business Service) ----------
    CASH_SESSIONS {
        uuid id PK
        text device_id
        text cashier_name
        timestamptz opened_at
        numeric opening_amount
        text status "Abierta, Cerrada"
        numeric sales_total "nullable"
        numeric expenses_total "nullable"
        numeric expected_amount "nullable"
        numeric counted_amount "nullable"
        numeric difference "nullable"
        text justification
        timestamptz closed_at "nullable"
    }
    CASH_MOVEMENTS {
        uuid id PK
        uuid cash_session_id
        text type "Venta, Egreso, AnulacionVenta"
        numeric amount
        text reason
        text category
        text user_name
        timestamptz occurred_at
    }
    CASH_SESSIONS ||..o{ CASH_MOVEMENTS : "registra"
    CASH_SESSIONS ||..o{ SALES : "cobra"

    %% ---------- audit (Business Service) ----------
    AUDIT_ENTRIES {
        uuid id PK
        timestamptz occurred_at
        text user_name
        text device_id
        text action
        text detail
        uuid event_id "nullable"
    }

    %% ---------- sync (Sync Service) ----------
    RECEIVED_EVENTS {
        uuid event_id PK "clave de idempotencia"
        text device_id
        text module
        text event_type
        uuid entity_id
        int version
        timestamptz timestamp
        timestamptz received_at
        text payload "JSON"
    }
    MODULE_VERSIONS {
        text device_id PK
        text module PK
        bigint version
        timestamptz updated_at
    }
    RECEIVED_EVENTS }o..|| MODULE_VERSIONS : "avanza la versión"
    RECEIVED_EVENTS |o..o| AUDIT_ENTRIES : "origen del registro"
```

| Schema | Tablas | Requisitos principales |
| --- | --- | --- |
| `identity` | `users`, `devices`, `attendance`, `user_changes` | RF-001 a RF-008, RF-075, RNF-004 |
| `inventory` | `products`, `lots`, `stock_movements` | RF-009 a RF-025, RF-095, RF-109 |
| `sales` | `sales`, `sale_lines` | RF-037 a RF-048, RF-091, RF-096 |
| `cash` | `sessions`, `movements` | RF-052 a RF-062, RF-106 |
| `audit` | `entries` | RF-075 |
| `sync` | `received_events`, `module_versions` | RF-080, RF-081, RF-102, RF-104 |

## Base local del puesto (SQLite)

La copia local permite vender sin conexión. Repite las tablas de inventario,
ventas y caja con prefijo de dominio y agrega la cola de salida (*Outbox*).

| Tabla | Contenido |
| --- | --- |
| `inventory_products`, `inventory_lots`, `inventory_movements` | Catálogo descargado del servidor y stock local por lote |
| `sales_sales`, `sales_lines` | Ventas del puesto |
| `cash_sessions`, `cash_movements` | Caja del turno |
| `sync_outbox` | Eventos pendientes de enviar al Sync Service (clave `event_id`) |
| `sync_state` | Versión del catálogo ya descargada |
| `identity_cached_users` | Credenciales en caché para iniciar sesión sin conexión |
