#!/usr/bin/env bash
# SCRUM-41 · HU-T06 (RNF-008): respaldo completo y diario de la base central del VPS (todos los schemas,
# incluidos usuarios y equipos de identity). Complementa el respaldo lógico del Business Service, que el
# administrador genera y recupera desde la app (SCRUM-36 · HU-23).
#
# Instalación en el VPS (una vez):
#   sudo install -m 750 deploy/vps/respaldo-completo.sh /usr/local/bin/farmacia-respaldo
#   echo '30 3 * * * root /usr/local/bin/farmacia-respaldo >> /var/log/farmacia-respaldo.log 2>&1' | sudo tee /etc/cron.d/farmacia-respaldo
#
# RPO de 24 h: un respaldo diario a las 03:30. Retención: 14 días (RESPALDO_DIAS).
# Si falla, avisa al Business Service, que lo incluye en las alertas por correo del Notification Worker.
set -euo pipefail

PROYECTO="${PROYECTO:-farmacia}"
CARPETA="${RESPALDO_CARPETA:-/var/backups/farmacia}"
DIAS="${RESPALDO_DIAS:-14}"
ARCHIVO="$CARPETA/farmacia-$(date +%Y%m%d-%H%M%S).dump"

avisar_falla() {
  local detalle="$1"
  docker compose -p "$PROYECTO" exec -T postgres psql -U postgres -d farmacia -v ON_ERROR_STOP=1 -c \
    "INSERT INTO inventory.settings (key, value, updated_at, updated_by)
     VALUES ('backup.last_failure', now()::text || '|' || '$detalle', now(), 'respaldo completo')
     ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value, updated_at = now();" >/dev/null || true
}
trap 'avisar_falla "El respaldo completo del VPS falló (línea $LINENO)"' ERR

mkdir -p "$CARPETA"
docker compose -p "$PROYECTO" exec -T postgres pg_dump -U postgres -d farmacia --format=custom > "$ARCHIVO.tmp"
mv "$ARCHIVO.tmp" "$ARCHIVO"
find "$CARPETA" -name 'farmacia-*.dump' -mtime +"$DIAS" -delete
echo "$(date -Iseconds) respaldo completo: $ARCHIVO ($(du -h "$ARCHIVO" | cut -f1))"
