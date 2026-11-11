# Lab 6–7 (Hafta 6–7): Bölüm 3 gece görevi (Rota07), menü ve göstergeler

İlk sürüm 2026-11-03'te kuruldu; kaynak, kullanıcının Hafta 7 istekleriydi (föy metni o gün gelmemişti).

2026-11-10'da iki güncelleme geldi:
- **Menü ve göstergeler:** Kullanıcı istedi; §8'de.
- **Föye göre yeniden yerleşim:** "Laboratuvar Föyü: Kâğıt Görev" metni geldi. Lab 6 bu metinle yapılmıştı; değerlendirme tablosu Hafta 6 ve 7'yi tek ödev sayıyor. 7. haftanın notu şunlardan veriliyor: gece ve ışıldak; köprü, kazanma ve kaybetme, şafak, üsse dönüş; gece ışığı, hüzme, iz mermisi, çöküş. Bu yüzden gece seviyesi Hafta 6'nın kâğıt haritası v0.1'in üstüne yeniden kuruldu (`Bolum3Harita`, 1 kare = 200 m).

Sahne: `Rota07`, `Bolum3_GriKutu`'nun kopyası ("Save As"). Önceki lab sahneleri bayt bayt aynı kaldı (MD5 testi).

Durum değerleri: IMPLEMENTED, VERIFIED (testle ya da görüntüyle doğrulandı), PARTIALLY VERIFIED, NOT TESTED, KULLANICI.

## 1. Föy ve değerlendirme → uygulama
| No | Kaynak | Karşılığı | Durum |
|---|---|---|---|
| REQ-601 | Föy 05: gri kutu, kâğıtla aynı ölçek | `Bolum3_GriKutu` (Lab 6) | VERIFIED (Lab6Tests) |
| REQ-710 | "Bölüm 3 kâğıtta doğuyor… 7. haftada Unity'de büyüyecek" | Gece haritası kâğıt v0.1'den kuruldu (§4). Her öğe kendi karesinde, 1 kare = 200 m | VERIFIED (`Lab7Tests.PaperMap_*`, üstten görüntü 15) |
| REQ-711 | GDD §9.4 "dar hedef", föy "bir köprü" | Tek köprü H5–J5, görevin hedefi (`anaHedef`). 600 m uzunluk, güverte 25 m; vurulan yerin ±60 m'si çöker | VERIFIED (PlayMode, görüntü 08–10) |
| REQ-712 | "Gece ve ışıldak çalışıyor" | 3 ışıldak: F4 135°, K5 270°, H7 0°; ışın 1200 m. Tarar, yakalar, izler; ay ışığı, sis, gökyüzü | VERIFIED |
| REQ-713 | "Kazanma ve kaybetme, şafak, üsse dönüş" | Kazanma: köprü yıkılır, uçak şafaktan önce işaretli bölgeye iner ve durur. Kaybetme: çakılma, vurulma ya da şafak. Şafak 480 s'de | VERIFIED (`Lab7Bolum3PlayTests`, `Lab7PlayTests`) |
| REQ-714 | "Estetik: gece ışığı, hüzme, iz mermisi, çöküş" | HDR lambalar ve bloom, görünür huzmeler, iz mermileri, köprü ve kule çöküşü, yangın ve duman, şafak gökyüzü | VERIFIED (görüntüler `Logs/lab7/gorsel/`) |
| REQ-715 | Kâğıt: ses dinleyici (S), "duyulan kare", "öncü işaret" | H3, Ø 2000 m. Işıldaklar duyulan yere döner, toplar oraya ateş eder; HUD "DUYULDU <kare>" | VERIFIED |
| REQ-716 | Tek kural "Bulut" | E5–G5, Orta bandı (140–270 m): ışıldak uçağı yakalayamaz | VERIFIED |
| REQ-717 | Tepeler 120 m: Alçak'ı keser, Orta'yı kesmez | E4, F7, G7 | VERIFIED (EditMode) |
| REQ-718 | Kullanıcı: menü sistemi | Ana menü ve Esc duraklatma menüsü (§8) | VERIFIED (PlayMode, görüntüler) |
| REQ-719 | Kullanıcı: gece görevine hız ve gaz göstergeleri | 0–500 km/sa kadranı ve gaz kolu (§8) | VERIFIED |
| REQ-720 | Kullanıcı (2026-11-10): "fren mekanizması" | F/B: havada hava freni, yerde teker freni (§11) | VERIFIED (EditMode) |
| REQ-721 | Kullanıcı: "daha rahat bir uçuş deneyimi … rahat kullanabileceğim bir şekilde" | Kolay uçuş: tuş bırakınca düz uçuş, yatış/eğim komutu, hız koruması, yer koruması, kendiliğinden kalkış ve teker koyma, harita sınırı dönüşü (§11) | VERIFIED (testler); elle his KULLANICI |
| REQ-722 | Kullanıcı: "menzile girdiğimde kuleyi otomatik vuracak şekilde" | Nişan sistemi: kilit 1600 m, güdümlü bomba, 1200 m'de kendiliğinden atış (§11) | VERIFIED (PlayMode) |
| REQ-723 | Kullanıcı: "hedefleri net gösteren bir GPS gibi bir şey" | GPS haritası + seçili hedefin HUD'da büyük işareti ve durumu (§11) | VERIFIED (PlayMode); görünüm KULLANICI |
| — | Föy 01–04: kâğıtta ısınma, eşe oynatma, gözlem, tek değişiklik | Basılı formlar hazır (§9) | KULLANICI |
| — | Föy 06: laboratuvar GDD'si (Word şablonu) | İçerik `Lab6_GDD_Taslagi.md`'de | Şablona aktarmak KULLANICI |

İlk Lab 7 isteklerinden (2026-11-03) korunanlar:
- Karanlık ve sisli arazi.
- Pist.
- Kuleleri vurmak: kuleler artık isteğe bağlı hedef, 150 puan.
- İşaretli bölgeye iniş.
- Yere düşünce oyun biter.
- Aerodinamik uçuş.
- Duman.
- Uçağın önünde ışık.

Değişen: "3 köprü" kalktı. Föy tek köprü istiyor (D-086).

## 2. Oynanış
1. **Kalkış:** Uçak pistin batı ucunda (8. satır), burnu doğuya (090°), motor rölantide durur. ↑ ile gaz verilir; hız 150–175 km/sa olunca S ile burun kaldırılır. Havalanınca pist ışıkları söner (karartma).
2. **Saldırı:** Köprüye iki yoldan gidilir (kâğıttaki Yol 1 ve Yol 2):
   - **Yol 1, tepe arkası:** Alçaktan, F7–G7 sırtının güneyinden.
   - **Yol 2, açık:** Bulutların (E5–G5) içinden, Orta irtifada.
   HUD köprünün ve kulelerin yerini işaretler. Köprüyü bir bomba yıkar, ama dar hedeftir: bomba 9 m'lik güvertenin yaklaşık 25 m yakınına düşmeli. Yük 4 bomba; pistin üstünden alçak geçiş yükü yeniler.
3. **Tehditler:**
   - Işıldak yakalayınca uçaksavar nişanlı ateş eder.
   - Ses dinleyici duyunca (gaz ≥ 0,15, H3'e 1000 m'den yakın) ışıldaklar ve toplar duyulan yere döner. Ses gecikmeli geldiği için duyulan yer, uçağın birkaç saniye önceki yeridir.
   - Gazı kısıp süzülen uçak duyulmaz; bulutun içinde ışık yakalayamaz.
4. **Üsse dönüş:** Köprü çökünce pist ışıkları ve yeşil iniş bölgesi yanar. İniş bölgesi, 27 eşiğinden itibaren 750 m'dir (kâğıttaki B8–D8). Pist 27'ye (270°) inilir ve bölgede durulur.
5. **Şafak:** Görevin 480. saniyesi. Son 2 dakikada gökyüzü, ışık ve sis aydınlanır. 90 s önce bildirim gelir, son 60 s'de uyarı yanıp söner. İnmeden şafak sökerse görev biter ("ŞAFAK SÖKTÜ").
6. **Bitiş:** Ekranda köprünün durumu, şafağa kalan süre, toplam süre, hedefler, ikmal, kalan can ve puan görünür. R ile yeniden başlanır, "Ana Menü" ile menüye dönülür.

Puan: köprü 400 + kule başına 150 + iniş 300 + 2 × kalan can (INFERRED).

| Tuş | İş |
|---|---|
| W / S | Burun aşağı / yukarı (Hafta 3 kuralı: W burnu indirir) |
| A / D | Yatış |
| Q / E | Dümen (yerde teker yönü) |
| ↑ / ↓ | Gaz (rölantide yerde fren) |
| F (ya da B) | Fren, basılı tut: havada hava freni, yerde teker freni (§11) |
| K | Kolay uçuş aç/kapa (Rota07'de açık başlar) |
| Space | Bomba; hedef kilitliyken güdümlü bomba |
| T | Sıradaki hedefi seç |
| O | Otomatik atış aç/kapa (açık başlar) |
| M | GPS ölçeği: 3,2 / 6,4 km |
| L | İniş feneri aç/kapa |
| Esc | Duraklat / menü |
| R | Görev bitince yeniden başla |

## 3. Uçuş fiziği (aerodinamik mod)
Eski kinematik model değişmedi ve varsayılan olarak kaldı (Lab 3–6 sahneleri). Rota07'de `aerodinamik = true`.

Kuvvetler: q = ½ρv², ρ = 1,225.

| Büyüklük | Değer | Not |
|---|---|---|
| Kütle, kanat alanı | 3000 kg, 24 m² | Hurricane Mk I sınıfı, yuvarlatıldı |
| Kaldırma | C_L = 0,25 + 5,0·α; tutunma açısı 16° (C_Lmax 1,65); sonrasında %55'e düşer | |
| Sürükleme | C_D = 0,025 + 0,064·C_L², tutunma ötesinde 1,2/rad ek | |
| İtki | gaz · min(12 000 N, 615 kW / v) | Pervane yaklaşık sabit güç verir |
| Hesaplanan | Tutunma hızı 34,8 m/s (125 km/sa); 70 m/s'de tırmanma ≈ 14 m/s; tam gazla azami hız ≈ 115 m/s | `Lab7UcusFizigiTests` |

Kumanda (2026-11-10'da yeniden yapıldı; kullanıcı "rahat kontrol edemiyorum" dedi):
- **Klavye → çubuk:** Tuş uçağı doğrudan döndürmez, bir kumanda çubuğunu hareket ettirir. Çubuk basılı tuşla 0,33 s'de tam sapmaya gider, bırakınca 12/s hızla ortalanır. Expo 0,35'tir: kısa dokunuş küçük, uzun basış tam kumanda verir.
- **Yunuslama = yük faktörü komutu:**
  - Çubuk, trim yükü ile tam çekişteki 4,5 g (tam itişte −1,5 g) arasında bir hücum açısı ister.
  - Koruma açıkken (`tutunmaKorumasi`) çekiş, tutunma açısının 1° altında durur. Hızlıyken g sınırı bağlar, yavaşken kanat (manevra hızı ≈ 75 m/s).
  - Burun, uçuş yolunun dönüşünü üç eksende izler; hücum açısı hatası kısa periyotla (5/s × v/60) kapanır.
- **Yatış:**
  - Kanatçık bir yatış hızı ister: pb/2V = 0,07, b = 12 m; 50 m/s'de ~33°/s, 110 m/s'de ~74°/s, en çok 100°/s. Yatış sönümü τ = 0,12 s.
  - Bırakınca yatış olduğu yerde kalır (spiral nötr). Yalnız 8°'nin altındaki yatışı dihedral yavaşça düzeltir (0,6/s).
- **Sapma:** Dikey kuyruk burnu hava akımına çevirir; dümen (Q/E) ek sapma hızı verir.
- **Trim:** Dönüş yardımı (`donusYardimi` = 1) dengelenmiş hücum açısını, o anki yatış ve tırmanma açısının gerektirdiği yük için seçer. Yatırılan uçak irtifa kaybetmeden döner (g·tanφ/v). Kaldırma her zaman gerçek hücum açısından hesaplanır; hız düşerse uçak çöker.

Ölçüm (Rota07 ayarları, `Lab7UcusTepkiOlcumu`, `Logs/lab7/ucus_tepki_once.txt` → `ucus_tepki_sonra.txt`):

| Durum | Önce | Sonra |
|---|---|---|
| 80 m/s, D 1 s basılı | 92° yatış, bırakınca 106° (sırtüstüne) | 38°, bırakınca 46° |
| 110 m/s, D 1 s basılı | 92° → 106° | 51° → 62° |
| 70 m/s, 30° yatış, 10 s eller çekili | yatış 0°'ye döndü, yön 7° değişti | yatış 37° kaldı, yön 57° değişti, irtifa −4 m |
| D'ye 0,1 s dokunuş (80 m/s) | 5,1° | 0,5° (ince düzeltme) |
| 50 m/s, S 1 s basılı | burun +16,6°, 2,0 g, AoA 14,5° | burun +16,7°, 2,0 g, AoA 14,7° |
| 80 m/s, S 1 s basılı | +16,5°, 2,9 g | +29,9°, 4,4 g (manevra hızında tam çekiş) |
| 45 m/s, rölanti, S 3 s | tutunma kaybı, AoA 27,6° | tutunma kaybı yok, AoA 15,0°; uçak çöker, hız 29 m/s'ye düşer |
| 70 m/s, eller çekili 30 s | ±4,8 m | ±4,7 m |

Yerde:
- Tekerler ağırlığı taşır; yuvarlanma sürtünmesi 0,03, rölantide fren 0,45.
- Kanatlar düz kalır, burun −1° ile +14° arasında tutulur, yanal kayma söner.
- Kalkış: pistin 356. metresinde, 48,9 m/s'de (176 km/sa) teker kesilir; 150 m'ye 32,6 s'de çıkılır (`Logs/lab7/ucus_olcumu.txt`, 2026-11-10).

İniş sınırları (Rota07):
- Hız ≤ 60 m/s, düşüş ≤ 4,5 m/s ("sert iniş"), burun hatası ≤ 16°, yatış ≤ 15°, eğim ≤ 12°.
- Su üstünde temas "suya düştü" sayılır.
- Otopilotla 3°'lik yaklaşma testi: teker koyarken düşüş 0,77 m/s, teker koyduktan sonra 247 m'de durma.

## 4. Harita: kâğıt v0.1 → gece (`GeceHaritasi`, `GeceArazisi`)
Kâğıt harita 12 × 9 kare = 2400 × 1800 m, dünyanın merkezinde duruyor. Dışarısı 8 × 8 km araziyle devam eder; sisin arkasında dağ halkası vardır. `GeceHaritasi.Kare("H5")` bir karenin dünya konumunu verir, `KareAdi(x, z)` tersini.

| Kâğıtta | Gece seviyesinde |
|---|---|
| Pist B8–D8 | 1400 × 40 m asfalt, 8. satırda, 090°/270°. D8'in doğu kenarında biter, 800 m batıya uzar (kâğıt dışına). İşaretli iniş bölgesi B8–D8'i kapsar |
| Nehir I1–I4, H5–J5, J6–J9 | Kuzeyden I sütunuyla gelen dere, köprünün altında 500 × 190 m'lik geniş göl, güneyde J sütunu. Kâğıt dışında kıvrılarak sürer |
| Köprü H5–J5, 25 m | 600 m çelik kafes köprü: 12 × 50 m Pratt açıklık, taş ayaklar, kıyı ayakları |
| Tepeler E4, F7, G7 (120 m) | Ağaçlı tepeler, çevrenin 120 m üstünde. F7–G7 bir sırt oluşturur |
| Işıldaklar F4, K5, H7 | 28 m ahşap kuleler; kâğıttaki ok yönünden başlayıp ±55° tarar |
| Uçaksavarlar G4, K4, I7 | Kum torbalı mevziler; ateş menzili 800 m |
| S = H3 | Dört borulu akustik dinleyici (dönem modeli), 1000 m |
| Bulut E5–G5 | Üç kare bulut kümesi, 140–270 m; ay ışığında gri |

Arazi ve görünüm:
- **Arazi:** Yükseklik saf bir fonksiyondur. Kâğıt alanında dalgalanma kısıktır ki 120 m'lik tepeler okunsun. Yaklaşma koridoru dardır (60 m tam, 140 m'de biter) ve 3 m daha alçaktır; böylece yaklaşmanın 200 m yanındaki F7–G7 sırtı yerinde kalır.
- **Köylerin yeri:** köprünün batı ucu (F6–G6), doğu kıyısı (L6) ve kuzeybatı (C3).
- **Yolların yeri:** havaalanından köprüye, köprünün üstünden doğuya ve iki kıyı boyunca.
- **Çalışma anı:** Arazi parçaları, orman (parça başına tek mesh) ve renk haritası (2048²) Lab 7'deki gibi üretilir. Harita değiştiği için renk haritası yeniden pişirildi (`YenidenKurDokularla`).

## 5. Kurallar ve bileşenler
| Kural | Bileşen | Değerler |
|---|---|---|
| Ana hedef, isteğe bağlı kuleler | `GeceGorevi.anaHedef` | Köprü yıkılınca İniş aşaması başlar; kule yıkılınca "söndü (+150)" |
| Şafak | `GeceGorevi.safakSuresi` 480 s, `safakUyarisi` 90 s; `Safak` | Son %28'de ay ışığı doğuda alçak bir güneşe döner (7°, 272°) ve ısınır; ambiyans, sis ve gökyüzü aydınlanır, yıldızlar söner; gökyüzü shader'ında `_Safak` parıltısı. Malzemenin kopyasıyla çalışır, asset değişmez |
| Köprünün kısmi çöküşü | `Yikilabilir.cokmeYaricapi` 60 m, `Patlama.SonNokta` | Yıkan patlamanın 60 m yakınındaki paneller düşer (en az biri). Yangın, kırılan uçtaki iki ayakta çıkar. Paneller tek tek çarpıştırıcılıdır, ayakta kalan açıklığa çarpılır |
| Bulut | `Bulut` (kare + kutu), `Isildak` | Kutunun içindeki uçak yakalanamaz; tutulan uçak buluta girince 1,4 s içinde kaybedilir |
| Ses dinleyici | `SesDinleyici` (Lab 5), `Isildak.kulak`, `UcaksavarMevzii.kulak` | 1000 m, 343 m/s gecikme, gaz < 0,15 duyulmaz. Işıldak duyulan noktaya 30°/s döner ve ±10° tarar. Top duyulan noktaya ateş eder (Lab 5 kuralı) |
| Uçaksavar | `UcaksavarMevzii` | Menzil 800 m; gece göz yalnız ışıldak tutarken ya da 250 m içinde sayılır; 0,35 s'de bir, 6 hasar; mermiler iz mermisi |
| İz mermisi | `Assets/Lab7/Prefabs/IzMermisi.prefab` | Lab 5 mermisi (değerler kopyalandı) + HDR çekirdek + 0,16 s'lik izi (TrailRenderer). Lab 5 prefabı değişmedi |
| HUD | `GeceHud` | Sol üstte köprü, ışıldak, bomba, can, puan ve "ŞAFAK m:ss". Ortada görev ve uzaklık. Uçuş satırlarında bulut içindeyken "BULUT F5 — ışıldak göremez". Duyulan yerde kırmızı kare "DUYULDU H5". Uyarılar: tutunma, ışıldak, yer, ses dinleyici, şafak |

## 6. Dosyalar
**Çalışma anı scriptleri (Runtime):**
- Yeni: `Bulut`, `Safak`, `AnaMenu`, `DuraklatmaMenusu`, `MenuArayuzu`, `MenuDugmesi`, `OyunAyarlari`.
- Değişen:
  - `GeceHaritasi`: kâğıt yerleşimi.
  - `GeceGorevi`: ana hedef, şafak.
  - `GeceHud`: göstergeler, şafak, bulut, öncü işaret.
  - `GeceSonuEkrani`: köprü ve şafak satırı, Ana Menü düğmesi.
  - `Isildak`: bulut, ses.
  - `Yikilabilir`: kısmi çöküş.
  - `Patlama`: son nokta.
  - `Bolum3Harita`: kare ↔ dünya.
  - `BombaYuku` ve `UcakIsiklari`: duraklatmada tuş yok sayılır.

**Editör (Editor):**
- `Lab7SceneBuilder`, `Lab7Yapilar` (köprü, kuleler, toplar, ses dinleyici, bulut), `Lab7Varliklar` (iz mermisi, bulut malzemesi).
- `Lab7Arayuz` (göstergeler ve duraklatma menüsü), `MenuSceneBuilder`, `BuildCommands.BuildWindowsOyun`.

**Varlıklar:**
- `Assets/Lab7/UI/gece-hiz-kadrani.png`: Blender `build_ui_assets.py night_speed`, kaynağı `Exports/Lab7_UI/`.
- Gökyüzü shader'ına `_Safak` eklendi.

**Testler:**
- EditMode: `Lab7Tests` (kâğıt yerleşimi dahil), `MenuTests`, `Lab7YardimTests` (§11).
- PlayMode: `Lab7PlayTests`, `Lab7Bolum3PlayTests`, `MenuPlayTests`, `Lab7YardimPlayTests` (§11).

**Yardımlar (2026-11-10, §11):** Runtime `UcusYardimi`, `UcusYardimcisi`, `NisanSistemi`, `GpsHaritasi`; değişen `UcusKontrol` (kolay uçuş, fren, dış sınırlar), `UcakAerodinamigi` (hava freni), `Bomba` (güdüm), `BombaYuku` (güdüm kancası), `GeceHud`, `GeceGorevi.Bildir`, `AnaMenu` (tuş tablosu). Editor `Lab7Yardim`.
- Elle çalıştırılan görsel kayıtlar: `Lab7GorselKayit` → `Logs/lab7/gorsel/`, `MenuGorselKayit` → `Logs/lab7_menu/gorsel/`.

## 7. Ölçümler (2026-11-10)
| Ölçüm | Sonuç | Kaynak |
|---|---|---|
| Köprü, ortanın 100 m doğusuna bir bomba | 12 panel düştü, 8 s sonra güvertenin ortalama 27,4 m altında (nehirde); uzaktaki 30 panel kıpırdamadı | `Logs/lab7/ucus_olcumu.txt` |
| Bulut | 70 m'de ve 330 m'de yakalandı; buluta (200 m) girince 1,4 s'de kaybedildi | `Logs/lab7/bolum3_olcumu.txt` |
| Ses dinleyici | 414 m'de duydu; F4'ün başı 3 s'de 94°'den 69°'ye döndü; toplar 3 s'de duyulan yere 8 mermi attı; gaz 0,1'de sessiz | aynı |
| Şafak | Kısaltılmış gecede, gecenin %86'sında harman 0,50; şafakta 1,00 ve görev bitti | aynı |
| İniş | Otopilotla 3°'lik yaklaşma: düşüş 0,77 m/s, teker koyduktan sonra 247 m'de durma, puan 1350 (yalnız iniş testi; toplar kapalı) | `ucus_olcumu.txt` |
| Player | `Builds/Windows_Oyun` 15 s açık kaldı, günlükte hata yok | `Logs/lab7_bolum3/player_oyun.log` |

## 8. Menü ve göstergeler (kullanıcı isteği, 2026-11-10)
**Ana menü** (`AnaMenu` sahnesi):
- Arka plan ve düğmeler Lab 4'teki Blender görselleri: `menu-arkaplan.png` ve `dugme-seti.png`. Düğme setinin dört satırı düğme durumlarıdır: normal, üzerinde/seçili, basılı, devre dışı.
- Paneller:
  - Ana panel: Göreve Başla (Rota07), Bölüm Seç, Seçenekler, Kontroller, Çıkış.
  - Bölüm Seç: Hafta 2–7'nin altı sahnesi; build'de olmayan sahne devre dışı görünür.
  - Seçenekler: grafik (DÜŞÜK/YÜKSEK), ekran (tam ekran/pencere), ses (%100…%0). Ayarlar PlayerPrefs'e kaydedilir.
  - Kontroller: tuş tablosu.
- Kullanım: fare ya da ↑/↓ + Enter; Esc alt panelden geri döner. Türkçe büyük harf kullanılır ("SINIR HATTI", "GRİ KUTU").

**Duraklatma menüsü** (Esc):
- Seçenekler: Devam, Yeniden Başla, Ana Menü, Çıkış. Menü açıkken `Time.timeScale` 0 olur ve ses durur.
- Rota07'de sahneye gömülüdür. Eski lab sahnelerinde ise ana menünün oluşturduğu kalıcı kopya çalışır; böylece o sahneler hiç değişmeden Esc ile menüye dönebilir.
- Bir sahne yüklenince oyun her zaman devam ettirilir.

**Göstergeler** (Rota07):
- **Hız kadranı:** 0–500 km/sa. Kâğıt dokulu, pirinç çerçeveli, Lab 4 stilinde; Blender'da üretildi. Yeşil bant 150–420 km/sa, kırmızı bant 420 km/sa üstü. İbre Lab 4'ün ibresi.
- **Gaz kolu:** Lab 4'ün plakası ve topuzu.
- İkisinin altında dijital okuma var: "254 km/sa", "GAZ %70".
- Uçuş metni göstergelerin sağına kaydı. Görev bitince göstergeler gizlenir.

**Player:**
- `BuildWindowsOyun` → `Builds/Windows_Oyun/NU.D.36.exe`, menüyle açılır.
- `BuildWindowsRota07` → doğrudan gece görevi.

## 9. Kâğıt adımları ve teslim (kullanıcıya kalanlar)
- **Föy 01–04** (ısınma, tarayıcıdaki "3 · Gece" kipi, eşe oynatma, gözlem, v0.2): Kit ve tarayıcı oyunu bende yok; eşle oynamak senin işin. Basılı formlar: `Exports/Lab6/kagit/bolum3_kagit_formlari.html`. Tarayıcıda açıp Yazdır → PDF yaparsın. İçinde:
  - görev sayfası, v0.1 değerleriyle dolu;
  - gözlem formu, test sorusu ve sürüm yazılı;
  - yineleme kaydı.
- **v0.2 değişikliği:** Gözleme göre seçtiğin tek değişiklik `Bolum3Harita.cs`'de tek satırdır. Gri kutu ve gece seviyesi aynı veriden kurulur (`NU36 → Lab 6 → Create…`, `NU36 → Lab 7 → Rebuild…`).
- **Föy 06 (GDD):** İçerik `Documentation/Lab6_GDD_Taslagi.md`'de: 7. hafta bölümü ve kâğıt → Unity değişiklik günlüğü. Word şablonuna aktarıp PDF yapmak senin işin.
- **Teslim zip'i:** `Tools/Teslim/teslim_zip.ps1 -OgrenciNo <no> -GddPdf <pdf>` → `Teslim/BMU1421_<no>_H6-7.zip` (Assets, ProjectSettings, Packages ve GDD).
- **Yalnız gece görevi zip'i (2026-11-10, kullanıcı isteği):** `Game Folder/NU.D.36_GeceGorevi.zip` (5,35 MB, çalışma alanının dışında). İçinde kırpılmış bir Unity projesi var: yalnız AnaMenu ve Rota07 ile bunların GUID ve tür bağımlılıkları (~120 varlık), `AircraftModelPostprocessor`, Packages, ProjectSettings (build listesi: AnaMenu, Rota07) ve `BENIOKU.txt`. Önceki lab sahneleri, testler, sahne üretici editör betikleri, Library ve build'ler yok. Doğrulama zip'ten açılan kopyada yapıldı: derleme hatasız; iki sahnede eksik script ya da kopuk referans yok; Windows build başarılı, player 20 s hatasız çalıştı. PlayMode duman testi de geçti: menü → GÖREVE BAŞLA → Rota07, yalnız gazla kalkış, 30 s'de +53 m.
- **Elle uçuş hissi:** Bombalama, bulutun içinden geçiş, şafaktan önce iniş: NOT TESTED (otopilot ve PlayMode testleri var, insan eli yok).

## 10. Sınırlar
- Ses yok: ses dinleyici bir kuraldır, oyunda ses efekti yoktur.
- Ağaçların çarpışma kutusu yok.
- Tepeler yamaçları dik birer "mesa" gibi görünüyor (kâğıttaki kare tepe biçimi korundu).
- Kâğıttaki can (3) ve bomba (2) sayıları gerçek zamanlı oyuna birebir taşınmadı. Değişiklik günlüğünde var.

## 11. Yardımlar: kolay uçuş, fren, otomatik vuruş, GPS (kullanıcı isteği, 2026-11-10)
Kullanıcı: "Uçağı halen rahat kontrol edemiyorum … fren mekanizmasına ve daha rahat bir uçuş deneyimine ihtiyacım var … menzile girdiğimde kuleyi otomatik vuracak şekilde ve hedefleri net gösteren bir GPS gibi bir şeye ihtiyacım var." Kararlar: DecisionLog D-095 … D-100.

Hepsi Rota07'de açık başlar. Föyün zorluğu (dar hedef, gerçekçi uçuş) iki tuş uzaktadır: **K** kolay uçuşu, **O** otomatik atışı kapatır. Föyün notlandırdığı özelliklerin hiçbiri değişmedi (gece, ışıldak, köprü, kazanma/kaybetme, şafak, üsse dönüş, iz mermisi, çöküş).

### 11.1 Kolay uçuş (`UcusKontrol.kolayUcus`, `UcusYardimi`)
Fizik aynı kaldı (kaldırma, sürükleme, itki, tutunma, yer). Değişen yalnız tuşların ne istediği:

| Tuş | Kolay uçuşta | Bırakınca |
|---|---|---|
| A / D | Yatış açısı ister: en çok 60°; yavaşken kanadın taşıyabildiği kadar (n ≤ 0,85·n_azami) | Kanatlar düzelir (ters dönmüşse de) |
| W / S | Uçuş yolu açısı ister: W en çok 45° dalış, S en çok 20° tırmanış | Düz uçuş (irtifa korunur) |
| ↑ / ↓ | Gaz (aynı) | — |

- **Hız koruması:** 1,25·V_s (≈ 157 km/sa) altında tırmanış izni azalır, daha yavaşta uçak hafif alçalarak hız kazanır. 1,3·V_s (≈ 163 km/sa) altında motor kendiliğinden güç ekler (HUD: "HIZ KORUMASI"). Tutunma kaybı olmaz.
- **Kalkış:** Gaz %50'nin üstündeyken 1,35·V_s'de (≈ 170 km/sa) burun kendiliğinden kalkar. Yani yalnız ↑ ile kalkılır.

### 11.2 Fren (`UcusYardimcisi`, F ya da B basılı)
- **Havada:** hava freni, ΔC_D = 0,12 (100 m/s'de ≈ 5 m/s², 60 m/s'de ≈ 1,8 m/s²). Kolay uçuşta 1,4·V_s (≈ 175 km/sa) altında kendiliğinden kapanır.
- **Yerde:** teker freni (μ 0,45) her gazda çalışır; eskiden yalnız rölantide çalışıyordu.

### 11.3 Korumalar (`UcusYardimcisi`, her karede uçuştan önce)
| Koruma | Kural | HUD |
|---|---|---|
| Yer ve yapı tabanı | Önündeki 6 s'de (en az 400 m) arazinin 50 m, kule/köprü/evin 25 m üstünde kalır; yalnız yükselen zemin sayılır; gerekirse 25°'ye kadar tırmanır ve tam güç verir | "YER KORUMASI" |
| Alçalma zarfı | Yere yakın düşüş hızı ≤ 1 + √(4·h) m/s: uçak kendisi yumuşar. 15 m altında 57 m/s'den hızlıysa alçalmaz, yavaşlamasını bekler (fren yardım eder) | — |
| Yalnız piste teker | Havaalanı kutusunda (pist ± 2500 m, ± 150 m) gaz < %70 iken: pistin üstünde (kenardan 25 m, eşikten 150 m önce) teker koyabilir, başka yerde 10 m'de tutulur | "PİSTE HİZALAN" |
| Kendiliğinden teker koyma | Pistin üstünde 15 m altında uçak yere iner (hız koruması onu havada tutmasın diye) | "OTOMATİK İNİŞ" |
| Pas geçme | Kutuda gaz ≥ %70: kalkış ya da pas geçme sayılır, taban yeniden 50 m | — |
| Yere yakın yatış | 20 m altında yatış ≤ 10° | — |
| Harita sınırı | Merkezden 2300 m ötede dışarı uçarken kendiliğinden merkeze döner (dağlar 2800 m'de başlar) | "HARİTA SINIRI" |

### 11.4 Nişan sistemi ve otomatik vuruş (`NisanSistemi`, `Bomba.Gudumle`)
- **Hedef seçimi:** Ayakta duran hedeflerden (3 ışıldak kulesi + köprü) en yakını, öndekine ağırlık verilerek. T bir sonrakini seçer; seçilen, yıkılana kadar kalır.
- **Kilit:** 1600 m içinde, uçuş yönünün ±60° içinde ve arazi görüşü kesmiyorsa (Hafta 5 görüş hattı kuralı). Durumlar: HEDEF (T) → BURNU HEDEFE ÇEVİR / GÖRÜŞ YOK → KİLİTLİ → MENZİLDE — ATEŞ → BOMBA YOLDA.
- **Güdümlü bomba:** Kilitliyken bırakılan bomba 160 m/s'ye hızlanır, en çok 150°/s döner. Hedefin üstüne nişan alarak yükseklikte kalır (kalan yatay mesafenin %30'u, en çok 120 m), son anda tepeden dalar. Hedef ondan önce yıkılırsa serbest düşer. Uçağa zararı yalnız patlamanın 120 m yakınındaysa olur.
- **Otomatik atış (O, açık başlar):** Kilitli hedef 1200 m'ye girince bomba kendiliğinden atılır. Her hedefe uçuşta bir bomba gider; iki atış arasında en az 2 s vardır. Bir hedef yıkılınca menzildeki sıradaki hedefe hemen geçer.
- **Değişmeyenler:** 4 bomba, pist üstünden alçak geçişle ikmal, kilit yokken Space ile serbest düşen Lab 5 bombası.

### 11.5 GPS (`GpsHaritasi`, HUD sağ alt)
- **Harita:** Kuzey yukarıda, uçak ortada; arazinin pişirilmiş renk haritası, kâğıt ızgarası (A–L × 1–9, 200 m).
- **Çizilenler:** pist, iniş aşamasında yeşil bölge; kuleler turuncu, köprü sarı, yıkılan gri. Seçili hedef halka içinde: kilitliyken sarı, menzildeyken kırmızı, yanıp söner; uçaktan ona rota çizgisi.
- **Tehlikeler:** uçaksavarların 800 m, ses dinleyicinin 1000 m menzil halkası.
- **Yazılar:** üstte kare, yön ve ölçek (M: 3,2 / 6,4 km); altta hedef, uzaklık, kerteriz ve durum. İniş aşamasında iniş bölgesine uzaklık ve pist yönü.
- **3B HUD:** Seçili hedefin işareti büyür, köşeli parantez ve durum yazısı alır.

### 11.6 Ölçümler (`Logs/lab7/yardim_olcumu.txt`, 2026-11-10)
| Ölçüm | Sonuç |
|---|---|
| Yalnız gazla kalkış (hiç W/S yok) | 14,7 s'de 185 km/sa'da teker kesti; 25 s sonra yerden 58 m yüksekte, 329 km/sa |
| Tepeye doğru 25 s W basılı (E4, 120 m) | Çarpma yok; yere en çok 35 m yaklaştı |
| Klavye tarzı iniş (W bas/bırak, F, A/D ile hizalama) | Teker koyuşta düşüş 2,49 m/s (sınır 4,5); eşikten 181 m sonra yere değdi, 269 m'de durdu; görev tamamlandı, 1350 puan |
| Harita sınırı (eller çekili, batıya doğru) | Kendiliğinden döndü; merkeze en çok 2676 m uzaklaştı (dağlar 2800 m'de) |
| F4 kulesine kuzeyden uçuş | 1600 m'de kilit, 1200 m'de kendiliğinden güdümlü bomba, tek bombayla yıkıldı. Hemen ardından menzildeki köprüye atış |
| Köprüye nehir boyunca uçuş | Tek güdümlü bombayla yıkıldı (12 panel), iniş aşaması başladı |

### 11.7 Testler ve durum
- **EditMode `Lab7YardimTests` (12 test):**
  - yasa fonksiyonları;
  - tuş bırakınca kanat düzelir ve irtifa korunur; tırmanış/dalış komutu;
  - yavaşta tam çekişle tutunma yok; ters uçuştan düzelme; dış tabanın W'yi yenmesi;
  - kendiliğinden kalkış; hava freni ve kendiliğinden kapanması; hız koruması;
  - her gazda teker freni; Rota07 kablolaması; GPS dönüşümleri.
- **PlayMode `Lab7YardimPlayTests` (8 test):** §11.6'daki ölçümler, Space'in güdümlü/serbest kuralı, GPS işaretleri.
- **Eski Lab 7 testleri:** `Lab7PlayTests` ve `Lab7Bolum3PlayTests` yardımları kapatıp (`YardimlariKapat`) föy modelini ölçmeye devam eder.
- **Durum:** IMPLEMENTED ve testle VERIFIED. Elle klavye hissi, GPS'in okunurluğu ve gece ekranındaki görünüm: KULLANICI (manuel test).
