# LifeQuest mobil uygulaması (.NET MAUI)

Web'deki kullanıcı ekranlarının native iOS ve Android karşılığı. Aynı API'yi kullanır; yönetim paneli web'de kalır.

## Yapı

| Proje | İçerik |
|---|---|
| `src/LifeQuest.Mobile.Core` (net10.0) | API istemcisi ve modeller, native oturum, TR/EN sözlük, biçimlendirme, tüm ekran ViewModel'leri. MAUI'ye bağlı değil; Linux CI'da derlenir ve test edilir. |
| `src/LifeQuest.Mobile` (net10.0-android; net10.0-ios) | XAML sayfalar, ortak bileşenler, platform servisleri (Keychain/Keystore, paylaşım, yerel bildirim), tema ve kaynaklar. |
| `tests/LifeQuest.Mobile.Tests` | Oturum yarışları, hata ayrıştırma, biçimlendirme, sözlük ve ViewModel testleri (sahte API ile). |
| `tests/LifeQuest.Api.IntegrationTests/MobileClientContractTests.cs` | Mobil çekirdeğin gerçek API'ye karşı uçtan uca sözleşme testi. |

`LifeQuest.slnx` çekirdeği ve testleri içerir (CI'da derlenir). MAUI uygulaması `LifeQuest.Mobile.slnx` ile açılır.

## Oturum

Mobil istemci her isteğe `X-LifeQuest-Client: native` başlığı ekler. Bu başlıkla giriş, kayıt ve yenileme refresh
token'ı yanıt gövdesinde döndürür; çerez kullanılmaz. Token cihazın güvenli deposunda (iOS Keychain, Android Keystore)
saklanır, access token yalnızca bellektedir. Sunucu her yenilemede token'ı döndürür ve eski token'ın yeniden
kullanımında oturum ailesini kapatır; bu yüzden istemcide yenileme tek uçuşludur (aynı anda gelen 401'ler tek bir
yenilemeyi bekler). `TOKEN_STALE` → yenile ve tekrarla; `ACCOUNT_SUSPENDED` → oturum kapanır.

Native modda token yalnızca gövdeden okunur: web'deki bir betik başlığı ekleyerek çerezdeki token'ı gövdeye çıkaramaz.

## Metinler, ikonlar ve tema

- **Sözlük:** web'in kullanıcı bölümlerinden (`src/LifeQuest.Web/src/app/core/i18n/sections`) üretilir. Web'de metin
  değişince:
  ```bash
  node tools/mobile/gen-strings.mjs
  ```
  Yalnızca mobilde olan birkaç metin `Localization/MobileStrings.cs`'te. XAML'de `{l:Tr Today.Suggestions}`; dil
  değişince ekran yenilenmeden yeni dilde görünür.
- **İkonlar:** web ikon setinden (`ui/icon.ts`) üretilir: `node tools/mobile/gen-icons.mjs`. Sekme simgeleri
  `Resources/Images/tab_*.svg`.
- **Renkler:** `Resources/Styles/Colors.xaml` web token'larıyla (`styles/_tokens.scss`) aynı; açık ve koyu tema.
- **Yazı tipi:** Nunito (SIL Open Font License, `Resources/Fonts/OFL.txt`).

CI, üretilen dosyaların web ile güncel olduğunu kontrol eder.

## Hatırlatma

Günlük hatırlatma cihazda zamanlanır (Plugin.LocalNotification): önümüzdeki 7 gün için ayrı bildirimler kurulur,
uygulama açıldıkça ve görev tamamlandıkça yenilenir; o gün görev tamamlandıysa o günün bildirimi atlanır. Ayar yalnızca
bu cihazdadır ve web'deki push hatırlatmasından bağımsızdır. Uzak push (FCM/APNs) sonraki adım.

## Derin bağlantı

`lifequest://party/{kod}` parti davetini açar; oturum yoksa girişten sonra açılır. Paylaşılan davet metni web
bağlantısını (`https://…/party/{kod}`) içerir; uygulaması olmayan kişi web'den katılır. Universal Links / App Links
(alan adı doğrulaması) sonraki adım.

## Geliştirme ortamı

Önkoşullar (bir kez):

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer && sudo xcodebuild -license accept
```
```bash
sudo dotnet workload install maui
```

Android için Android Studio → Settings → Android SDK: **Android 16 (API 36)**, **Build-Tools 36**, **Command-line
Tools**. JDK olarak Android Studio'nun JBR'ı kullanılır (csproj'ta otomatik).

> .NET for iOS 26.5.10318 Xcode 26.6 ister. Geliştirme derlemesinde (Debug) sürüm kontrolü kapalıdır; Xcode 26.5 ile
> çalışır. Yayın derlemesi doğru Xcode'u ister.

### Çalıştırma

Yerel API (`http://localhost:5080`) açık olmalı. iOS simülatörü bilgisayarın `localhost`'unu, Android emülatörü
`10.0.2.2`'yi kullanır (Debug'da otomatik). Şifresiz HTTP yalnızca bu iki adres için açıktır.

```bash
dotnet build src/LifeQuest.Mobile -f net10.0-ios
```
```bash
xcrun simctl install booted src/LifeQuest.Mobile/bin/Debug/net10.0-ios/iossimulator-arm64/LifeQuest.Mobile.app && xcrun simctl launch booted com.gvnaitech.lifequest
```
```bash
dotnet build src/LifeQuest.Mobile -f net10.0-android -t:Run
```

Farklı bir API için derleme parametresi: `-p:LifeQuestApiBaseUrl=https://…` (davet bağlantıları için
`-p:LifeQuestWebBaseUrl=https://…`).

### Testler

```bash
dotnet test tests/LifeQuest.Mobile.Tests
```

## Sonraki adımlar

- Uzak push (FCM/APNs): cihaz token kaydı ve sunucuda platforma göre gönderici.
- Universal Links / App Links ile https davet bağlantılarının doğrudan uygulamada açılması.
- Mağaza yayını: imzalama, sürümleme, Release API adresi.
