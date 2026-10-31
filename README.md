# KAHRIMAN VOLUME MIXER

Windows 10/11 için, **Windows ana ses seviyesini (Master Volume)** fare tekerleğiyle hızlı ve akıcı biçimde kontrol eden mini DJ ses mikseri.

Ekranın kenarında sürekli duran küçük bir dikey fader: imleci üzerine getir, tekerleği çevir. Tıklamak gerekmez. Mikser ile Windows ses seviyesi iki yönlü ve gerçek zamanlı senkronizedir.

![platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6) ![.NET](https://img.shields.io/badge/.NET-8-512BD4) ![build](https://img.shields.io/badge/exe-tek%20dosya-FF8A1F)

## Özellikler

- **Tekerlekle ses kontrolü** — imleç fader penceresinin üzerindeyken tekerlek yukarı: ses artar, aşağı: azalır. Pencereden çıkınca tekerlek normal işine döner (global hook yok).
- **Akıllı hızlandırma** — yavaş çevirme %1,5, normal %5, hızlı %12 adım (ayarlanabilir). Hareketler hedefte birikir.
- **DJ mikseri gibi akıcı geçiş** — varsayılan 150 ms, yeni hareket gelince geçiş yeni hedefe döner; ses azaltma iki kat hızlı ve ilk adım beklemeden uygulanır.
- **Dikey fader** — sürükle/tıkla, büyük yüzde göstergesi, turuncu seviye dolgusu, koyu/açık tema.
- **LOW / HIGH** — tek tıkla %10'a in, tekrar tıkla önceki seviyeye dön. LOW ↔ HIGH arasında geçiş. Sağ tık: mevcut seviyeyi o butona kaydet.
- **Sessize alma** — MUTE butonu veya fader üzerinde **orta fare tuşu**. Windows'un yerel mute'u kullanılır; seviye kaybolmaz.
- **Windows ana sesi** — Windows Core Audio ile varsayılan ses çıkış aygıtının ana ses seviyesi kontrol edilir. Windows %42 ise mikser %42 gösterir; mikserden %80 yapınca Windows %80 olur.
- **İki yönlü, anlık senkron** — Windows ses simgesi, klavye ses tuşları veya başka bir uygulama sesi değiştirirse fader anında güncellenir.
- **Aygıt değişimi** — varsayılan hoparlör/kulaklık değişince yeni aygıt ≈1 sn içinde otomatik algılanır ve onun seviyesi gösterilir.
- **Açılışta Windows'un mevcut seviyesi okunur** — kayıtlı eski seviye Windows'a asla otomatik uygulanmaz.
- **Dış değişiklikleri algılar** — Windows ses mikserinden yapılan değişiklikler fader'a yansır.
- **Always on Top, taşınabilir, boyutlandırılabilir, sistem tepsisi** — konum/boyut hatırlanır, çoklu monitör ve yüksek DPI (Per-Monitor V2) desteklenir.
- **Güvenli kapatma** — uygulama kapanırken Windows sesi sıfırlanmaz, mevcut seviye korunur.
- **Tek EXE, kurulum yok, yönetici izni yok.**

## Hızlı başlangıç

1. [Releases](../../releases) sayfasından `KahrimanVolumeMixer-vX.Y.Z-win-x64.exe` dosyasını indir.
2. Çift tıkla. (.NET kurulumu gerekmez.)
3. İmleci fader'ın üzerine getir ve tekerleği çevir — Windows ana sesi değişir.

> İlk açılışta Windows SmartScreen "tanınmayan uygulama" uyarısı verebilir (EXE imzasız). **Ek bilgi → Yine de çalıştır** ile açabilirsin.

Ayrıntılı kullanım: [docs/KULLANIM.md](docs/KULLANIM.md)

## Kısayollar

| Eylem | Nasıl |
|---|---|
| Ses artır / azalt | Fader üzerinde tekerlek (pencere odakta değilken de çalışır) |
| Hassas ayar | Tekerleği yavaş çevir |
| Hızlı ayar | Tekerleği hızlı çevir |
| Doğrudan seviye | Fader'a tıkla veya sürükle |
| Sessiz / aç | Orta fare tuşu, MUTE butonu veya `M` |
| LOW ↔ önceki seviye | LOW butonu |
| HIGH ↔ önceki seviye | HIGH butonu |
| Ön ayarı kaydet | LOW / HIGH üzerinde sağ tık |
| Klavye | Pencere odaktayken `↑` / `↓` |
| Taşı | Üst çubuktan sürükle |
| Boyutlandır | Pencere kenarından sürükle |
| Tepsiye küçült | `–` butonu (tepsi simgesine tıklayınca geri gelir) |

## Derleme

Gereksinim: Windows 10/11 ve [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). (İsteğe bağlı: Visual Studio 2022 17.8+ ve ".NET masaüstü geliştirme" iş yükü.)

```powershell
git clone https://github.com/<kullanici>/KahrimanVolumeMixer.git
cd KahrimanVolumeMixer

# Testler + tek EXE
.\build.ps1
# → publish\KahrimanVolumeMixer.exe
```

Elle:

```powershell
dotnet test KahrimanVolumeMixer.sln -c Release

dotnet publish src/KahrimanVolumeMixer/KahrimanVolumeMixer.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true -o publish
```

Visual Studio'da: `KahrimanVolumeMixer.sln` aç → `KahrimanVolumeMixer` projesine sağ tık → **Yayımla** → `win-x64` profili.

Geliştirirken çalıştırma: `dotnet run --project src/KahrimanVolumeMixer`

### Otomatik derleme ve sürüm (GitHub Actions)

`.github/workflows/build.yml` her push / PR'da Windows'ta derler, testleri çalıştırır ve EXE'yi artefakt olarak yükler. Bir sürüm etiketi gönderildiğinde EXE otomatik olarak **Releases** bölümüne eklenir:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## Proje yapısı

```
KahrimanVolumeMixer.sln
src/
  KahrimanVolumeMixer.Core/        Platformdan bağımsız mantık (test edilebilir)
    VolumeRamp.cs                  Hedefi değiştirilebilir yumuşak geçiş
    WheelAccelerator.cs            Tekerlek hızı → adım (akıllı hızlandırma)
    MasterVolumeController.cs      Windows ana sesiyle iki yönlü senkron, dış değişiklik algılama
    LevelMemory.cs                 LOW/HIGH "git–geri dön" hafızası
    AppSettings.cs, SettingsStore.cs
  KahrimanVolumeMixer/             WPF uygulaması
    Audio/MasterVolumeBackend.cs   Core Audio: varsayılan aygıt, AudioEndpointVolume, OnVolumeNotification
    Audio/MixerEngine.cs           Tekerlek → geçiş → Windows ana ses; zamanlayıcılar
    Controls/FaderControl.cs       Dikey DJ fader (OnRender)
    MainWindow, SettingsWindow, TrayIcon, ThemeManager, StartupManager
tests/
  KahrimanVolumeMixer.Core.Tests/  xUnit birim testleri
docs/
  KULLANIM.md, TEST_PLANI.md
```

## Teknik kararlar

- **C# / .NET 8 / WPF**, ses için **Windows Core Audio API** (`NAudio.Wasapi` 2.2.1 üzerinden `IAudioSessionManager2` / `ISimpleAudioVolume`).
- **Tekerlek yalnızca kendi penceremizde** `PreviewMouseWheel` ile yakalanır; sistem genelinde hook yoktur. Windows 10/11'in varsayılan "Etkin olmayan pencereleri üzerine gelindiğinde kaydır" özelliği sayesinde pencere odakta olmasa da çalışır.
- **Ses kaynağı:** `MMDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)` → `AudioEndpointVolume.MasterVolumeLevelScalar` (Windows'ta görünen yüzdeyle aynı ölçek) ve `AudioEndpointVolume.Mute`.
- **Windows → mikser:** `OnVolumeNotification` anında bildirir; arka plan iş parçacığından UI iş parçacığına aktarılır, ardışık bildirimler birleştirilir. 250 ms'lik yoklama yedek olarak çalışır. Kendi yazdığımız değerler dış değişiklik sayılmaz.
- **Aygıt değişimi:** varsayılan aygıt kimliği ≈1 sn'de bir kontrol edilir; değişirse eski olay aboneliği ve COM nesneleri bırakılır, yeni aygıta bağlanılır ve onun seviyesi benimsenir.
- **Açılış:** Windows'un mevcut seviyesi ve sessiz durumu okunur; kayıtlı seviye yalnızca LOW/HIGH "geri dön" hafızası için tutulur.
- **Geçiş:** zaman tabanlı üstel yaklaşım + asgari hız; ≈60 Hz zamanlayıcı yalnızca geçiş sürerken çalışır. Boşta CPU kullanımı ihmal edilebilir düzeydedir.
- **Ayarlar:** `%APPDATA%\KahrimanVolumeMixer\settings.json` (atomik yazım). Hata günlüğü aynı klasörde `error.log`.
- **Başlangıçta çalıştırma:** `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (yönetici izni gerekmez).

## Bilinen sınırlamalar

- Hiç ses çıkış aygıtı yoksa "AYGIT YOK" görünür; aygıt takılınca ≈1 sn içinde bağlanır.
- Windows'ta "Etkin olmayan pencereleri kaydır" kapalıysa tekerlek için önce pencereye tıklamak gerekir (Ayarlar → Bluetooth ve cihazlar → Fare).
- Yalnızca varsayılan çıkış aygıtının ana sesi kontrol edilir (uygulama bazlı ses kontrolü yok).
