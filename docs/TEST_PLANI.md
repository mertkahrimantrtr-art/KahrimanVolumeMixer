# Test Planı

İki katman vardır:

1. **Otomatik birim testleri** (`tests/KahrimanVolumeMixer.Core.Tests`, xUnit) — ses mantığının tamamı sahte (fake) ses aygıtıyla test edilir. GitHub Actions her derlemede çalıştırır: `dotnet test KahrimanVolumeMixer.sln -c Release`
2. **Elle kabul testleri** — gerçek Windows ses aygıtı gerektiren senaryolar. Her sürümden önce aşağıdaki liste uygulanır.

## Otomatik testlerin kapsadığı senaryolar

| Senaryo | Test |
|---|---|
| Açılışta Windows seviyesinin okunması, kayıtlı seviyenin uygulanmaması | `Attach_AdoptsWindowsLevel_AndNeverAppliesSavedLevel` |
| Mikser → Windows (%80, %0) | `SetLevel_WritesToWindows` |
| Windows → mikser (%25) | `ExternalChange_IsDetected`, `OwnWrites_AreNotReportedAsExternal` |
| Sessize alma / açma, seviyenin korunması, dış mute | `Mute_KeepsLevel`, `ExternalMute_IsDetected`, `Attach_AdoptsMuteState` |
| Varsayılan aygıt değişimi | `DeviceSwitch_AdoptsNewDeviceLevel` |
| Aygıt yok / çıkarıldı — çökmeme | `NoDevice_DoesNotThrow`, `RemovedDevice_Disconnects_WithoutThrowing`, `DeadDeviceOnAttach_IsRejected` |
| %0–100 sınırlandırma | `Level_IsClamped`, `Target_IsClamped` |
| Hızlı / yavaş tekerlek, akıllı hızlandırma | `WheelAcceleratorTests` |
| Akıcı geçiş: süre, monotonluk, yeniden hedefleme, azaltma önceliği | `VolumeRampTests` |
| LOW/HIGH ve geri dönüş | `LevelMemoryTests` |
| Ayarların kalıcılığı, bozuk dosya | `SettingsStoreTests` |

## Elle kabul testleri (Windows 10 ve 11)

| # | Adım | Beklenen | Sonuç |
|---|---|---|---|
| 1 | Windows sesini %42 yap, uygulamayı aç | Mikser %42 gösterir; Windows sesi değişmez | ☐ |
| 2 | Fader üzerinde tekerleği çevir / %80'e getir | Windows ses simgesindeki değer aynı anda değişir, %80 olur | ☐ |
| 3 | Windows ses simgesinden veya klavye ses tuşlarından %25 yap | Fader anında %25 gösterir | ☐ |
| 4 | Fader'ı en alta çek | Windows sesi %0 | ☐ |
| 5 | Tekerleği hızlıca / yavaşça çevir | Hızlı: büyük adımlar, takılma yok; yavaş: ≈%1–2 adım | ☐ |
| 6 | Orta tuş, MUTE butonu; Windows'tan sessize al | Windows sessiz / açık; Windows'tan yapılan sessize alma mikserde "MUTE" gösterir | ☐ |
| 7 | %75'teyken LOW, LOW; HIGH, HIGH; LOW'a sağ tık | %10 → %75; ön ayar ↔ önceki seviye; sağ tık LOW'u mevcut seviyeye kaydeder | ☐ |
| 8 | Kulaklık tak / varsayılan aygıtı değiştir | ≈1 sn içinde yeni aygıtın seviyesi gösterilir, kontrol yeni aygıta geçer | ☐ |
| 9 | Tüm ses aygıtlarını devre dışı bırak | "AYGIT YOK", gri nokta, çökme yok; aygıt geri gelince bağlanır | ☐ |
| 10 | Ayarları ve pencere konumunu değiştir, kapat, aç | Ayarlar, konum, boyut korunur; seviye Windows'tan okunur | ☐ |
| 11 | Uygulamayı kapat (✕ ve Alt+F4) | Windows sesi aynı seviyede kalır | ☐ |
| 12 | %150 DPI ekranda ve ikinci monitörde aç, monitörler arası taşı | Keskin görünüm, doğru ölçek | ☐ |
| 13 | Görev Yöneticisi: 5 dk boşta | CPU ≈%0, bellek sabit | ☐ |
| 14 | EXE'ye ikinci kez çift tıkla | Yeni örnek açılmaz, mevcut pencere öne gelir | ☐ |
| 15 | "Windows başlangıcında çalıştır" aç, oturumu kapat/aç | Uygulama başlar | ☐ |
