## Özet

| Soru | Cevap |
|---|---|
| Kullanıcı ilk 5 dakikada uygun bir quest buluyor mu? | **Evet.** Tüm senaryolarda kullanıcıların %100'ünde ilk günün ilk önerisi hem kısa hem ilgili. |
| Öneriler geri bildirimle isabetleniyor mu? | **Kısmen.** Öğrenme, öğrenmesiz duruma göre son 5 günde isabeti **+5 puan** artırıyor (%72'ye karşı %67). Ancak mutlak isabet zamanla düşüyor (%81 → %72), çünkü en ilgili template'ler tamamlanıp cooldown'a girdikçe havuz daralıyor. |
| Analiz sonrası motor değişiklikleri işe yaradı mı? | **Evet.** İlk sürüme göre gizli ilgi keşfi **%48 → %61**, tekrar eden öneri **%45 → %34**, north-star **6,25 → 6,42**, geç dönem isabeti **%70 → %72**. |
| Keşif ile isabet arasındaki denge kullanıcının kontrolünde mi? | **Evet.** Discovery Radius, Sakin'den Şaşırt Beni'ye isabeti %86'dan %56'ya indirirken gizli ilgi keşfini %49'dan %70'e, katalog kapsamını 29'dan 42 template'e çıkarıyor. |
| Cold start kartları değer katıyor mu? | **Az beyan eden kullanıcıda evet.** Yalnızca tek ilgi beyan edildiğinde isabet **+4 puan** (%68 → %72), north-star 5,80 → 6,11. Zengin beyanda fark gürültü içinde. |
| Efor sınırı erişilebilirliği koruyor mu? | **Evet.** Hareket kısıtı olan persona için kapasiteyi aşan öneri, beyanla %0, beyansız %3. |

## Bulgular

**1. Rastgele keşif pahalıydı, güdümlü keşif değil.** İlk sürümde keşif slotu her gün ilgi skoru düşük, rastgele bir quest seçiyordu ve bu öneriler nadiren kabul görüyordu. Ablasyonda slot kaldırıldığında north-star 6,42'den 7,12'ye çıkıyor ama gizli ilgi keşfi %61'den %46'ya düşüyor. Yani keşif ürün tezi için gerekli; önemli olan **nereye** keşif yapıldığı. Keşif slotu artık önce Taste Graph ile kullanıcının sevdiği bir ilgiye komşu quest'leri deniyor. Dengeli modda günlerin yarısında, Sakin modda beşte birinde açılıyor.

**2. "Gösterildi ama seçilmedi" bir sinyaldir.** İlk sürümde önerilerin %45'i, kullanıcıya son 7 günde zaten gösterilmiş bir template'in tekrarıydı; tekrar cezası yalnızca 3 günlük pencereye bakıyordu. Pencere 7 güne çıkarılıp her görmezden gelinen gösterim için ceza eklenince tekrar %34'e indi.

**3. Tekrar ve yenilik cezaları isabetten ödün veriyor, ama bilerek.** Yalnızca ilgi skoruna bakan greedy motor en yüksek north-star'ı veriyor (8,20). Buna karşılık önerilerinin üçte ikisi tekrar, gizli ilgi keşfi %43 ve 30 günde yalnızca 20 farklı template görülüyor. Bu tam olarak analizin kaçınmak istediği "kahve seviyor → hep kahve" döngüsü. Bunu isteyen kullanıcı Sakin modu seçerek bilinçli olarak alabiliyor (Sakin: north-star 7,62, isabet %86).

**4. Zamanla düşen isabet bir katalog derinliği sorunu.** Cezalar kapatıldığında da düşüş sürüyor (tekrar cezası yokken %88 → %79). Kullanıcı başına 30 günde yaklaşık 35 farklı template tüketiliyor. En sevilenler cooldown'a girince motor bir sonraki en iyi seçeneğe iniyor. Öğrenme ve "sevdiğini tekrarla" bu düşüşü yavaşlatıyor ama durdurmuyor.

**5. North-star'daki darboğaz öneri değil, uygulanabilirlik.** Öğrenme isabeti artırsa da north-star'da anlamlı fark yaratmıyor (öğrenmeli 6,42 ±0,14, öğrenmesiz 6,53 ±0,17). Kabulü belirleyen asıl etkenler süre, bütçe ve aynı anda en fazla 5 aktif görev kuralı. North-star'ı artırmanın en kısa yolları: kısa, ücretsiz ve şehirden bağımsız quest'lerin payını artırmak ve önerileri "sonra yaparım" ile kaybetmemek.

**6. Mod ayarları: Şaşırt Beni fazla agresifti, Sakin fazla kapalıydı.** Tarama, herkes ilgili moda zorlanarak yapıldı.
- **Şaşırt Beni:** yenilik ağırlığı 0,35'ten 0,25'e inince north-star 4,49'dan 4,96'ya (+%10), isabet %51'den %56'ya çıkıyor. Gizli ilgi keşfi %70–74 arasında gürültü içinde kalıyor. Mod yine de Dengeli'den belirgin biçimde daha keşifçi (42'ye karşı 36 farklı template).
- **Sakin:** %10 keşif oranının etkisi gürültü içinde. %20'de gizli ilgi keşfi %43'ten %49'a çıkıyor, north-star %4 düşüyor (7,97 → 7,62). Sakin modda keşif yalnızca Taste Graph komşusu ve risksiz adaylarla yapılıyor.
- **Persona bazında (önceki ayar → güncel):**
  - **Az vakitli çalışan** (Sakin): gizli ilgi keşfi **%5 → %45**, north-star değişmiyor.
  - **Şaşırt Beni kaşifi:** north-star 4,03 → 4,29, isabet %48 → %51. Hâlâ en düşük north-star'a sahip persona; bu mod bilinçli olarak keşif isteyenler için.
  - **Kültür meraklısı** (Sakin): bedeli ödeyen persona. Beyan ettiği ilgiler zaten komşularını kapsıyor; north-star 7,26 → 6,69. Sakin keşif oranı `Recommendation:ExplorationRateChill` ile ayarlanabilir veya kapatılabilir.

**7. Sevdiğini tekrarla küçük ama bedava bir kazanç.** 5 puan verilen deneyim, cooldown bittikten sonra 0,2 yerine 0,6 yenilik skoruyla, yani bilinen kategorideki yeni bir template kadar değerli sayılarak yeniden önerilebiliyor. Kazanç: geç dönem isabeti %70 → %72, north-star 6,33 → 6,42. Gizli ilgi keşfi değişmiyor (%62'ye karşı %61). 0,8 north-star'ı 6,56'ya çıkarıyor ama sevilen tekrarı yeni template'lerin önüne koyuyor. Bu yüzden 0,6 seçildi; 0,8 ilk A/B deneyi için iyi bir aday. 30 günlük simülasyon ufku, cooldown'lar (7–30 gün) nedeniyle bu etkiyi olduğundan küçük gösteriyor olabilir.

## Kodda yapılan iyileştirmeler

| Değişiklik | Neden | Etki |
|---|---|---|
| Keşif slotu Taste Graph komşularını önceliklendirir (`GuidedExploration`) | Rastgele keşif düşük kabul görüyordu | Gizli ilgi keşfi %48 → %61 (tüm değişikliklerle) |
| Dengeli modda keşif slotu olasılıksal, %50 (`ExplorationRateExplore`) | Her gün keşif, north-star'ı düşürüyordu | North-star korundu |
| Görmezden gelinen öneri cezası, 7 günlük pencere (`IgnoredOfferPenalty`, `IgnoredOfferWindowDays`) | Önerilerin neredeyse yarısı tekrardı | Tekrar %45 → %34 |
| Şaşırt Beni yeniliği 0,35 → 0,25 (`NoveltySurpriseMe`) | Mod anlamlı deneyimi fazla düşürüyordu | Şaşırt Beni north-star +%10 |
| Sakin modda %20 güdümlü keşif (`ExplorationRateChill`) | Sakin kullanıcı gizli ilgilerini hiç keşfetmiyordu | Az vakitli çalışanda gizli ilgi %5 → %45 |
| Sevdiğini tekrarla (`LovedRepeatNovelty` = 0,6) | Çok sevilen deneyimler cooldown sonrası da cezalanıyordu | Geç dönem isabeti %70 → %72, north-star +%1,4 |

Tüm ayarlar yönetim panelindeki **Öneri ayarları** ekranından veya `appsettings.json → Recommendation` üzerinden değiştirilebilir. Etkileri panelin **Deneyler** sekmesinde A/B testiyle ölçülebilir.

## Öneriler (öncelik sırasıyla)

1. **Katalog derinliği.** Her ilgi alanında en az 3-4 template olmalı; özellikle kısa, ücretsiz ve şehirden bağımsız görevler. Topluluk fikirleri bu içeriğin ucuz kaynağı.
2. **İlk A/B deneyleri.** Sırasıyla: sevdiğini tekrarla 0,6'ya karşı 0,8, Sakin keşif oranı %20'ye karşı %10, Şaşırt Beni yeniliği 0,25'e karşı 0,30. Birincil metrik north-star, koruma metriği "ilgimi çekmedi" oranı.
3. **Cold start kartlarını hedefle.** İki ya da daha az ilgi seçen kullanıcıya kart adımı vurgulu gösterilmeli; zengin beyanda atlanabilir kalmalı.
4. **Gerçek veriyle doğrulama.** Saklanan skor dökümleri ve deney sonuçları contextual bandit'e geçiş için gereken veriyi zaten topluyor.
