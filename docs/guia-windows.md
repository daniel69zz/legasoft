# Guía de ejecución en Windows

Esta guía usa PowerShell desde la raíz del repositorio. El servidor se ejecuta
en Docker y la interfaz es una aplicación de escritorio Avalonia.

## 1. Comprobar requisitos

Abre Docker Desktop y espera a que el motor esté listo. Comprueba las herramientas:

```powershell
dotnet --version
dotnet --list-sdks
docker version
docker compose version
```

La selección del SDK está en [global.json](../global.json): .NET 8.0.426,
con la política de avance indicada en ese archivo. La primera compilación
necesita conexión para descargar dependencias e imágenes.

## 2. Preparar variables locales

[Docker Compose](../docker-compose.yml) incluye valores para desarrollo y puede
arrancar sin un archivo .env. Si necesitas personalizarlos, copia el ejemplo
solo cuando todavía no exista una configuración local:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Consulta [.env.example](../.env.example) para conocer las variables disponibles.
Las contraseñas incluidas son de demostración.

## 3. Iniciar el servidor

Desde la raíz del repositorio:

```powershell
docker compose -p farmacia up -d --build
docker compose -p farmacia ps
```

Se levantan PostgreSQL, Identity, Business, Sync, Notification, Gateway y Mailpit.
Los servicios aplican sus migraciones de la base central al arrancar.
Mantén el nombre de proyecto farmacia en los comandos siguientes para trabajar
con los mismos contenedores y volumen.

## 4. Abrir la aplicación

```powershell
dotnet run --project src/frontend/Pharmacy.Desktop
```

Se abre una ventana de escritorio. Para la demo, usa admin o cajero1 con
la contraseña Farmacia2026! si no has cambiado las credenciales. El puesto
predeterminado es PUESTO-01. Consulta los demás roles en [la guía del prototipo](../src/README.md).

## 5. Consultar servicios locales

| Recurso | Dirección predeterminada |
| --- | --- |
| Salud del Gateway | http://localhost:5100/health |
| Swagger de las API | http://localhost:5100/swagger |
| Correos de prueba en Mailpit | http://localhost:8025 |
| PostgreSQL | localhost:5433, base farmacia |

La respuesta de /health comprueba el Gateway; por sí sola no confirma el estado
de todos los servicios. Usa también el estado de los contenedores y sus logs.

## 6. Detener y reanudar

Para detener los servicios locales conservando el volumen de datos:

```powershell
docker compose -p farmacia stop
```

Para reanudar esos mismos contenedores:

```powershell
docker compose -p farmacia start
```

La ventana de escritorio se cierra por separado. Después de actualizar el código,
usa up -d --build como se describe abajo para reconstruir los servicios.

## 7. Actualizar desde GitHub

Cierra primero la aplicación para que no bloquee los archivos de compilación.
Comprueba la rama y los cambios locales:

```powershell
git status --short --branch
git fetch origin
```

Si deseas ejecutar la versión de main y no tienes cambios pendientes, continúa:

```powershell
git switch main
git pull --ff-only origin main
docker compose -p farmacia up -d --build
dotnet run --project src/frontend/Pharmacy.Desktop
```

Si Git informa cambios pendientes o divergencia, revisa el mensaje antes de
continuar. No es necesario borrar las bases de datos para reconstruir las imágenes.
