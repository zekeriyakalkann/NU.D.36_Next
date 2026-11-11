# Lab 8 (Hafta 8): Bölüm 4 · Kurşun Dağları — Plan

Tarih: 2026-11-11. Durum: **Fazlar 1–5 tamamlandı** (11.11.2026); ayrıntılar ve plandan sapmalar `Lab8_Implementation.md`'de. Kullanıcıda: video, zip, OYS.

Kaynak: Konuşmaya yapıştırılan "Laboratuvar Föyü: Bölüm 4 · Kurşun Dağları" metni. Teslim: **15.11.2026 Pazar 23:59**.

Sınıflar:
- **CONFIRMED:** Föyde yazıyor.
- **INFERRED:** Föyde yazmıyor; bizim uygulama kararımız. Gerekçesi yanında yazılı.
- **UNKNOWN:** Föyden çıkmıyor; engel olup olmadığı yanında yazılı.

## 1. Elimizdeki malzeme
| Öğe | Durum | Ne yapılacak |
|---|---|---|
| Föyün metni (adımlar 01–09, puanlama, teslim) | Var | Tek kaynak |
| Haritanın çizimi | Yalnız yazıları geldi: bölge adları, sayılar, kalkış, güvenli bölge, üs, 0:00–5:00 çizgisi | Ray şekli ve bölge zamanları INFERRED (§4). Engel değil |
| GDD §9.3 (oyunda insan yok), §17 (arma ve bayrak yok) | Kurs GDD'si çalışma alanında yok | Föydeki iki kural olduğu gibi uygulanır |
| Asset'ler (tren, ray, arazi yüzeyi, tank, düşman uçağı) | Yok. Kullanıcı indirecek (§7) | Gelene kadar yer tutucularla geliştirilir. Model gelince yer tutucunun yerine geçer |
| "GÖREV İLERLEMESİ 0 / 19" | Föydeki 19 kontrol kutusunun sayacı | Oyun özelliği değil. 19 madde §3'teki REQ'lere bağlandı |

## 2. Kullanıcı kararları (2026-11-11)
1. **Asset'leri kullanıcı indirir.** Ben bağlantıları ve lisansları listelerim (§7), aktarırım, ölçeklerim ve `VARLIKLAR.md`'ye yazarım. Not: Kenney ve Poly Pizza otomatik indirmeye 403 veriyor.
2. **Arazinin biçimi bizden, yüzeyi asset'ten.** Dağ yükseklikleri ray hattına göre üretilir (Unity Terrain). Kaya ve yamaç dokuları Poly Haven'dan (CC0), kaya ve uçurum modelleri CC0 paketlerden gelir.
   - Risk: Değerlendirici arazinin biçimini de hazır asset'ten bekleyebilir. Bu seçim `VARLIKLAR.md`'de ve GDD notunda açıkça yazılacak.
3. **Üsse varmak = üs bölgesine alçaktan girmek.** İniş gerekmez. Hafta 5'teki `UsseVarildi` tetikleyicisi bunun örneği.
4. **Rahat uçuş ve hedefi kolay vurma.** Bölüm 3'ün yardımları taşınır (kolay uçuş, fren, korumalar, güdümlü bomba, GPS). Yanına makineli nişan yardımı ve otomatik atış eklenir (§5.6). Hepsi açık başlar ve tuşla kapatılır. Föyün kuralları (cephane, irtifa, hasar türleri) yardımlar açıkken de geçerlidir.

## 3. Gereksinimler (föy → uygulama → test)
| REQ | Föy | Sınıf | Uygulama | Test |
|---|---|---|---|---|
| REQ-801 | 01: Tren (lokomotif ve en az 3 vagon), ray, sarp dağlık arazi, tank, düşman uçağı; hepsi `Assets/Bolum4` içinde | CONFIRMED | Kullanıcının indirdiği modeller `Assets/Bolum4/Modeller/…` klasörüne gider. Arazi yüzeyi `Assets/Bolum4/Dokular` | TEST-801 |
| REQ-802 | 01: Boylar gerçeğe yakın, lokomotif ≈ 15 m | CONFIRMED | Modeller ölçeklenir: lokomotif 15 m, vagon 12–14 m, tank 7 m, uçak kanat açıklığı 10–12 m | TEST-802 |
| REQ-803 | 01: Her asset için `Assets/VARLIKLAR.md`'de bir satır (varlık, kaynak bağlantısı, lisans, değiştirildi mi); CC BY ise yazarın adı | CONFIRMED | Dosya, model aktarılırken doldurulur | TEST-803 |
| REQ-804 | 01: Gerçek bir devletin arması, bayrağı, sembolü yok | CONFIRMED | Aktarırken her model gözle denetlenir. Varsa malzeme ya da boyayla kapatılır ve kayda yazılır | Görsel kontrol (KULLANICI onayı) |
| REQ-805 | 02: Yeni sahne; dağlar arasında tek ray; bir uçta kalkış istasyonu, öbür uçta güvenli bölge, kalkışın yakınında üs | CONFIRMED | Sahne `Bolum4_KursunDaglari`, `Lab8SceneBuilder` ile kurulur (§4) | TEST-804, üstten görüntü |
| REQ-806 | 02: Havadan bakınca ray kesintisiz | CONFIRMED | Ray, eğri boyunca 4 m'lik parçalarla döşenir; boşluk denetlenir | TEST-805 |
| REQ-807 | 02: Bölüm 3'teki uçak trenin üstünde, havada başlar; kontroller ve kamera Bölüm 3'teki gibi | CONFIRMED | Sahne Rota07'nin kopyasıdır: uçak, kamera, girdi ve yardımlar aynen gelir. Uçak lokomotifin 150 m üstünde, ray yönünde ve 220 km/sa hızla başlar | TEST-806 |
| REQ-808 | 03: Tren rayda ilerler, virajda raydan çıkmaz, vagonlar lokomotifi izler | CONFIRMED | `RayHatti` (eğri ve yay uzunluğu) ile `Tren` (her araç iki boji noktasından yerleşir) | TEST-807, TEST-808 |
| REQ-809 | 03: Düşmanlar kapalıyken 5 dk ±15 sn'de varış | CONFIRMED | Hız profili yay uzunluğundan hesaplanır: kalkış ivmesi + seyir + duruş = 300 s | TEST-809 |
| REQ-810 | 03: Trenin canı var | CONFIRMED | `Saglik` trenin kökünde. Vagonların çarpıştırıcıları aynı canı paylaşır | TEST-810 |
| REQ-811 | 04: Üç ateş bölgesi; toplam 4 tank, 4 uçaksavar, iki dalgada 4 düşman uçağı | CONFIRMED | Bölge 1: 2 tank, 1 uçaksavar. Bölge 2: 2 uçaksavar, 2 uçak (1. dalga). Bölge 3: 2 tank, 1 uçaksavar, 2 uçak (2. dalga) | TEST-811 |
| REQ-812 | 04: Tren yaklaşınca ortaya çıkarlar; aralarda kısa sakinlik | CONFIRMED | `AtesBolgesi` trenin ray üzerindeki uzaklığıyla etkinleşir (§4) | TEST-812 |
| REQ-813 | 04: Tank trene, uçaksavar uçağa, düşman uçağı ikisine ateş eder; ateş görülür | CONFIRMED | Her atışta namlu parlaması ve iz mermisi | TEST-813 |
| REQ-814 | 04: Her düşman türü bir prefab'tan gelir | CONFIRMED | `Tank.prefab`, `Ucaksavar.prefab`, `DusmanUcagi.prefab` | TEST-814 |
| REQ-815 | 04: Vurulan tank ve uçaksavar susar ve tüter; vurulan düşman uçağı dumanla geri çekilir, düşmez; susan bir daha ateş etmez | CONFIRMED | `Susturulabilir` bileşeni: canı bitince ateşi kapatır, duman çıkarır, uçağı çekilme durumuna sokar | TEST-815 |
| REQ-816 | 04: Oyunda insan yok | CONFIRMED | Modellerde insan figürü olmaz. Varsa aktarırken kaldırılır | Görsel kontrol |
| REQ-817 | 05: Düşman ateşi treni ve uçağı vurur | CONFIRMED | Mermi `Saglik.Vur` ile hasar verir (mevcut düzen) | TEST-816 |
| REQ-818 | 05: Uçaksavar yalnız alçaktaki uçağa ateş eder; yüksekte güvendesin ama oradan tankı vuramazsın | CONFIRMED (kural); INFERRED (sınır) | Uçaksavar, uçak topun 300 m üstünü geçince susar. Bomba yerden 300 m'nin üstünde bırakılamaz (HUD: "ÇOK YÜKSEK") | TEST-817 |
| REQ-819 | 05: Tren canı bitince "Tren durdu", uçak canı bitince "Vuruldun"; bölüm baştan başlar | CONFIRMED | Yazı 4 s görünür, sonra sahne yeniden yüklenir (R ile hemen) | TEST-818 |
| REQ-820 | 06: 1 200 mermi, 2 bomba; sayılar ekranda ve her atışta azalıyor; sıfırda silah ateş etmiyor | CONFIRMED | `Makineli` (yeni), `BombaYuku` (kapasite 2) | TEST-819 |
| REQ-821 | 06: Tank yalnız bombayla susar; tanklar ikişer durur; iyi bırakılan bomba ikisini birden susturur | CONFIRMED | Tank canı yalnız patlamadan düşer. Çift aralığı 16 m, bomba yarıçapı 25 m | TEST-820 |
| REQ-822 | 06: Uçaksavar ve düşman uçağı makineliyle susar | CONFIRMED | Makineli mermisi bunlara hasar verir | TEST-821 |
| REQ-823 | 07: Her atışta namlu parlaması ve ileri giden mermi izi | CONFIRMED | Kanattaki iki namluda parlama ve iz mermisi | TEST-822 |
| REQ-824 | 07: Motorun arkasından ince, yarı saydam siyah duman; vuruldukça koyulaşırsa daha iyi | CONFIRMED (artı puanlı kısım dahil yapılır) | `MotorDumani`: can azaldıkça yoğunluk ve opaklık artar | TEST-823 |
| REQ-825 | 08: Ekranda hep 5 bilgi: uçağın canı, trenin canı, mermi ve bomba, güvenli bölgeye kalan süre, o anki görev | CONFIRMED | `Bolum4Hud` | TEST-824 |
| REQ-826 | 08: Tren güvenli bölgeye girince görev "Üsse dön" olur | CONFIRMED | `KursunDaglariGorevi` | TEST-825 |
| REQ-827 | 08: Üsse varınca "Görev tamam": trenin kalan canı, susturulan ateş kaynağı, kalan cephane | CONFIRMED (ekran); karar 3 (varış = üs bölgesine alçaktan giriş) | Üs bölgesi: üssün 500 m çevresi, yerden 200 m altı | TEST-826 |
| REQ-828 | 08: Baştan sona bir tur: kalkış, üç bölge, güvenli bölge, üs, Görev tamam | CONFIRMED | Tam tur testi | TEST-827; elle oynanış KULLANICI |
| REQ-829 | 09: Oynanış videosu, en çok 7 dk; üç bölge, bir hasar anı ve Görev tamam görünür; `Video/bolum4.mp4` | CONFIRMED | KULLANICI kaydeder. Kayıt yönergesi verilir | — |
| REQ-830 | Teslim: zip'te Assets, ProjectSettings, Packages ve `.meta` dosyaları; kullanılmayan asset'ler silinmiş; `BMU1421_<No>_H8.zip` | CONFIRMED | §10: kırpılmış teslim kopyası. Çalışma alanından hiçbir şey silinmez | TEST-828 |
| REQ-831 | Kullanıcı: rahat uçuş ve hedefi kolay vurma | Kullanıcı isteği (föy dışı) | §5.6 | TEST-829 |

UNKNOWN (engel değil):
- Bölgelerin 5 dakikadaki tam zamanları, rayın şekli, üssün tam yeri: Çizim gelmedi; §4'teki varsayılanlar kullanılır.
- Kalkışta tren canının ve düşman değerlerinin sayısı: Föyde yok; §6'daki değerler INFERRED.

## 4. Harita ve zaman çizelgesi (INFERRED)
**Alan:** 8 × 8 km arazi. Oyun alanı yaklaşık 4,5 × 4 km; çevresi geçilmez yüksek dağlar. Öğleden sonra ışığı ve vadilerde hafif pus. Föy gece demiyor; Bölüm 3 geceydi, bu bölüm gündüz (INFERRED).

**Ray:** Yaklaşık 5,0 km, tek hat. At nalı biçimindedir: güvenli bölge, üsse ~2,5–3 km uzakta kalır. Böylece dönüş ~40 s sürer ve video 7 dakikanın altında kalır.

| Ray (m) | Zaman | Yer | İçerik |
|---|---|---|---|
| 0 | 0:00 | Kalkış istasyonu (güneybatı) | Peron, depo binası. Üs (kısa pist ve hangarlar) istasyonun ~700 m batısında |
| 0–1000 | 0:00–1:05 | Etekler | Sakin. Tren hızlanır |
| 1000–1800 | ~1:05–1:50 | **1 · Dar Vadi** | Dik duvarlı dar vadi (duvarlar 250–400 m). 2 tank vadi yanındaki yolda, 1 uçaksavar sırtta |
| 1800–2300 | ~1:50–2:20 | — | Sakinlik |
| 2300–3000 | ~2:20–3:00 | **2 · Viyadük** | Derin boğazın (~120 m) üstünde 400 m'lik kemerli taş viyadük. 2 uçaksavar boğazın kenarında; 1. dalga: 2 düşman uçağı |
| 3000–3600 | ~3:00–3:30 | — | Sakinlik |
| 3600–4400 | ~3:30–4:15 | **3 · Son Viraj** | Dağ yamacında keskin viraj (R ≈ 300 m). 2 tank virajın içinde, 1 uçaksavar; 2. dalga: 2 düşman uçağı |
| 4400–5000 | 4:15–5:00 | Güvenli bölge | Tren yavaşlar ve tahkimli depoda durur. Görev "Üsse dön" olur |

- Hız: seyir ≈ 17,9 m/s (64 km/sa). 20 s'lik kalkış ivmesi ve 20 s'lik duruşla toplam 300 s.
- Bölge etkinleşmesi: Lokomotif bölgenin başına 600 m kala düşmanlar ortaya çıkar. HUD duyurur: "1 · DAR VADİ — 2 tank, 1 uçaksavar".
- Düşman uçakları bölgenin dışından gelir.

## 5. Sistemler
### 5.1 Sahne
- `Lab8SceneBuilder` (Editor) Rota07'yi `Assets/Scenes/Bolum4_KursunDaglari.unity`'ye kopyalar ("Save As" düzeni, Lab 5–7 gibi).
- Gece dünyası silinir. Kalanlar: uçak, kamera, girdi, yardımlar, hız ve gaz göstergeleri, duraklatma menüsü.
- Eklenenler: gündüz ışığı ve pus, arazi, ray, tren, istasyonlar, üs, bölgeler, HUD.
- Önceki sahneler bayt bayt aynı kalır (MD5 testi). Sahne Build Settings'in sonuna eklenir.
- Ana menüde değişenler (INFERRED, küçük): "Bölüm Seç" listesine Bölüm 4 eklenir, "GÖREVE BAŞLA" Bölüm 4'ü açar.

### 5.2 Arazi ve ray
- **Arazi (`KursunDaglariArazisi`, Editor):**
  - Unity Terrain, 8 km, 1025² yükseklik çözünürlüğü.
  - Yükseklik: sırt gürültüsüyle sarp dağlar. Ray koridoru oyulur: tren yatağı düz, iki yanı yamaç. Viyadüğün altına boğaz, istasyon, güvenli bölge ve üs için düzlük.
  - Doku katmanları (Poly Haven): dik yamaçta kaya, düzlükte çimen ve kaya, ray yatağında çakıl.
  - Kaya ve uçurum modelleri CC0 paketlerden.
- **`RayHatti` (Runtime):** Kontrol noktalarından Catmull-Rom eğrisi ve yay uzunluğu tablosu. `Nokta(s)`, `Yon(s)`, `Uzunluk` verir.
- **Ray döşeme (Editor):** Ray asset'inin düz parçası eğri boyunca 4 m aralıkla dizilir. Parçalar 250 m'lik birleşik mesh'ler halinde toplanır.
- **Viyadük:** Kemerli taş viyadük kendi üreticimizle yapılır (Lab 7 köprüsü gibi). Hedef değildir, yıkılmaz.

### 5.3 Tren
`Tren` bileşeni:
- Lokomotif ile 3 vagon (ya da daha fazlası) ray üzerindeki uzaklıklarıyla ilerler.
- Her aracın ön ve arka bojisi eğri üstündedir. Araç bu iki noktaya hizalanır, böylece virajda raydan çıkmaz.
- Can: 100.
- Can biterse tren 6 s'de yavaşlayıp durur, kara duman çıkar ve "TREN DURDU" yazar.
- Olaylar: `GuvenliBolgeyeVardi`, `Durdu`. Özellikler: `KalanSure`, `Ilerleme`.

### 5.4 Düşmanlar
Her düşman türü tek prefab'tır. Canı biten düşman `Susturulabilir` ile susar: ateş bileşenleri kapanır, duman çıkar, sayaç artar.
- **Tank:**
  - Gövde ve kule (asset). Kule trenin en yakın vagonuna döner.
  - 900 m içinde 4 s'de bir mermi atar: namlu parlaması ve görünür iz.
  - Can yalnız patlamadan düşer; makineli mermisi kıvılcım çıkarır ama hasar vermez.
  - Susunca kulesi eğilir, tüter.
- **Uçaksavar:**
  - Lab 7'deki mevzi modeli ve `UcaksavarMevzii` kullanılır.
  - Gündüz göz (Radar) ve görüş hattı ile ateş eder. Yeni `atesTavani` ayarı 300 m (varsayılanı kapalı; eski sahneler etkilenmez).
  - Makineliyle susar.
- **Düşman uçağı:**
  - Kinematik yapay zekâ (fizik değil): giriş → saldırı → sıyrılma → yeniden saldırı.
  - Hedef seçimi: oyuncu yakınsa ve öndeyse oyuncu, değilse tren.
  - Atış: hedef ±8° konide ve 500 m içindeyse ateş eder.
  - Yerden en az 80 m yüksekte kalır.
  - Canı bitince ateşi keser, duman izi bırakarak tırmanır ve haritadan çıkar. Düşmez.

### 5.5 Oyuncunun silahları ve hasar
- **Makineli (`Makineli`, yeni):**
  - Ateş tuşu: Sol Ctrl ya da farenin sol tuşu.
  - 1 200 mermi, iki kanat namlusu sırayla. Toplam 16 atım/s, yani yaklaşık 75 s'lik ateş.
  - Mermi hızı: 600 m/s artı uçağın hızı; menzil 1 000 m; hasar 10.
  - Her atışta namlu parlaması ve iz mermisi (Lab 7 iz mermisinin oyuncu rengi).
  - Mermi bitince ateş etmez; HUD "MERMİ YOK" der.
- **Bomba:**
  - `BombaYuku` kapasitesi 2; ikmal yok.
  - Bomba yarıçapı 25 m, hasar 100. Tankı yalnız bu düşürür.
  - Yerden 300 m'nin üstünde bırakılamaz.
- **Hasar:** Uçak canı 100. Uçaksavar ve düşman uçağı mermileri uçağı, tank ve düşman uçağı mermileri treni vurur. Oyuncunun mermisi treni vurmaz.
- **Motor dumanı (`MotorDumani`):** Motorun egzozundan ince, yarı saydam siyah duman. Can azaldıkça yoğunluğu ve opaklığı artar; %30'un altında kalınlaşır.
- **Çakılma:** Yere çakılınca "ÇAKILDIN" yazar ve bölüm baştan başlar (INFERRED). Yer koruması açıkken nadir olur.

### 5.6 Yardımlar (karar 4)
| Tuş | Yardım | Bölüm 4'te |
|---|---|---|
| K | Kolay uçuş ve korumalar | Aynen. Harita sınırı yeni haritaya göre; "yalnız piste teker" üsse göre |
| F/B | Fren | Aynen |
| T | Hedef seç | Tank çiftleri (bomba) ile uçaksavar ve düşman uçağı (makineli) arasında sırayla |
| O | Otomatik atış (açık başlar) | Bomba: kilitli tank çiftine menzilde güdümlü bomba (yerden 300 m altında). Makineli: yardım konisinde hedef varken kısa seri (0,4 s); boşa mermi harcamaz |
| — | Makineli nişan yardımı (yeni) | Hedef ±6° konide ve 900 m içindeyse mermiler öncü noktaya yönelir. HUD'da öncü nişan halkası çıkar |
| M | GPS | Yeniden çizilir: ray, tren, bölgeler, etkin düşmanlar, güvenli bölge ve üs; seçili hedefe rota |

Güdüm bugün `Yikilabilir` hedefine bağlı. Bunun yerine bir hedef noktası kabul edecek şekilde genelleştirilir. Lab 7 davranışı aynı kalır; Lab 7 testleri bunu korur.

### 5.7 Görev akışı ve HUD
- **`KursunDaglariGorevi`:** Eskort → Üsse dön → Görev tamam. Ayrıca Tren durdu / Vuruldun / Çakıldın. Susturulanları sayar (12 üzerinden).
- **`Bolum4Hud`:** Sol üstte her an beş satır:
  - UÇAK %
  - TREN %
  - MERMİ 1200 · BOMBA 2
  - GÜVENLİ BÖLGE 3:12
  - GÖREV: "Treni koru — 2 · Viyadük" ya da "Üsse dön (2,4 km)"
- HUD'da ayrıca:
  - bölge duyuruları;
  - düşman işaretleri (tür ve uzaklık);
  - öncü nişan;
  - uyarılar: "UÇAKSAVAR ATEŞİ — yüksel", "ÇOK YÜKSEK — bomba bırakılamaz", "TREN VURULUYOR";
  - hız ve gaz göstergeleri (Lab 7).
- **"GÖREV TAMAM" paneli:**
  - trenin kalan canı;
  - susturulan ateş kaynağı (x / 12; türlere göre);
  - kalan mermi ve bomba;
  - süre;
  - R = yeniden, Ana Menü.

## 6. Değerler (INFERRED; denge testinde ayarlanır)
| Değer | Başlangıç |
|---|---|
| Tren | 100 can; seyir 17,9 m/s; varış 300 s |
| Uçak | 100 can; başlangıç 220 km/sa, lokomotifin 150 m üstünde |
| Tank | 100 can (yalnız patlama); menzil 900 m; 4 s'de bir; trene 4 hasar; çift aralığı 16 m |
| Uçaksavar | 60 can; menzil 1 000 m; tavan topun 300 m üstü; 0,35 s'de bir; uçağa 3 hasar |
| Düşman uçağı | 80 can; 75 m/s; koni ±8°, 500 m; uçağa ve trene 2 hasar |
| Makineli | 1 200 mermi; 16/s; 600 m/s + uçak hızı; 1 000 m; 10 hasar |
| Bomba | 2; yarıçap 25 m; hasar 100; bırakma tavanı yerden 300 m |
| Üs bölgesi | Üssün 500 m çevresi, yerden 200 m altı; yalnız "Üsse dön" aşamasında |

Denge hedefi: Tankları hiç vurmazsan trenin canı iki tank bölgesinde biter. Bir tank çifti susarsa tren yaralı ama sağ varır.

## 7. İndirme listesi (kullanıcı)
Hepsini bir klasöre koy, örneğin `NU.D.36_Next/Indirilenler/Bolum4/`. **`Assets` içine koyma**; ben denetleyip aktaracağım. FBX ya da OBJ seç. Yalnız GLB varsa Blender'la FBX'e çeviririm.

| Varlık | Önerilen kaynak | Lisans | Yedek |
|---|---|---|---|
| Tren (lokomotif, en az 3 vagon) ve ray | **Kenney · Train Kit**: kenney.nl/assets/train-kit (Download, giriş yok) | CC0 | Quaternius · Modular Train: quaternius.com/packs/modulartrain.html (CC0, FBX) |
| Tank | **Quaternius · Animated Tanks**: quaternius.com/packs/animatedtanks.html | CC0 | Poly Pizza'da "tank", lisans süzgeci CC0 |
| Düşman uçağı | Poly Pizza: "fighter plane" / "ww2 plane", CC0 ya da CC BY | Modele göre | Sketchfab: "low poly ww2 fighter", Downloadable süzgeci, CC0 ya da CC BY |
| Arazi yüzeyi | **Poly Haven** dokuları, 2K, JPG; Diffuse, Normal (**GL**) ve Rough: `cliff_side` (dik yamaç), `aerial_rocks_02` (kayalık), `aerial_grass_rock` (düzlük), `gravel_road` (ray yatağı) | CC0 | Benzer adlı başka Poly Haven dokuları |
| Kaya ve uçurum modelleri | **Quaternius · Ultimate Nature Pack**: quaternius.com/packs/ultimatenature.html | CC0 | Quaternius · Stylized Nature MegaKit |

Her indirmede şunları not et ya da sayfa bağlantısını bana ver: sayfa bağlantısı, lisans, CC BY ise yazarın adı. **Lisansı belli olmayanı alma.**

Uçaksavar asset listesinde yok; Lab 7'deki mevzi modeli kullanılır.

## 8. Uygulama sırası
Asset'ler beklenmez. Yer tutucularla ilerlenir, model gelince yerine konur.

| Faz | İş | Hedef gün |
|---|---|---|
| 1 | Sahne kopyası, arazi, `RayHatti`, ray, tren, istasyon, güvenli bölge, üs, uçağın başlangıcı. 5 dk testi | 11–12.11 |
| 2 | `Susturulabilir`, tank, uçaksavar tavanı, düşman uçağı, makineli, bomba kuralları, hasar, başarısızlıklar | 12–13.11 |
| 3 | HUD, görev akışı, Görev tamam, efektler (parlama, iz, motor dumanı), yardımlar (nişan, güdüm, GPS), ana menü | 13.11 |
| 4 | Asset'leri aktarma, ölçek, arma denetimi, `VARLIKLAR.md`, görünüm, denge, tam tur testleri, Windows build | 13–14.11 |
| 5 | Kırpılmış teslim kopyası ve doğrulaması; video yönergesi; zip betiği | 14.11 |
| — | KULLANICI: elle oynayış, video kaydı, zip, OYS yüklemesi | 14–15.11 |

Her fazın sonunda derleme, EditMode ve PlayMode testleri çalışır; eski lab testleri de dahil.

Değişen ortak scriptlerin hepsi geriye uyumludur; varsayılan değerleri eski davranışı korur:
- `Saglik` / `Mermi`: hasar türü ve tankın "yalnız patlama" ayarı.
- `UcaksavarMevzii`: `atesTavani`.
- `BombaYuku`: bırakma tavanı.
- `NisanSistemi` / `Bomba`: hedef noktası.
- `GpsHaritasi`: gerekirse ortak çizim.

Kararlar `DecisionLog.md`'ye D-101'den başlayarak yazılır. Uygulama belgesi `Documentation/Lab8_Implementation.md`'dir.

## 9. Testler
| TEST | Tür | Ölçülen |
|---|---|---|
| TEST-801 | EditMode | `Assets/Bolum4` içinde beş varlık türü (tren, ray, arazi dokusu, tank, uçak) var |
| TEST-802 | EditMode | Lokomotif 15 m ±2; vagon, tank ve uçak boyu aralıkta |
| TEST-803 | EditMode | `VARLIKLAR.md`'de her `Bolum4/Modeller` ve `Dokular` alt klasörü için satır var; lisans sütunu boş değil |
| TEST-804 | EditMode | Sahne yapısı: istasyon rayın başında, güvenli bölge sonunda, üs istasyona 1,5 km'den yakın; Build Settings; önceki sahneler MD5 aynı |
| TEST-805 | EditMode | Ray parçaları arasında 0,5 m'den büyük boşluk yok; eğri sürekli |
| TEST-806 | PlayMode | Uçak havada, lokomotifin 150 m ±20 üstünde başlar; ilk 5 s'de irtifa korunur; yardım ayarları Rota07'deki gibi |
| TEST-807 | EditMode | Yay uzunluğu tablosu: `Nokta(s)` adımları eşit (±%1) |
| TEST-808 | PlayMode | Virajda her boji raya 0,2 m'den yakın; ardışık araçlar arası uzaklık sabit (±0,3 m) |
| TEST-809 | PlayMode | Düşmanlar kapalı, hızlandırılmış zaman: varış 300 s ±15 |
| TEST-810 | PlayMode | Vagona isabet trenin canını düşürür |
| TEST-811 | EditMode | 3 bölge; sayılar 2T+1U, 2U+2D, 2T+1U+2D; toplam 4/4/4 |
| TEST-812 | PlayMode | Düşmanlar tren yaklaşmadan gizli; yaklaşınca etkin; bölgeler arası sakinlikte ateş yok |
| TEST-813 | PlayMode | Her tür ateş eder; namlu parlaması ve iz nesnesi oluşur |
| TEST-814 | EditMode | Sahnedeki her düşman kendi prefab'ının örneği |
| TEST-815 | PlayMode | Susturulan tank ve uçaksavar ateş etmez ve duman çıkarır; vurulan uçak çekilir, yere düşmez, ateş etmez |
| TEST-816 | PlayMode | Ateş bölgesinde alçakta uçağın canı azalır |
| TEST-817 | PlayMode / EditMode | Topun 300 m üstünde uçaksavar susar; 300 m üstünde bomba bırakılamaz |
| TEST-818 | PlayMode | Tren canı 0 → "TREN DURDU" ve yeniden yükleme; uçak canı 0 → "VURULDUN" ve yeniden yükleme |
| TEST-819 | PlayMode | 1 200 / 2 ile başlar; her atışta azalır; sıfırken ateş yok |
| TEST-820 | PlayMode | Mermi tankı düşürmez; çiftin arasına düşen bomba iki tankı da susturur |
| TEST-821 | PlayMode | Makineli uçaksavarı ve düşman uçağını susturur |
| TEST-822 | PlayMode | Her atışta parlama ve iz |
| TEST-823 | PlayMode | Motor dumanı başlangıçta var; can %30'dayken yayın ve opaklık artmış |
| TEST-824 | PlayMode | Beş HUD bilgisi her aşamada dolu |
| TEST-825 | PlayMode | Tren güvenli bölgede → görev "ÜSSE DÖN" |
| TEST-826 | PlayMode | Üs bölgesine alçak giriş → "GÖREV TAMAM" ve üç değer doğru |
| TEST-827 | PlayMode | Tam tur (otopilot ve hızlandırılmış zaman): kalkış → 3 bölge → güvenli bölge → üs → Görev tamam |
| TEST-828 | Betik | Teslim kopyası derlenir, sahnede kopuk referans yok, build alınır, player hatasız açılır |
| TEST-829 | PlayMode | Yardımlar: makineli nişan yardımıyla düşman uçağı vurulur; T/O ile tank çiftine güdümlü bomba gider; K kapalıyken de föy kuralları aynı |

Görsel kayıtlar (elle) `Logs/lab8/gorsel/` klasörüne gider: üstten harita, üç bölge, hasar anı, HUD ve Görev tamam ekranı.

## 10. Teslim paketi (REQ-830)
- Föy "kullanmadığın asset'leri sil" diyor, ama proje birikimli: Lab 2–7 sahneleri aynı projede. **Çalışma alanından hiçbir şey silinmez.**
- Lab 7'deki "yalnız gece görevi" zip'i gibi **kırpılmış bir teslim kopyası** üretilir:
  - `AnaMenu` ve `Bolum4_KursunDaglari` ile bunların bağımlılıkları;
  - `Assets/VARLIKLAR.md`;
  - Packages, ProjectSettings (build listesi: AnaMenu, Bölüm 4);
  - `Video/bolum4.mp4`.
- Bu kez kopyalamayı elle değil, betik yapar: `NU36.Editor.TeslimKopyasi` ve `Tools/Teslim/teslim_h8.ps1 -OgrenciNo <no> -Video <mp4>` → `Teslim/BMU1421_<no>_H8.zip`.
- Kopya ayrı açılıp derlenir ve build alınarak doğrulanır.

## 11. Riskler
| Risk | Önlem |
|---|---|
| Asset'ler geç gelir | Yer tutucuyla geliştirme; modeller faz 4'te takılır |
| Model biçimi (GLB), yönü, ölçeği; armalar | Blender'la çeviri (çalışma alanı araçları), ölçek testi, gözle denetim |
| Arazinin biçimi asset'ten değil (karar 2) | `VARLIKLAR.md` ve GDD notunda açıkça yazılır; yüzey ve kayalar asset'ten |
| Ortak script değişiklikleri Lab 7'yi bozar | Varsayılanlar eski davranış; bütün eski testler her fazda çalışır |
| Düşman uçağı yapay zekâsı karmaşıklaşır | Kinematik ve dört durumlu tutulur; fizik yok |
| Performans: 8 km arazi, binlerce ray parçası, parçacıklar | Birleşik mesh, parçacık sınırları; build'de FPS ölçümü |
| 7 dakikalık video | Rota at nalı (dönüş ~40 s); görev havada başlar |
| Süre (4 gün) | Faz sırası; asset'ler beklenmez; video için build en geç 14.11 akşamı |
| Unity Editor açıkken batch mode | Batch işlerinden önce süreç denetlenir (şu an Editor kapalı) |

## 12. Kullanıcı görevleri
1. §7'deki asset'leri indirmek ve klasöre koymak (sayfa bağlantısı ve lisansla).
2. Elle oynayış ve his denetimi; arma ve insan figürü denetiminin son onayı.
3. Videoyu kaydetmek (en çok 7 dk; üç bölge, bir hasar anı, Görev tamam). Yönerge faz 5'te verilir.
4. Öğrenci numarasıyla zip'i oluşturmak (betik hazır olur) ve OYS'ye yüklemek.
