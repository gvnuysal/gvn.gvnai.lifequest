# LifeQuest Öneri Motoru — Offline Simülasyon Raporu

> Bu rapor `tools/LifeQuest.Simulation` tarafından üretilir. 142 template'lik gerçek katalog, 8 persona × 20 tohum = **160 sentetik kullanıcı**, **30 gün**. Motor, domain aggregate'leri, ödül hesabı ve öğrenme kuralları üretimdeki kodun kendisidir; yalnızca kullanıcı davranışı modellenir. Sonuçlar deterministiktir: aynı komut aynı sayıları üretir.

```bash
dotnet run --project tools/LifeQuest.Simulation -- --docs docs
```

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

## Sonuçlar

![Önerilerin isabeti zaman içinde](images/sim-learning-curve.svg)

### Ana senaryolar

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **V0** İlk sürüm (analiz öncesi) | %75 ±1 | %80 → %72 | %42 | 6.59 ±0.12 | 4.1 | 4.8 | 38 | %40 | %66 ±3 | %100 |
| **A** Tam motor + cold start kartları | %76 ±1 | %83 → %73 | %44 | 6.99 ±0.14 | 4.2 | 4.9 | 39 | %28 | %70 ±2 | %100 |
| **B** Tam motor, kartsız | %75 ±1 | %85 → %71 | %44 | 7.07 ±0.14 | 4.2 | 4.7 | 39 | %30 | %67 ±3 | %100 |
| **C** Öğrenme kapalı | %76 ±1 | %86 → %70 | %45 | 7.40 ±0.18 | 4.2 | 4.7 | 36 | %33 | %56 ±3 | %100 |
| **D** Yalnızca ilgi skoru (greedy) | %95 ±0 | %99 → %95 | %52 | 8.58 ±0.15 | 4.2 | 4.0 | 21 | %62 | %44 ±3 | %100 |

### Ablasyon: tam motordan tek bileşen çıkarıldığında

![Her bileşen neyi satın alıyor?](images/sim-ablation.svg)

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **A** Tam motor + cold start kartları | %76 ±1 | %83 → %73 | %44 | 6.99 ±0.14 | 4.2 | 4.9 | 39 | %28 | %70 ±2 | %100 |
| **X-Rep** A − tekrar cezası | %83 ±1 | %89 → %78 | %48 | 7.87 ±0.13 | 4.2 | 4.2 | 30 | %53 | %75 ±3 | %100 |
| **X-Nov** A − yenilik | %81 ±1 | %86 → %78 | %47 | 7.56 ±0.14 | 4.2 | 4.4 | 35 | %32 | %74 ±2 | %100 |
| **X-Div** A − çeşitlilik | %78 ±1 | %84 → %74 | %45 | 7.20 ±0.15 | 4.2 | 4.7 | 38 | %28 | %73 ±3 | %100 |
| **X-Exp** A − keşif slotu | %85 ±1 | %92 → %83 | %48 | 7.69 ±0.15 | 4.2 | 4.8 | 33 | %34 | %56 ±3 | %100 |
| **X-Ign3** A, eski tekrar penceresi (3 gün) | %78 ±1 | %83 → %75 | %45 | 7.21 ±0.14 | 4.2 | 4.8 | 36 | %43 | %71 ±3 | %100 |

### Cold start: kullanıcı onboarding'de yalnızca tek ilgi beyan ederse

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **S-A** Tek ilgi beyanı + kartlar | %75 ±1 | %81 → %73 | %43 | 6.69 ±0.15 | 4.2 | 4.9 | 40 | %28 | %72 ±3 | %100 |
| **S-B** Tek ilgi beyanı, kartsız | %72 ±1 | %83 → %70 | %42 | 6.72 ±0.16 | 4.2 | 4.7 | 39 | %30 | %71 ±3 | %100 |

### Discovery Radius

![Keşif modu bir tercih](images/sim-radius.svg)

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **R-Chill** Herkes Sakin | %87 ±0 | %93 → %85 | %50 | 8.12 ±0.14 | 4.2 | 4.6 | 31 | %40 | %60 ±3 | %100 |
| **R-Explore** Herkes Dengeli | %76 ±1 | %84 → %74 | %44 | 6.96 ±0.13 | 4.2 | 5.0 | 40 | %25 | %69 ±3 | %100 |
| **R-Surprise** Herkes Şaşırt Beni | %59 ±1 | %69 → %53 | %35 | 5.48 ±0.12 | 4.1 | 5.2 | 47 | %17 | %82 ±2 | %100 |

### Mod ayarları: Şaşırt Beni yeniliği ve Sakin keşif oranı

![Mod ayarı taraması](images/sim-tuning.svg)

Herkes ilgili moda zorlanarak çalıştırıldı. Üretim değerleri: Şaşırt Beni yeniliği **0.25**, Sakin keşif oranı **0.2**.

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **SN0.35** Şaşırt Beni, yenilik 0.35 | %54 ±1 | %68 → %50 | %33 | 4.96 ±0.13 | 4.1 | 5.3 | 50 | %15 | %80 ±2 | %100 |
| **SN0.30** Şaşırt Beni, yenilik 0.30 | %56 ±1 | %69 → %51 | %34 | 5.19 ±0.13 | 4.1 | 5.3 | 48 | %15 | %81 ±2 | %100 |
| **SN0.25** Şaşırt Beni, yenilik 0.25 | %59 ±1 | %69 → %53 | %35 | 5.48 ±0.12 | 4.1 | 5.2 | 47 | %17 | %82 ±2 | %100 |
| **SN0.20** Şaşırt Beni, yenilik 0.20 | %61 ±1 | %70 → %57 | %36 | 5.72 ±0.12 | 4.1 | 5.0 | 45 | %17 | %81 ±2 | %100 |
| **CR0.0** Sakin, keşif oranı 0.0 | %91 ±1 | %97 → %90 | %52 | 8.51 ±0.15 | 4.2 | 4.5 | 27 | %44 | %54 ±3 | %100 |
| **CR0.1** Sakin, keşif oranı 0.1 | %89 ±1 | %95 → %87 | %52 | 8.42 ±0.14 | 4.2 | 4.5 | 30 | %42 | %60 ±3 | %100 |
| **CR0.2** Sakin, keşif oranı 0.2 | %87 ±0 | %93 → %85 | %50 | 8.12 ±0.14 | 4.2 | 4.6 | 31 | %40 | %60 ±3 | %100 |
| **CR0.3** Sakin, keşif oranı 0.3 | %85 ±0 | %91 → %82 | %49 | 8.04 ±0.14 | 4.2 | 4.5 | 33 | %38 | %62 ±3 | %100 |

Persona bazında önceki mod ayarları (A0: yenilik 0,35, Sakin'de keşif yok) ile güncel ayarlar (A):

| Persona | Keşif modu | İsabet A0 → A | North-star A0 → A | Gizli ilgi keşfi A0 → A |
|---|---|---|---|---|
| Kahve ve kafe tutkunu | Explore | %65 → %65 | 5.66 → 5.66 | %88 → %88 |
| Kültür meraklısı | Chill | %89 → %83 | 7.47 → 7.33 | %73 → %68 |
| Düşük bütçeli öğrenci | Explore | %80 → %80 | 7.51 → 7.51 | %73 → %73 |
| Az vakitli çalışan | Chill | %100 → %97 | 6.35 → 6.20 | %95 → %100 |
| Hareket kısıtı olan kullanıcı | Explore | %76 → %76 | 6.78 → 6.78 | %83 → %83 |
| Sosyal kelebek | Explore | %77 → %77 | 9.68 → 9.68 | %80 → %80 |
| Şehir bilgisi vermeyen doğa sever | Explore | %77 → %77 | 8.25 → 8.25 | %55 → %55 |
| Şaşırt Beni kaşifi | SurpriseMe | %51 → %53 | 4.19 → 4.50 | %35 → %30 |

### Sevdiğini tekrarla

5 puan (veya "daha fazla") verilen template cooldown bittikten sonra 0,2 yerine bu yenilik skoruyla yeniden önerilebilir. 0,2 özelliğin kapalı olması demektir. Üretim değeri **0.6**.

| Senaryo | İsabet | İlk 5 → son 5 gün | Kabul | North-star / hafta | Puan | Kategori | Katalog | Tekrar | Gizli ilgi keşfi | Gün 1 |
|---|---|---|---|---|---|---|---|---|---|---|
| **LR0.2** Sevdiğini tekrarla, yenilik 0.2 | %75 ±1 | %82 → %71 | %43 | 6.85 ±0.14 | 4.2 | 4.9 | 41 | %25 | %71 ±3 | %100 |
| **LR0.4** Sevdiğini tekrarla, yenilik 0.4 | %76 ±1 | %82 → %72 | %44 | 6.97 ±0.14 | 4.2 | 4.9 | 40 | %27 | %71 ±2 | %100 |
| **LR0.6** Sevdiğini tekrarla, yenilik 0.6 | %76 ±1 | %83 → %73 | %44 | 6.99 ±0.14 | 4.2 | 4.9 | 39 | %28 | %70 ±2 | %100 |
| **LR0.8** Sevdiğini tekrarla, yenilik 0.8 | %76 ±1 | %83 → %74 | %45 | 7.12 ±0.14 | 4.2 | 4.8 | 39 | %29 | %69 ±3 | %100 |

### Erişilebilirlik: hareket kısıtı olan persona

| Senaryo | Kapasitesini aşan öneri | İsabet | North-star / hafta |
|---|---|---|---|
| Efor sınırı beyan edildi | %0 | %76 | 6.78 |
| Efor sınırı beyan edilmedi | %5 | %76 | 6.34 |

### Persona kırılımı (senaryo A)

| Persona | Keşif modu | İsabet | İlk 5 gün | Son 5 gün | North-star / hafta | Tamamlanan kategori | Gizli ilgi keşfi |
|---|---|---|---|---|---|---|---|
| Kahve ve kafe tutkunu | Explore | %65 | %74 | %66 | 5.66 | 4.3 | %88 |
| Kültür meraklısı | Chill | %83 | %91 | %78 | 7.33 | 4.6 | %68 |
| Düşük bütçeli öğrenci | Explore | %80 | %84 | %79 | 7.51 | 4.4 | %73 |
| Az vakitli çalışan | Chill | %97 | %99 | %94 | 6.20 | 4.3 | %100 |
| Hareket kısıtı olan kullanıcı | Explore | %76 | %78 | %74 | 6.78 | 5.4 | %83 |
| Sosyal kelebek | Explore | %77 | %87 | %75 | 9.68 | 5.5 | %80 |
| Şehir bilgisi vermeyen doğa sever | Explore | %77 | %85 | %75 | 8.25 | 4.4 | %55 |
| Şaşırt Beni kaşifi | SurpriseMe | %53 | %62 | %45 | 4.50 | 6.0 | %30 |

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
