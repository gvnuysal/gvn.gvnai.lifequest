# LifeQuest Öneri Motoru — Offline Simülasyon Raporu

> Bu rapor `tools/LifeQuest.Simulation` tarafından üretilir. 100 template'lik gerçek katalog, 8 persona × 20 tohum = **160 sentetik kullanıcı**, **30 gün**. Motor, domain aggregate'leri, ödül hesabı ve öğrenme kuralları üretimdeki kodun kendisidir; yalnızca kullanıcı davranışı modellenir. Sonuçlar deterministiktir: aynı komut aynı sayıları üretir.

```bash
dotnet run --project tools/LifeQuest.Simulation -- --docs docs
```

## Özet

| Soru | Cevap |
|---|---|
| Kullanıcı ilk 5 dakikada uygun bir quest buluyor mu? | **Evet.** Tüm senaryolarda kullanıcıların %100'ünde ilk günün ilk önerisi hem kısa hem ilgili. |
| Öneriler geri bildirimle isabetleniyor mu? | **Kısmen.** Öğrenme, öğrenmesiz duruma göre son 5 günde isabeti **+7 puan** artırıyor (%71 → %64). Ancak mutlak isabet zamanla düşüyor (%82 → %71), çünkü en ilgili template'ler tamamlanıp cooldown'a girdikçe havuz daralıyor. |
| Analiz sonrası motor değişiklikleri işe yaradı mı? | **Evet.** İlk sürüme göre gizli ilgi keşfi **%48 → %60**, tekrar eden öneri **%45 → %34**. İsabet (%73 → %74) ve north-star (6,25 → 6,32) korunuyor. |
| Keşif ile isabet arasındaki denge kullanıcının kontrolünde mi? | **Evet.** Discovery Radius, Sakin'den Şaşırt Beni'ye isabeti %89'dan %49'a indirirken keşfi %44'ten %75'e, katalog kapsamını 26'dan 46 template'e çıkarıyor. |
| Cold start kartları değer katıyor mu? | **Az beyan eden kullanıcıda evet.** Yalnızca tek ilgi beyan edildiğinde isabet **+5 puan** (%67 → %72) artıyor. Zengin beyanda fark gürültü içinde. |
| Efor sınırı erişilebilirliği koruyor mu? | **Evet.** Hareket kısıtı olan persona için kapasiteyi aşan öneri, beyanla %0, beyansız %3. |

## Bulgular

**1. Rastgele keşif pahalıydı, güdümlü keşif değil.** İlk sürümde keşif slotu her gün, ilgi skoru düşük rastgele bir quest seçiyordu ve bu öneriler nadiren kabul görüyordu. Ablasyonda slot kaldırıldığında north-star 6,32'den 6,76'ya çıkıyor ama gizli ilgi keşfi %60'tan %46'ya düşüyor. Keşfin ürün tezi için gerekli olduğu, ancak **nereye** keşif yapıldığının önemli olduğu görüldü. Bu yüzden keşif slotu artık önce Taste Graph ile kullanıcının sevdiği bir ilgiye komşu olan quest'leri deniyor ve Dengeli modda günlerin yarısında açılıyor.

**2. "Gösterildi ama seçilmedi" bir sinyaldir.** İlk sürümde önerilerin %45'i, kullanıcıya son 7 günde zaten gösterilmiş bir template'in tekrarıydı. Tekrar cezası yalnızca 3 günlük bir pencereye bakıyordu. Pencere 7 güne çıkarılıp her görmezden gelinen gösterim için ceza eklendiğinde tekrar %34'e indi. Katalog kapsamı arttı; isabet anlamlı ölçüde değişmedi.

**3. Tekrar ve yenilik cezaları isabetten ödün veriyor, ama kasıtlı olarak.** Yalnızca ilgi skoruna bakan greedy motor en yüksek north-star'ı (8,20) veriyor. Buna karşılık önerilerin üçte ikisi tekrar, gizli ilgi keşfi %43 ve 30 günde yalnızca 20 farklı template görülüyor; bu, analizin kaçınmak istediği "kahve seviyor → hep kahve" döngüsü. Aynı davranışı isteyen kullanıcı Sakin modu seçerek bunu bilinçli olarak alabiliyor (Sakin: north-star 7,93, isabet %89).

**4. Zamanla düşen isabet bir katalog derinliği sorunu.** Cezalar kapatıldığında da düşüş sürüyor (tekrar cezası yokken %88 → %77). Kullanıcı başına 30 günde yaklaşık 35 farklı template tüketiliyor. En sevilenler cooldown'a girince motor bir sonraki en iyi seçeneğe iniyor. Öğrenme bu düşüşü yavaşlatıyor ama durduramıyor.

**5. North-star'daki darboğaz öneri değil, uygulanabilirlik.** Öğrenme isabeti artırsa da north-star'da anlamlı fark yaratmıyor (öğrenmeli 6,32 ±0,15, öğrenmesiz 6,41 ±0,17). Kabul oranını belirleyen asıl etkenler süre, bütçe ve aynı anda en fazla 5 aktif görev kuralı. North-star'ı artırmanın en kısa yolu kısa, ücretsiz ve şehirden bağımsız quest'lerin payını artırmak.

**6. Persona bazında dikkat çeken noktalar.**
- **Şaşırt Beni kaşifi** en düşük north-star'a (3,78) sahip, geç dönem isabeti %43 ve gizli ilgisini hiç keşfetmiyor (%0). Bu mod şu ayarla fazla agresif.
- **Az vakitli çalışan** Sakin modda %100 isabet alıyor ama gizli ilgisini (meditasyon) hiç keşfetmiyor (%0). Sakin modda keşif tamamen kapalı.
- **Şehir bilgisi vermeyen** kullanıcıda ve sosyal kelebekte gizli ilgi keşfi %20. Şehir gerektiren quest'ler filtrelendiği için havuz daralıyor.

## Kodda yapılan iyileştirmeler

| Değişiklik | Neden | Etki (V0 → A) |
|---|---|---|
| Keşif slotu Taste Graph komşularını önceliklendirir (`GuidedExploration`) | Rastgele keşif düşük kabul görüyordu | Gizli ilgi keşfi %48 → %60 |
| Dengeli modda keşif slotu olasılıksal: %50 (`ExplorationRateExplore`) | Her gün keşif north-star'dan yiyordu | North-star korundu (6,25 → 6,32) |
| Görmezden gelinen öneri cezası, 7 günlük pencere (`IgnoredOfferPenalty`, `IgnoredOfferWindowDays`) | Önerilerin neredeyse yarısı tekrardı | Tekrar %45 → %34 |

Tüm ayarlar `appsettings.json → Recommendation` üzerinden değiştirilebilir ve birim testleriyle korunur.

## Öneriler (öncelik sırasıyla)

1. **Katalog derinliği.** Her ilgi alanında en az 3-4 template; özellikle kısa, ücretsiz ve şehirden bağımsız görevler. Zamanla düşen isabetin ve north-star darboğazının ortak çözümü bu.
2. **"Sevdiğin bir deneyimi tekrarla."** 5 puan verilmiş template'ler cooldown bittikten sonra yenilik cezasından muaf tutulmalı. Keşif tezine zarar vermeden geç dönem isabetini destekler.
3. **Şaşırt Beni ayarı.** Yenilik ağırlığı 0,35'ten 0,28'e indirilerek A/B testi yapılmalı. Mod en fazla keşfi veriyor ama anlamlı deneyimi Dengeli moda göre üçte bir azaltıyor (6,25 → 4,17).
4. **Sakin modda hafif keşif.** %10 olasılıkla, yalnızca Taste Graph komşusu olan ve risksiz keşif önerileri. Sakin kullanıcının da gizli ilgilerine kapı açar.
5. **Cold start kartlarını hedefle.** İki ya da daha az ilgi seçen kullanıcıya kart adımı vurgulu gösterilmeli; zengin beyanda atlanabilir kalmalı.
6. **Gerçek veriyle doğrulama.** Saklanan skor dökümleri ve `/api/v1/admin/metrics` ile aynı metrikler canlı veride izlenmeli. Contextual bandit'e geçiş için gereken veri zaten toplanıyor.

## Sonuçlar

![Önerilerin isabeti zaman içinde](images/sim-learning-curve.svg)

### Ana senaryolar

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **V0** İlk sürüm (analiz öncesi) | %73 ±1 | %78 → %70 | %41 | 6.25 ±0.14 | 4.1 | 4.8 | 34 | %45 | %48 ±3 | %100 |
| **A** Tam motor + cold start kartları | %74 ±1 | %82 → %71 | %41 | 6.32 ±0.15 | 4.1 | 4.8 | 35 | %34 | %60 ±3 | %100 |
| **B** Tam motor, kartsız | %72 ±1 | %83 → %68 | %41 | 6.28 ±0.15 | 4.1 | 4.7 | 36 | %34 | %63 ±3 | %100 |
| **C** Öğrenme kapalı | %69 ±1 | %84 → %64 | %40 | 6.41 ±0.17 | 4.1 | 4.9 | 35 | %34 | %57 ±3 | %100 |
| **D** Yalnızca ilgi skoru (greedy) | %94 ±1 | %98 → %94 | %51 | 8.20 ±0.16 | 4.2 | 4.3 | 20 | %66 | %43 ±3 | %100 |

### Ablasyon: tam motordan tek bileşen çıkarıldığında

![Her bileşen neyi satın alıyor?](images/sim-ablation.svg)

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **A** Tam motor + cold start kartları | %74 ±1 | %82 → %71 | %41 | 6.32 ±0.15 | 4.1 | 4.8 | 35 | %34 | %60 ±3 | %100 |
| **X-Rep** A − tekrar cezası | %81 ±1 | %88 → %77 | %46 | 7.18 ±0.15 | 4.1 | 4.4 | 28 | %57 | %66 ±3 | %100 |
| **X-Nov** A − yenilik | %80 ±1 | %85 → %77 | %45 | 7.01 ±0.14 | 4.1 | 4.5 | 31 | %41 | %61 ±3 | %100 |
| **X-Div** A − çeşitlilik | %75 ±1 | %83 → %71 | %42 | 6.46 ±0.15 | 4.1 | 4.7 | 34 | %35 | %68 ±3 | %100 |
| **X-Exp** A − keşif slotu | %79 ±1 | %90 → %76 | %44 | 6.76 ±0.16 | 4.1 | 4.7 | 32 | %37 | %46 ±3 | %100 |
| **X-Ign3** A, eski tekrar penceresi (3 gün) | %76 ±1 | %82 → %72 | %42 | 6.46 ±0.15 | 4.1 | 4.7 | 33 | %48 | %62 ±3 | %100 |

### Cold start: kullanıcı onboarding'de yalnızca tek ilgi beyan ederse

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **S-A** Tek ilgi beyanı + kartlar | %72 ±1 | %79 → %70 | %39 | 5.90 ±0.16 | 4.1 | 4.7 | 36 | %33 | %61 ±3 | %100 |
| **S-B** Tek ilgi beyanı, kartsız | %67 ±1 | %77 → %67 | %37 | 5.65 ±0.17 | 4.1 | 4.7 | 37 | %32 | %67 ±3 | %100 |

### Discovery Radius

![Keşif modu bir tercih](images/sim-radius.svg)

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **R-Chill** Herkes Sakin | %89 ±1 | %95 → %88 | %50 | 7.93 ±0.16 | 4.2 | 4.5 | 26 | %50 | %44 ±3 | %100 |
| **R-Explore** Herkes Dengeli | %72 ±1 | %82 → %68 | %41 | 6.25 ±0.14 | 4.1 | 5.0 | 37 | %29 | %64 ±3 | %100 |
| **R-Surprise** Herkes Şaşırt Beni | %49 ±1 | %65 → %43 | %29 | 4.17 ±0.12 | 4.0 | 5.1 | 46 | %17 | %75 ±3 | %100 |

### Erişilebilirlik: hareket kısıtı olan persona

| Senaryo | Kapasitesini aşan öneri | İsabet | North-star / hafta |
|---|---|---|---|
| Efor sınırı beyan edildi | %0 | %72 | 5.71 |
| Efor sınırı beyan edilmedi | %3 | %69 | 5.43 |

### Persona kırılımı (senaryo A)

| Persona | Keşif modu | İsabet | İlk 5 gün | Son 5 gün | North-star / hafta | Tamamlanan kategori | Gizli ilgi keşfi |
|---|---|---|---|---|---|---|---|
| Kahve ve kafe tutkunu | Explore | %62 | %68 | %64 | 4.45 | 4.4 | %78 |
| Kültür meraklısı | Chill | %88 | %92 | %83 | 7.35 | 4.7 | %88 |
| Düşük bütçeli öğrenci | Explore | %72 | %78 | %69 | 6.53 | 4.4 | %63 |
| Az vakitli çalışan | Chill | %100 | %100 | %100 | 5.93 | 4.5 | %0 |
| Hareket kısıtı olan kullanıcı | Explore | %72 | %81 | %67 | 5.71 | 4.0 | %95 |
| Sosyal kelebek | Explore | %69 | %80 | %64 | 8.66 | 5.9 | %20 |
| Şehir bilgisi vermeyen doğa sever | Explore | %70 | %73 | %70 | 7.62 | 4.9 | %20 |
| Şaşırt Beni kaşifi | SurpriseMe | %50 | %55 | %43 | 3.78 | 6.0 | %0 |

## Yöntem

**Personalar.** Her personanın gizli bir gerçek ilgi haritası vardır (listede olmayan ilgiler 0,15). Onboarding'de bunun yalnızca bir kısmını beyan eder; "gizli" ilgiler kullanıcının sevdiği ama söylemediği alanlardır. Sistem bunları keşfederse (öğrenilmiş ağırlık ≥ 0,4 ya da o etiketle bir quest tamamlanırsa) "gizli ilgi keşfi" sayılır.

| Persona | Beyan edilen | Gizli | Bütçe | Haftalık süre | Şehir | Fiziksel kapasite |
|---|---|---|---|---|---|---|
| Kahve ve kafe tutkunu | coffee, cafe-culture, street-food | architecture, photography | Medium | 300 dk | var | Vigorous |
| Kültür meraklısı | cinema, museums, art | writing, architecture | Medium | 300 dk | var | Vigorous |
| Düşük bütçeli öğrenci | reading, languages, friends | science, astronomy | Free | 600 dk | var | Vigorous |
| Az vakitli çalışan | podcasts, yoga, cooking | meditation | Low | 120 dk | var | Vigorous |
| Hareket kısıtı olan kullanıcı | crafts, drawing, art | writing, music-making | Low | 300 dk | var | Light |
| Sosyal kelebek | friends, board-games, live-music | dance, volunteering | Medium | 600 dk | var | Vigorous |
| Şehir bilgisi vermeyen doğa sever | walking, photography, nature | gardening, astronomy | Free | 300 dk | yok | Vigorous |
| Şaşırt Beni kaşifi | photography, cooking | cycling, dance | Medium | 600 dk | var | Vigorous |

**Davranış modeli.** Her gün motor 3 öneri üretir (yerel saat 18:30). Kabul olasılığı `0,85 × ilgi^1,5`; bütçe konforunu aşarsa ×0,2, oturum süresini aşarsa ×0,35, fiziksel kapasiteyi aşarsa 0. Aynı anda en fazla 5 aktif görev. Kabul edilen görev `0,55 + 0,4 × ilgi` olasılıkla tamamlanır (günlük aynı gün, haftalık 1-4 gün, macera 3-10 gün). Tamamlananların %70'i puanlanır: `1 + 4 × (ilgi ± gürültü)`; 5 puanın bir kısmı "daha fazla", 1-2 puanın bir kısmı "daha az" tercihi taşır. Kabul edilmeyen önerilerin %60'ı sebep belirtilerek geçilir (ilgi < 0,35 → ilgimi çekmedi, sonra pahalı / zamanım yok / bugün değil). Öğrenme adımları API handler'larıyla aynı `InterestLearning` sabitlerini kullanır.

**Metrikler.**

- **İsabet:** gerçek ilgisi ≥ 0,5 olan öneri payı (± kullanıcılar arası standart hata).
- **North-star:** haftalık anlamlı deneyim; tamamlanmış ve puansız ya da ≥ 4 puanlı quest sayısı.
- **Tekrar:** son 7 günde aynı kullanıcıya zaten gösterilmiş template'in payı.
- **Çeşitlilik:** günlük 3'lü listedeki farklı kategori sayısı.
- **Katalog kapsamı:** 30 günde gösterilen farklı template sayısı.
- **Gün 1:** ilk günün ilk önerisi hem kısa hem ilgili mi ("ilk 5 dakikada uygun quest" başarı ölçütü).

## Sınırlar

- Davranış modeli bir varsayımdır; mutlak değerler değil, **senaryolar arası farklar** yorumlanmalıdır (ortak rastgele sayılar kullanıldığı için bu farklar gürültüye karşı dayanıklıdır).
- "Gerçek ilgi" statiktir; gerçek kullanıcıların zevki deneyimle değişir. Ayrıca hava, mekân açıklığı ve sosyal etki modellenmedi.
- Gerçek kullanıcı verisi geldiğinde aynı metrikler `/api/v1/admin/metrics` ve saklanan skor dökümleriyle doğrulanmalı, ağırlıklar A/B testiyle ayarlanmalıdır.
