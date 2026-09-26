## Özet

| Soru | Cevap |
|---|---|
| Kullanıcı ilk 5 dakikada uygun bir quest buluyor mu? | **Evet.** Tüm senaryolarda kullanıcıların %100'ünde ilk günün ilk önerisi hem kısa hem ilgili. |
| Öneriler geri bildirimle isabetleniyor mu? | **Kısmen.** Öğrenme, öğrenmesiz duruma göre son 5 günde isabeti **+3 puan** artırıyor (%73'e karşı %70). Mutlak isabet zamanla düşüyor (%83 → %73), çünkü en ilgili template'ler tamamlanıp cooldown'a girdikçe havuz daralıyor. |
| Katalog derinliği işe yaradı mı? | **Evet, en büyük tek iyileştirme bu.** 100'den 142 template'e (her ilgide ≥ 4 görev, ≥ 1 kısa görev) çıkınca north-star **6,42 → 6,99** (+%9), gizli ilgi keşfi **%61 → %70**, tekrar eden öneri **%34 → %28**. |
| Analiz sonrası motor değişiklikleri işe yaradı mı? | **Evet.** Aynı katalogla ilk sürüme göre gizli ilgi keşfi **%66 → %70**, tekrar **%40 → %28**, north-star **6,59 → 6,99**. |
| Keşif ile isabet arasındaki denge kullanıcının kontrolünde mi? | **Evet.** Discovery Radius, Sakin'den Şaşırt Beni'ye isabeti %87'den %59'a indirirken gizli ilgi keşfini %60'tan %82'ye, katalog kapsamını 32'den 47 template'e çıkarıyor. |
| Cold start kartları değer katıyor mu? | **Az beyan eden kullanıcıda isabet için evet.** Yalnızca tek ilgi beyan edildiğinde isabet %72 → %75. North-star farkı gürültü içinde. |
| Efor sınırı erişilebilirliği koruyor mu? | **Evet.** Hareket kısıtı olan persona için kapasiteyi aşan öneri, beyanla %0, beyansız %5. |

## Bulgular

**1. Katalog derinliği, motor ayarlarından daha çok şey kazandırdı.** 13 ilgi alanında 3'ten az görev vardı (bahçecilikte hiç yoktu); 6 ilgi alanında da kısa görev yoktu. 42 yeni görevle her ilgi alanı en az 4 göreve ve en az 1 kısa göreve ulaştı.
- **Tam motor (A), 100 → 142 template:**
  - north-star 6,42 → 6,99
  - gizli ilgi keşfi %61 → %70
  - tekrar %34 → %28
  - 30 günde görülen farklı template 35 → 39
- **Persona bazında gizli ilgi keşfi** (en çok gizli ilgisi daha önce zayıf ilgi alanlarında olan personalar kazandı):
  - az vakitli çalışan (meditasyon) %45 → %100
  - sosyal kelebek (dans, gönüllülük) %48 → %80
  - şehir bilgisi vermeyen (bahçecilik) %25 → %55
  - Şaşırt Beni kaşifi (dans, bisiklet) %20 → %30
- **Düşenler:** Kültür meraklısında %80 → %68 ve öğrencide %83 → %73. Persona başına 20 kullanıcı olduğundan bu farklar gürültü sınırında. Büyüyen katalogda keşif slotunun daha çok alana dağılmasının da payı olabilir.
- **Kapı:** Kural artık `CatalogSafetyRules.ValidateInterestCoverage` ile CI'da. Yönetim panelindeki katalog sağlığı kartı da zayıf ilgi alanlarını gösteriyor.

**2. Rastgele keşif pahalıydı, güdümlü keşif değil.** Ablasyonda keşif slotu kaldırılınca north-star 6,99'dan 7,69'a çıkıyor ama gizli ilgi keşfi %70'ten %56'ya düşüyor. Keşif ürün tezi için gerekli; önemli olan **nereye** keşif yapıldığı. Keşif slotu önce Taste Graph ile kullanıcının sevdiği bir ilgiye komşu quest'leri deniyor. Dengeli modda günlerin yarısında, Sakin modda beşte birinde açılıyor.

**3. "Gösterildi ama seçilmedi" bir sinyaldir.** Eski 3 günlük tekrar penceresiyle önerilerin %43'ü son 7 günde gösterilmiş bir template'in tekrarı oluyor. 7 günlük pencere ve gösterim başına ceza ile %28.

**4. Tekrar ve yenilik cezaları isabetten ödün veriyor, ama bilerek.** Yalnızca ilgi skoruna bakan greedy motor en yüksek north-star'ı veriyor (8,58). Buna karşılık önerilerinin %62'si tekrar, gizli ilgi keşfi %44 ve 30 günde yalnızca 21 farklı template görülüyor. Bu tam olarak analizin kaçınmak istediği "kahve seviyor → hep kahve" döngüsü. Bunu isteyen kullanıcı Sakin modu seçebiliyor (north-star 8,12, isabet %87).

**5. Zamanla düşen isabet sürüyor.** Derin katalog düşüşü yavaşlattı (son 5 gün %72 → %73) ama durdurmadı. Tekrar cezası kapatıldığında da düşüş devam ediyor (%89 → %78). En sevilenler cooldown'a girince motor bir sonraki en iyi seçeneğe iniyor. Kalıcı çözüm: katalog büyümeye devam etmeli (topluluk fikirleri) ve "sevdiğini tekrarla" canlı veride ayarlanmalı.

**6. North-star'daki darboğaz öneri değil, uygulanabilirlik.** Öğrenme isabeti artırsa da north-star'ı artırmıyor (öğrenmesiz 7,40 ±0,18, öğrenmeli 6,99 ±0,14). Kabulü belirleyen asıl etkenler süre, bütçe ve aynı anda en fazla 5 aktif görev kuralı. Kısa ve ücretsiz görevlerin çoğalması tam da bu yüzden north-star'ı artırdı.

**7. Mod ayarları.** Tarama, herkes ilgili moda zorlanarak yapıldı.
- **Şaşırt Beni:** Yenilik ağırlığı 0,35'ten 0,25'e inince north-star 4,96'dan 5,48'e (+%10), isabet %54'ten %59'a çıkıyor. Gizli ilgi keşfi %80–82 arasında sabit. 0,20'de de keşif korunuyor; bir sonraki A/B adayı.
- **Sakin:** Derin katalogla Sakin moddaki keşfin kazancı küçüldü.
  - %0 → %20 keşif oranı: gizli ilgi keşfi %54 → %60, north-star %5 düşüyor (8,51 → 8,12).
  - Önceki katalogda az vakitli çalışanın tek umudu bu keşif slotuydu. Artık meditasyon görevleri beyan ettiği ilgilere (yoga) komşu olduğu için slot olmadan da %95 keşfediyor.
  - %20'yi %10'a indirmek canlı veride A/B ile denenmeli.

**8. Sevdiğini tekrarla küçük ama bedava bir kazanç.** 5 puan verilen deneyim cooldown'dan sonra 0,2 yerine 0,6 yenilik skoruyla yeniden önerilebiliyor. Kazanç: north-star 6,85 → 6,99, geç dönem isabeti %71 → %73; keşif değişmiyor. 0,8 north-star'ı 7,12'ye çıkarıyor, ilk A/B deneyi için aday.

## Kodda yapılan iyileştirmeler

| Değişiklik | Neden | Etki |
|---|---|---|
| Katalog 100 → 142 template; ilgi kapsama kuralı (≥ 4 görev, ≥ 1 kısa) | 13 ilgi alanı neredeyse boştu | North-star +%9, gizli ilgi keşfi %61 → %70, tekrar %34 → %28 |
| Keşif slotu Taste Graph komşularını önceliklendirir (`GuidedExploration`) | Rastgele keşif düşük kabul görüyordu | Gizli ilgi keşfi (aynı katalogla ilk sürüme göre) %66 → %70 |
| Dengeli modda keşif slotu olasılıksal, %50 (`ExplorationRateExplore`) | Her gün keşif, north-star'ı düşürüyordu | North-star korundu |
| Görmezden gelinen öneri cezası, 7 günlük pencere | Önerilerin neredeyse yarısı tekrardı | Tekrar %43 → %28 |
| Şaşırt Beni yeniliği 0,35 → 0,25 | Mod anlamlı deneyimi fazla düşürüyordu | Şaşırt Beni north-star +%10 |
| Sakin modda %20 güdümlü keşif | Sakin kullanıcı gizli ilgilerini keşfedemiyordu | Gizli ilgi keşfi %54 → %60 (derin katalogla kazanç küçüldü) |
| Sevdiğini tekrarla (`LovedRepeatNovelty` = 0,6) | Çok sevilen deneyimler cooldown sonrası da cezalanıyordu | North-star +%2 |

Motor ayarları yönetim panelindeki **Öneri ayarları** ekranından değiştirilebilir. Etkileri **Deneyler** sekmesinde A/B testiyle ölçülebilir.

## Öneriler (öncelik sırasıyla)

1. **Katalog büyümeye devam etmeli.** En büyük kaldıraç içerik. Topluluk fikirleri kuyruğu ve katalog sağlığı kartındaki uyarılar yeni görevlerin nereye ekleneceğini gösterir. Sonraki hedef: ilgi başına 6 görev.
2. **İlk A/B deneyleri.** Sırasıyla:
   - sevdiğini tekrarla 0,6'ya karşı 0,8
   - Sakin keşif oranı %20'ye karşı %10
   - Şaşırt Beni yeniliği 0,25'e karşı 0,20

   Birincil metrik north-star, koruma metriği "ilgimi çekmedi" oranı.
3. **Cold start kartlarını hedefle.** İki ya da daha az ilgi seçen kullanıcıya kart adımı vurgulu gösterilmeli.
4. **Gerçek veriyle doğrulama.** Saklanan skor dökümleri ve deney sonuçları contextual bandit'e geçiş için gereken veriyi topluyor.
