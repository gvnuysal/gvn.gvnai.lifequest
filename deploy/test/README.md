# deploy/test

Bu Mac'teki Docker üzerinde çalışan LifeQuest test ortamı. Ayrıntılı rehber: [docs/deploy-test.md](../../docs/deploy-test.md).

| Dosya | Görev |
|---|---|
| `docker-compose.yml` | Caddy (HTTPS) + web + api + postgres; proje adı `lifequest-test` |
| `Caddyfile` | İki alan adı için TLS ve yönlendirme (`TLS_MODE=internal` → yerel sertifika otoritesi) |
| `.env.example` | Gizli değer şablonu; gerçeği `~/.lifequest-test/.env` |
| `deploy.sh` | Derle → yedekle → yayına al → sağlık kontrolü → gerekirse geri al |
| `rollback.sh` | Önceki sürüme dön (`--restore-db` ile veritabanını da) |
| `setup-local.sh` | hosts kaydı + yerel sertifika güveni (bir kez, yönetici şifresi ister) |
| `lib.sh` | Script'lerin ortak ayarları |

```bash
deploy/test/deploy.sh
```

Otomatik deploy: `Deploy_Lifequest_Test` dalına push → `.github/workflows/deploy-test.yml`.
