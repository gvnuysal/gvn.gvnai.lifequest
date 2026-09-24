<div align="center">

<img src="docs/images/banner.svg" alt="LifeQuest — Gerçek hayatı oyunlaştıran kişisel keşif platformu" width="100%" />

<br />

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.dev)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![Gvn.GvnFramework](https://img.shields.io/badge/Gvn.GvnFramework-1.0.0--preview-F26B4F?style=for-the-badge)](https://github.com/gvnuysal/gvn.gvnframework)

[![Tests](https://img.shields.io/badge/backend%20tests-199%20passing-22A559?style=flat-square)](#testler)
[![Web tests](https://img.shields.io/badge/web%20tests-19%20passing-22A559?style=flat-square)](#testler)
[![PWA](https://img.shields.io/badge/PWA-mobil%20öncelikli-8B5CF6?style=flat-square)](#web-istemcisi)

**Ekranda daha uzun kalmanı değil, gerçek hayatta daha çok şey yaşamanı hedefleyen bir oyun.**

[Ekranlar](#ekranlar) · [Nasıl çalışır?](#nasil-calisir) · [Mimari](#mimari) · [Hızlı başlangıç](#hizli-baslangic) · [API](#api) · [Analiz değerlendirmesi](docs/analiz-degerlendirmesi.md) · [Simülasyon raporu](docs/simulasyon-raporu.md)

</div>

---

## ✨ Nedir?

LifeQuest; zamanına, bütçene, ilgi alanlarına ve ne kadar keşif istediğine göre **gerçek dünyada yapabileceğin görevler (quest)** önerir. Tamamladığın her deneyim sana **Life XP** ve Keşif, Kültür, Öğrenme, Sosyal, Hareket, Yaratıcılık alanlarında **kategori XP'si** kazandırır. Sistem geri bildirimlerinden öğrenir ve önerileri giderek sana göre şekillendirir.

| | |
|---|---|
| 🧭 **Kontrollü keşif** | Sakin · Dengeli · Şaşırt Beni. Yeniliğin dozunu sen seçersin. |
| 💡 **Açıklanabilir öneriler** | Her önerinin yanında *"neden bunu önerdik?"* açıklaması ve skor dökümü vardır. |
| 🌱 **Sağlıklı oyunlaştırma** | Streak yok, kayıp korkusu yok. Süresi dolan görevin cezası da yok. |
| 🕸️ **Taste Graph** | *Kahve → Kafe Kültürü → Mimari → Fotoğrafçılık* gibi komşu ilgi alanlarına geçiş. |
| 🔒 **Önce mahremiyet** | Konum takibi yok, fotoğraf doğrulaması yok. Tek tıkla tüm veriler silinir (KVKK/GDPR). |
| 🛡️ **Güvenli katalog** | 100 editoryal template; her biri güvenlik kontrol listesinden CI'da geçer. Gece açık hava görevi yok; efor sınırına saygı. |
| 🃏 **Hızlı ısınma** | Onboarding'deki "Sana göre mi?" kartları ilk günden isabetli öneri sağlar. |
| 📬 **Suçlamayan haftalık özet** | Bildirim yalnızca seçersen; varsayılan, uygulama içi haftalık özet. |

---

<a id="ekranlar"></a>

## 📱 Ekranlar

<table>
  <tr>
    <td align="center" width="33%"><img src="docs/images/today.png" alt="Bugünün önerileri" width="240" /><br /><sub><b>Bugün</b><br />Günün 3 önerisi ve gerekçeleri</sub></td>
    <td align="center" width="33%"><img src="docs/images/quest-why.png" alt="Neden bunu önerdik?" width="240" /><br /><sub><b>Neden bunu önerdik?</b><br />Skor bileşenleri şeffaf</sub></td>
    <td align="center" width="33%"><img src="docs/images/celebration.png" alt="Tamamlama kutlaması" width="240" /><br /><sub><b>Tamamladın!</b><br />XP, seviye ve deneyim puanı</sub></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/images/onboarding.png" alt="Onboarding" width="240" /><br /><sub><b>Onboarding</b><br />Bir dokunuş: ilgimi çekiyor · İki: çok seviyorum</sub></td>
    <td align="center"><img src="docs/images/suggest.png" alt="Bağlamsal öneri" width="240" /><br /><sub><b>"2 saatim var"</b><br />Bağlama göre öneri</sub></td>
    <td align="center"><img src="docs/images/progress.png" alt="İlerleme" width="240" /><br /><sub><b>İlerleme</b><br />Life seviyesi ve Life Profile</sub></td>
  </tr>
  <tr>
    <td align="center"><img src="docs/images/active.png" alt="Görevlerim" width="240" /><br /><sub><b>Görevlerim</b><br />Devam eden görevler ve geçmiş</sub></td>
    <td align="center"><img src="docs/images/today-dark.png" alt="Koyu tema" width="240" /><br /><sub><b>Koyu tema</b><br />Sistem tercihine uyar</sub></td>
    <td align="center"><img src="docs/images/profile-dark.png" alt="Profil" width="240" /><br /><sub><b>Profil</b><br />Öğrenilen ilgiler görünür</sub></td>
  </tr>
</table>

---

<a id="nasil-calisir"></a>

## 🎯 Nasıl çalışır?

### Quest yaşam döngüsü

```mermaid
stateDiagram-v2
    direction LR
    [*] --> Offered: Günlük / bağlamsal öneri
    Offered --> Accepted: Kabul et
    Offered --> Skipped: Geç (sebep)
    Accepted --> Completed: Tamamladım (+XP)
    Accepted --> Skipped: Bırak
    Offered --> Expired: Gün bitti
    Accepted --> Expired: Süre doldu (cezasız)
    Completed --> [*]: Puan 1-5 · daha fazla/az
```

### Öneri motoru

```mermaid
flowchart LR
    A[Katalog<br/>güvenli template'ler] --> E{Uygunluk filtresi<br/>cooldown · bütçe · süre · şehir<br/>efor · gece açık hava · ilgimi çekmedi}
    P[Profil<br/>ilgiler · hedefler<br/>Discovery Radius] --> S
    H[Geçmiş<br/>tamamlanan · geçilen<br/>puanlar] --> S
    G[Taste Graph] --> S
    E --> S[Skorlama]
    S --> D[Seçim<br/>çeşitlilik · kısa görev<br/>keşif slotu]
    D --> X["Neden bunu önerdik?"<br/>açıklaması]
    X --> Q[UserQuest snapshot]
```

**Skor:** `Interest + Novelty + Context + GoalFit + Diversity + FeedbackFit − Repetition − Friction − Risk`

- Her bileşen 0–1 arasına normalize edilir. Ağırlıklar `appsettings.json → Recommendation` bölümünden yönetilir.
- Discovery Radius, İlgi ve Yenilik ağırlığını değiştirir.
- Diversity her seçimden sonra yeniden hesaplanır. Bu, *"kahve seviyor → hep kahve"* döngüsünü kırar.
- İlk slot mümkünse kısa bir görevdir, böylece ilk 5 dakikada uygulanabilir bir öneri olur.
- Keşif modlarında bir slot kontrollü keşfe ayrılır (Dengeli %50, Şaşırt Beni her gün). Önce Taste Graph'ta sevdiğin bir ilgiye komşu quest'ler denenir. Seçim kullanıcı + gün ile tohumlanır, yani tekrarlanabilir ve test edilebilir.
- Son 7 günde gösterilip seçilmeyen öneriler her gösterimde biraz daha geriye düşer.
- "İlgimi çekmedi" ilgi ağırlığını düşürür. "Pahalı / zamanım yok / uzak" ise ilgiyi değil, benzer maliyet ve süredeki önerilerin skorunu etkiler.

### Ekonomi (deterministik, LLM üretmez)

| Kural | Değer |
|---|---|
| Life XP | `BaseXP(tür) × Zorluk × Yenilik` |
| BaseXP | Günlük 30 · Haftalık 120 · Macera 250 · Destansı 500 |
| Zorluk | Kolay 1.0 · Orta 1.5 · Zor 2.0 · Kahramanca 3.0 |
| Yenilik | Yeni kategori 1.25 · yeni template 1.10 · tanıdık 1.0 |
| Kategori XP | Birincil = Life × 2/3 · ikincil = Life × 2/9 (Haftalık/Orta → 180 + 120 + 40) |
| Seviye | `base × (L−1) × L / 2` (Life 100, kategori 60) |
| Çift XP koruması | Append-only `xp_transactions` defteri, `(source_type, source_id)` benzersiz |

### Offline simülasyon

Motor saf olduğu için gerçek domain koduyla 8 persona × 20 tohum × 30 gün simüle edilir (`tools/LifeQuest.Simulation`). Analiz sonrası yapılan motor değişiklikleri, isabet ve north-star'ı koruyarak gizli ilgi keşfini **%48 → %60**'a çıkardı, tekrar eden öneriyi **%45 → %34**'e indirdi. Ayrıntılar: [simülasyon raporu](docs/simulasyon-raporu.md).

<img src="docs/images/sim-ablation.svg" alt="Simülasyon: bileşen ablasyonu" width="100%" />

```bash
dotnet run --project tools/LifeQuest.Simulation -- --docs docs
```

### AI Quest Master altyapısı

Anlatım `IQuestNarrator` portu arkasında. Prompt yalnızca PII'siz yapısal alanlardan kurulur. Çıktı `NarrationGuard`'dan geçer: uzunluk, URL, para/XP, uydurma sayı, riskli ifade ve kişisel veri talebi kontrol edilir. 2 sn zaman aşımında veya ihlalde template metnine dönülür. Kategori, süre, maliyet ve XP her zaman template'ten gelir. Varsayılan implementasyon deterministik `TemplateQuestNarrator`.

---

<a id="mimari"></a>

## 🏗️ Mimari

Modular monolith olarak kuruldu. Backend, [**Gvn.GvnFramework**](https://github.com/gvnuysal/gvn.gvnframework) NuGet paketleriyle Clean Architecture katmanlarına ayrılır.

```mermaid
flowchart TB
    subgraph Client["🌐 LifeQuest.Web · Angular 21 PWA"]
        UI[Standalone bileşenler · signals · tasarım sistemi]
    end
    subgraph Api["LifeQuest.Api"]
        C[Controller'lar · JWT · rate limit · PII maskeleme]
    end
    subgraph App["LifeQuest.Application"]
        M[CQRS / MediatR · validasyon · QuestOfferService]
    end
    subgraph Dom["LifeQuest.Domain"]
        D[Aggregate'ler · XP kuralları · Recommendation Engine]
    end
    subgraph Infra["LifeQuest.Infrastructure"]
        I[EF Core · repository'ler · katalog cache · Hangfire job'ları]
    end
    DB[(PostgreSQL)]
    UI -- "/api/v1" --> C --> M --> D
    M --> I --> DB
    I -. uygular .-> D
```

<details>
<summary><b>Proje yapısı</b></summary>

```
src/
├── LifeQuest.Domain          Aggregate'ler, iş kuralları, öneri motoru (saf, I/O'suz)
├── LifeQuest.Application     CQRS use case'leri, öneri orkestrasyonu, DTO'lar
├── LifeQuest.Infrastructure  EF Core + PostgreSQL, repository'ler, seed, job'lar, modüller
├── LifeQuest.Api             Controller'lar, kimlik, rate limit, log maskeleme
└── LifeQuest.Web             Angular PWA istemcisi
tests/
├── LifeQuest.Domain.Tests            Öneri motoru, ekonomi ve aggregate testleri
├── LifeQuest.Application.Tests       Narration guard/fallback, metrikler, haftalık özet
├── LifeQuest.Catalog.Tests           Editoryal güvenlik kuralları (CI kapısı)
└── LifeQuest.Api.IntegrationTests    Testcontainers PostgreSQL ile uçtan uca testler
tools/
└── LifeQuest.Simulation              Offline öneri simülasyonu ve rapor üretimi
docs/
├── analiz-degerlendirmesi.md         Ürün analizi değerlendirmesi ve framework bulguları
└── simulasyon-raporu.md              Persona simülasyonu, ablasyon ve öneriler
```

Modüller (framework `IModule`, `LoadModules` ile yüklenir): **Persistence · Identity · Profile · Catalog · Quest · Progression · Notification · Admin**

</details>

<details>
<summary><b>Gvn.GvnFramework kullanımı</b></summary>

| Paket | LifeQuest'te kullanımı |
|---|---|
| Core | `Result<T>`, `Error`, `Guard`, exception'lar, `Batch` |
| Domain | `AggregateRoot`, `Entity`, `ValueObject` (`QuestReward`), `ISoftDeletable`, `IRepository`, `IUnitOfWork` |
| Application | `ICommand`/`IQuery` + handler'lar, pipeline behavior'lar, `PagedRequest`/`PagedResult` |
| EntityFramewokCore | `GvnDbContext<T>` (audit, soft delete, domain event), `EfRepository`, `UnitOfWork<T>` |
| Security | JWT, BCrypt, `ICurrentUserService` |
| Caching | Katalog snapshot cache'i (Memory / Redis) |
| BackgroundJobs | Periyodik job'lar (`IRecurringJob`, `IBackgroundJobService`) |
| Logging · AspNetCore · Swagger | Serilog, `ApiControllerBase`, middleware'ler, OpenAPI + Scalar |
| Modularity · DepedencyInjection | Modül kaydı, `Decorate` |

Framework'te bulunan sorunlar ve LifeQuest'teki geçici çözümler [analiz değerlendirmesi](docs/analiz-degerlendirmesi.md#4-gvngvnframework-bulguları) dokümanında.

</details>

---

<a id="hizli-baslangic"></a>

## 🚀 Hızlı başlangıç

**Gereksinimler:** .NET 10 SDK · Node 20+ · Docker

**1. Framework paketlerini hazırla.** LifeQuest, Gvn.GvnFramework'ü yerel bir NuGet kaynağından kullanır. Framework reposunu bu reponun **yanına** klonlayıp paketle:

```bash
git clone https://github.com/gvnuysal/gvn.gvnframework.git ../Gvn.GvnFramework
```

```bash
dotnet pack ../Gvn.GvnFramework/Gvn.GvnFramework.sln -c Release -o ../Gvn.GvnFramework/artifacts
```

**2. Veritabanını başlat.**

```bash
docker compose up -d postgres
```

**3. API'yi çalıştır.** Migration'lar ve katalog seed'i açılışta uygulanır.

```bash
dotnet run --project src/LifeQuest.Api
```

**4. Web istemcisini kur ve çalıştır.** Geliştirme proxy'si `/api` isteklerini API'ye yönlendirir.

```bash
npm --prefix src/LifeQuest.Web install
```

```bash
npm --prefix src/LifeQuest.Web start
```

| Adres | |
|---|---|
| http://localhost:4200 | 📱 LifeQuest web uygulaması |
| http://localhost:5080/scalar/v1 | 📄 Scalar API arayüzü |
| http://localhost:5080/hangfire | ⏰ Hangfire dashboard (yalnızca localhost) |
| http://localhost:5080/health | ✅ Sağlık kontrolü |

---

<a id="testler"></a>

## 🧪 Testler

```bash
dotnet test LifeQuest.slnx
```

```bash
npm --prefix src/LifeQuest.Web test -- --watch=false
```

| Paket | Kapsam |
|---|---|
| `LifeQuest.Domain.Tests` (60) | Öneri motoru (analizdeki "kahve" senaryosu, efor ve gece açık hava filtreleri, güdümlü keşif dahil), XP/seviye ekonomisi, quest durum makinesi, başarımlar, katalog kuralları, haftalık özet metni |
| `LifeQuest.Application.Tests` (19) | Narration guard (masum kelimelerde yanlış pozitif yok), zaman aşımı/fallback, PII'siz prompt, north-star hesabı, özet idempotency'si |
| `LifeQuest.Catalog.Tests` (104) | 100 seed template'in her biri ve katalog dengesi (CI kapısı) |
| `LifeQuest.Api.IntegrationTests` (16) | Gerçek PostgreSQL (Testcontainers): günlük öneri idempotency'si, eşzamanlı tamamlamada çift XP olmaması, yatay erişim, token rotasyonu ve çalınma tespiti, hesap silme, başlangıç kartları, admin metrik yetkisi, haftalık özet |
| `LifeQuest.Web` (19, vitest) | Token yenileme interceptor'ı (tek uçuşlu refresh), hata ayrıştırma, formatlayıcılar, JWT rol okuma |

---

<a id="web-istemcisi"></a>

## 📱 Web istemcisi

Angular 21 ile yazıldı: standalone bileşenler, signals, zoneless, Reactive Forms. UI kütüphanesi yok; tasarım sistemi CSS değişkenleriyle kurulu. 6 kategori rengi ve açık/koyu tema var. İkonlar kütüphanesiz SVG.

- **Oturum:** access token yalnızca bellekte tutulur. Refresh token açılışta oturumu sessizce geri yükler. 401 alındığında tek seferlik yenileme yapılır.
- **PWA:** service worker yalnızca uygulama kabuğunu önbelleğe alır. API yanıtları mahremiyet nedeniyle önbelleğe alınmaz.
- **Erişilebilirlik:** 44 px dokunma hedefleri, görünür odak, AA kontrast, `prefers-reduced-motion` desteği.

> ⚠️ Refresh token şu an `localStorage`'da tutuluyor. Üretim öncesinde httpOnly + SameSite çereze taşınması planlanıyor.

---

<a id="api"></a>

## 🔌 API (v1)

| Uç | Açıklama |
|---|---|
| `POST /api/v1/auth/register` · `login` · `refresh` · `logout` | Kısa ömürlü JWT, rotasyonlu refresh token |
| `GET /api/v1/catalog/interests` | İlgi alanı kataloğu |
| `GET /api/v1/profile` · `PUT …/onboarding` · `PATCH …/preferences` · `PUT …/interests` | Profil ve tercihler |
| `DELETE /api/v1/profile` | Hesabı ve tüm verileri kalıcı silme (şifre onaylı) |
| `GET /api/v1/quests/today` | Günün 3 önerisi (kullanıcının saat dilimine göre) |
| `POST /api/v1/quests/suggestions` | Bağlamsal öneri: `{ "availableMinutes": 120, "maxCost": "Medium" }` |
| `GET /api/v1/quests/active` · `history` · `{id}` | Aktif görevler, sayfalı geçmiş, skor dökümlü detay |
| `POST /api/v1/quests/{id}/accept` · `complete` · `skip` · `feedback` | Yaşam döngüsü ve geri bildirim |
| `GET /api/v1/progress` · `GET /api/v1/achievements` | XP, seviyeler, başarımlar |
| `GET /api/v1/onboarding/starter-cards` | Cold start kartları (onboarding'de `starterReactions` ile yanıtlanır) |
| `GET /api/v1/summaries/latest` · `POST …/{id}/read` | Okunmamış haftalık özet (yoksa 204) |
| `GET /api/v1/admin/metrics?days=7` | North-star, funnel, skip sebepleri, keşif kabulü (yalnızca `admin` rolü) |

Hatalar `{ code, message, type }` listesi olarak döner. HTTP kodları: 400 doğrulama · 401 · 404 · 409 çakışma/geçersiz geçiş · 429.

<details>
<summary><b>Konfigürasyon</b></summary>

| Ayar | Açıklama |
|---|---|
| `ConnectionStrings:LifeQuest` | PostgreSQL bağlantısı |
| `Jwt:Secret` | **En az 32 bayt.** Üretimde `Jwt__Secret` ortam değişkeni veya user-secrets ile verilir. `appsettings.Development.json` içindeki değer yalnızca geliştirme içindir. |
| `Database:MigrateOnStartup` | Yalnızca geliştirme için. Üretimde migration ayrı bir adımdır. |
| `Cache:UseRedis` | `docker compose --profile redis up -d` ile Redis |
| `BackgroundJobs:Enabled`, `Hangfire:*` | Hangfire (şu an InMemory) |
| `Quests:*`, `Recommendation:*` | Günlük öneri sayısı, aktif görev sınırı, skor ağırlıkları, keşif oranları, tekrar penceresi |
| `Narration:TimeoutMilliseconds` | Anlatım zaman aşımı (varsayılan 2000) |
| `Admin:BootstrapEmails` | Açılışta admin rolü verilecek hesaplar (geliştirmede `admin@lifequest.local`) |

Migration eklemek için:

```bash
dotnet ef migrations add <Ad> -p src/LifeQuest.Infrastructure -s src/LifeQuest.Infrastructure -o Persistence/Migrations
```

</details>

---

## 🗺️ Yol haritası

- [x] **Faz 1 · Core:** modular monolith, kimlik, onboarding, katalog, XP, başarımlar, öneri motoru, web istemcisi
- [x] Analiz riskleri: 100 template'lik güvenli katalog, efor/erişilebilirlik, cold start kartları, north-star paneli, haftalık özet, AI anlatım altyapısı, offline simülasyon
- [ ] Katalog derinliği (ilgi başına 3-4 template) · simülasyon önerilerinin A/B testi
- [ ] Refresh token'ın httpOnly çereze taşınması · Hangfire için kalıcı PostgreSQL storage
- [ ] **Faz 2 · Intelligence:** gerçek LLM adaptörü, gelişmiş Taste Graph, contextual bandit
- [ ] Push kanalı ve kullanıcı seçimli günlük hatırlatma
- [ ] **Faz 3 · Real World:** mekân/etkinlik kaynakları, hava durumu bağlamı
- [ ] **Faz 4 · Social:** Quest Party, ortak görevler, kontrollü paylaşım

<div align="center">
<br />
<sub>Gvn.GvnFramework ile geliştirildi · <a href="https://github.com/gvnuysal">@gvnuysal</a></sub>
</div>
