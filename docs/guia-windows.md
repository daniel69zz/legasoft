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
