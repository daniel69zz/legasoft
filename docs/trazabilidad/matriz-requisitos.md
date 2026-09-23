# Matriz de trazabilidad: requisito → artefactos

> Fuente: Documento 2 (SRS v2), sección 6 — Matriz de trazabilidad bidireccional.
> Documentos: [`docs/entrevistas/`](../entrevistas/) · [`docs/srs/`](../srs/) · [`docs/arquitectura/`](../arquitectura/) · [`modelos/c4/`](../../modelos/c4/).

Sentido directo: cada requisito con su fuente de elicitación, la historia de usuario Jira que lo planifica, el componente C4 que lo implementa y el caso de prueba que lo verifica.

| Requisito | Módulo | Prioridad | Fuente | Historia | Componente C4 | Caso de prueba |
| --- | --- | --- | --- | --- | --- | --- |
| RF-001 | Usuarios | Must | NEC-02 \| ENT-001 y ENT-002 | HU-01 | Identity Service (AuthController, servicio de tokens, usuarios y roles) | CP-RF-001 |
| RF-002 | Usuarios | Must | NEC-02 \| ENT-001 | HU-02 | Identity Service (AuthController, servicio de tokens, usuarios y roles) | CP-RF-002 |
| RF-003 | Usuarios | Must | NEC-02 \| ENT-001 | HU-02 | Identity Service (AuthController, servicio de tokens, usuarios y roles) | CP-RF-003 |
| RF-004 | Usuarios | Must | NEC-02 \| ENT-001 | HU-02 | Identity Service (AuthController, servicio de tokens, usuarios y roles) | CP-RF-004 |
| RF-005 | Usuarios | Must | NEC-02 \| ENT-001 | HU-02 | Identity Service (AuthController, servicio de tokens, usuarios y roles) | CP-RF-005 |
| RF-006 | Usuarios | Must | NEC-15 \| ENT-002 | HU-03 | Identity Service (dispositivos autorizados) | CP-RF-006 |
| RF-007 | Usuarios | Should | ENT-002 | HU-03 | Identity Service (dispositivos autorizados) | CP-RF-007 |
| RF-008 | Asistencia | Must | NEC-16 \| ENT-002 | HU-04 | Identity Service (dispositivos autorizados) / Business Service | CP-RF-008 |
| RF-009 | Medicamentos | Must | NEC-03 y NEC-35 \| ENT-001 y VAL-001 | HU-05 | Business Service (inventario) / InventoryLocalService | CP-RF-009 |
| RF-010 | Medicamentos | Should | NEC-03 \| ENT-001 | HU-05 | Business Service (inventario) / InventoryLocalService | CP-RF-010 |
| RF-011 | Medicamentos | Should | NEC-03 \| ENT-001 | HU-05 | Business Service (inventario) / InventoryLocalService | CP-RF-011 |
| RF-012 | Medicamentos | Must | NEC-03 y NEC-07 \| ENT-001 | HU-05 | Business Service (inventario) / InventoryLocalService | CP-RF-012 |
| RF-013 | Inventario | Must | NEC-04 \| ENT-001 | HU-06 | InventoryLocalService / Business Service (inventario) | CP-RF-013 |
| RF-014 | Inventario | Must | NEC-04 \| ENT-001 | HU-06 | InventoryLocalService / Business Service (inventario) | CP-RF-014 |
| RF-015 | Inventario | Must | NEC-04 \| ENT-001 | HU-06 | InventoryLocalService / Business Service (inventario) | CP-RF-015 |
| RF-016 | Inventario | Must | NEC-04 y NEC-28 \| ENT-001 | HU-06 | InventoryLocalService / Business Service (inventario) | CP-RF-016 |
| RF-017 | Inventario | Must | NEC-04 \| ENT-001 | HU-06 | InventoryLocalService / Business Service (inventario) | CP-RF-017 |
| RF-018 | Inventario | Must | NEC-04 \| ENT-001 | HU-06 | InventoryLocalService / Notification Service (programador de alertas) | CP-RF-018 |
| RF-019 | Inventario | Should | NEC-04 \| ENT-001 | HU-06 | InventoryLocalService / Business Service (inventario) | CP-RF-019 |
| RF-020 | Lotes | Must | NEC-05 \| ENT-001 | HU-07 | InventoryLocalService / Business Service (inventario) | CP-RF-020 |
| RF-021 | Lotes | Should | NEC-05 \| ENT-001 | HU-07 | InventoryLocalService / Business Service (inventario) | CP-RF-021 |
| RF-022 | Lotes | Must | NEC-05 y NEC-33 \| ENT-001 y ENT-002 | HU-07 | InventoryLocalService / Notification Service (programador de alertas) | CP-RF-022 |
| RF-023 | Lotes | Must | NEC-05 \| ENT-001 | HU-07 | InventoryLocalService / Business Service (inventario) | CP-RF-023 |
| RF-024 | Lotes | Must | NEC-05 \| ENT-001 | HU-07 | Pharmacy.Domain / SalesLocalService | CP-RF-024 |
| RF-025 | Lotes | Must | NEC-05 \| ENT-001 | HU-07 | InventoryLocalService / Business Service (inventario) | CP-RF-025 |
| RF-026 | Proveedores | Must | NEC-06 \| ENT-001 | HU-08 | Business Service (compras y proveedores) | CP-RF-026 |
| RF-027 | Proveedores | Must | NEC-06 \| ENT-001 | HU-08 | Business Service (compras y proveedores) | CP-RF-027 |
| RF-028 | Proveedores | Should | NEC-06 \| ENT-001 | HU-08 | Business Service (compras y proveedores) | CP-RF-028 |
| RF-029 | Proveedores | Should | NEC-06 \| ENT-001 | HU-08 | Business Service (compras y proveedores) | CP-RF-029 |
| RF-030 | Compras | Must | NEC-06 \| ENT-001 | HU-09 | Business Service (compras y proveedores) | CP-RF-030 |
| RF-031 | Compras | Must | NEC-06 \| ENT-001 | HU-09 | Business Service (compras y proveedores) | CP-RF-031 |
| RF-032 | Compras | Must | NEC-06 y NEC-05 \| ENT-001 | HU-09 | Business Service (compras y proveedores) | CP-RF-032 |
| RF-033 | Compras | Must | NEC-08 \| ENT-001 | HU-09 | Business Service (compras y proveedores) | CP-RF-033 |
| RF-034 | Proveedores | Must | NEC-23 y NEC-24 \| ENT-002 | HU-10 | Business Service (compras y proveedores) | CP-RF-034 |
| RF-035 | Proveedores | Should | NEC-23 \| ENT-002 | HU-10 | Business Service (compras y proveedores) | CP-RF-035 |
| RF-036 | Proveedores | Should | NEC-24 \| ENT-002 | HU-10 | Business Service (compras y proveedores) | CP-RF-036 |
| RF-037 | Ventas | Must | NEC-07 \| ENT-001 y ENT-002 | HU-11 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-037 |
| RF-038 | Ventas | Must | NEC-07 \| ENT-001 y ENT-002 | HU-11 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-038 |
| RF-039 | Ventas | Must | NEC-07 \| ENT-001 | HU-11 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-039 |
| RF-040 | Ventas | Must | NEC-07 \| ENT-001 | HU-11 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-040 |
| RF-041 | Ventas | Must | NEC-07 \| ENT-001 | HU-11 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-041 |
| RF-042 | Ventas | Must | NEC-17 y NEC-27 \| ENT-001 y ENT-002 | HU-12 | SalesLocalService / CashLocalService / Business Service (caja) | CP-RF-042 |
| RF-044 | Ventas | Must | NEC-17 \| ENT-002 | HU-12 | SalesLocalService / CashLocalService / Business Service (caja) | CP-RF-044 |
| RF-045 | Ventas | Must | NEC-17 \| ENT-002 | HU-12 | SalesLocalService / CashLocalService / Business Service (caja) | CP-RF-045 |
| RF-046 | Ventas | Must | NEC-08 \| ENT-001 | HU-11 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-046 |
| RF-047 | Ventas | Must | NEC-07, NEC-22 y NEC-27 \| ENT-001 | HU-11 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-047 |
| RF-048 | Ventas | Must | NEC-18 \| ENT-002 | HU-13 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-048 |
| RF-049 | Recibos | Must | NEC-10 y NEC-27 \| ENT-001 y ENT-002 | HU-14 | SalesLocalService / Business Service (ventas) | CP-RF-049 |
| RF-050 | Recibos | Should | NEC-10 \| ENT-002 | HU-14 | SalesLocalService / Business Service (ventas) | CP-RF-050 |
| RF-051 | Recibos | Should | NEC-10 \| ENT-002 | HU-14 | SalesLocalService / Business Service (ventas) | CP-RF-051 |
| RF-052 | Caja | Must | NEC-09 \| ENT-001 | HU-15 | CashLocalService / Business Service (caja) | CP-RF-052 |
| RF-053 | Caja | Must | NEC-09 \| ENT-001 | HU-15 | CashLocalService / Business Service (caja) | CP-RF-053 |
| RF-054 | Caja | Must | NEC-19 \| ENT-002 | HU-15 | CashLocalService / Business Service (caja) | CP-RF-054 |
| RF-055 | Caja | Must | NEC-20 \| ENT-002 | HU-15 | CashLocalService / Business Service (caja) | CP-RF-055 |
| RF-056 | Caja | Must | NEC-09 \| ENT-001 | HU-15 | CashLocalService / Business Service (caja) | CP-RF-056 |
| RF-057 | Caja | Must | NEC-09 \| ENT-001 y ENT-002 | HU-15 | CashLocalService / Business Service (caja) | CP-RF-057 |
| RF-058 | Caja | Must | NEC-09 y NEC-19 \| ENT-001 y ENT-002 | HU-16 | CashLocalService / Business Service (caja) | CP-RF-058 |
| RF-059 | Caja | Must | NEC-09, NEC-19 y NEC-31 \| ENT-001 y ENT-002 | HU-16 | CashLocalService / Business Service (caja) | CP-RF-059 |
| RF-060 | Caja | Should | NEC-09 \| ENT-001 y ENT-002 | HU-16 | CashLocalService / Business Service (caja) | CP-RF-060 |
| RF-061 | Caja | Must | NEC-21 \| ENT-002 | HU-16 | CashLocalService / Business Service (caja) | CP-RF-061 |
| RF-062 | Caja | Must | NEC-21 \| ENT-002 | HU-16 | CashLocalService / Business Service (caja) | CP-RF-062 |
| RF-063 | Facturación | Must | NEC-10 \| ENT-001 | HU-17 | SalesLocalService / Business Service (ventas) | CP-RF-063 |
| RF-064 | Facturación | Must | NEC-10 \| ENT-001 | HU-17 | SalesLocalService / Business Service (ventas) | CP-RF-064 |
| RF-065 | Facturación | Should | NEC-10 \| ENT-001 | HU-17 | SalesLocalService / Business Service (ventas) | CP-RF-065 |
| RF-066 | Facturación | Should | NEC-10 \| ENT-001 | HU-17 | SalesLocalService / Business Service (ventas) | CP-RF-066 |
| RF-067 | Facturación | Should | NEC-10 \| ENT-001 | HU-17 | SalesLocalService / Business Service (ventas) | CP-RF-067 |
| RF-068 | Reportes | Must | NEC-11 y NEC-22 \| ENT-001 y ENT-002 | HU-18 | Business Service (reportes) | CP-RF-068 |
| RF-069 | Reportes | Must | NEC-11 \| ENT-001 | HU-18 | Business Service (reportes) | CP-RF-069 |
| RF-070 | Reportes | Must | NEC-11 \| ENT-001 | HU-18 | Business Service (reportes) | CP-RF-070 |
| RF-071 | Reportes | Must | NEC-11 \| ENT-001 | HU-18 | Business Service (reportes) | CP-RF-071 |
| RF-072 | Reportes | Must | NEC-11 y NEC-33 \| ENT-001 y ENT-002 | HU-18 | Business Service (reportes) | CP-RF-072 |
| RF-073 | Dashboard | Should | NEC-12 \| ENT-001 y ENT-002 | HU-19 | Business Service (reportes) | CP-RF-073 |
| RF-074 | Dashboard | Should | NEC-22 \| ENT-002 | HU-19 | Business Service (reportes) | CP-RF-074 |
| RF-075 | Auditoría | Must | NEC-10 y NEC-28 \| ENT-001 | HU-20 | Business Service (escritor de auditoría) / PostgreSQL (schema audit) | CP-RF-075 |
| RF-080 | Sincronización | Must | NEC-14 \| ENT-001 | HU-22 | SQLite local / OutboxRepository | CP-RF-080 |
| RF-081 | Sincronización | Must | NEC-14 \| ENT-001 | HU-22 | Sync Worker / API Gateway / Sync Service | CP-RF-081 |
| RF-082 | Respaldo | Should | ENT-001 \| AS-IS 4.3 | HU-23 | PostgreSQL central administrado | CP-RF-082 |
| RF-083 | Respaldo | Should | ENT-001 \| AS-IS 4.3 | HU-23 | PostgreSQL central administrado | CP-RF-083 |
| RF-084 | Clientes | Should | NEC-34 \| VAL-001 | HU-24 | Business Service (clientes) | CP-RF-084 |
| RF-085 | Clientes | Should | NEC-34 \| VAL-001 | HU-24 | Business Service (clientes) | CP-RF-085 |
| RF-086 | Clientes | Should | NEC-34 \| VAL-001 | HU-24 | Business Service (clientes) | CP-RF-086 |
| RF-087 | Clientes | Should | NEC-34 \| VAL-001 | HU-24 | Business Service (clientes) | CP-RF-087 |
| RF-088 | Recetas | Should | NEC-35 \| VAL-001 | HU-25 | Business Service (recetas) | CP-RF-088 |
| RF-089 | Recetas | Should | NEC-35 \| VAL-001 | HU-25 | Business Service (recetas) | CP-RF-089 |
| RF-090 | Recetas | Must | NEC-35 \| VAL-001 | HU-25 | SalesLocalService / Business Service (recetas) | CP-RF-090 |
| RF-091 | Recetas | Must | NEC-35 \| VAL-001 | HU-25 | SalesLocalService / Business Service (recetas) | CP-RF-091 |
| RF-095 | Inventario | Should | NEC-37 \| VAL-001 | HU-07 | Pharmacy.Domain / SalesLocalService | CP-RF-095 |
| RF-096 | Ventas | Should | NEC-38 \| VAL-001 | HU-27 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | CP-RF-096 |
| RF-097 | Configuración | Should | NEC-39 \| VAL-001 | HU-28 | Business Service | CP-RF-097 |
| RF-098 | Configuración | Should | NEC-39 \| VAL-001 | HU-28 | Business Service | CP-RF-098 |
| RF-099 | Configuración | Should | NEC-39 \| VAL-001 | HU-28 | Business Service | CP-RF-099 |
| RF-100 | Escalabilidad | Could | NEC-40 \| VAL-001 | HU-29 | Business Service / Sync Service (versión por módulo y dispositivo) | CP-RF-100 |
| RF-101 | Facturación | Must | NEC-10 y NEC-27 \| ENT-001 | HU-17 | SalesLocalService / Business Service (ventas) | CP-RF-101 |
| RF-102 | Sincronización | Must | NEC-14 \| ENT-001 | HU-22 | Sync Service (IdempotencyStore) | CP-RF-102 |
| RF-103 | Reportes | Must | NEC-26 \| ENT-001 | HU-18 | Business Service (reportes) | CP-RF-103 |
| RF-104 | Reportes | Must | NEC-01 y NEC-25 \| ENT-001 | HU-18 | Sync Service / Business Service / PostgreSQL central | CP-RF-104 |
| RF-105 | Reportes | Should | NEC-30 \| ENT-002 | HU-18 | Business Service (reportes) | CP-RF-105 |
| RF-106 | Caja | Must | NEC-31 \| ENT-002 | HU-16 | CashLocalService / Business Service (caja) | CP-RF-106 |
| RF-107 | Compras | Must | NEC-32 \| ENT-002 | HU-09 | Business Service (compras y proveedores) | CP-RF-107 |
| RF-108 | Compras | Must | NEC-32 \| ENT-002 | HU-09 | Business Service (compras y proveedores) | CP-RF-108 |
| RF-109 | Inventario | Should | NEC-29 \| ENT-001 | HU-06 | InventoryLocalService / Business Service (inventario) | CP-RF-109 |
| RNF-001 | Rendimiento | Must | Derivado técnico de NEC-07, NEC-11 y NEC-12 | HU-T01 | Aplicación Desktop (SQLite local) / Business Service | CP-RNF-001 |
| RNF-002 | Rendimiento | Should | Derivado técnico de NEC-11 y NEC-12 | HU-T01 | Business Service (reportes) / PostgreSQL central | CP-RNF-002 |
| RNF-003 | Seguridad | Must | Derivado técnico de NEC-14 | HU-T02 | API Gateway (YARP) / Identity Service | CP-RNF-003 |
| RNF-004 | Seguridad | Must | Derivado técnico de NEC-02 y NEC-15 | HU-T02 | Identity Service | CP-RNF-004 |
| RNF-005 | Usabilidad | Should | Derivado técnico de NEC-07 y NEC-17 | HU-T03 | Aplicación Desktop (Avalonia Views, ViewModels) | CP-RNF-005 |
| RNF-006 | Disponibilidad | Must | NEC-14 \| ENT-001 | HU-T04 | SQLite local / Sync Worker / Sync Service | CP-RNF-006 |
| RNF-007 | Mantenibilidad | Should | Derivado técnico del alcance del proyecto | HU-T05 | Pharmacy.Domain / Pharmacy.Application / servicios cloud | CP-RNF-007 |
| RNF-008 | Respaldo / Recuperación | Should | ENT-001 \| AS-IS 4.3 | HU-T06 | PostgreSQL central administrado | CP-RNF-008 |
