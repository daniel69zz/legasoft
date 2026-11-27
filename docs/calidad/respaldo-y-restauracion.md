# Respaldo y restauración (SCRUM-36 · HU-23 y SCRUM-41 · HU-T06)

| Qué | Cómo | Dónde |
| --- | --- | --- |
| Respaldo del negocio (ventas, inventario, caja y auditoría) | Automático cada 24 h y a pedido desde **Parámetros → Respaldos** (solo Administrador). COPY de PostgreSQL en un `.zip` con manifiesto | Volumen `backups` del Business Service (`/backups`), 14 días |
| Recuperación del negocio | **Parámetros → Recuperar el respaldo elegido**, con confirmación. Antes guarda el estado actual («Antes de restaurar») y queda en la auditoría | La misma app |
| Respaldo completo (incluye usuarios y equipos) | `deploy/vps/respaldo-completo.sh` con cron diario a las 03:30 (`pg_dump --format=custom`) | `/var/backups/farmacia` en el VPS, 14 días |
| Aviso de falla | Si un respaldo falla o el último tiene más de 26 h, el Notification Worker lo incluye en el correo de alertas | Mailpit (local) o SMTP configurado |

**RPO:** 24 horas (un respaldo diario). **RTO objetivo:** 4 horas.

## Simulacro de restauración completa (CP-RNF-008)

Se hace en un servidor de prueba, nunca sobre producción. Anotar la hora de inicio y de fin.

```sh
# 1. Base vacía nueva (simula la pérdida de la base central)
docker compose -p farmacia-simulacro up -d postgres
# 2. Restaurar el último respaldo completo
ultimo=$(ls -t /var/backups/farmacia/farmacia-*.dump | head -1)
docker compose -p farmacia-simulacro exec -T postgres pg_restore -U postgres -d farmacia --clean --if-exists < "$ultimo"
# 3. Levantar los servicios y comprobar
docker compose -p farmacia-simulacro up -d
curl -s http://localhost:5100/health
```

| Fecha | Respaldo usado | Antigüedad del respaldo | Inicio | Fin | Duración | Resultado |
| --- | --- | --- | --- | --- | --- | --- |
| | | | | | | |

**Se acepta** si la duración es de 4 horas o menos y el respaldo usado no tiene más de 24 horas.

### Verificación hecha en el Sprint 3 (base local de prueba)

- Respaldo del negocio: 17 tablas y 63 filas; recuperación en la misma transacción con el trigger de la
  bitácora desactivado solo durante la carga; después el trigger volvió a rechazar cambios.
- Un cliente registrado después del respaldo desapareció al recuperar, y la auditoría registró
  «Recuperación de respaldo» con el nombre del respaldo de seguridad.
