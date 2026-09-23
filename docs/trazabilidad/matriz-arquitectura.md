# Matriz de trazabilidad: requisitos → arquitectura

> Fuente: Documento 3 v3, sección 9 — Relación con los requisitos y atributos de calidad.
> Diagramas: [`modelos/c4/`](../../modelos/c4/) · Decisiones: [`docs/arquitectura/adr/`](../arquitectura/adr/).

## Áreas funcionales del SRS → contenedores y componentes C4

| Área | Contenedores | Componentes |
| --- | --- | --- |
| Ventas | Aplicación Desktop, SQLite local, Business Service, PostgreSQL (`sales`) | SalesLocalService, Pharmacy.Domain, LocalRepositories, módulo de ventas del Business Service |
| Inventario | Aplicación Desktop, SQLite local, Business Service, PostgreSQL (`inventory`) | InventoryLocalService, reglas FEFO de Pharmacy.Domain, módulo de inventario del Business Service |
| Caja | Aplicación Desktop, SQLite local, Business Service, PostgreSQL (`cash`) | CashLocalService, Pharmacy.Domain, módulo de caja del Business Service |
| Operación sin conexión | Aplicación Desktop, SQLite local | Servicios locales, SQLite DbContext, OutboxRepository |
| Sincronización | Sync Worker, API Gateway, Sync Service, PostgreSQL (`sync`) | SyncWorker, SyncStateRepository, SyncController, IdempotencyStore, VersionManager, EventDispatcher |
| Pagos en efectivo | Aplicación Desktop, SQLite local, Business Service, PostgreSQL (`cash`) | SalesLocalService, CashLocalService, módulo de caja del Business Service |
| Usuarios y seguridad | API Gateway, Identity Service, PostgreSQL (`identity`) | AuthController, servicio de tokens, middleware JWT, gestión de dispositivos autorizados |
| Reportes | Business Service, Notification Service, PostgreSQL | Módulo de reportes, generación de reportes programados, adaptador SMTP |
| Auditoría | Business Service, Sync Service, PostgreSQL (`audit`) | Escritor de auditoría, registro de eventos con usuario, dispositivo y versión |
| Clientes y recetas | Aplicación Desktop, Business Service, PostgreSQL | SalesLocalService (verificación de receta), módulos de clientes y recetas del Business Service, Pharmacy.Application |
| Respaldo | PostgreSQL central administrado, SQLite local | Respaldos automáticos del servicio administrado; Outbox y SyncWorker como resguardo hasta sincronizar |

## Requisitos no funcionales → decisiones de arquitectura

| RNF | Atributo | Decisión arquitectónica | ADR |
| --- | --- | --- | --- |
| RNF-001, RNF-002 | Rendimiento | Operación local sobre SQLite (Dapper para lecturas intensivas); reportes consolidados en PostgreSQL | ADR-002, ADR-007 |
| RNF-003, RNF-004 | Seguridad | JWT y refresh tokens, dispositivos autorizados, usuarios PostgreSQL con permisos por schema y TLS en todas las comunicaciones | ADR-005, ADR-006 |
| RNF-005 | Usabilidad | Una única aplicación de escritorio Avalonia (MVVM) para todos los roles | — |
| RNF-006 | Disponibilidad | Ejecución local de ventas, caja e inventario; la caída de Internet o de un servicio cloud no detiene el puesto | ADR-002, ADR-003, ADR-008 |
| RNF-007 | Mantenibilidad | Servicios con fronteras claras, lógica compartida sin dependencias de infraestructura y pruebas con xUnit | ADR-004, ADR-006 |
| RNF-008 | Respaldo / recuperación | PostgreSQL administrado con respaldos automáticos; Outbox como resguardo local | ADR-002, ADR-005 |

## Atributos de calidad transversales

| Atributo | Decisión arquitectónica |
| --- | --- |
| Integridad | Transacción SQLite única por operación, Outbox en la misma transacción e idempotencia por `eventId` en el Sync Service |
| Consistencia | Reglas de negocio únicas en Pharmacy.Domain y Pharmacy.Application, ejecutadas en el puesto y en el servidor |
| Escalabilidad | Servicios desplegables por separado y schemas por dominio que permiten extraer dominios o bases en el futuro |
| Trazabilidad | Mensajes con usuario, dispositivo, fecha y versión, auditoría central y logging correlacionado con Serilog |
