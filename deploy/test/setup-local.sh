#!/usr/bin/env bash
# Bu Mac'i test alan adları için hazırlar (bir kez). Yönetici şifresi yalnızca iki adımda istenir (sudo):
#   1) /etc/hosts'a iki alan adını 127.0.0.1 olarak ekler (işaretli blok; tekrar çalıştırmak güvenli).
#   2) Caddy'nin yerel sertifika otoritesini macOS System anahtar zincirine güvenilir olarak ekler.
# Önce en az bir kez deploy/test/deploy.sh çalışmış olmalı (Caddy sertifika otoritesini ilk açılışta üretir).
# Geri almak için: deploy/test/setup-local.sh --uninstall
set -euo pipefail
source "$(dirname "$0")/lib.sh"
load_env

HOSTS=/etc/hosts
BEGIN="# >>> lifequest-test >>>"
END="# <<< lifequest-test <<<"
CA_FILE="$STATE_DIR/caddy-local-root.crt"
CA_SHA="$STATE_DIR/caddy-local-root.sha1"

remove_hosts_block() {
  if grep -q "$BEGIN" "$HOSTS"; then
    sudo sed -i '' "/$BEGIN/,/$END/d" "$HOSTS"
  fi
}

flush_dns() { sudo dscacheutil -flushcache; sudo killall -HUP mDNSResponder 2>/dev/null || true; }

if [[ "${1:-}" == "--uninstall" ]]; then
  step "hosts kaydı kaldırılıyor"
  remove_hosts_block; flush_dns; ok "Kaldırıldı"
  if [[ -f "$CA_SHA" ]]; then
    step "Yerel sertifika otoritesi güvenilir listeden kaldırılıyor"
    sudo security delete-certificate -Z "$(cat "$CA_SHA")" /Library/Keychains/System.keychain || true
    rm -f "$CA_FILE" "$CA_SHA"; ok "Kaldırıldı"
  fi
  exit 0
fi

cat <<PLAN
Bu script şunları yapacak:
  • $HOSTS dosyasına: 127.0.0.1 $UI_HOST $API_HOST
  • Caddy yerel sertifika otoritesini System anahtar zincirine güvenilir kök olarak ekler
    (yalnızca bu makinede, yalnızca Caddy'nin ürettiği test sertifikaları için)
Yönetici şifreniz istenecek.
PLAN
read -r -p "Devam edilsin mi? [e/H] " answer
[[ "$answer" =~ ^[eE]$ ]] || fail "İptal edildi."

step "1/2 hosts kaydı"
[[ -f /etc/hosts.lifequest-backup ]] || sudo cp "$HOSTS" /etc/hosts.lifequest-backup
remove_hosts_block
printf '%s\n127.0.0.1 %s %s\n::1 %s %s\n%s\n' "$BEGIN" "$UI_HOST" "$API_HOST" "$UI_HOST" "$API_HOST" "$END" | sudo tee -a "$HOSTS" >/dev/null
flush_dns
ok "$UI_HOST ve $API_HOST → 127.0.0.1 (yedek: /etc/hosts.lifequest-backup)"

if [[ "${TLS_MODE:-internal}" != "internal" ]]; then
  info "TLS_MODE=$TLS_MODE: gerçek sertifika kullanılıyor, yerel sertifika otoritesi gerekmiyor."
  exit 0
fi

step "2/2 Yerel sertifika otoritesi"
require_docker
container="$(compose ps -q caddy)"
[[ -n "$container" ]] || fail "Caddy çalışmıyor. Önce: deploy/test/deploy.sh"
docker cp "$container:/data/caddy/pki/authorities/local/root.crt" "$CA_FILE"
openssl x509 -in "$CA_FILE" -noout -fingerprint -sha1 | cut -d= -f2 | tr -d ':' > "$CA_SHA"
info "$(openssl x509 -in "$CA_FILE" -noout -subject)"
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain "$CA_FILE"
ok "Güvenilir yapıldı. Tarayıcıyı yeniden başlatın (Firefox kendi sertifika deposunu kullanır)."

printf '\nHazır: https://%s\n' "$UI_HOST"
