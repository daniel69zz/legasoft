# Matriz de trazabilidad inversa

> Fuente: Documento 2 (SRS v2), sección 6 — Matriz de trazabilidad bidireccional.
> Documentos: [`docs/entrevistas/`](../entrevistas/) · [`docs/srs/`](../srs/) · [`docs/arquitectura/`](../arquitectura/) · [`modelos/c4/`](../../modelos/c4/).

## Historia de usuario → requisitos

Sentido inverso: desde cada historia Jira hacia los requisitos que cubre, sus componentes C4, sus fuentes y sus casos de prueba.

| Historia | Requisitos cubiertos | Componente C4 | Fuente | Casos de prueba |
| --- | --- | --- | --- | --- |
| HU-01 | RF-001 | Identity Service (AuthController, servicio de tokens, usuarios y roles) | NEC-02 \| ENT-001 y ENT-002 | CP-RF-001 |
| HU-02 | RF-002, RF-003, RF-004, RF-005 | Identity Service (AuthController, servicio de tokens, usuarios y roles) | NEC-02 \| ENT-001 | CP-RF-002, CP-RF-003, CP-RF-004, CP-RF-005 |
| HU-03 | RF-006, RF-007 | Identity Service (dispositivos autorizados) | NEC-15 \| ENT-002 ; ENT-002 | CP-RF-006, CP-RF-007 |
| HU-04 | RF-008 | Identity Service (dispositivos autorizados) / Business Service | NEC-16 \| ENT-002 | CP-RF-008 |
| HU-05 | RF-009, RF-010, RF-011, RF-012 | Business Service (inventario) / InventoryLocalService | NEC-03 y NEC-35 \| ENT-001 y VAL-001 ; NEC-03 \| ENT-001 ; NEC-03 y NEC-07 \| ENT-001 | CP-RF-009, CP-RF-010, CP-RF-011, CP-RF-012 |
| HU-06 | RF-013, RF-014, RF-015, RF-016, RF-017, RF-018, RF-019, RF-109 | InventoryLocalService / Business Service (inventario) / InventoryLocalService / Notification Service (programador de alertas) | NEC-04 \| ENT-001 ; NEC-04 y NEC-28 \| ENT-001 ; NEC-29 \| ENT-001 | CP-RF-013, CP-RF-014, CP-RF-015, CP-RF-016, CP-RF-017, CP-RF-018, CP-RF-019, CP-RF-109 |
| HU-07 | RF-020, RF-021, RF-022, RF-023, RF-024, RF-025, RF-095 | InventoryLocalService / Business Service (inventario) / InventoryLocalService / Notification Service (programador de alertas) / Pharmacy.Domain / SalesLocalService | NEC-05 \| ENT-001 ; NEC-05 y NEC-33 \| ENT-001 y ENT-002 ; NEC-37 \| VAL-001 | CP-RF-020, CP-RF-021, CP-RF-022, CP-RF-023, CP-RF-024, CP-RF-025, CP-RF-095 |
| HU-08 | RF-026, RF-027, RF-028, RF-029 | Business Service (compras y proveedores) | NEC-06 \| ENT-001 | CP-RF-026, CP-RF-027, CP-RF-028, CP-RF-029 |
| HU-09 | RF-030, RF-031, RF-032, RF-033, RF-107, RF-108 | Business Service (compras y proveedores) | NEC-06 \| ENT-001 ; NEC-06 y NEC-05 \| ENT-001 ; NEC-08 \| ENT-001 ; NEC-32 \| ENT-002 | CP-RF-030, CP-RF-031, CP-RF-032, CP-RF-033, CP-RF-107, CP-RF-108 |
| HU-10 | RF-034, RF-035, RF-036 | Business Service (compras y proveedores) | NEC-23 y NEC-24 \| ENT-002 ; NEC-23 \| ENT-002 ; NEC-24 \| ENT-002 | CP-RF-034, CP-RF-035, CP-RF-036 |
| HU-11 | RF-037, RF-038, RF-039, RF-040, RF-041, RF-046, RF-047 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | NEC-07 \| ENT-001 y ENT-002 ; NEC-07 \| ENT-001 ; NEC-08 \| ENT-001 ; NEC-07, NEC-22 y NEC-27 \| ENT-001 | CP-RF-037, CP-RF-038, CP-RF-039, CP-RF-040, CP-RF-041, CP-RF-046, CP-RF-047 |
| HU-12 | RF-042, RF-044, RF-045 | SalesLocalService / CashLocalService / Business Service (caja) | NEC-17 y NEC-27 \| ENT-001 y ENT-002 ; NEC-17 \| ENT-002 | CP-RF-042, CP-RF-044, CP-RF-045 |
| HU-13 | RF-048 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | NEC-18 \| ENT-002 | CP-RF-048 |
| HU-14 | RF-049, RF-050, RF-051 | SalesLocalService / Business Service (ventas) | NEC-10 y NEC-27 \| ENT-001 y ENT-002 ; NEC-10 \| ENT-002 | CP-RF-049, CP-RF-050, CP-RF-051 |
| HU-15 | RF-052, RF-053, RF-054, RF-055, RF-056, RF-057 | CashLocalService / Business Service (caja) | NEC-09 \| ENT-001 ; NEC-19 \| ENT-002 ; NEC-20 \| ENT-002 ; NEC-09 \| ENT-001 y ENT-002 | CP-RF-052, CP-RF-053, CP-RF-054, CP-RF-055, CP-RF-056, CP-RF-057 |
| HU-16 | RF-058, RF-059, RF-060, RF-061, RF-062, RF-106 | CashLocalService / Business Service (caja) | NEC-09 y NEC-19 \| ENT-001 y ENT-002 ; NEC-09, NEC-19 y NEC-31 \| ENT-001 y ENT-002 ; NEC-09 \| ENT-001 y ENT-002 ; NEC-21 \| ENT-002 ; NEC-31 \| ENT-002 | CP-RF-058, CP-RF-059, CP-RF-060, CP-RF-061, CP-RF-062, CP-RF-106 |
| HU-17 | RF-063, RF-064, RF-065, RF-066, RF-067, RF-101 | SalesLocalService / Business Service (ventas) | NEC-10 \| ENT-001 ; NEC-10 y NEC-27 \| ENT-001 | CP-RF-063, CP-RF-064, CP-RF-065, CP-RF-066, CP-RF-067, CP-RF-101 |
| HU-18 | RF-068, RF-069, RF-070, RF-071, RF-072, RF-103, RF-104, RF-105 | Business Service (reportes) / Sync Service / Business Service / PostgreSQL central | NEC-11 y NEC-22 \| ENT-001 y ENT-002 ; NEC-11 \| ENT-001 ; NEC-11 y NEC-33 \| ENT-001 y ENT-002 ; NEC-26 \| ENT-001 ; NEC-01 y NEC-25 \| ENT-001 ; NEC-30 \| ENT-002 | CP-RF-068, CP-RF-069, CP-RF-070, CP-RF-071, CP-RF-072, CP-RF-103, CP-RF-104, CP-RF-105 |
| HU-19 | RF-073, RF-074 | Business Service (reportes) | NEC-12 \| ENT-001 y ENT-002 ; NEC-22 \| ENT-002 | CP-RF-073, CP-RF-074 |
| HU-20 | RF-075 | Business Service (escritor de auditoría) / PostgreSQL (schema audit) | NEC-10 y NEC-28 \| ENT-001 | CP-RF-075 |
| HU-22 | RF-080, RF-081, RF-102 | SQLite local / OutboxRepository / Sync Worker / API Gateway / Sync Service / Sync Service (IdempotencyStore) | NEC-14 \| ENT-001 | CP-RF-080, CP-RF-081, CP-RF-102 |
| HU-23 | RF-082, RF-083 | PostgreSQL central administrado | ENT-001 \| AS-IS 4.3 | CP-RF-082, CP-RF-083 |
| HU-24 | RF-084, RF-085, RF-086, RF-087 | Business Service (clientes) | NEC-34 \| VAL-001 | CP-RF-084, CP-RF-085, CP-RF-086, CP-RF-087 |
| HU-25 | RF-088, RF-089, RF-090, RF-091 | Business Service (recetas) / SalesLocalService / Business Service (recetas) | NEC-35 \| VAL-001 | CP-RF-088, CP-RF-089, CP-RF-090, CP-RF-091 |
| HU-27 | RF-096 | SalesLocalService / Pharmacy.Application / Business Service (ventas) | NEC-38 \| VAL-001 | CP-RF-096 |
| HU-28 | RF-097, RF-098, RF-099 | Business Service | NEC-39 \| VAL-001 | CP-RF-097, CP-RF-098, CP-RF-099 |
| HU-29 | RF-100 | Business Service / Sync Service (versión por módulo y dispositivo) | NEC-40 \| VAL-001 | CP-RF-100 |
| HU-T01 | RNF-001, RNF-002 | Aplicación Desktop (SQLite local) / Business Service / Business Service (reportes) / PostgreSQL central | Derivado técnico de NEC-07, NEC-11 y NEC-12 ; Derivado técnico de NEC-11 y NEC-12 | CP-RNF-001, CP-RNF-002 |
| HU-T02 | RNF-003, RNF-004 | API Gateway (YARP) / Identity Service / Identity Service | Derivado técnico de NEC-14 ; Derivado técnico de NEC-02 y NEC-15 | CP-RNF-003, CP-RNF-004 |
| HU-T03 | RNF-005 | Aplicación Desktop (Avalonia Views, ViewModels) | Derivado técnico de NEC-07 y NEC-17 | CP-RNF-005 |
| HU-T04 | RNF-006 | SQLite local / Sync Worker / Sync Service | NEC-14 \| ENT-001 | CP-RNF-006 |
| HU-T05 | RNF-007 | Pharmacy.Domain / Pharmacy.Application / servicios cloud | Derivado técnico del alcance del proyecto | CP-RNF-007 |
| HU-T06 | RNF-008 | PostgreSQL central administrado | ENT-001 \| AS-IS 4.3 | CP-RNF-008 |

## Necesidad → requisitos

Cobertura de cada necesidad del catálogo `NEC` (Documento 1) por los requisitos del SRS.

| Necesidad | Requisitos | Cantidad |
| --- | --- | --- |
| NEC-01 | RF-104 | 1 |
| NEC-02 | RF-001, RF-002, RF-003, RF-004, RF-005, RNF-004 | 6 |
| NEC-03 | RF-009, RF-010, RF-011, RF-012 | 4 |
| NEC-04 | RF-013, RF-014, RF-015, RF-016, RF-017, RF-018, RF-019 | 7 |
| NEC-05 | RF-020, RF-021, RF-022, RF-023, RF-024, RF-025, RF-032 | 7 |
| NEC-06 | RF-026, RF-027, RF-028, RF-029, RF-030, RF-031, RF-032 | 7 |
| NEC-07 | RF-012, RF-037, RF-038, RF-039, RF-040, RF-041, RF-047, RNF-001, RNF-005 | 9 |
| NEC-08 | RF-033, RF-046 | 2 |
| NEC-09 | RF-052, RF-053, RF-056, RF-057, RF-058, RF-059, RF-060 | 7 |
| NEC-10 | RF-049, RF-050, RF-051, RF-063, RF-064, RF-065, RF-066, RF-067, RF-075, RF-101 | 10 |
| NEC-11 | RF-068, RF-069, RF-070, RF-071, RF-072, RNF-001, RNF-002 | 7 |
| NEC-12 | RF-073, RNF-001, RNF-002 | 3 |
| NEC-14 | RF-080, RF-081, RF-102, RNF-003, RNF-006 | 5 |
| NEC-15 | RF-006, RNF-004 | 2 |
| NEC-16 | RF-008 | 1 |
| NEC-17 | RF-042, RF-044, RF-045, RNF-005 | 4 |
| NEC-18 | RF-048 | 1 |
| NEC-19 | RF-054, RF-058, RF-059 | 3 |
| NEC-20 | RF-055 | 1 |
| NEC-21 | RF-061, RF-062 | 2 |
| NEC-22 | RF-047, RF-068, RF-074 | 3 |
| NEC-23 | RF-034, RF-035 | 2 |
| NEC-24 | RF-034, RF-036 | 2 |
| NEC-25 | RF-104 | 1 |
| NEC-26 | RF-103 | 1 |
| NEC-27 | RF-042, RF-047, RF-049, RF-101 | 4 |
| NEC-28 | RF-016, RF-075 | 2 |
| NEC-29 | RF-109 | 1 |
| NEC-30 | RF-105 | 1 |
| NEC-31 | RF-059, RF-106 | 2 |
| NEC-32 | RF-107, RF-108 | 2 |
| NEC-33 | RF-022, RF-072 | 2 |
| NEC-34 | RF-084, RF-085, RF-086, RF-087 | 4 |
| NEC-35 | RF-009, RF-088, RF-089, RF-090, RF-091 | 5 |
| NEC-37 | RF-095 | 1 |
| NEC-38 | RF-096 | 1 |
| NEC-39 | RF-097, RF-098, RF-099 | 3 |
| NEC-40 | RF-100 | 1 |

Requisitos sin necesidad directa (derivación técnica o fuente ENT/AS-IS): RF-007, RF-082, RF-083, RNF-007, RNF-008.
