# LifeQuest Analiz Değerlendirmesi

*"LifeQuest – Detaylı Ürün, Teknik Mimari ve MVP Analizi" dokümanının değerlendirmesi, uygulamaya eklenen katkılar ve açık riskler.*

## 1. Genel değerlendirme

Analiz güçlü ve uygulanabilir bir temel sunuyor:

- **Doğru ürün tezi.** Farklılaştırıcı unsur görev listesi değil, kontrollü keşif üreten ve geri bildirimden öğrenen recommendation engine. Başarı ölçütü ekran süresi değil, gerçek deneyim.
- **Sağlıklı oyunlaştırma ilkeleri.** Cezalandırıcı streak yok, XP'yi LLM üretmiyor, öneriler açıklanabilir.
- **Doğru teknik kararlar.** Modular monolith, PostgreSQL, MVP'de mesaj kuyruğu ve graph DB yok, template/snapshot ayrımı, idempotent completion.
- **Privacy-by-design.** Kesin konum yok, fotoğraf doğrulaması zorunlu değil, UserId istemciden alınmıyor.

Analizde eksik kalan veya "yapılabilir" diye bırakılan noktaların bir kısmı uygulamada somutlaştırıldı. Bunlar aşağıda.

---

## 2. Uygulamaya eklenen katkılar

### Ürün ve ekonomi

| Konu | Analizde | Uygulamada |
|---|---|---|
| Kategori XP dağılımı | Yalnızca örnek (180 + 120 + 40) | Kural: birincil = Life × 2/3, ikincil = Life × 2/9. Ek B'deki örneği birebir üretir ve test edilir. |
| Seviye eğrisi | Tanımsız | `base × (L-1) × L / 2`. Life base 100, kategori base 60, üst sınır 100. |
| Novelty çarpanı | Formülde var, değeri yok | Yeni kategori 1.25, yeni template 1.10, tanıdık 1.0. Sunum anında snapshot'a yazılır. |
| XP denetlenebilirliği | Yok | Append-only **XP defteri** (`xp_transactions`). `(source_type, source_id)` benzersiz olduğu için eşzamanlı istek veya tekrar çalışan job çift XP yazamaz. |
| "Günlük" kavramı | Saat dilimi belirtilmemiş | Profilde IANA saat dilimi var. Günlük öneri ve süre bitişi kullanıcının **yerel gününe** göre hesaplanır. |
| Başarı ölçütü: "ilk 5 dakikada bir quest" | Hedef olarak var | Günlük önerinin ilk slotu mümkünse kısa (Daily) bir quest'tir. |
| Quest süre dolumu | Expired durumu var | Kabul sonrası tür bazlı süre verilir (Daily 1 gün … Epic 30 gün). Süre dolumunun **cezası yok**. En fazla 5 aktif quest ile biriktirip unutma önlenir. |
| Bağlamsal öneri ("bu akşam 2 saatim var") | Senaryo olarak var | `POST /quests/suggestions`: süre ve bütçe bağlamı. Günlük 3 tur sınırı var; eski kabul edilmemiş öneriler geri çekilir. |

### Recommendation engine

- **Sinyal ayrımı.** Skip sebepleri iki gruba ayrıldı. *"İlgimi çekmedi"* ilgi sinyalidir ve ağırlığı düşürür, template'i 14 gün bloklar. *Pahalı / zamanım yok / uzak* ise **friction** sinyalidir: ilgiyi değil, benzer maliyet veya süredeki önerilerin skorunu etkiler. Analizdeki "completed ≠ loved" ilkesi de asimetrik öğrenme katsayılarıyla uygulandı:
  - tamamlama: +0.03
  - puan 5: +0.10
  - "daha fazla": +0.08
  - "daha az": −0.10
- **Manipülasyon koruması.** Aynı geri bildirimi tekrarlayarak ilgi ağırlığı şişirilemez; sinyal yalnızca ilk kez uygulanır.
- **Taste Graph MVP'de.** Analiz bunu Faz 2'ye bırakmıştı. Tek adım komşuluk (ağırlık × güven × 0.6) ucuz olduğu için şimdiden ilgi skoruna katılıyor ve açıklamaya yansıyor: *"Sinema ilgin Yazarlık alanına kapı açtığı için…"*
- **Contextual bandit öncesi kontrollü keşif.** Chill dışındaki modlarda bir slot keşif slotudur. Kullanıcı + gün ile tohumlanmış rastgelelik sayesinde sonuç tekrarlanabilir ve test edilebilir.
- **Offline değerlendirme verisi hazır.** Her önerinin skor bileşenleri ayrı kolonlarda saklanıyor. Analizin istediği offline diversity/relevance ölçümü ve ileride bandit eğitimi için veri toplanmaya başladı.
- **Saf engine.** I/O yok, deterministik. §21'deki kahve senaryosu dahil 14 birim testle doğrulanıyor.

### Güvenlik ve mahremiyet

- Refresh token **rotasyonu ve yeniden kullanım tespiti**: çalınan token kullanılırsa tüm oturum ailesi iptal edilir. Token'lar hash'lenerek saklanır.
- 5 hatalı girişte 15 dakika kilitlenme. Kullanıcı yokken de hash doğrulaması yapılır, böylece yanıt süresinden e-posta varlığı anlaşılamaz. IP bazlı rate limit.
- **Veri minimizasyonu.** Doğum tarihi yerine yalnızca doğum yılı tutulur; 18+ kontrolü muhafazakârdır.
- **KVKK/GDPR.** Hesap silme, tüm kullanıcı verisini veritabanı cascade'i ile kalıcı olarak kaldırır. Analizde KVKK hiç anılmıyordu; Türkiye pazarı için zorunlu.
- **Loglarda PII maskeleme.** Şifre ve token tamamen, e-posta kısmen maskelenir. Correlation id temizlenerek loglara taşınır.
- **Yatay erişim koruması.** Quest'ler her zaman `(id, userId)` ile sorgulanır; başkasına ait kayıt için 404 döner (kaydın varlığı sızdırılmaz).
- **Optimistic concurrency.** PostgreSQL `xmin` sistem kolonu kullanılır, ek kolon gerekmez.
- **Güvenlik açığı olan transitive paketler.** Framework üzerinden gelen bu paketler güvenli sürümlere sabitlendi (bkz. §4).

### Kapsam kararları

- Yaşam döngüsü sadeleştirildi. `Generated` ve `InProgress` ayrı durum olarak tutulmuyor, çünkü MVP'de ayrı tetikleyicileri yok.
- Başarımlar kodda tanımlı ve test edilebilir (11 adet). Streak veya "günlük giriş" başarımı bilinçli olarak yok.
- Social (Quest Party), Notification ve AI Quest Master analizdeki fazlamaya uygun olarak dışarıda bırakıldı.

---

## 3. Analizde açık kalan riskler ve öneriler

Durum etiketleri: ✅ uygulandı · 🧭 karar (politika) · 🗺️ yol haritası. Öneri motoru üzerindeki etkiler [offline simülasyon raporunda](simulasyon-raporu.md) ölçüldü.

| # | Risk / öneri | Durum | Nasıl ele alındı |
|---|---|---|---|
| 1 | **Katalog ürünün kendisidir.** 39 template ile çeşitlilik hızla tükenir. | ✅ | Katalog **100 template**e çıktı (6 kategori × 16–17). `CatalogSafetyRules` editoryal kontrol listesi, `LifeQuest.Catalog.Tests` içinde CI kapısı olarak çalışıyor: her template ve katalog dengesi (kategori başına ≥ 12, ≥ 2 Daily, ücretsiz ≥ %35, şehirden bağımsız ≥ %45). Seeder upsert yapıyor ve template `Version`'ını artırıyor; kurala uymayan template `NeedsReview` olarak işaretleniyor ve önerilmiyor. Simülasyon, katalog derinliğinin hâlâ zamanla düşen isabetin ana sebebi olduğunu gösteriyor; ilgi başına 3-4 template hedefi yol haritasında. |
| 2 | **Tamamlama beyana dayalıdır**, XP şişirilebilir. | 🧭 | Politika: XP ve seviye **özel** kalır. Herkese açık liderlik tablosu, sosyal karşılaştırma ya da XP karşılığı ödül doğrulama mekanizması olmadan açılmaz. Kodda bu yüzeylerin hiçbiri yok. Narration guard da LLM çıktısında XP/para ifadesini reddediyor. |
| 3 | **Cold start.** | ✅ | Onboarding'de "Sana göre mi?" adımı: kategori başına 2, toplam 12 başlangıç kartı (`GET /onboarding/starter-cards`). 👍 ilgileri +0,15 artırıyor (yoksa Learned olarak ekliyor), 👎 −0,10 düşürüyor. Simülasyon: tek ilgi beyan eden kullanıcıda isabet %67 → %72. Zengin beyanda etki gürültü içinde, bu yüzden adım atlanabilir. |
| 4 | **North-star tanımı.** | ✅ | `GET /admin/metrics` (yalnızca admin rolü): anlamlı deneyim (tamamlanmış ve puansız ya da puan ≥ 4) / aktif kullanıcı / hafta, offered → accepted → completed hunisi, skip sebepleri, yeni kategori ve keşif kabul oranı. Web'de `/yonetim` ekranı var. Admin hesapları `Admin:BootstrapEmails` ayarından atanıyor. |
| 5 | **Güvenlik tek boyutlu değil.** | ✅ | Motorda iki yeni sert filtre: `outdoor_at_night` (açık hava + gece + hemen yapılacak bağlam) ve `effort_limit`. Template kuralları: açık havada Night gün dilimi yok, risk ≤ 0,3, Vigorous efor için risk etiketi zorunlu. Hava durumu entegrasyonu 🗺️ Faz 3'te. |
| 6 | **Erişilebilirlik ve kapsayıcılık.** | ✅ | `PhysicalEffort` (None…Vigorous) tüm template'lerde tanımlı ve quest detayında gösteriliyor. Kullanıcı onboarding'de veya profilde efor sınırı seçiyor. Simülasyonda hareket kısıtı olan personada kapasiteyi aşan öneri %3'ten %0'a indi. |
| 7 | **AI Quest Master guardrail'leri.** | ✅ altyapı / 🗺️ LLM | `IQuestNarrator` portu. `NarrationPromptBuilder` yalnızca yapısal ve PII'siz alanları kullanıyor; kullanıcı adı, e-posta, şehir ve serbest metin prompt'a girmiyor. `NarrationGuard` uzunluk, URL, para/XP, uydurma sayı, riskli ifade ve kişisel veri talebi kontrolü yapıyor. 2 sn zaman aşımı veya ihlalde template metnine dönülüyor ve `lifequest.narration.fallback` metriği artıyor. Kategori, süre, maliyet ve XP her zaman template'ten geliyor. Gerçek LLM adaptörü Faz 2'de. |
| 8 | **OpenIddict zamanlaması.** | 🧭 | Karar: MVP'de framework JWT ve rotasyonlu refresh token yeterli. Geçiş tetikleyicileri: harici/üçüncü taraf istemci, kurumsal SSO ya da mobil uygulamada PKCE ihtiyacı. |
| 9 | **Bildirimler.** | ✅ uygulama içi / 🗺️ push | `NotificationPreference` (Kapalı / Haftalık özet, varsayılan haftalık özet). `WeeklySummaryJob` her Pazartesi kullanıcının yerel haftasına göre idempotent özet üretiyor ve metin suçlayıcı olmayan bir tonda yazılıyor. Bugün ekranında kart olarak gösteriliyor. Karar: push kanalı ve günlük hatırlatma, kullanıcının açıkça seçmesi şartıyla sonraki fazda. |

**Simülasyonla bulunup düzeltilen sorunlar** (8 persona × 20 tohum × 30 gün, gerçek domain kodu):
- İlk sürümün keşif slotu rastgeleydi. Artık Taste Graph komşularını önceliklendiriyor ve Dengeli modda %50 olasılıkla açılıyor.
- Görmezden gelinen öneriler tekrar ediyordu. 7 günlük pencere ve gösterim başına ceza ile tekrar %45 → %33.
- Şaşırt Beni modu anlamlı deneyimi fazla düşürüyordu. Yenilik ağırlığı 0,35 → 0,25 ile keşif kaybedilmeden north-star %12 arttı.
- Sakin kullanıcı gizli ilgilerini hiç keşfetmiyordu. Sakin modda %20 olasılıkla yalnızca Taste Graph komşusu önerilir; az vakitli çalışan personada gizli ilgi keşfi %5 → %45.
- Toplamda gizli ilgi keşfi %48 → %62; isabet (%73 → %74) ve north-star (6,25 → 6,33) korundu.

---

## 4. Gvn.GvnFramework bulguları

LifeQuest geliştirilirken framework'te tespit edilen sorunlar aşağıda. Hepsi framework değiştirilmeden LifeQuest tarafında aşıldı; kalıcı çözüm framework'e aittir.

| # | Önem | Bulgu | LifeQuest'teki geçici çözüm |
|---|---|---|---|
| 1 | **Kritik** | `ValidationBehavior`, hata durumunda `(TResponse)Result.Fail(...)` cast'i yapıyor. `Result<T>` dönen her command'de doğrulama hatası `InvalidCastException` → **500** üretiyor. | `ResultValidationBehavior` aynı pipeline sırasında framework behavior'ının yerine geçiyor. |
| 2 | Yüksek | `IDomainEvent`, `INotification` değil. `GvnDbContext` ise `IMediator.Publish(object)` çağırıyor; `INotification` olmayan event'te MediatR hata fırlatıyor. | `LifeQuestDomainEvent : DomainEvent, INotification` |
| 3 | Yüksek | Domain event'ler `SaveChangesAsync` içinde, **commit'ten sonra** yayınlanıyor. Bir handler hata verirse başarıyla yazılmış istek 500 dönüyor. `OutboxMessage` tanımlı ama kullanılmıyor. | Handler'lar yalnızca yan etkisiz iş yapıyor (log). |
| 4 | Yüksek | `LoggingBehavior` ve `PerformanceBehavior`, request ve response'u `{@Request}` ile olduğu gibi logluyor: şifre, token ve e-posta loglara düşüyor. | Serilog destructuring policy ile maskeleme. |
| 5 | Yüksek | `BackgroundJobs`: `UseInMemory=false` iken hiçbir storage yapılandırılmıyor (`SqlServerConnectionString` okunmuyor), PostgreSQL desteği yok. `Hangfire.RecurringJobAdmin` 1.0.5 ise Hangfire 1.7.18 metapaketini ve **güvenlik açığı olan** `Newtonsoft.Json 11` ile `System.Data.SqlClient 4.4` sürümlerini getiriyor. | InMemory storage (job'lar idempotent). Transitive paketler `Directory.Packages.props` içinde güvenli sürümlere sabitlendi. |
| 6 | Orta | `CorrelationIdMiddleware` kimliği log context'e taşımıyor ve istemci header'ını temizlemiyor (log enjeksiyonu). `CorrelationIdEnricher` sabit id verilmezse **her log satırına farklı** id üretiyor. | `CorrelationIdLogContextMiddleware` |
| 7 | Orta | `OutboxConfiguration` içinde `nvarchar(max)` var; SQL Server'a özgü, PostgreSQL'de çalışmaz. | Outbox kullanılmıyor. |
| 8 | Orta | `Security` paketi soyutlamalar ile implementasyonları birlikte taşıyor. Application katmanı `IPasswordHasher` için ASP.NET Core bağımlılığı alıyor. Refresh token ve `JwtOptions` doğrulaması yok. | Refresh token LifeQuest'te. Secret uzunluğu açılışta doğrulanıyor. |
| 9 | Orta | `UseGvnSwagger` içeride `UseRouting` ve `UseEndpoints` çağırıyor; yanlış sırada kullanılırsa `[Authorize]` uçları hata verir (örnek projede auth, routing'den önce). | `UseRouting` açıkça auth'tan önce çağrılıyor. |
| 10 | Orta | `ApiControllerBase`, `ErrorType.Failure`'ı 500'e çeviriyor ve 403/429 için tip yok. İş kuralı hatası `Failure` ile dönülürse sunucu hatası gibi görünür. | İş hataları yalnızca Validation / NotFound / Conflict / Unauthorized. |
| 11 | Düşük | Paket adlarında yazım hatası: `EntityFramewokCore`, `DepedencyInjection`. **NuGet'e yayınlanmadan önce** düzeltilmeli; sonrası kırıcı değişiklik olur. | — |
| 12 | Düşük | `net10.0` hedeflenirken EF Core 9.0.4, `Microsoft.AspNetCore.OpenApi` 9.0.4 ve JwtBearer 9.0.4 kullanılıyor; 10.x ile hizalanmalı. | Npgsql 9.0.4 ile uyumlu tutuldu. |
| 13 | Düşük | `EfRepository` context'i dışarı açmıyor; `GetByIdAsync` child koleksiyonları yüklemiyor. `AuditableEntity.SetCreated(null)` ile audit'te kullanıcı bilgisi hiç dolmuyor. | Repository'lerde context ayrıca tutuluyor. |
| 14 | Düşük | `ModuleLoader` statik liste kullanıyor (testlerde host başına birikir). `IModule.ConfigureServices`, `IConfiguration` almıyor. | Konfigürasyon `IServiceProvider` üzerinden okunuyor. |
| 15 | Düşük | Aynı sürüm numarasıyla (`1.0.0-preview`) yeniden paketleme, NuGet önbelleği yüzünden tüketicilere ulaşmıyor. | Her pakette sürüm artırılmalı (README). |

**Öneri:** 1–5 numaralı maddeler framework'ün bir sonraki `preview` sürümünde düzeltilirse, LifeQuest'teki geçici çözümler (özellikle `ResultValidationBehavior`, log maskeleme ve paket sabitlemeleri) kaldırılabilir.

---

**Yönetim (sonradan eklendi).** Admin artık kullanıcıları askıya alabiliyor/silebiliyor, rol verebiliyor; katalogda template oluşturup düzenleyebiliyor, inceleme kuyruğunu yönetebiliyor ve öneri ağırlıklarını panelden anında değiştirebiliyor. Her işlem gerekçesiyle `admin_audit_entries` tablosuna yazılır. Admin'in düzenlediği template seed senkronundan çıkar; seed verisi artık admin kararlarını ezmez.

## 5. Sonraki adımlar (öncelik sırasıyla)

1. Katalog derinliği: ilgi başına 3-4 template, özellikle kısa, ücretsiz ve şehirden bağımsız görevler (simülasyon öneri 1).
2. Framework 1–5 düzeltmeleri ve Hangfire için kalıcı PostgreSQL storage.
3. "Sevdiğini tekrarla" muafiyeti ve mod ayarlarının (Şaşırt Beni 0,25, Sakin %20) canlı veride A/B ile doğrulanması.
4. Gerçek LLM adaptörü (`IQuestNarrator`) ve guard ihlal oranının izlenmesi.
5. Push kanalı ve kullanıcı seçimli günlük hatırlatma; hava durumu entegrasyonu.
