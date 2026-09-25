# deploy/test script'lerinin ortak ayarları. Doğrudan çalıştırılmaz; source edilir.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="$ROOT/deploy/test/docker-compose.yml"
PROJECT="lifequest-test"
STATE_DIR="${LIFEQUEST_TEST_HOME:-$HOME/.lifequest-test}"
ENV_FILE="${LIFEQUEST_TEST_ENV:-$STATE_DIR/.env}"
BACKUP_DIR="$STATE_DIR/backups"

step() { printf '\n\033[1;36m▶ %s\033[0m\n' "$*"; }
info() { printf '  %s\n' "$*"; }
ok()   { printf '  \033[32m✓ %s\033[0m\n' "$*"; }
warn() { printf '  \033[33m! %s\033[0m\n' "$*"; }
fail() { printf '\n\033[1;31m✗ %s\033[0m\n' "$*" >&2; exit 1; }

compose() { docker compose -p "$PROJECT" -f "$COMPOSE_FILE" --env-file "$ENV_FILE" "$@"; }

load_env() {
  [[ -f "$ENV_FILE" ]] || fail "Ortam dosyası yok: $ENV_FILE
  Oluşturmak için: mkdir -p $STATE_DIR && cp $ROOT/deploy/test/.env.example $ENV_FILE && chmod 600 $ENV_FILE"
  set -a; # shellcheck disable=SC1090
  source "$ENV_FILE"; set +a
  [[ ${#JWT_SECRET} -ge 32 ]] || fail "JWT_SECRET en az 32 karakter olmalı ($ENV_FILE). Üretmek için: openssl rand -base64 48"
  [[ -n "${POSTGRES_PASSWORD:-}" ]] || fail "POSTGRES_PASSWORD boş ($ENV_FILE)."
  [[ -n "${UI_HOST:-}" && -n "${API_HOST:-}" ]] || fail "UI_HOST ve API_HOST tanımlı olmalı ($ENV_FILE)."
  # Elle çalıştırılan compose komutları yayındaki sürümü kullansın.
  local deployed; deployed="$(current_tag)"
  export IMAGE_TAG="${IMAGE_TAG:-${deployed:-current}}"
}

require_docker() {
  command -v docker >/dev/null || fail "docker komutu bulunamadı (PATH: $PATH)."
  docker info >/dev/null 2>&1 || fail "Docker çalışmıyor. Docker Desktop'ı başlatın."
}

# HTTPS isteğini DNS'e bakmadan bu makinedeki Caddy'ye gönderir (hosts kaydı olmasa da çalışır).
https_get() {
  local host="$1" path="$2"
  curl -fsS --max-time 5 -k --resolve "$host:443:127.0.0.1" "https://$host$path"
}

# Yanıt gövdesi beklenen metni içeriyor mu? (curl | grep -q, pipefail ile yanlış negatif verir.)
https_contains() {
  local body
  body="$(https_get "$1" "$2" 2>/dev/null)" || return 1
  [[ "$body" == *"$3"* ]]
}

# API /health, UI ana sayfası ve config.js (doğru API adresi) hazır olana kadar bekler (en fazla ~120 sn).
wait_healthy() {
  for _ in $(seq 1 40); do
    if https_contains "$API_HOST" /health "Healthy" \
       && https_contains "$UI_HOST" / "<app-root" \
       && https_contains "$UI_HOST" /config.js "$API_HOST"; then
      return 0
    fi
    sleep 3
  done
  return 1
}

current_tag()  { cat "$STATE_DIR/current-tag" 2>/dev/null || true; }
previous_tag() { cat "$STATE_DIR/previous-tag" 2>/dev/null || true; }
