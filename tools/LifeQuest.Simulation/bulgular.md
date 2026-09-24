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
