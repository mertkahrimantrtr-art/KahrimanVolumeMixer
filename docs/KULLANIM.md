# KAHRIMAN VOLUME MIXER — Kullanım Kılavuzu

## Arayüz

```
┌────────────┐
│ ●     ⚙ – ✕│  ● bağlantı (yeşil: Chrome bağlı, gri: yok) · ayarlar · tepsiye küçült · kapat
│    75%     │  anlık seviye (sessizde "MUTE")
│   CHROME   │  durum: CHROME / CHROME ×2 (oturum sayısı) / CHROME YOK
│  ─ ██ ─    │
│  ─ ██ ─    │  dikey fader — tekerlek, tıklama, sürükleme
│ ═══██═══   │  ← fader kapağı
│  ─ ██ ─    │
│ [LOW][HIGH]│
│ [  🔊   ]  │  MUTE
└────────────┘
```

## Ses ayarı

- **Tekerlek:** imleci pencerenin herhangi bir yerine getir ve çevir. Tıklaman gerekmez.
  - Yavaş çevirme → hassas (%1,5 / çentik)
  - Normal → %5 / çentik
  - Hızlı çevirme → %12 / çentik
  - Hassas dokunmatik yüzey kaydırması orantılı işlenir.
- **Tıkla / sürükle:** fader üzerinde istediğin noktaya tıkla veya sürükle; seviye anında uygulanır.
- **Klavye:** pencere odaktayken `↑` / `↓`, sessiz için `M`.
- Ses %0'ın altına inmez, %100'ün üstüne çıkmaz.

## LOW / HIGH

- **LOW** (varsayılan %10): sesi hızla kısar. Tekrar basınca kısmadan önceki seviyeye döner.
  Örnek: %75 → LOW → %10 → LOW → %75.
- **HIGH** (varsayılan %75): aynı mantıkla HIGH'a çıkar / önceki seviyeye döner.
- LOW'dayken HIGH'a, HIGH'dayken LOW'a basarak ikisi arasında geçiş yapabilirsin.
- **Sağ tık:** butonun seviyesini mevcut seviye olarak kaydeder (ör. %30'dayken LOW'a sağ tık → LOW = %30).

## Sessize alma

- MUTE butonu, fader üzerinde **orta tuş (tekerleğe basma)** veya `M`.
- Windows'un kendi sessize alma özelliği kullanılır; seviye korunur, tekrar açınca aynı seviyeden devam eder.
- Sessizdeyken tekerleği yukarı çevirmek, LOW/HIGH veya fader'a tıklamak sesi otomatik açar.

## Pencere

- **Taşıma:** üst çubuktan (bağlantı noktasının yanı) sürükle.
- **Boyut:** kenarlardan sürükle (en küçük 76 × 200).
- **Tepsiye küçültme:** `–` butonu. Sistem tepsisindeki simgeye sol tıkla göster/gizle; sağ tıkla menü (Sessiz, LOW, HIGH, Her zaman üstte, Ayarlar, Çıkış).
- **Kapatma:** `✕` veya tepsi menüsünden Çıkış. Chrome'un sesi olduğu gibi kalır.
- Konum ve boyut kaydedilir. Kaydedilen monitör artık bağlı değilse pencere ana ekranın sağ kenarında açılır.
- Uygulama zaten açıkken EXE'ye tekrar çift tıklamak mevcut pencereyi öne getirir.

## Ayarlar

| Ayar | Açıklama |
|---|---|
| Akıllı hızlandırma | Dönüş hızına göre yavaş/normal/hızlı adım. Kapalıysa sabit adım kullanılır. |
| Sabit adım | Hızlandırma kapalıyken çentik başına değişim. `↑`/`↓` tuşları da bunu kullanır. |
| Yavaş / Normal / Hızlı adım | Hızlandırma basamakları. |
| Yumuşak ses geçişi | Kapalıysa ses anında değişir. |
| Geçiş süresi | 20–500 ms (varsayılan 150). Ses azaltma bu sürenin yarısında tamamlanır. |
| LOW / HIGH seviyesi | Ön ayarlar. |
| Her zaman üstte | Diğer pencerelerin üzerinde kal. |
| Windows başlangıcında çalıştır | Oturum açınca otomatik başlat (yönetici izni gerekmez). |
| Tema | Koyu / Açık. |
| Pencere saydamlığı | %30–100. |

Ayarlar değiştirildiği anda uygulanır ve `%APPDATA%\KahrimanVolumeMixer\settings.json` dosyasına kaydedilir.

## Sorun giderme

| Belirti | Çözüm |
|---|---|
| "CHROME YOK" yazıyor | Chrome'da ses çalan bir sekme aç. Windows, Chrome ilk kez ses çıkarana kadar ses oturumu oluşturmaz. |
| Tekerlek çalışmıyor, önce tıklamam gerekiyor | Windows Ayarlar → Bluetooth ve cihazlar → Fare → "Etkin olmayan pencereleri üzerine geldiğimde kaydır" seçeneğini aç. |
| Pencere görünmüyor | Tepsi simgesine tıkla; ya da EXE'yi tekrar çalıştır (öne gelir). |
| Ayarları sıfırlamak | Uygulamayı kapat, `%APPDATA%\KahrimanVolumeMixer\settings.json` dosyasını sil. |
| Beklenmeyen davranış | `%APPDATA%\KahrimanVolumeMixer\error.log` dosyasına bak. |
