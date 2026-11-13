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
