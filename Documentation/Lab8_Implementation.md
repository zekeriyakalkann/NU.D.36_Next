# Lab 8 (Hafta 8): Bölüm 4 · Kurşun Dağları — Uygulama

Plan: `Lab8_Plan.md`. Geliştirme 11.11.2026'da başladı. Kararlar: `DecisionLog.md` D-101 … D-125. Test sonuçları: `TestReport.md` §12.

| Faz | İş | Durum |
|---|---|---|
| 1 | Sahne kopyası, arazi, ray hattı, ray, tren, istasyon, güvenli bölge, üs, uçağın başlangıcı, 5 dk testi | **Tamam** (11.11.2026) |
| 2 | Susturulabilir düşmanlar, tank, uçaksavar tavanı, düşman uçağı, makineli, bomba kuralları, hasar, başarısızlıklar | **Tamam** (11.11.2026) |
| 3 | HUD, görev akışı, Görev tamam, efektler, yardımlar, ana menü | **Tamam** (11.11.2026) |
| 4 | Görünüm, denge, `VARLIKLAR.md` denetimi, tam tur, build | **Tamam** (11.11.2026); elle oynayış kullanıcıda |
| 5 | Teslim kopyası, video yönergesi, zip betiği | **Tamam** (11.11.2026); video ve zip kullanıcıda |

## 1. Faz 1: Ne yapıldı

### 1.1 Sahne
- `Lab8SceneBuilder` (menü **NU36 → Lab 8 → Create Bolum4 Scene**; batch: `-executeMethod NU36.Editor.Lab8SceneBuilder.Create`), Rota07'yi `Assets/Scenes/Bolum4_KursunDaglari.unity` adıyla kopyalar. Var olan sahnenin üstüne yazmaz.
  - **Rebuild Bolum4 (delete and create)** komutu yalnız kurucunun ürettiklerini siler ve yeniden kurar. Bunlar sahne ile `Bolum4/Malzemeler`, `Prefablar`, `Meshler`, `Arazi` ve `GunduzVolume`'dur. İndirilen modellere ve dokulara dokunmaz.
- Gece dünyası silinir. Uçak, kamera, girdi, Bölüm 3 yardımları, hız ve gaz göstergeleri ile duraklatma menüsü aynen kalır (föy 02: "kontroller ve kamera Bölüm 3'teki gibi").
- Gündüz ayarları:
  - Güneş 1,6 şiddetinde, (38°, 140°) açısından gelir.
  - Ortam ışığı Trilight.
  - Üstel kare sis, yoğunluk 0,00016.
  - Gündüz Volume: ACES, bloom, renk ayarı, vinyet.
- Sahne Build Settings'in sonuna eklenir. Önceki sahneler bayt bayt aynıdır (MD5 testleri).

### 1.2 Harita (`KursunDaglariHaritasi`, INFERRED; föyün çizimi gelmedi)
- **Ray hattı:**
  - At nalı biçiminde tek bir hat, 18 kontrol noktasından geçer.
  - Eğri merkezcil (centripetal) Catmull-Rom'dur (`RayEgrisi`) ve 1 m'lik eşit yay uzunluğu adımlarıyla yeniden örneklenir.
  - Uzunluk **5 217 m**.
  - En dar viraj **R 204 m** (s = 3 785 m, Son Viraj).
  - En dik eğim %4,5.
- **Kalkış istasyonu:** Güneybatıda, s = 0–200 m. **Üs:** Hattın başından 822 m batıda; 1 000 m'lik pist, hangarlar, kule.
- **Güvenli bölge:** Hattın son 320 m'si; kapı s = 4 897 m'de. Kapı, depolar ve kum torbaları var.
- **Viyadük:** Boğazın üstünde, s = 2 562–2 808 m (246 m); güverte ve ayaklardan oluşur. Nehir boğazın dibinden akar.
- **Ateş bölgeleri:** Hat uzunluğunun kesirleri olarak verilir. Kalkıştan itibaren lokomotifin bölgede olduğu süreler:

| Bölge | Ray (m) | Lokomotif orada | İçerik (Faz 2) |
|---|---|---|---|
| 1 · Dar Vadi | 1 043–1 878 | 1:09–1:59 | 2 tank (bir çift), 1 uçaksavar. Duvarlar 330 m |
| 2 · Viyadük | 2 400–3 130 | 2:30–3:14 | 2 uçaksavar, 1. dalga: 2 uçak |
| 3 · Son Viraj | 3 652–4 486 | 3:46–4:36 | 2 tank (bir çift), 1 uçaksavar, 2. dalga: 2 uçak |

Bölgeler arasında sakin aralıklar var (31 s ve 32 s). Son bölge güvenli bölgeden 411 m önce biter.

### 1.3 Arazi (`Lab8Arazi`, Editor; plan kararı 2)
- Unity Terrain, 8 × 8 km, 1 025² yükseklik örneği.
- Sırt gürültüsüyle 300–1 250 m'lik dağlar, güneyde alçak bir ova, harita kenarında geçilmez yüksek bir halka.
- Hat boyunca oyulmuş bir vadi:
  - Rayın altında düz bir yatak var (hattın 0,25 m altında). İki yanda duvarlar dağa yükselir.
  - Dar Vadi'de duvarlar 330 m yüksekliğinde.
  - Son Viraj'da geniş bir sekiye oturur.
- Ayrıca nehir boğazı, mevzi düzlükleri ve havaalanı düzlüğü kodla oyulur.
- Doku katmanları Poly Haven'dan, ağaçlar ve kayalar Quaternius'tan gelir. 2 020 ağaç ve kaya arazi ağacı olarak dağıtılır.

### 1.4 Ray (`Lab8Varliklar.RayMeshleri`)
- Quaternius'un düz ray parçası ray açıklığı 1,435 m olacak biçimde ölçeklenir.
- Parçalar eğri boyunca bükülerek döşenir. Her köşe, kendi yay uzunluğundaki eğri noktasına taşınır.
- Parçalar 22 birleşik mesh'te toplanır.
- Ölçüm: Çelik üçgenlerin kapsadığı aralıklarda **en büyük boşluk 0,000 m**. Çelik, merkez hattan en çok 0,62 m uzakta.

### 1.5 Tren (`Tren`, `TrenAraci`)
- Lokomotif (kömür vagonuyla 15,0 m) ve 4 vagon (8,9–10,2 m). Hepsi prefab'tır (`Bolum4/Prefablar/Tren`).
- Araçlar kinematik Rigidbody ve BoxCollider taşır, bütün tren tek bir `Saglik` paylaşır.
- **Yerleştirme:** Her aracın iki bojisi eğri üzerindedir ve gövde bu iki bojiyi birleştiren doğrultuda durur. Bu yüzden virajda raydan çıkmaz. Araçlar arasında 0,8 m boşluk var.
- **Tarife (saf fonksiyonlar, test edilir):**
  - 20 s'lik kalkış ivmesi.
  - Seyir hızı hat uzunluğundan hesaplanır: **59,8 km/sa**.
  - Kapıdan tam **300 s**'de geçilir.
  - Ardından 33 s'lik düzgün frenleme. Tren s = 5 172 m'de, tampona 37 m kala durur.
- **Can biterse:** Tren 6 s'de yavaşlayıp durur ve `Durdu` olayı gelir. Duman ve "TREN DURDU" yazısı Faz 2/3'te eklenecek.
- Olaylar ve özellikler: `GuvenliBolgeyeVardi`, `Durdu`, `KalanSure`, `Konum`.

### 1.6 Uçak
- Lokomotifin 150 m üstünde ve 120 m gerisinde, ray yönünde, 220 km/sa hızla havada başlar (`yerdeBasla` = false).
- Bomba kapasitesi 2 (föy 06).
- Yardımlar (`UcusYardimcisi`) yeni haritaya göre ayarlandı: pist üsse göre, harita sınırı 2 700 m.
- Nişan sistemi Bölüm 3'ün hedeflerine bağlı olduğu için şimdilik kaldırıldı; Faz 3'te Bölüm 4 için yeniden gelecek.
- Zemin yüksekliği arazi çarpıştırıcısından ışınla ölçülür (`ZeminBaglayici`, `ZeminOlcer`). Böylece çakılma algılama ve yer koruması yeni araziyle çalışır.

### 1.7 HUD (`Bolum4Hud`, Faz 3'ün bir kısmı erken geldi)
Sol üstte beş satır var: UÇAK %, TREN %, MERMİ — · BOMBA 2, GÜVENLİ BÖLGE m:ss, GÖREV. Mermi Faz 2'de makineliyle bağlanacak. Hız ve gaz göstergeleri ile tuş yardımı da ekranda.

### 1.8 Varlıklar
- `Assets/VARLIKLAR.md` yazıldı. Her varlığın kaynak bağlantısı, lisansı ve değişikliği kayıtlı; CC BY olan düşman uçağının yazarı da yazılı.
- İndirmeler `Indirilenler/Bolum4/` klasöründe (`KAYNAKLAR.txt`). Bu klasör projeye girmez.
- Asistan indirdi (kullanıcı izniyle). Kenney sitesi otomatik indirmeye 403 verdiği için tren ve ray Quaternius Modular Train'den geldi.
- GLB modeller Blender'la FBX'e çevrildi (`Tools/Blender/bolum4/glb_to_fbx.py`, .blend kaydetmez).

## 2. Faz 1'de bulunan ve düzeltilen hatalar
- **Ray mevzi düzlüğünün altında kalıyordu:** Dar Vadi'deki tank çiftinin düzlüğü (raydan 32 m, karışma yarıçapı 71 m) araziyi hattın 1,39 m üstüne çıkarıyordu (s = 1 408 m). Son Viraj'da da ~0,5 m'lik bir yükselme vardı. Raylar ise yalnız 0,16 m yüksekliğinde. Yeni test bunu buldu.
  - **Düzeltme:** Mevzilerden sonra ray yatağı önceliklidir. Hattın 10 m çevresinde arazi yatağın üstüne çıkamaz, 22 m'ye kadar yumuşak bir geçiş var (D-104).
  - **Sonrası:** Viyadük dışında arazi her yerde hattın 0,15–0,51 m altında; raylar açıkta.
- **`RayEgrisi.EnYakin` yalnız 1 m'lik örnek noktalarına bakıyordu:** Hattın yanındaki bir noktanın uzaklığı 0,5 m'ye kadar yanlış çıkıyordu. Artık iki komşu segmente izdüşüm de yapılıyor. Hata 0,05 m'nin altında (D-105).

## 3. Testler (Faz 1)
| TEST | Test | Sonuç |
|---|---|---|
| TEST-804 | `Lab8Tests.Scene_StationAtTheStart_SafeZoneAtTheEnd_BaseNearTheStation`, `Scene_InBuildSettings_EarlierScenesUnchanged` | Geçti: istasyon s = 101 m, güvenli bölge s = 5 055 m, üs 822 m |
| TEST-805 | `Scene_RailIsUnbroken_FromStationToSafeZone` | Geçti: en büyük boşluk 0,000 m |
| TEST-806 | `Scene_AircraftStartsInTheAir_…` (EditMode), `Start_InTheAirAboveTheTrain_KeepsFlyingWithNoInput_AssistsOn` (PlayMode) | Geçti: 150 m üstte; ilk 5 s'de yükseklik değişimi 0,6 m, en alçak noktada yerden 148 m |
| TEST-807 | `Curve_ArcLengthSamples_AreEvenlySpaced` | Geçti: 10 m'lik adımlar 9,999–10,000 m |
| TEST-808 | `Train_StaysOnTheRails_KeepsItsSpacing_ArrivesInFiveMinutes` | Geçti: bojiler hattan 0,000 m; araç arası boşluk 0,80–0,81 m; R 203 m'lik virajdan geçildi |
| TEST-809 | aynı test + `Schedule_PassesTheSafeZoneGateAtFiveMinutes_ThenStopsInsideTheZone` | Geçti: **300,0 s** (düşmanlar yokken, 20× hızlandırılmış oyun zamanı) |
| TEST-811 (veri) | `Zones_ThreeInOrder_WithCalmGaps_WorksheetCounts` | Geçti: 2T+1U, 2U+2D, 2T+1U+2D; toplam 4 / 4 / 4 |
| Mühendislik önerisi | `Curve_LengthFitsTheFiveMinuteRun_NoKinks_GentleGrades`, `Curve_NearestPoint_…`, `Scene_GroundUnderTheLine_IsTheRailBed_…`, `Scene_TrainOnTheLine_…`, `Scene_NightWorldRemoved_DaylightAndHud`, `Train_TimeLeftCountsDown_AndShowsOnTheHud` | Geçti |

Ölçümler `Logs/lab8/harita_olcumu.txt` ve `Logs/lab8/tren_olcumu.txt` dosyalarında. Görsel kayıtlar `Logs/lab8/gorsel/sahne_*.png` (Lab8Gorsel, Faz 1 sahnesi; arazi düzeltmesinden önce alındı).

## 4. Plandan sapmalar
| Plan | Uygulama | Neden |
|---|---|---|
| Ray ≈ 5,0 km, seyir 64 km/sa | 5,2 km, 59,8 km/sa | Hat çizimden çıktı; hız, uzunluğa göre hesaplanıyor. Varış yine 300 s |
| Son Viraj R ≈ 300 m | R 204 m | Çizimin sonucu. Tren iki bojiyle raydan çıkmıyor (TEST-808) |
| Viyadük 400 m, kemerli taş | 246 m; güverte ve ayaklar | Boğazın hattı kestiği yer 246 m. Kemerli görünüm Faz 4'teki görünüm işinde |
| Asset'leri kullanıcı indirir (karar 1) | Asistan indirdi | Kullanıcı izin verdi (`Indirilenler/Bolum4/KAYNAKLAR.txt`) |
| Tren: Kenney Train Kit | Quaternius Modular Train | Kenney otomatik indirmeye 403 verdi; plandaki yedek kaynak kullanıldı |

## 5. Faz 4'e kalan görünüm notları (görüntülerden)
- Arazi fazla sarı ve koyu toprak renginde. Dik yamaçlarda doku esniyor ve çizgili görünüyor (Terrain'in üstten izdüşümü).
- Tren çok koyu görünüyor: Quaternius'un düz renk malzemeleri gölgede neredeyse siyah.
- Viyadük düz bir güverte ile ayaklardan oluşuyor; kemerli taş görünümü yok.
- Düşman uçağının dokusu, model inceleme görüntüsünde malzemeye bağlı değildi (beyaz görünüyordu). Faz 2'de prefab'ta yeni bir Lit malzemeye bağlandı; ekranda henüz görülmedi.

## 6. Faz 2: Düşmanlar, silahlar, hasar (11.11.2026)

### 6.1 Hasar kuralları (ortak kod, geriye uyumlu; D-108, D-109)
- **`Saglik`:**
  - Bir **taraf** alanı var (`Notr`, `Dost`, `Dusman`). Aynı taraftan gelen mermi ya da patlama hasar vermez. Uçak ve tren `Dost`, düşmanlar `Dusman`. Bu yüzden oyuncunun mermisi ve bombası treni vurmaz.
  - **Yalnız patlama** seçeneği var: Tank mermiden hasar almaz, mermi zırhtan seker (`Sekti` olayı).
  - Varsayılanlar (`Notr`, kapalı) Lab 3–7 davranışını aynen korur. Eski `Vur(int)` çağrısı değişmedi.
- **Ateş edenin tarafı:** `Mermi`, `Patlama.Yarat` ve `Bomba` ateş edenin tarafını taşır; bombanın tarafı onu bırakan uçağınkidir. `Mermi`, isteğe bağlı bir isabet efekti de gösterebilir.
- **Tavanlar (föy 05, sınırlar INFERRED, 300 m):**
  - `BombaYuku.birakmaTavani`: Yerden 300 m'nin üstünde bomba bırakılmaz.
  - `UcaksavarMevzii.atesTavani`: Uçak topun 300 m üstündeyse uçaksavar ateş etmez.
  - Varsayılanları 0, yani kapalı.

### 6.2 Düşmanlar (her tür bir prefab: `Assets/Bolum4/Prefablar/Dusmanlar`; D-110 … D-112)
| Tür | Model | Can, hasar | Davranış |
|---|---|---|---|
| Tank (`Tank.prefab`) | Quaternius tankı, 7 m gövde. Kule ve namlu dönen bir pivota taşındı | 100. **Yalnız patlamadan** hasar alır | Kule trenin en yakın aracına döner. 900 m içinde 5 s'de bir öncü nişanlı mermi atar (0,35° saçılma). Trene 2 hasar, namlu alevi ve iz |
| Uçaksavar (`Ucaksavar.prefab`) | Hafta 7 mevzisi (kum torbaları, kaide, kalkanlı namlu) | 60. Makineliyle susar | Gündüz kendi gözüyle görür (her yöne, arazi görüşü keser). 1 000 m içinde 0,35 s'de bir ateş eder, uçağa 3 hasar. **Topun 300 m üstünde susar** |
| Düşman uçağı (`DusmanUcagi.prefab`) | Poly Pizza "Aeroplane" (dokusuyla), 11 m kanat açıklığı | 80. Makineliyle susar | Kinematik, 75 m/s, 38°/s dönüş, dönüşte yatış. Saldırı → sıyrılma → yeniden saldırı. ±8° konide ve 500 m içinde 0,25 s'de bir 2 hasarlık mermi. Hedef: yakındaki ve öndeki oyuncu, yoksa tren. Yerden en az 80 m'de kalır. Vurulunca duman izi bırakarak tırmanır ve haritadan çıkar; düşmez |

- **`Susturulabilir`:** Canı biten birimin ateş bileşenlerini kalıcı olarak kapatır ve siyah duman başlatır. Tankın kulesi eğilir, uçaksavarın namlusu düşer. Görev sayacına bildirir.
- **`AtesBolgesi`:**
  - Bölgenin birimleri gizli başlar. Lokomotif bölgeye 150 m kala ortaya çıkarlar, uçak dalgası da 1,5 km dışarıdaki giriş noktalarından gelir.
  - Lokomotif bölgeden çıkınca yer birimleri ateşi keser, uçaklar çekilir. Böylece arada sakin bir kesim kalır.
  - Sahnedeki uçağı ve treni prefab'lara bölge verir; prefab'larda sahne referansı yok.
  - Bölge zamanları (tren saatine göre): Dar Vadi 1:00–1:59, Viyadük 2:21–3:14, Son Viraj 3:37–4:36. Aradaki sakin kesimler 22 s ve 23 s.
  - Plan 600 m önden uyanmayı öngörüyordu. Bölgeler arası yalnız ~520 m olduğu için bu hiç sakinlik bırakmazdı; 150 m seçildi (D-111).

### 6.3 Oyuncunun silahları
- **`Makineli`:**
  - Sol Ctrl ya da farenin sol tuşu.
  - 1 200 mermi; iki kanat namlusundan sırayla saniyede 16 atım.
  - Mermi hızı 600 m/s artı uçağın hızı, menzil 1 000 m, hasar 10.
  - Her atışta namlu alevi ve sarı-beyaz iz mermisi; isabette kıvılcım.
  - Mermi bitince ateş etmez, HUD'da "MERMİ YOK" yazar.
  - Faz 3'teki nişan yardımı için `NisanYonu` ve `OtomatikTetik` kancaları var.
- **Bomba:** Kapasite 2, ikmal yok. Yerden 300 m'nin üstünde bırakılamaz. Hafta 5'in 25 m'lik patlaması ve 120 m'lik parça kuralı aynen geçerli.

### 6.4 Görev akışı (`KursunDaglariGorevi`, D-114)
- **Akış:** Eskort → (tren güvenli bölgede) "Üsse dön" → (üssün 500 m çevresinde, yerden 200 m altında) "Görev tamam".
- **Başarısızlıklar:**
  - Trenin canı biterse "TREN DURDU", uçağınki biterse "VURULDUN", çakılırsa "ÇAKILDIN".
  - 4 s sonra bölüm baştan başlar; R tuşuyla hemen başlar.
- Susturulan ateş kaynaklarını türlerine göre sayar (12 üzerinden). HUD'un görev satırını ve bölge duyurularını verir.
- "Görev tamam" panelinin görünümü Faz 3'te yapılacak.

### 6.5 Denge (ilk ölçüm, D-113)
Oyuncu hiçbir şey yapmazsa tren bölge başına **46 / 12 / 62** hasar alır (toplam 120) ve son bölgede durur. Plan §6'nın hedefi tutuyor: Tanklar hiç vurulmazsa tren ikinci tank bölgesinde biter.
- Bir tank çifti erken susturulursa, kalan hasar yaklaşık 12 + 62 = 74 olur ve tren yaralı ama sağ varır.
- İlk değerlerle (3 hasar, 4,5 s) bir tank çifti tek başına 75 hasar veriyordu. Bu yüzden hasar 2'ye, atış aralığı 5 s'ye çekildi.

### 6.6 Testler (Faz 2)
| TEST | Test | Ölçüm |
|---|---|---|
| TEST-810 | `Tank_FiresAtTheTrain_…` | Tank mermisi treni vurdu: 100 → 98 |
| TEST-811 | `Lab8DusmanTests.Scene_ThreeZones_WorksheetCounts_…` | 2T+1U, 2U+2D, 2T+1U+2D; 12 kaynak |
| TEST-812 | `Zones_HiddenUntilTheTrainComes_CalmBetweenThem` | Tren gelene kadar gizli; sakin kesimlerde **0 atış** |
| TEST-813 | `Tank_…`, `Aa_…`, `EnemyAircraft_FlyIn_…` | Üç tür de ateş ediyor; namlu alevi ve iz mermisi var |
| TEST-814 | `Scene_EveryEnemyIsAnInstanceOfItsTypesPrefab` | 4 tank, 4 uçaksavar, 4 uçak; her biri kendi prefab'ının örneği |
| TEST-815 | `Bomb_BetweenATankPair_…`, `MachineGun_Silences…` | Susan tank ve uçaksavar ateş etmiyor ve tütüyor. Vurulan uçak çekiliyor ve tırmanıyor (düşüş 0,0 m), ateş etmiyor |
| TEST-816 | `Aa_FiresAtALowAircraft_…` | 150 m'de uçağın canı 100 → 94 |
| TEST-817 | `Aa_…`, `Bomb_RefusedAbove300m_…` | 400 m üstte uçaksavar 0 atış yaptı. 480 m'de bomba bırakılamadı, 200 m'de bırakıldı |
| TEST-818 | `TrainDestroyed_…`, `AircraftDestroyed_…` | "TREN DURDU" / "VURULDUN"; 4 s sonra yeniden yükleme |
| TEST-819 | `Rounds_1200_CountDown_…` | 1 s'de 16 mermi; sayaç düşüyor; sıfırda ateş yok; HUD "MERMİ YOK" |
| TEST-820 | `Bomb_BetweenATankPair_…` | 20 mermi tanka hasar vermedi. Çiftin ortasına düşen tek bomba iki tankı da susturdu |
| TEST-821 | `MachineGun_Silences…` | Uçaksavar 13 mermiyle, düşman uçağı 14 mermiyle sustu |
| TEST-822 | `Rounds_1200_…` | Her atışta iz mermisi ve namlu alevi |
| Mühendislik | Saf fonksiyon testleri: taraf ve yalnız patlama kuralı, öncü nişan, atış konisi; boyutlar (tank, düşman uçağı); tank çiftleri 16 m aralıklı, yerde | Geçti |

Ölçümler: `Logs/lab8/dusman_olcumu.txt`.

### 6.7 Faz 2'de bulunan ve düzeltilen hatalar
- **Düşman uçakları yere çok iniyordu:** En alçak nokta yerden 40 m'ydi, çünkü sert sınır en az yüksekliğin yarısındaydı. Artık sert sınır 80 m ve yükselme 160 m'de başlıyor; ölçüm 80 m.
- **Test hatası:** Uçaksavar testleri uçağı Dar Vadi'nin ve boğazın duvarlarının içine koyuyordu; görüş hattı kapanıyordu. Testler artık görüş hattı açık bir nokta seçiyor.

### 6.8 Henüz yapılmayan (Faz 3–4)
- HUD'daki uyarılar ("UÇAKSAVAR ATEŞİ — yüksel", "ÇOK YÜKSEK", "TREN VURULUYOR"), düşman işaretleri ve öncü nişan.
- "Görev tamam" paneli, motor dumanı, nişan yardımı ve otomatik atış, GPS, ana menüdeki bağlantı.
- Vurulan uçak şimdilik havada donuyor (4 s sonra bölüm yeniden başlıyor); düşüş görünümü yok.
- Elle oynayış: Düşmanların ve silahların hissi henüz klavyeyle denenmedi.

(Faz 3'te ilk iki madde yapıldı; aşağıya bakın.)

## 7. Faz 3: HUD, Görev tamam, motor dumanı, yardımlar, GPS, ana menü (11.11.2026)

### 7.1 HUD (`Bolum4Hud`, D-117)
- **Beş satır (föy 08), her aşamada dolu:** UÇAK %, TREN %, MERMİ · BOMBA, GÜVENLİ BÖLGE süresi, GÖREV. Yüzdeler başlangıç canına göre hesaplanır.
- **Uyarılar (ekranın ortasının altında):**
  - "UÇAKSAVAR ATEŞİ — 300 m üstüne yüksel": etkin bir bölgede bir uçaksavar uçağa ateş ederken.
  - "TREN VURULUYOR": trenin canı son 2 s'de düştüyse.
  - "ÇOK YÜKSEK — 300 m altında bomba bırakılır": bomba reddedilince ya da kilitli tank çifti varken uçak tavanın üstündeyse.
  - "MERMİ YOK".
- **Düşman işaretleri:** Etkin bölgelerdeki canlı her düşmanın üstünde `[ ]` işareti, türü ve uzaklığı (km). Seçili hedef `>[ ]<` biçiminde ve sarı.
- **Öncü halka `( + )`:** Makineli yardımının nişan noktası. Hedef konideyken yeşile döner.
- **"GÖREV TAMAM" paneli:**
  - Trenin kalan canı.
  - Susturulan ateş kaynağı: x / 12, ayrıca türlere göre (tank, uçaksavar, uçak).
  - Kalan mermi ve bomba, süre.
  - "R yeniden başla · Esc menü".

### 7.2 Yardımlar (`Bolum4Nisan`, kullanıcı kararı 4, D-115)
- **Hedef seçimi:** Etkin bölgelerin canlı ateş kaynakları arasından, öndekine ağırlık verilerek en yakını seçilir. **T** mesafe sırasıyla bir sonrakine geçer.
  - Tank çifti tek bir bomba hedefidir; nişan noktası iki tankın arasındaki zemin.
  - Uçaksavar ve düşman uçağı makineli hedefidir.
- **O, atış yardımını açıp kapatır (açık başlar):**
  - **Makineli:** Bir makineli hedefi burnun ±6° konisinde ve 900 m içindeyse mermiler öncü noktaya yönelir ve makineli kendiliğinden ateş eder. Konide hedef yokken ateş etmez, mermi harcanmaz.
  - **Bomba:** Kilitli bir tank çiftine bırakılan bomba güdümlüdür. Yardım açıkken çift 900 m içindeyse ve uçak 300 m'nin altındaysa bombayı sistem kendisi bırakır. Çift başına bir bomba atılır; iki bırakış arasında en az 2 s olur.
- **Föyün kuralları yardımla da geçerli:** 1 200 mermi, 2 bomba, 300 m tavanı, tank yalnız bombayla susar.
- K (kolay uçuş), F/B (fren) ve korumalar Bölüm 3'ten aynen geliyor.
- **Plandan fark:** Planda nişan yardımının tuşu yoktu. O tuşu, yardımın üç parçasını (nişan, otomatik atış, otomatik bomba) birlikte açıp kapatıyor. Tek tuş, yardımsız oyunu tek hamlede geri veriyor.

### 7.3 Güdümlü bombanın nokta hedefi (ortak kod, D-116)
- `Bomba.Gudumle(Func<Vector3>)`: Bomba artık bir noktaya da güdülebiliyor.
- Noktaya güdülen bomba için **yakınlık tapası** eklendi: bomba, o adımda noktaya varacaksa patlar.
- Lab 7'nin `Yikilabilir` hedefli güdümü aynen kaldı.
- **Bulunan hata:** İlk sürümde nişan noktası yerden 1 m yukarıdaydı. Bomba noktaya ulaşıyor, hiçbir şeye çarpmadan çevresinde gidip geliyordu. Bomba izinin kaydı bunu gösterdi.

### 7.4 Motor dumanı (`MotorDumani`, föy 07)
- Motorun arkasından dünya uzayında iz bırakan ince, yarı saydam siyah duman.
- Can azaldıkça artıyor; canın %30'unun altında eğri dikleşiyor ve duman kalınlaşıyor.
- **Ölçüm:** Tam canda saniyede 10 parçacık, opaklık 0,22, boyut 0,6 m. %30 canda saniyede 35 parçacık, opaklık 0,54, boyut 1,6 m.

### 7.5 GPS (`Bolum4Gps`)
- Sağ altta kuzeyi yukarıda tutan bir harita.
- Harita açılışta koddan çiziliyor: arazi yükseklik renkleri ve gölgeleme, ray hattı, yeşil güvenli bölge, istasyon ve üs.
- Haritanın üstünde hareket edenler:
  - uçak (ok);
  - tren;
  - etkin bölgelerin canlı düşmanları (kırmızı: tank, turuncu: uçaksavar, mor: uçak);
  - seçili hedef ve ona giden çizgi.
- Altta tek satır bilgi var: hedef, uzaklık ve durum; "Üsse dön" aşamasında üsse uzaklık.
- **M** haritayı gizler ya da gösterir. Görsel dosya kullanılmıyor.

### 7.6 Ana menü (D-118; plan §5.1, INFERRED)
- "GÖREVE BAŞLA" Bölüm 4'ü açar. Alt başlık "Bölüm 4 · Kurşun Dağları" oldu.
- "Bölüm Seç" listesine 7. satır olarak "Hafta 8 · Kurşun Dağları" eklendi.
- Kontroller tablosuna "Ctrl / Sol tık — Makineli" eklendi.
- **Nasıl uygulandı:** `Lab8Menu` (yeniden çalıştırılabilir, bölüm kurucusu da çağırır) AnaMenu sahnesini günceller. `MenuSceneBuilder` da artık Bölüm 4'ü yazar.
- **Test etkisi:** `MenuTests` ve `MenuPlayTests` bu iki beklentiye göre güncellendi; Rota07'nin duraklatma menüsü testleri değişmedi.

### 7.7 Testler (Faz 3, `Lab8Faz3PlayTests`)
| TEST | Test | Ölçüm |
|---|---|---|
| TEST-823 | `EngineSmoke_ThinAtTheStart_DenserAndDarkerWhenHit` | Duman baştan var; %30 canda 10 → 35/s, opaklık 0,22 → 0,54 |
| TEST-824, 825, 826 | `Hud_FiveItemsInEveryPhase_UsseDon_ThenGorevTamamWithItsValues` | Beş satır eskort, dönüş ve bitiş aşamalarında dolu. Tren güvenli bölgede → "Üsse dön". Üssün üstünde alçakta → panel: %77, 1 / 12, 1000 mermi · 2 bomba |
| Uyarılar, GPS | `Warnings_AaFire_TrainHit_TooHigh_AndGpsShowsTheEnemies` | Üç uyarı görüldü; GPS 3 düşman gösterdi; gizli GPS M'yi dinlemeyi sürdürüyor |
| TEST-829 | `Assist_MachineGun_AimsAndFiresByItself_…` | Burnun 4° yanındaki uçaksavar, tuşa basılmadan 15 mermiyle sustu |
| TEST-829 | `Assist_GuidedBomb_ReleasedByItself_SilencesATankPair` | 734 m'den ve 237 m yükseklikten otomatik bırakılan tek bomba iki tankı da susturdu |
| TEST-829 | `Assist_Off_NoAutomaticFire_NoAutomaticBomb` | Yardım kapalıyken kendiliğinden ateş de bomba da yok |
| Menü | `MenuTests`, `MenuPlayTests` | "GÖREVE BAŞLA" Bölüm 4'ü yüklüyor; liste 7 bölüm |

Ölçümler: `Logs/lab8/faz3_olcumu.txt`.

### 7.8 Faz 3 sonunda yapılmamış olanlar (Faz 4'te ele alındı, §8)
- **Ekranda görülmedi:** HUD, işaretler, GPS, duman ve iz mermileri (görüntü testi yok).
- **Klavyeyle oynanmadı:** Elle oynayış ve his denetimi.
- Görünüm işleri (arazi rengi, tren, viyadük) ve vurulan uçağın düşüş görünümü.
- Tam tur testi, Windows build ve teslim kopyası.

## 8. Faz 4: Görünüm, tam tur, varlık denetimi, build (11.11.2026)

### 8.1 Görünüm (D-119, D-120)
- **Ortam ışığı:** Sahne batch modda ışık pişirilmeden kurulduğu için ortam ışığı yoktu ve gölgede kalan her şey siyahtı. Bölüm 4'e `GeceOrtami` eklendi; açılışta ortam ışığını yeniden hesaplıyor (Lab 7, D-080 ile aynı çözüm).
- **Tren:** Quaternius'un renkleri (0,03–0,16) gün ışığında siluet gibi görünüyordu. Malzeme adına göre dönem renklerine açıldı: siyah lokomotif, gri kömür vagonu, kahverengi yük vagonları, yeşil konteyner.
- **Arazi:** Katmanlara daha yeşil bir ton verildi; renk doygunluğu +4'ten −6'ya indi. Zeytin/sarı ton azaldı ama tamamen gitmedi.
- **Vurulan uçak:** Artık havada donmuyor. O anki hızıyla düşüyor, burnu gidiş yönüne bakıyor, yavaşça yuvarlanıyor ve yerde duruyor.
- **HUD:** "GÖREV TAMAM" yalnız panelde görünüyor (bildirimde ikinci kez çıkmıyor). GPS bilgi satırı panelin içinde iki satıra bölünüyor.
- **Bilinen sınırlar (görünüm):**
  - Viyadük düz bir güverte ile ayaklardan oluşuyor; kemerli taş görünümü yok.
  - Dik yamaçlarda doku esniyor; Terrain dokuyu üstten izdüşürüyor.

Görüntüler: `Logs/lab8/gorsel/sahne_01 … 12` (editörde) ve `tur_01 … 06` (oyun içinde, HUD'lu).

### 8.2 Tam tur testi (TEST-827, `Lab8TurTests`, D-121)
- **Testin kapsamı:** Komut dosyalı bir pilot uçağı her karede seçili hedefin yakınında açık bir noktaya ya da lokomotifin üstüne koyar. Ateşi oyunun atış yardımı açar. Süre 5 kat hızlıdır.
  - Bu test görev döngüsünü ve yardımlı dengeyi ölçer; uçuşu ölçmez.
- **Sonuç:**
  - Tren güvenli bölgeye **300 s**'de vardı, canı **%98**.
  - **12 / 12** ateş kaynağı susturuldu: 4 tank, 4 uçaksavar, 4 uçak.
  - 120 mermi ve 2 bomba harcandı.
  - Üsse dönüş 30 s sürdü; "Görev tamam" ekranı geldi. Toplam süre **5:31**, yani video 7 dakikanın altında kalır.
- **Not:** Yardımlar açıkken görev kolay; bu, kullanıcının "rahat uçuş ve kolay vurma" isteğiyle uyumlu. Yardımlar kapalıyken (K ve O) denge, Faz 2'deki "oyuncu yoksa tren son bölgede ölür" ölçümüne dayanıyor.

### 8.3 Varlık denetimi (TEST-801, TEST-803, `Lab8VarlikTests`)
- Beş varlık türü (tren, ray, arazi dokusu, tank, düşman uçağı) `Assets/Bolum4` içinde. İndirilen dosyaların hiçbiri bu klasörün dışında değil.
- `VARLIKLAR.md`'de her model ve doku klasörü için bir satır var. Her satırda kaynak bağlantısı, lisans ve "değiştirildi mi" sütunu dolu. CC BY olan düşman uçağının satırında yazar adı yazıyor.
- Kayıt Faz 2–4 değişikliklerine göre güncellendi: tren renkleri, tankın ölçeği ve kulesi, düşman uçağının ölçeği ve malzemesi, arazi tonu.

### 8.4 Build
Komutlar: `BuildWindowsOyun` (menüden başlar; `Builds/Windows_Oyun/`) ve `BuildWindowsBolum4` (doğrudan Bölüm 4'ten başlar, video için; `Builds/Windows_Bolum4/`). Sonuç: `TestReport.md` §12.2.

## 9. Kullanıcı denemesinden sonra: arcade uçuş, ses, treni bulmak (11.11.2026)
Kullanıcı build'i oynadı ve şunları söyledi: uçağı rahat uçurmak mümkün değil (2–3 lab'dır aynı sorun); treni ve tankları göremiyor; uçağın sesi yok.

- **Arcade uçuş (D-122):** Bölüm 4'te varsayılan mod. Tuşlar uçağı doğrudan hareket ettiriyor.
  - **A/D:** Doğrudan döner (en çok 60°/s); uçak kendiliğinden yatar.
  - **W/S:** Tırmanır ya da dalar (en çok 40°/s); bırakınca düz uçar.
  - **Hız:** Gazla 145–340 km/sa. Tutunma kaybı yok.
  - **Korumalar:** Yere ve vadi duvarına çarpmaz, harita kenarından kendiliğinden döner.
  - **K:** ARCADE → KOLAY → GERÇEKÇİ modları arasında geçer; föyün modeli bir tuş uzakta kalır.
  - Lab 3–7 sahneleri değişmedi.
- **Ses (D-123):** Projede hiç ses yoktu. Motor sesi gaz ve hızla perdesini değiştiriyor; makineli, uçaksavar, düşman uçağı, tank ve patlama sesleri var. Hepsi kodla üretiliyor; indirilen dosya yok.
- **Treni ve hedefleri bulmak (D-124):**
  - HUD treni her zaman işaretliyor (can ve uzaklık); tren ekran dışındaysa kenarda bir ok çıkıyor.
  - Seçili hedef için de kenar oku var.
  - Lokomotifin üstünde 150 m'lik yarı saydam yeşil bir ışık sütunu var.
- **Testler (`Lab8ArkadTests`, 6/6):**
  - Elleri bırakınca 8 s'de 0,0 m yükseklik değişimi.
  - D 1,5 s basılı → 81° dönüş; bırakınca 0,3°/s'ye iniyor, kanatlar düz.
  - S 1 s basılı → 34° tırmanış; bırakınca 2,5 s'de düz uçuşa dönüyor.
  - Yere dalış ve Dar Vadi duvarı → çarpma yok.
  - Harita kenarından dönüyor; K mod döngüsü çalışıyor.
  - Motor perdesi 55 → 85 Hz; atışlar duyuluyor; tren arkadayken de kenar okuyla işaretli.
  - Ölçümler: `Logs/lab8/arkad_olcumu.txt`.
- **Elle doğrulama:** Kullanıcının yeniden denemesi bekleniyor. Uçuşun "çözüldüğü", ancak kullanıcı uçurduktan sonra söylenebilir.

## 10. Faz 5: Teslim paketi (11.11.2026, REQ-830, D-125)
- **Neden kırpılmış kopya:** Föy "kullanmadığın asset'leri sil" diyor, ama proje birikimli. Çalışma alanından hiçbir şey silinmiyor; betik ayrı ve kırpılmış bir kopya üretiyor.
- **`NU36.Editor.TeslimKopyasi.Listele`:** Bölüm 4'ün ihtiyaç listesini yazar:
  - AnaMenu ve Bolum4_KursunDaglari'nın bütün bağımlılıkları;
  - bütün `Assets/Scripts`;
  - `Assets/Settings` (URP);
  - girdi eylemleri ve `VARLIKLAR.md`.
  Ayrıca projede zaten çözülmeyen GUID'leri bir taban dosyasına yazar.
- **`Tools/Teslim/teslim_h8.ps1 -OgrenciNo <no> [-Video <mp4>] [-BuildYok] [-KopyayiSakla]`:**
  1. Listeyi yazdırır.
  2. Dosyaları `.meta`'larıyla, klasör `.meta`'larıyla, `Packages` ve `ProjectSettings` ile birlikte `%TEMP%\nu36_<no>\P` altına kopyalar. Yol kısa tutuldu: Windows'un 260 karakter sınırı uzun yolda URP paketini bozuyordu.
  3. Kopyayı Unity'de açar (`TeslimKopyasi.Dogrula`): build listesini iki sahneye indirir, eksik script arar, sahne/prefab/malzeme/asset dosyalarındaki her GUID'in kopyada çözülmesini denetler ve build alır.
  4. `Teslim\BMU1421_<no>_H8.zip` dosyasını yazar: kökte `Assets`, `Packages`, `ProjectSettings` ve `Video/bolum4.mp4`.
- **Kopyada olmayanlar:** Lab 2–7 sahneleri ve testler. Testler dışarıda, çünkü Lab 2–7 testleri kopyada olmayan sahnelere bağlı.
- **Deneme (`-OgrenciNo TEST`, videosuz):**
  - 261 dosya; kopyada **0** kopuk referans (şablondan kalma 11 URP GUID'i ana projede de çözülmüyor, yok sayıldı).
  - Kopyadan build başarılı (0 hata, 177 MB); oyuncu hatasız açıldı.
  - Zip **39 MB**; 581 girdi, 291 `.meta`; iki sahne ve `VARLIKLAR.md` içinde. `EditorBuildSettings` yalnız iki sahneyi içeriyor.
- **Denemede bulunan iki sorun:**
  - `SerializedProperty.objectReferenceInstanceIDValue` Unity 6.6'da kaldırılmış; derleme hatası verdi. Yerine dosya taramasıyla GUID denetimi yazıldı.
  - Uzun yol, paket dosyalarının açılmasını engelledi; kopya kısa bir yola alındı.
- **Video yönergesi:** `Documentation/Lab8_Video_Yonergesi.md`.
- **Kullanıcıda kalanlar:**
  - Videoyu kaydetmek (en çok 7 dk; üç bölge, bir hasar anı, Görev tamam).
  - Betiği öğrenci numarası ve videoyla çalıştırmak.
  - Zip'i OYS'ye yüklemek.
  - `Teslim\BMU1421_TEST_H8.zip` deneme dosyasıydı; kullanıcının izniyle silindi (11.11.2026).
