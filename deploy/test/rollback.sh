#!/usr/bin/env bash
# Bir önceki sürüme geri döner. İsteğe bağlı olarak son (veya verilen) veritabanı yedeğini geri yükler.
# Kullanım: deploy/test/rollback.sh
#           deploy/test/rollback.sh --restore-db [yedek.sql.gz]   (veritabanını yedekteki hâline döndürür)
set -euo pipefail
source "$(dirname "$0")/lib.sh"

require_docker
load_env
PREVIOUS="$(previous_tag)"
CURRENT="$(current_tag)"
[[ -n "$PREVIOUS" ]] || fail "Geri dönülecek önceki sürüm kaydı yok ($STATE_DIR/previous-tag)."

step "Uygulama geri alınıyor: $CURRENT → $PREVIOUS"
IMAGE_TAG="$PREVIOUS" compose up -d api web
echo "$PREVIOUS" > "$STATE_DIR/current-tag"
echo "$CURRENT" > "$STATE_DIR/previous-tag"
ok "Yayında: $PREVIOUS"

if [[ "${1:-}" == "--restore-db" ]]; then
  file="${2:-$(ls -1t "$BACKUP_DIR"/lifequest-*.sql.gz 2>/dev/null | head -1)}"
  [[ -f "$file" ]] || fail "Yedek bulunamadı: ${file:-$BACKUP_DIR}"
  warn "Test veritabanı şu yedeğe döndürülecek: $file"
  warn "Yedekten sonra yapılan tüm değişiklikler (kayıtlar, görevler) kaybolur."
  read -r -p "  Onaylamak için 'geri-yukle' yazın: " answer
  [[ "$answer" == "geri-yukle" ]] || fail "İptal edildi."
  gunzip -c "$file" | compose exec -T postgres psql -q -U lifequest -d lifequest -v ON_ERROR_STOP=1 >/dev/null
  compose restart api >/dev/null
  ok "Veritabanı geri yüklendi ve API yeniden başlatıldı"
fi
