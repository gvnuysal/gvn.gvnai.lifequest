## Özet

| Soru | Cevap |
|---|---|
| Kullanıcı ilk 5 dakikada uygun bir quest buluyor mu? | **Evet.** Tüm senaryolarda kullanıcıların %100'ünde ilk günün ilk önerisi hem kısa hem ilgili. |
| Öneriler geri bildirimle isabetleniyor mu? | **Kısmen.** Öğrenme, öğrenmesiz duruma göre son 5 günde isabeti **+6 puan** artırıyor (%70'e karşı %64). Ancak mutlak isabet zamanla düşüyor (%81 → %70), çünkü en ilgili template'ler tamamlanıp cooldown'a girdikçe havuz daralıyor. |
| Analiz sonrası motor değişiklikleri işe yaradı mı? | **Evet.** İlk sürüme göre gizli ilgi keşfi **%48 → %62**, tekrar eden öneri **%45 → %33**. İsabet (%73 → %74) ve north-star (6,25 → 6,33) korunuyor. |
| Keşif ile isabet arasındaki denge kullanıcının kontrolünde mi? | **Evet.** Discovery Radius, Sakin'den Şaşırt Beni'ye isabeti %86'dan %54'e indirirken keşfi %52'den %75'e, katalog kapsamını 29'dan 43 template'e çıkarıyor. |
| Cold start kartları değer katıyor mu? | **Az beyan eden kullanıcıda evet.** Yalnızca tek ilgi beyan edildiğinde isabet **+4 puan** (%67 → %71) artıyor. Zengin beyanda fark gürültü içinde. |
| Efor sınırı erişilebilirliği koruyor mu? | **Evet.** Hareket kısıtı olan persona için kapasiteyi aşan öneri, beyanla %0, beyansız %3. |

## Bulgular

**1. Rastgele keşif pahalıydı, güdümlü keşif değil.** İlk sürümde keşif slotu her gün, ilgi skoru düşük rastgele bir quest seçiyordu ve bu öneriler nadiren kabul görüyordu. Ablasyonda slot kaldırıldığında north-star 6,33'ten 6,88'e çıkıyor ama gizli ilgi keşfi %62'den %46'ya düşüyor. Keşfin ürün tezi için gerekli olduğu, ancak **nereye** keşif yapıldığının önemli olduğu görüldü. Bu yüzden keşif slotu artık önce Taste Graph ile kullanıcının sevdiği bir ilgiye komşu olan quest'leri deniyor ve Dengeli modda günlerin yarısında, Sakin modda beşte birinde açılıyor.

**2. "Gösterildi ama seçilmedi" bir sinyaldir.** İlk sürümde önerilerin %45'i, kullanıcıya son 7 günde zaten gösterilmiş bir template'in tekrarıydı. Tekrar cezası yalnızca 3 günlük bir pencereye bakıyordu. Pencere 7 güne çıkarılıp her görmezden gelinen gösterim için ceza eklendiğinde tekrar %33'e indi. Katalog kapsamı arttı; isabet anlamlı ölçüde değişmedi.

**3. Tekrar ve yenilik cezaları isabetten ödün veriyor, ama kasıtlı olarak.** Yalnızca ilgi skoruna bakan greedy motor en yüksek north-star'ı (8,20) veriyor. Buna karşılık önerilerin üçte ikisi tekrar, gizli ilgi keşfi %43 ve 30 günde yalnızca 20 farklı template görülüyor; bu, analizin kaçınmak istediği "kahve seviyor → hep kahve" döngüsü. Aynı davranışı isteyen kullanıcı Sakin modu seçerek bunu bilinçli olarak alabiliyor (Sakin: north-star 7,59, isabet %86).

**4. Zamanla düşen isabet bir katalog derinliği sorunu.** Cezalar kapatıldığında da düşüş sürüyor (tekrar cezası yokken %88 → %78). Kullanıcı başına 30 günde yaklaşık 36 farklı template tüketiliyor. En sevilenler cooldown'a girince motor bir sonraki en iyi seçeneğe iniyor. Öğrenme bu düşüşü yavaşlatıyor ama durduramıyor.

**5. North-star'daki darboğaz öneri değil, uygulanabilirlik.** Öğrenme isabeti artırsa da north-star'da anlamlı fark yaratmıyor (öğrenmeli 6,33 ±0,14, öğrenmesiz 6,41 ±0,17). Kabul oranını belirleyen asıl etkenler süre, bütçe ve aynı anda en fazla 5 aktif görev kuralı. North-star'ı artırmanın en kısa yolu kısa, ücretsiz ve şehirden bağımsız quest'lerin payını artırmak.

**6. Mod ayarları: Şaşırt Beni fazla agresifti, Sakin fazla kapalıydı.** Tarama (herkes ilgili moda zorlanarak):
- **Şaşırt Beni:** yenilik ağırlığı 0,35'ten 0,25'e indiğinde gizli ilgi keşfi %75'te sabit kalıyor, north-star 4,17'den 4,68'e (+%12), isabet %49'dan %54'e çıkıyor. 0,20'de keşif düşmeye başlıyor (%70), yani 0,25 kırılma noktası. Mod yine de Dengeli'den belirgin biçimde daha keşifçi (43'e karşı 37 farklı template).
- **Sakin:** %10 keşif oranının etkisi gürültü içinde (gizli ilgi %44 → %46). %20'de etki belirgin: gizli ilgi %52, north-star %4 düşüyor (7,93 → 7,59). Sakin modda keşif yalnızca Taste Graph komşusu ve risksiz adaylarla yapılıyor; böyle aday yoksa slot normal öneriye dönüyor.

Persona bazında (önceki ayar → güncel):
- **Az vakitli çalışan** (Sakin): gizli ilgi keşfi **%5 → %45**, north-star 5,55 → 5,79. En büyük kazanç burada.
- **Şaşırt Beni kaşifi:** north-star 3,77 → 4,13, isabet %46 → %49. Hâlâ en düşük north-star'a sahip persona; bu mod bilinçli olarak keşfi seçenler için.
- **Kültür meraklısı** (Sakin): bedeli ödeyen persona. Beyan ettiği ilgiler zaten komşularını kapsadığı için keşiften kazancı yok; north-star 7,21 → 6,70. Sakin keşif oranı `Recommendation:ExplorationRateChill` ile ayarlanabilir veya kapatılabilir.
- **Şehir bilgisi vermeyen** kullanıcıda gizli ilgi keşfi %30'da kalıyor; şehir gerektiren quest'ler filtrelendiği için havuz dar.

> Düzeltme: bu raporun önceki sürümünde persona tablosu ve efor karşılaştırması, senaryo listesinin sırası değiştiği için yanlışlıkla ilk sürüm (V0) verisini gösteriyordu. Tablolar artık güncel motoru (A) gösteriyor.

## Kodda yapılan iyileştirmeler

| Değişiklik | Neden | Etki (V0 → A) |
|---|---|---|
| Keşif slotu Taste Graph komşularını önceliklendirir (`GuidedExploration`) | Rastgele keşif düşük kabul görüyordu | Gizli ilgi keşfi %48 → %62 (tüm değişikliklerle) |
| Dengeli modda keşif slotu olasılıksal: %50 (`ExplorationRateExplore`) | Her gün keşif north-star'dan yiyordu | North-star korundu (6,25 → 6,33) |
| Görmezden gelinen öneri cezası, 7 günlük pencere (`IgnoredOfferPenalty`, `IgnoredOfferWindowDays`) | Önerilerin neredeyse yarısı tekrardı | Tekrar %45 → %33 |
| Şaşırt Beni yeniliği 0,35 → 0,25 (`NoveltySurpriseMe`) | Mod anlamlı deneyimi fazla düşürüyordu | Şaşırt Beni north-star +%12, keşif sabit |
| Sakin modda %20 güdümlü keşif (`ExplorationRateChill`) | Sakin kullanıcı gizli ilgilerini hiç keşfetmiyordu | Az vakitli çalışanda gizli ilgi %5 → %45 |

Tüm ayarlar `appsettings.json → Recommendation` üzerinden değiştirilebilir ve birim testleriyle korunur.

## Öneriler (öncelik sırasıyla)

1. **Katalog derinliği.** Her ilgi alanında en az 3-4 template; özellikle kısa, ücretsiz ve şehirden bağımsız görevler. Zamanla düşen isabetin ve north-star darboğazının ortak çözümü bu.
2. **"Sevdiğin bir deneyimi tekrarla."** 5 puan verilmiş template'ler cooldown bittikten sonra yenilik cezasından muaf tutulmalı. Keşif tezine zarar vermeden geç dönem isabetini destekler.
3. **Cold start kartlarını hedefle.** İki ya da daha az ilgi seçen kullanıcıya kart adımı vurgulu gösterilmeli; zengin beyanda atlanabilir kalmalı.
4. **Gerçek veriyle doğrulama.** Mod ayarları (Şaşırt Beni 0,25, Sakin %20) dahil; saklanan skor dökümleri ve `/api/v1/admin/metrics` ile aynı metrikler canlı veride izlenmeli. Contextual bandit'e geçiş için gereken veri zaten toplanıyor.
