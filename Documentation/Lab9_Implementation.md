# Lab 9 (Hafta 9): Bölüm 5 · Kartal Geçidi — Uygulama

- **Plan:** `Lab9_Plan.md`.
- **Geliştirme:** 24.11.2026, tek oturumda (kullanıcı isteği).
- **Kararlar:** `DecisionLog.md` D-126 … D-137.
- **Test sonuçları:** `TestReport.md` §13.

## 1. Föyün maddeleri
| # | Föy maddesi | Durum | Nasıl | Kanıt |
|---|---|---|---|---|
| 1 | Oyunda Bölüm 5 var: Bölüm 4'ten ayrı, yeni bir sahne | **VERIFIED** | `Bolum5_KartalGecidi` (Bölüm 4'ün kopyası, Bölüm 4 dünyası silinmiş). Build Settings'te; ana menüde "GÖREVE BAŞLA" ve "Bölüm Seç → Hafta 9" ile açılır | TEST-901 (EditMode); Bölüm 4 sahnesi MD5 ile aynı |
| 2 | Uçak koya bir geçidin içinden geçerek varıyor | **VERIFIED** (harita, başlangıç); elle uçuş KULLANICI | Uçak, Kartal Geçidi'nin batı ağzında havada başlar. Sırt, geçit dışında 600 m'nin altından aşılamaz | TEST-902: taşma dolgusu; geçit tabanı ≤ 274 m, duvarlar tabandan ≥ 839 m yüksek. Görüntüler 01, 02 |
| 3 | Koyda aynı anda 4 çıkarma gemisi var; aralıklı duruyorlar | **VERIFIED** | 4 şerit, 300 m aralıklı. Başlangıç uzaklıkları 1 000 / 700 / 1 300 / 1 600 m | TEST-903: oyun başında 4 gemi denizde, en yakın ikisi 454 m aralıklı |
| 4 | Kıyıya yanaşan gemi karaya 3 tank indiriyor | **VERIFIED** | Yanaşır, rampa iner, 3 tank sırayla iner ve kıyının arkasında sıraya girer | TEST-904; görüntü 04, 07 |
| 5 | Vurulan gemi duruyor: kalan tanklarını indiremiyor | **VERIFIED** | Can 0 → gemi durur, yanar, yana yatar; güvertedeki tanklar kalır | TEST-905: 1 tank inmişti, 2'si gemide kaldı; görüntü 06 |
| 6 | Durdurulan geminin yerine yenisi geliyor; o da tank indiriyor | **VERIFIED** | Şerit 6 s sonra 1 400 m açıktan yeni gemi alır | TEST-906 |
| 7 | Düşman uçakları gemileri koruyor: oyuncuya saldırıyor | **VERIFIED** | İki koruma uçağı koyun üstünde devriye uçar; oyuncu koya 2,4 km yaklaşınca ona saldırır | TEST-907: 46 atış, oyuncunun canı 100 → 90 |
| 8 | Karaya çıkan tank sayısı ekranda yazıyor | **VERIFIED** | HUD'un ikinci satırı: "KARADA TANK x / 12" | TEST-904/908: "KARADA TANK 3 / 12"; görüntüler 01–03, 09 |
| 9 | Karaya 12 tank çıkınca bölüm başarısız oluyor | **VERIFIED** | "KIYI DÜŞTÜ" paneli, 4 s sonra bölüm baştan başlar (R ile hemen) | TEST-909 |
| 10 | 12 geminin hepsi geldikten sonra kıyı düşmediyse bölüm tamamlanıyor | **VERIFIED** | Bütün gemiler sonuçlanınca karada 12'den az tank varsa "BÖLÜM TAMAM" paneli çıkar | TEST-910 (11 durduruldu, 1 indirdi, 3 tank → tamam); TEST-912 (yardımlarla tam tur) |
| 11 | Hiçbir yerde insan görünmüyor: gemiden yalnız araç iniyor | **VERIFIED** (kod ve ad denetimi); son gözle denetim KULLANICI | Gemi temel şekillerden kurulu; içinde figür yok. Gemiden yalnız tank iner | TEST-911; görüntüler 04–07 |

## 2. Ne yapıldı
### 2.1 Sahne ve harita
- **`Lab9SceneBuilder`:** Bölüm 4 sahnesini kopyalar ve Bölüm 4 dünyasını siler.
  - Kalanlar: uçak (arcade uçuş, makineli, bomba, nişan yardımı, motor dumanı ve sesi), kamera, güneş, Volume, ortam ışığı, savaş sesleri, duraklatma menüsü.
  - Var olan sahnenin üstüne yazmaz. **Rebuild** komutu yalnız kendi ürettiklerini siler.
- **`KartalGecidiHaritasi` (INFERRED, D-130):** Saf fonksiyonlar; testler de aynı sayıları kullanır.
  - Batıda oval bir başlangıç vadisi.
  - Kuzey–güney uzanan, 950 m'den yüksek bir sırt.
  - **Kartal Geçidi:** 4 km, S biçimli; taban 60 m genişliğinde, 200–275 m yükseklikte.
  - Kıyı ovası, 80 m'lik kumsal.
  - İki burun arasında, doğuya açık iç bükey **Kartal Koyu**.
- **Arazi (`Lab9Arazi`):** 8 km, 1 025² örnek; 3 s'de üretilir.
  - Bölüm 4'ün katmanları kullanılır, kumsal ve deniz dibi için Poly Haven kum dokusu eklendi.
  - 2 200 çam ve 380 kaya dağıtıldı. Geçidin tabanı, kumsal ve tankların toplanma alanı açık bırakıldı.
- **Deniz (D-131):** Kodla üretilmiş dalga normal haritalı, kayan, parlak bir yüzey. y = 0'da "Arazi" katmanında bir çarpıştırıcı var: mermi ve bomba suya çarpar, yer koruması denizi zemin sayar.
- **Kumsal engelleri:** Şeritlerin dışında, iki sıra çelik engel.

### 2.2 Çıkarma filosu
- **`CikarmaGemisi`:** İlkellerden kurulan LCT benzeri gemi (D-128, D-129).
  - 240 can, 7 m/s.
  - Rampa 3 s'de iner, 5 s'de bir tank çıkar.
  - Arkasında köpük izi; vurulunca duman ve ateş.
- **`CikarmaTanki`:** Rampadan kumsala iner, karaya değdiği an sayılır. Sonra kıyının 110 m gerisindeki park yerine sürer ve araziye oturur.
- **`CikarmaFilosu`:** 4 şerit, toplam 12 gemi, yerine yeni gemi gelmesi (D-127).
  - Sayaçlar: gelen, durdurulan, boşaltan, karadaki tank.

### 2.3 Koruma uçuşları
- `DusmanUcagi`'na koruma kipi eklendi (D-132).
- `KorumaUcuslari` aynı anda iki uçak tutar; toplam 6 uçak var, vurulanın yerine 30 s sonra yenisi gelir.
- Uçak, Bölüm 4'ün düşman uçağı prefab'ıdır.

### 2.4 Görev, HUD, GPS
- **`KartalGecidiGorevi` (D-135):**
  - Akış: Yaklaşma → Savunma → Tamam.
  - Başarısızlıklar: Kıyı düştü, Vuruldun, Çakıldın.
  - Duyurular: geçide giriş, koya çıkış, gemi durduruldu, tank karaya çıktı.
- **`Bolum5Hud`:** Sol üstte beş satır:
  - UÇAK %
  - **KARADA TANK x / 12**
  - GEMİ gelen / 12 · durdurulan
  - MERMİ · BOMBA
  - GÖREV
- **HUD'da ayrıca:**
  - Uyarılar: "GEMİ KIYIDA — TANK İNİYOR", "ÇOK YÜKSEK", "MERMİ YOK".
  - Gemi ve uçak işaretleri. Kıyıda tank indiren gemi kırmızıyla gösterilir.
  - Ekran dışındaki en yakın gemi için kenar oku.
  - Öncü nişan halkası.
  - "BÖLÜM TAMAM" / "KIYI DÜŞTÜ" paneli: karaya çıkan tank, durdurulan gemi, düşürülen uçak, kalan cephane, süre.
- **`Bolum5Gps`:** Denizi, kumsalı ve geçidi gösteren harita.
  - İşaretler: gemiler (turuncu; tank indirirken kırmızı; durunca gri), karadaki tanklar, koruma uçakları, oyuncu, seçili hedef ve ona giden çizgi.

### 2.5 Yardımlar (kullanıcı tercihi, D-133)
- Bölüm 4'teki gibi açık başlar: arcade uçuş, T hedef, O atış yardımı, M GPS, K uçuş modu.
- Gemi hem makineli hem bomba hedefidir:
  - O açıkken makineli, gemi konideyse kendiliğinden ateş eder.
  - Bomba, kilitli gemiye güdümlü gider; 900 m içinde ve 300 m'nin altında kendiliğinden bırakılır.
- 6 bomba, 1 200 mermi (D-134).

### 2.6 Ana menü (D-136)
- "GÖREVE BAŞLA" Bölüm 5'i açar.
- "Bölüm Seç" listesinde Bölüm 4 kalır, 8. satır olarak "Hafta 9 · Kartal Geçidi" eklendi.

## 3. Geliştirme sırasında bulunan ve düzeltilen hatalar
| Bulgu | Nasıl bulundu | Düzeltme |
|---|---|---|
| Sırtın doğu ucu kıyı ovasıyla karışıp alçalıyordu: geçidin yanındaki duvar tabandan yalnız 92 m yüksekti | TEST-902 | Sırt ağırlığı kıyı karışımından üstün tutuldu. Duvar artık en az 839 m |
| Tank boyu testi namluyu da ölçüyordu (8,5 m) | Test | Gövde ölçülüyor: 7 m (Bölüm 4 ile aynı) |
| Rampa 14° eğilince kumun altına gömülüp görünmüyordu | Görüntü 04 | 2°: kumsalın kenarına yaslanıyor |
| Kıyı düştükten sonra rampadaki tanklar sayacı 15'e çıkarıyordu; "KIYI DÜŞTÜ" iki kez yazıyordu | Görüntü 09 | Bölüm bitince sayı donar (12 / 12); başlık yalnız panelde |
| Başlangıç vadisi ve sırtın kenarları üstten bakınca düz çizgiydi | Görüntü 08 | Oval, gürültülü vadi kenarı; sırt kenarları geçitten uzakta ±300 m dalgalanıyor |

## 3.1 Kullanıcı denemesinden sonra: karaya çıkışın görünür olması (24.11.2026, D-138)
Kullanıcı oyunu oynadı ve "karaya çıkma kısmı tam olarak görünmüyor" dedi. Yapılanlar:
- **Gemi:**
  - Güverte 1,6 m'ye çıktı, yan duvarlar alçaldı; tankların kuleleri duvarın üstünden görünüyor.
  - Gemi pruvası su hizasına 6 m kala durur, suyu sıçratarak kıyıya oturur, pruvasını hafifçe kaldırır ve sallanmayı bırakır.
- **Rampa:**
  - 5,5 m uzunluğunda, kenar korkulukları ve basamak çıtaları var.
  - 4 s'de yumuşak bir hareketle iner. Açısı her yanaşmada, ucu tam kuma değecek şekilde hesaplanır (≈ 21°, uç su hizasında).
  - Ucun kuma değdiği an kum ve su sıçrar.
- **Tanklar:**
  - Güvertede yavaşça pruvaya ilerler, rampadan burnu aşağıda iner; çıkarken su sıçrar.
  - Kumsalda hızlanır, kıvrılarak kumsal çıkışından park yerine döner.
  - Arazinin eğimine göre öne ve yana yatar, arkasında kum tozu bırakır, park yerinde yavaşlayıp durur.
- **Kıyı kamerası:**
  - Bir gemi kıyıya oturunca HUD'un sağ üstünde inişin canlı görüntüsü açılır.
  - Kamera geminin yanında yavaşça döner. Alttaki yazı şeridi ve karadaki tank sayısını gösterir.
  - İniş bittikten 4 s sonra kapanır. **C** tuşu pencereyi gizler ya da gösterir.
- **Testler:**
  - EditMode: rampa ucu dört şeritte de su hizasında kuma değiyor (20,8°).
  - PlayMode: rampa hesaplanan açıya iniyor; ilk tank rampada 20° eğiliyor, kumsalda toz kaldırıyor; kıyı kamerası doğru gemiyi gösteriyor.
  - Görüntüler: `Logs/lab9/gorsel/04_hud_kiyi_kamerasi`, `05_tank_rampada`, `06_kumsala_cikis_toz`, `07_gemi_ve_kumsal`.

## 4. Bilinen sınırlar
- **Dik yamaçlarda doku esniyor:** Terrain dokuyu üstten izdüşürüyor (Bölüm 4'teki sınırın aynısı).
- **Tam tur testi:** Komut dosyalı pilot uçağı her karede hedefin yanına koyar (Lab 8 D-121 gibi). Bu test görev döngüsünü ve yardımlı dengeyi ölçer, klavyeyle uçuşu ölçmez. Yardımlarla tur 0:28 oyun zamanında bitti; gerçek oyunda gemiler arası uçuş süresi eklenir.
- **Yardımsız denge ölçülmedi:** Yardımlar kapalıyken (O ve K) denge yalnız hesapla tahmin ediliyor. İlk gemi yaklaşık 100 s sonra yanaşır; dört gemi hiç durdurulmazsa kıyı yaklaşık 4 dk'da düşer.

## 5. Kullanıcıda kalanlar
- **Klavyeyle oynamak:** Geçitten uçmak, gemileri durdurmak, koruma uçaklarıyla dövüşmek. His ve zorluk denetimi (çok kolay ya da zor gelirse sayılar `KartalGecidiHaritasi`'nda tek yerde).
- **Son gözle denetim:** İnsan figürü, bayrak ya da arma yok.
- **Teslim:** Föyde teslim kuralı (zip adı, video) verilmedi. İstenirse Hafta 8'in betiği Bölüm 5 için uyarlanabilir.
