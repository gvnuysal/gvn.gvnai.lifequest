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

1. **Katalog ürünün kendisidir.** Engine ne kadar iyi olursa olsun 39 template ile çeşitlilik hızla tükenir; cooldown'lar devreye girince öneri havuzu daralır. Analizdeki "ilk 100 güvenli template" hedefi MVP'nin kritik yoludur. Önerilenler: editoryal üretim süreci, template başına güvenlik checklist'i ve şehir bağımsız / şehir gerektiren dengesi.
2. **Tamamlama beyana dayalıdır.** XP şişirilebilir. XP ve seviye **özel** kalmalı; herkese açık liderlik tablosu, doğrulama mekanizması olmadan açılmamalı. Aksi halde "sağlıklı oyunlaştırma" ilkesi zarar görür.
3. **Cold start.** İlk gün yalnızca onboarding ilgileri var. Kısa bir "bunlardan hangisi sana göre?" kart seçimi, ilk haftanın isabetini ciddi biçimde artırır.
4. **North-star tanımı.** "Haftalık anlamlı tamamlanmış deneyim" ölçülebilir hale getirilmeli. Öneri: tamamlanmış ve (puan ≥ 4 **veya** puansız) quest sayısı / aktif kullanıcı. Funnel metrikleri `LifeQuest` meter'ında toplanıyor.
5. **Güvenlik etiketi tek boyutlu değil.** Gece yapılan açık hava görevleri, hava koşulu ve fiziksel efor ayrı risk boyutlarıdır. Faz 3'teki hava entegrasyonundan önce, açık hava görevleri için gün dilimi kısıtı (mevcut `DayParts`) katı tutulmalı.
6. **Erişilebilirlik ve kapsayıcılık.** Analizde yok. Template'lere erişilebilirlik etiketi (hareket kısıtı, ücretsiz alternatif) eklenmesi hem etik hem büyüme açısından değerli.
7. **AI Quest Master guardrail'leri.** Kullanıcının serbest metni prompt'a girmemeli (prompt injection). LLM çıktısı yalnızca başlık/anlatım üretmeli; kategori, süre, maliyet ve XP her zaman template'ten gelmeli. Engine bu ayrıma hazır: snapshot alanları template'ten, açıklama ayrı alandan geliyor.
8. **OpenIddict zamanlaması.** MVP'de framework JWT ve rotasyonlu refresh token yeterli. Harici istemci, SSO veya üçüncü taraf entegrasyonu gündeme geldiğinde OpenIddict'e geçilmeli.
9. **Bildirimler.** Kullanıcının seçtiği sıklık ilkesi doğru ama henüz uygulanmadı. Bildirim modülü eklenirken varsayılan "kapalı / haftalık özet" olmalı.

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

## 5. Sonraki adımlar (öncelik sırasıyla)

1. Katalogu 100 template'e çıkarmak ve editoryal güvenlik checklist'ini oluşturmak.
2. Framework 1–5 düzeltmeleri ve Hangfire için kalıcı PostgreSQL storage.
3. Cold-start kart seçimi ve bildirim tercihleri (Notification modülü).
4. Offline öneri değerlendirmesi: saklanan skor dökümleriyle diversity/relevance raporu.
5. İstemci (Angular veya MAUI), ardından Faz 2 (AI Quest Master, gelişmiş Taste Graph).
