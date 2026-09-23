# ADR-001: Selección de ASP.NET Core para los servicios cloud

| Campo | Detalle |
| --- | --- |
| Estado | Aceptado |
| Fecha | 22 de septiembre de 2026 |
| Documento | Documento 3 v3, sección 8 |

## Contexto

El sistema necesita servicios cloud para la sincronización, la identidad, las notificaciones y el negocio. La aplicación de escritorio se desarrolla en C# con Avalonia UI, el equipo tiene experiencia en .NET y se requiere compartir reglas de negocio con el puesto, ejecutar procesos en segundo plano y desplegar en contenedores Docker.

## Alternativas evaluadas

- **A1.** ASP.NET Core Web API (C#): alto rendimiento, Identity y JWT integrados, BackgroundService nativo y el mismo lenguaje que el cliente, lo que permite reutilizar Pharmacy.Domain y Pharmacy.Application.
- **A2.** Node.js con NestJS (TypeScript): ecosistema amplio, pero obliga a reescribir las reglas de negocio en otro lenguaje.
- **A3.** Spring Boot (Java): plataforma madura, con mayor consumo de recursos y sin experiencia previa del equipo.
- **A4.** Django REST Framework (Python): alta productividad en CRUD, con menor rendimiento y tipado débil para reglas complejas.

## Decisión

Se adopta ASP.NET Core (.NET 8 LTS) para todos los servicios cloud, cada uno desplegado como imagen Docker independiente.

## Consecuencias positivas

Un solo lenguaje en todo el sistema, reutilización directa de la lógica compartida, pruebas homogéneas con xUnit y soporte nativo para Identity, JWT, SignalR y procesos en segundo plano.

## Trade-offs y riesgos

El soporte de .NET 8 finaliza en noviembre de 2026, por lo que debe planificarse la migración a .NET 10 LTS. La elección concentra al equipo en el ecosistema .NET.
