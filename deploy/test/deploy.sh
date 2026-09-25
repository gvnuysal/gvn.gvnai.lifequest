#!/usr/bin/env bash
# LifeQuest test ortamına deploy: derle → yedekle → yayına al → sağlık kontrolü → (başarısızsa) geri al.
# Kullanım: deploy/test/deploy.sh            (elle veya GitHub Actions self-hosted runner'dan)
#           SKIP_BACKUP=1 deploy/test/deploy.sh
set -euo pipefail
source "$(dirname "$0")/lib.sh"

step "1/6 Ön kontroller"
require_docker
load_env
mkdir -p "$BACKUP_DIR"
ok "Docker çalışıyor, ortam dosyası: $ENV_FILE"

TAG="$(git -C "$ROOT" rev-parse --short HEAD)"
if [[ -n "$(git -C "$ROOT" status --porcelain --untracked-files=no)" ]]; then
  TAG="$TAG-dirty-$(date +%H%M%S)"
  warn "Commit edilmemiş değişiklikler var; sürüm etiketi: $TAG"
fi
PREVIOUS="$(current_tag)"
info "Yeni sürüm: $TAG · şu an yayında: ${PREVIOUS:-yok}"

step "2/6 İmajları derle (lifequest-api:$TAG, lifequest-web:$TAG)"
IMAGE_TAG="$TAG" compose build api web
ok "İmajlar hazır"

step "3/6 Veritabanı yedeği"
if [[ "${SKIP_BACKUP:-0}" == "1" ]]; then
  warn "SKIP_BACKUP=1: yedek atlandı"
elif compose ps --status running --services 2>/dev/null | grep -qx postgres; then
  file="$BACKUP_DIR/lifequest-$(date +%Y%m%d-%H%M%S)-before-$TAG.sql.gz"
  compose exec -T postgres pg_dump -U lifequest -d lifequest --clean --if-exists | gzip > "$file"
  ok "Yedek: $file ($(du -h "$file" | cut -f1))"
  # Yalnızca bu script'in ürettiği yedekler: en yeni 10 tanesi tutulur.
  ls -1t "$BACKUP_DIR"/lifequest-*.sql.gz 2>/dev/null | tail -n +11 | while read -r old; do rm -f -- "$old"; done
else
  info "Veritabanı henüz çalışmıyor (ilk kurulum); yedek gerekmiyor"
fi

step "4/6 Yayına al"
IMAGE_TAG="$TAG" compose up -d --remove-orphans
ok "Container'lar güncellendi"

step "5/6 Sağlık kontrolü (en fazla 120 sn)"
if ! wait_healthy; then
  warn "Sağlık kontrolü başarısız. Son loglar:"
  compose logs --tail=40 api web caddy || true
  if [[ -n "$PREVIOUS" ]]; then
    step "Geri alınıyor → $PREVIOUS"
    IMAGE_TAG="$PREVIOUS" compose up -d api web
    fail "Deploy başarısız; önceki sürüm ($PREVIOUS) yeniden yayında. Veritabanı yedeği: $BACKUP_DIR"
  fi
  fail "Deploy başarısız ve geri dönülecek önceki sürüm yok."
fi
ok "API ve UI sağlıklı"

step "6/6 Sürüm kaydı"
if [[ -n "$PREVIOUS" && "$PREVIOUS" != "$TAG" ]]; then
  echo "$PREVIOUS" > "$STATE_DIR/previous-tag"
fi
echo "$TAG" > "$STATE_DIR/current-tag"
# Eski sürüm imajlarını temizle: yayındaki ve bir önceki sürüm her zaman kalır, en yeni 5 sürüm tutulur.
keep="$TAG $(previous_tag)"
for repo in lifequest-api lifequest-web; do
  docker images "$repo" --format '{{.Tag}}' | grep -v -e '^current$' -e '^<none>$' | tail -n +6 | while read -r t; do
    [[ " $keep " == *" $t "* ]] || docker rmi "$repo:$t" >/dev/null 2>&1 || true
  done
done
prev="$(previous_tag)"
ok "Yayında: $TAG (önceki: ${prev:-yok})"

cat <<SUMMARY

  UI : https://$UI_HOST
  API: https://$API_HOST   (dokümantasyon: https://$API_HOST/scalar/v1)

  Loglar : docker compose -p $PROJECT logs -f api
  Durum  : docker compose -p $PROJECT ps
  Geri al: deploy/test/rollback.sh
SUMMARY
