# Sprint Planning — Sprint 1

**Proyecto:** Sistema de Gestión para Farmacéutica (Legasoft)
**Tablero:** Jira `SCRUM` (PROYECTO-FARMACIA) · Sprint 1 (id 2)
**Sprint:** del viernes 2 al viernes 16 de octubre de 2026 (2 semanas, 10 días hábiles)
**Documentos de entrada:** Product Backlog (SRS v2, matriz de trazabilidad), Documento 3 (C4 y ADR-001 a ADR-008), guía Jira–Scrum–ISO 29148 de la asignatura

## Asistentes

| Integrante | Rol Scrum | Rol técnico |
| --- | --- | --- |
| Oziel Rodman Ramos Torrez | Scrum Master / Product Owner delegado | Analista de sistemas |
| Luis Daniel Rojas Cáceres | Developer | Arquitecto y backend |
| Pedro Andrez Condorena Apaza | Developer | Frontend y QA |

---

## 1. ¿Por qué? — Objetivo del sprint

> **Que cada usuario de la farmacia ingrese con sus credenciales y vea solo las funciones de su rol (administradas por el Administrador), y que el farmacéutico mantenga el catálogo único de medicamentos, sobre una base técnica con CI y trazabilidad Jira–GitHub.**

Todos los módulos siguientes dependen de saber **quién** usa el sistema y **qué**
puede hacer (RF-001 a RF-005), y de tener un **catálogo** del que salen las ventas
y el inventario (RF-009 a RF-012). La base técnica (solución .NET, Gateway, CI y
trazabilidad) permite que cada historia posterior se entregue con pruebas y
evidencia verificable.

## 2. ¿Qué? — Sprint Backlog

Estimación con Planning Poker (escala Fibonacci). Capacidad inicial definida por
la guía: **20 SP**.

| Clave | Tipo | Elemento | RF | MoSCoW | Responsable | SP |
| --- | --- | --- | --- | --- | --- | --- |
| SCRUM-1 | Tarea técnica | Repositorio, solución .NET, Gateway y pipeline CI con xUnit | — | Must | Luis | 3 |
| SCRUM-2 | Historia | HU-01 Inicio de sesión con credenciales válidas | RF-001 | Must | Luis | 3 |
| SCRUM-3 | Tarea técnica | Integración GitHub–Jira y transición automática al fusionar el PR | — | Must | Oziel | 2 |
| SCRUM-10 | Historia | HU-02 Gestión de usuarios, roles y permisos | RF-002 a RF-005 | Must | Luis | 5 |
| SCRUM-15 | Historia | HU-05 Catálogo de medicamentos: registro, edición y búsqueda | RF-009 a RF-012 | Must | Pedro | 5 |
| | | | | | **Total** | **18** |

**Ocupación:** 18 SP de 20 SP = **90 %**. Se deja un 10 % de holgura porque el
equipo no tiene velocidad histórica y trabaja por primera vez con .NET 8,
Avalonia y YARP.

**Fuera de este sprint:** dispositivos autorizados y asistencia (HU-03, HU-04),
bloqueo por intentos y TLS (HU-T02), inventario y lotes (HU-06, HU-07) y ventas
(HU-11 en adelante). El prototipo ya adelanta parte de ellas; se planificarán en
los próximos sprints.

## 3. ¿Cómo? — Tareas (subtareas en Jira)

Capacidad por integrante: 4 h/día × 10 días − 8 h de eventos Scrum = **32 h**.

### SCRUM-1 · Base técnica (3 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-52 | Solución .NET 8 y docker-compose con PostgreSQL por schemas (ADR-001, ADR-005) | Luis | 5 |
| SCRUM-53 | API Gateway YARP: rutas, límite de solicitudes y correlación | Pedro | 3 |
| SCRUM-54 | Pipeline GitHub Actions: build y pruebas xUnit en cada PR | Pedro | 3 |
| SCRUM-55 | Migraciones EF Core iniciales y modelo entidad-relación | Luis | 4 |
| SCRUM-56 | Documentación de API: Swagger UI en el Gateway y colección Postman | Oziel | 3 |
| SCRUM-51 | `[Trait]` JiraIssue y Requirement en todas las pruebas xUnit | Oziel | 2 |

### SCRUM-2 · HU-01 Inicio de sesión (3 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-42 | `POST /auth/login` con JWT con rol y contraseñas con hash | Luis | 5 |
| SCRUM-43 | Shell de Avalonia y pantalla de inicio de sesión | Pedro | 7 |
| SCRUM-4 | Pruebas xUnit y ejecución de CP-RF-001 | Oziel | 4 |

### SCRUM-3 · Integración GitHub–Jira (2 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-3 | Instalar GitHub for Jira, regla de automatización «PR fusionado → Finalizado» y evidencia | Oziel | 3 |

### SCRUM-10 · HU-02 Usuarios, roles y permisos (5 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-44 | Matriz rol → permisos y semilla de los 7 roles del SRS | Oziel | 5 |
| SCRUM-45 | API de usuarios (alta, modificación, activar/desactivar) con auditoría | Luis | 8 |
| SCRUM-46 | Pantalla de usuarios (con edición de nombre y rol) y menú según el rol | Pedro | 8 |
| SCRUM-47 | Pruebas xUnit y ejecución de CP-RF-002 a CP-RF-005 | Oziel | 5 |

### SCRUM-15 · HU-05 Catálogo de medicamentos (5 SP)

| Clave | Tarea | Resp. | Horas |
| --- | --- | --- | --- |
| SCRUM-48 | API de catálogo: registrar, modificar, activar/desactivar y buscar | Luis | 7 |
| SCRUM-49 | Pantalla de catálogo (buscador, listado y formulario) | Pedro | 8 |
| SCRUM-50 | Pruebas xUnit y ejecución de CP-RF-009 a CP-RF-012 | Oziel | 5 |

### 3.1 Carga por integrante

| Integrante | Tareas | Horas | Capacidad | Uso |
| --- | --- | --- | --- | --- |
| Luis | SCRUM-52, 55, 42, 45, 48 | 29 | 32 | 91 % |
| Pedro | SCRUM-53, 54, 43, 46, 49 | 29 | 32 | 91 % |
| Oziel | SCRUM-56, 51, 3, 4, 44, 47, 50 | 27 | 32 | 84 % |
| **Equipo** | 21 tareas | **85** | **96** | **89 %** |

Oziel tiene menos horas de desarrollo porque además facilita los eventos Scrum y
coordina con el cliente.

### 3.2 Orden de trabajo

| Semana | Foco |
| --- | --- |
| 1 (2–9 oct) | Base técnica (SCRUM-52 a 54), login de punta a punta (SCRUM-42, 43), matriz de permisos (SCRUM-44) e integración GitHub–Jira (SCRUM-3) |
| 2 (12–16 oct) | API y pantalla de usuarios (SCRUM-45, 46), catálogo (SCRUM-48, 49), migraciones y API documentada (SCRUM-55, 56), pruebas con trazabilidad (SCRUM-4, 47, 50, 51) |

## 4. Criterios de aceptación

Los criterios Gherkin completos están en cada historia de Jira. Resumen:

| HU | Escenario principal | Escenario alterno |
| --- | --- | --- |
| HU-01 | Credenciales válidas → JWT con el rol y menú de su rol | Credenciales inválidas → mensaje genérico; usuario desactivado → rechazado |
| HU-02 | El administrador registra un usuario con rol y este solo ve sus funciones | Un cajero que entra a usuarios recibe 403; un usuario desactivado ya no inicia sesión |
| HU-05 | El farmacéutico registra un medicamento y lo encuentra por nombre, código, principio activo o categoría | Código duplicado → rechazado; medicamento desactivado → no se ofrece en ventas |

## 5. Definition of Done

1. Criterios de aceptación Gherkin cumplidos y CP-RF ejecutados en verde.
2. Pruebas xUnit con `[Trait("JiraIssue")]` y `[Trait("Requirement")]`.
3. Rama `feature/SCRUM-n-…`, commits `tipo(SCRUM-n): …` y PR con la clave en el título, revisado por otro integrante.
4. CI de GitHub Actions en verde.
5. Documentación actualizada (README, API, trazabilidad).
6. Ticket en *Finalizado* (automático al fusionar el PR).

## 6. Riesgos

| Riesgo | Mitigación | Responsable |
| --- | --- | --- |
| Configuración inicial más lenta de lo previsto | Se hace primero; se usa la holgura del 10 % | Oziel |
| Poca experiencia con Avalonia | Partir de la plantilla oficial MVVM dentro de SCRUM-43 | Pedro |
| Permisos por rol poco claros | Validar la matriz (SCRUM-44) con el Gerente General en la semana 1 | Oziel |
| Seguridad del token | JWT de corta duración; refresh token fuera del sprint | Luis |

## 7. Compromiso

El equipo se compromete con el **objetivo del sprint** y con **18 SP** (SCRUM-1,
2, 3, 10 y 15).

| Integrante | Conformidad |
| --- | --- |
| Oziel Rodman Ramos Torrez | ☐ |
| Luis Daniel Rojas Cáceres | ☐ |
| Pedro Andrez Condorena Apaza | ☐ |
