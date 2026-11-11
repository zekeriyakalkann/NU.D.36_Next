# Lab 8 · Bölüm 4 oynanış videosu — kayıt yönergesi

Föy 09: Video **en çok 7 dakika** olmalı. Videoda **üç ateş bölgesi**, **bir hasar anı** ve **"Görev tamam"** görünmeli. Dosya adı ve yeri: `Video/bolum4.mp4`. Zip'e bu yolla girmesini teslim betiği sağlar.

## 1. Hazırlık
- **Hangi oyunu açacaksın:** `Builds\Windows_Bolum4\NU.D.36.exe`. Doğrudan Bölüm 4'ten başlar; menüye girmeden kayda başlarsın.
- **Ekran ayarı:** Oyun açılınca Esc → Seçenekler'den tam ekranı ve kaliteyi istediğin gibi ayarla. Kaydedici pencereyi yakalayacağı için pencere modu da olur.
- **Başka programlar:** Kayıtta takılma olmaması için ağır programları (Docker, tarayıcı sekmeleri) kapat.

## 2. Kaydetme (iki yol)
1. **Windows Xbox Game Bar** (kurulum yok):
   - Oyun penceresi öndeyken **Win + Alt + R** ile kayda başla, aynı tuşlarla durdur.
   - Dosya `C:\Users\<sen>\Videos\Captures\` klasörüne `.mp4` olarak düşer.
2. **OBS Studio** (yüklüyse): Kaynak "Oyun Yakalama" ya da "Pencere Yakalama", çıktı biçimi mp4, 1080p, 30 fps.

## 3. Ne göstermeli (yaklaşık zaman çizelgesi, toplam ~5:30)
| Zaman | Olay | Ekranda görünmesi gereken |
|---|---|---|
| 0:00 | Başlangıç: uçak trenin üstünde, havada | HUD'un beş satırı, yeşil "TREN" işareti, ışık sütunu |
| ~1:00 | **1 · Dar Vadi** (2 tank, 1 uçaksavar) | Bölge duyurusu; tankların tren'e ateşi; tank çiftine bomba (T ile seç, O açıkken kendisi bırakır, ya da 300 m'nin altında Space) |
| ~2:20 | **2 · Viyadük** (2 uçaksavar, 2 düşman uçağı) | Makineli (Ctrl / sol tık); vurulan uçağın dumanla çekilmesi |
| ~3:35 | **3 · Son Viraj** (2 tank, 1 uçaksavar, 2 düşman uçağı) | İkinci tank çifti, ikinci uçak dalgası |
| herhangi bir an | **Hasar anı** | Alçakta bir uçaksavarın yakınından geç: "UÇAKSAVAR ATEŞİ" uyarısı, UÇAK yüzdesinin düşmesi, motor dumanının koyulaşması. Ya da tren vurulurken "TREN VURULUYOR" |
| ~5:00 | Tren güvenli bölgede | Görev satırı "Üsse dön" |
| ~5:30 | Üssün üstünde alçakta (yerden 200 m'nin altı) | **"GÖREV TAMAM"** paneli: trenin canı, x / 12, kalan cephane, süre |

İpuçları:
- **GPS (M):** Üs, haritada mavi pistin olduğu yer. Dönüşte GPS'teki "ÜS … km" satırını izle.
- **Uçuş:** Arcade modda yere ya da duvara çarpmazsın; alçalmak için W'yi basılı tutman yeterli. Uçuş modunu değiştirmek istersen K.
- **Video uzarsa:** Bölge aralarındaki sakin kesimleri kırpabilirsin. Toplam süre 7:00'ı geçmemeli.

## 4. Videoyu pakete koymak
1. Kaydı `bolum4.mp4` adıyla bir yere kaydet. Adı farklıysa betik zip içinde `Video/bolum4.mp4` olarak yeniden adlandırır.
2. Unity Editor kapalıyken, çalışma alanının kökünde şunu çalıştır:
   ```powershell
   powershell -ExecutionPolicy Bypass -File Tools\Teslim\teslim_h8.ps1 -OgrenciNo <öğrenci numaran> -Video "C:\...\bolum4.mp4"
   ```
3. Betik yaklaşık 15–25 dakika sürer:
   - listeyi çıkarır;
   - kopyayı hazırlar;
   - kopyayı Unity'de doğrular ve build alır;
   - `Teslim\BMU1421_<no>_H8.zip` dosyasını yazar.
   Build'i atlamak için `-BuildYok` ekle (~10 dk kısalır).
4. `Teslim\BMU1421_<no>_H8.zip` dosyasını OYS'ye yükle. Zip 7 dakikalık bir videoyla birkaç yüz MB olabilir; OYS'nin dosya sınırını kontrol et.
