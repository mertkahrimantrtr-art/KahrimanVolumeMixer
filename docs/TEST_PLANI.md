# Test Planı

İki katman vardır:

1. **Otomatik birim testleri** (`tests/KahrimanVolumeMixer.Core.Tests`, xUnit) — ses mantığının tamamı sahte (fake) ses oturumlarıyla test edilir. GitHub Actions her derlemede çalıştırır: `dotnet test KahrimanVolumeMixer.sln -c Release`
2. **Elle kabul testleri** — gerçek Windows + Chrome gerektiren senaryolar. Her sürümden önce aşağıdaki liste uygulanır.

## Otomatik testlerin kapsadığı senaryolar

| # | Senaryo | Test |
|---|---|---|
| 2 | Chrome kapalıyken çalışma (oturum yok, hata yok) | `Disconnected_SetMaster_DoesNotThrow_AndIsPending` |
| 3 | Yeniden bağlanma ve seviyenin benimsenmesi | `RemovedSessions_AreDropped_AndReconnectAdoptsAgain`, `FirstConnection_*` |
| 4 | Hızlı tekerlek | `FastMovement_UsesFastStep` |
| 5 | Yavaş / hassas tekerlek | `SlowMovement_UsesSlowStep`, `HighResolutionDelta_IsProportional` |
| 6 | Sessize alma / açma, seviyenin korunması | `Mute_KeepsLevel` |
| 7 | LOW/HIGH ve geri dönüş | `LevelMemoryTests` |
| 8 | Birden fazla oturum, göreli denge, yeni oturum | `SetMaster_ControlsAllSessions_PreservingRelativeBalance`, `Balance_SurvivesGoingToZeroAndBack`, `NewSession_WhileConnected_*` |
| 9 | Ayarların kalıcılığı, bozuk dosya | `SettingsStoreTests` |
| 10 | Dış değişiklik algılama (ses + mute) | `ExternalVolumeChange_*`, `ExternalMute_IsDetected`, `OwnWrites_AreNotReportedAsExternal` |
| — | Akıcı geçiş: süre, monotonluk, yeniden hedefleme, azaltma önceliği | `VolumeRampTests` |
| — | %0–100 sınırlandırma | `Master_IsClamped`, `Target_IsClamped` |
| — | Kapanan oturumda çökmeme | `DeadSession_IsRemoved_WithoutThrowing` |

## Elle kabul testleri (Windows 10 ve 11)

Hazırlık: Chrome'da YouTube vb. ses çalan bir sekme. Windows ses mikserini aç (Win11: Ayarlar → Ses → Ses karıştırıcı; Win10: tepsi hoparlör simgesi → Ses karıştırıcıyı aç).

| # | Adım | Beklenen | Sonuç |
|---|---|---|---|
| 1 | Chrome açıkken fader üzerinde tekerleği çevir | Yalnızca Chrome'un sesi değişir; Windows ana sesi ve diğer uygulamalar değişmez; yüzde göstergesi ve mikserdeki Chrome sürgüsü eşleşir | ☐ |
| 2 | Chrome'u tamamen kapat, uygulamayı çalıştır | "CHROME YOK", gri nokta; fader/butonlar hata vermez | ☐ |
| 3 | Uygulama açıkken Chrome'u aç ve ses çal | ≈1 sn içinde "CHROME", yeşil nokta; seviye Chrome'un seviyesini gösterir | ☐ |
| 4 | Tekerleği hızlıca çevir (aşağı) | Ses birkaç çentikte belirgin azalır, takılma/gecikme yok; kısma anında duyulur | ☐ |
| 5 | Tekerleği tek tek, yavaşça çevir | Her çentikte ≈%1–2 değişim | ☐ |
| 6 | Orta tuş, sonra tekrar orta tuş; MUTE butonu ile tekrarla | Sessiz / önceki seviye; mikserde Chrome sessiz simgesi | ☐ |
| 7 | %75'teyken LOW, LOW; HIGH, HIGH; LOW'a sağ tık | %10 → %75; ön ayar ↔ önceki seviye; sağ tık LOW'u mevcut seviyeye kaydeder | ☐ |
| 8 | Chrome'da iki farklı çıkış aygıtına (veya iki ayrı ses kaynağına) çal; mikserde birini %50'ye çek; fader'ı oynat | "CHROME ×2"; denge korunur (biri hep diğerinin yarısı) | ☐ |
| 9 | Ayarları ve pencere konumunu değiştir, kapat, aç | Tüm ayarlar, konum, boyut ve seviye korunur | ☐ |
| 10 | Windows mikserinden Chrome'u değiştir / sessize al | Fader ≈250 ms içinde güncellenir | ☐ |
| 11 | Uygulamayı kapat (✕ ve Alt+F4) | Chrome sesi aynı seviyede kalır | ☐ |
| 12 | %150 DPI ekranda ve ikinci monitörde aç, monitörler arası taşı | Keskin görünüm, doğru ölçek | ☐ |
| 13 | İkinci monitördeyken kapat, monitörü çıkar, aç | Pencere ana ekranda görünür | ☐ |
| 14 | Görev Yöneticisi: 5 dk boşta | CPU ≈%0, bellek sabit | ☐ |
| 15 | EXE'ye ikinci kez çift tıkla | Yeni örnek açılmaz, mevcut pencere öne gelir | ☐ |
| 16 | "Windows başlangıcında çalıştır" aç, oturumu kapat/aç | Uygulama başlar | ☐ |
