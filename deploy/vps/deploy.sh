#!/usr/bin/env bash
# Despliega en el VPS la última versión de main. Lo ejecuta GitHub Actions por SSH
# (es el comando forzado de la clave de despliegue) o una persona a mano.
set -euo pipefail

cd "$(dirname "$0")/../.."
set -a
[ -f .env ] && . ./.env
set +a

git fetch --quiet origin main
git checkout --quiet main
git merge --ff-only --quiet origin/main
echo "Desplegando $(git log -1 --format='%h %s')"

docker compose -p farmacia -f docker-compose.yml -f deploy/vps/docker-compose.vps.yml \
  up -d --build --remove-orphans

for _ in $(seq 1 40); do
  if curl -fsS "http://127.0.0.1:${GATEWAY_PORT:-5100}/health" >/dev/null; then
    echo "Gateway saludable en 127.0.0.1:${GATEWAY_PORT:-5100}"
    docker compose -p farmacia ps --format '{{.Service}}: {{.Status}}'
    exit 0
  fi
  sleep 3
done

echo "El Gateway no respondió a tiempo" >&2
docker compose -p farmacia logs --tail=50 >&2
exit 1
