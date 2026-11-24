# Lab 9 (Hafta 9): Bölüm 5 · Kartal Geçidi — Plan

Tarih: 2026-11-24. Durum: **uygulandı ve test edildi** (24.11.2026); sonuç ve sapmalar `Lab9_Implementation.md`'de. Kaynak: Konuşmaya yapıştırılan "Laboratuvar Föyü: Bölüm 5 · Kartal Geçidi" metni (11 kontrol maddesi). Kullanıcı tek seferde geliştirmeyi istedi ve tam yetki verdi. Bu yüzden özet → plan → geliştirme aşamaları arasında beklenmedi.

Sınıflar:
- **CONFIRMED:** Föyde yazıyor.
- **INFERRED:** Föyde yazmıyor; bizim kararımız. Gerekçesi yanında yazılı.
- **UNKNOWN:** Föyden çıkmıyor; engel olup olmadığı yanında yazılı.

## 1. Elimizdeki malzeme
| Öğe | Durum | Ne yapılacak |
|---|---|---|
| Föyün metni (11 madde) | Var | Tek kaynak |
| Harita çizimi, sayısal değerler (gemi hızı, can, süreler) | Yok | INFERRED (§4, §6). Engel değil |
| Teslim kuralları (zip adı, video) | Föyde yok | UNKNOWN. Engel değil: Hafta 8'in betiği aynı düzenle Bölüm 5 için de hazırlanır (§9) |
| Çıkarma gemisi modeli | Asset istenmiyor | Kendi üreticimizle (ilkellerden) yapılır: insan figürü yok, bayrak/arma yok |
| Tank, düşman uçağı, ağaç, kaya, dokular | Bölüm 4'te var (CC0 / CC BY, `VARLIKLAR.md`) | Yeniden kullanılır |
| Kum dokusu | Yok | Poly Haven `coast_sand_01` (CC0) indirildi: `Indirilenler/Bolum5/` |

## 2. Gereksinimler (föy → uygulama → test)
| REQ | Föy maddesi | Sınıf | Uygulama | Test |
|---|---|---|---|---|
| REQ-901 | Oyunda Bölüm 5 var: Bölüm 4'ten ayrı, yeni bir sahne | CONFIRMED | `Bolum5_KartalGecidi`, `Lab9SceneBuilder` ile Bölüm 4'ün kopyasından kurulur (uçak, kontroller, kamera, yardımlar, ses aynen). Bölüm 4 sahnesi bayt bayt aynı kalır | TEST-901 |
| REQ-902 | Uçak koya bir geçidin içinden geçerek varıyor | CONFIRMED | Batıdaki vadiden doğudaki koya, kuzey–güney uzanan yüksek sırtı kesen tek bir dar geçit (Kartal Geçidi). Uçak geçidin batı ağzında, havada başlar | TEST-902 |
| REQ-903 | Koyda aynı anda 4 çıkarma gemisi var; aralıklı duruyorlar | CONFIRMED | 4 şerit, kıyı boyunca 300 m aralıklı. Başlangıçta dört gemi de koyda, kıyıdan farklı uzaklıklarda | TEST-903 |
| REQ-904 | Kıyıya yanaşan gemi karaya 3 tank indiriyor | CONFIRMED | `CikarmaGemisi`: yanaşır → rampa iner → 3 tank sırayla iner, kıyıdan içeri sürer | TEST-904 |
| REQ-905 | Vurulan gemi duruyor: kalan tanklarını indiremiyor | CONFIRMED | Canı biten gemi yavaşlayıp durur, tüter, yana yatıp oturur; güvertede kalan tanklar inmez (indirme sırasında vurulsa da) | TEST-905 |
| REQ-906 | Durdurulan geminin yerine yenisi geliyor; o da tank indiriyor | CONFIRMED | `CikarmaFilosu`: şeridin gemisi durunca 6 s sonra açık denizden yenisi gelir | TEST-906 |
| REQ-907 | Düşman uçakları gemileri koruyor: oyuncuya saldırıyor | CONFIRMED | `DusmanUcagi`'na koruma kipi: koyun üstünde devriye; oyuncu koya yaklaşınca ona saldırır. `KorumaUcuslari` aynı anda 2 uçak tutar | TEST-907 |
| REQ-908 | Karaya çıkan tank sayısı ekranda yazıyor | CONFIRMED | `Bolum5Hud`: "KARADA TANK x / 12" her an | TEST-908 |
| REQ-909 | Karaya 12 tank çıkınca bölüm başarısız oluyor | CONFIRMED | `KartalGecidiGorevi`: 12. tank kıyıya değince "KIYI DÜŞTÜ" | TEST-909 |
| REQ-910 | 12 geminin hepsi geldikten sonra kıyı düşmediyse bölüm tamamlanıyor | CONFIRMED | 12 gemi gelir. Her biri ya durdurulur ya da indirmesini bitirir. Hepsi sonuçlanınca karada 12'den az tank varsa "BÖLÜM TAMAM" | TEST-910 |
| REQ-911 | Hiçbir yerde insan görünmüyor: gemiden yalnız araç iniyor | CONFIRMED | Gemi modeli ilkellerden: insan figürü yok. Gemiden yalnız tank iner. Sahnede insan modeli yok (ad ve model denetimi) | TEST-911, görsel kontrol |
| REQ-912 | (Kullanıcı tercihi) rahat uçuş ve kolay vurma | Kullanıcı isteği (föy dışı) | Bölüm 4'ün arcade uçuşu, nişan ve otomatik atış yardımları, GPS. Gemi hem makineli hem bomba hedefi | TEST-912 |

UNKNOWN (engel değil):
- "12 geminin hepsi geldikten sonra": Gemi indirmesini bitirdiğinde de yerine yenisinin gelip gelmeyeceği yazmıyor. Karar (INFERRED, D-127): Evet, şerit boşalınca yenisi gelir; böylece 12 gemi her durumda gelir ve bölüm her zaman sonuçlanır.
- Oyuncunun vurulması ve çakılması: Föyde yazmıyor. Bölüm 4'teki gibi "VURULDUN" ve "ÇAKILDIN" ile bölüm baştan başlar (INFERRED).

## 3. Föyün sayıları ve oyunun dengesi
- 4 gemi × 3 tank = 12. Oyuncu hiçbir şey yapmazsa ilk dört gemi kıyıyı düşürür. Föyün iki sayısı bu yüzden birbirine bağlı.
- 12 gemi × 3 = 36 tank gelebilir. Başarı için en çok 11 tank karaya çıkabilir: gemilerin çoğu yanaşmadan durdurulmalı.

## 4. Harita (INFERRED; çizim yok)
8 × 8 km. Kuzey +z, doğu +x. Deniz seviyesi y = 0.

| Bölge | Yer | İçerik |
|---|---|---|
| Başlangıç vadisi | Batı, x −3 000 … −1 000 | Dağlarla çevrili vadi, taban ~200 m. Uçak geçidin batı ağzına 600 m kala, yerden 150 m yüksekte, doğuya bakarak başlar |
| Sırt | x −1 000 … +700, kuzeyden güneye bütün harita | 950–1 450 m'lik dağlar; geçit dışında kesintisiz |
| **Kartal Geçidi** | z ≈ 0 çevresinde S biçimli, ~1,9 km | Taban 60 m genişliğinde, ~230–280 m yükseklikte. Duvarlar 700 m'nin üstüne çıkar. Doğu ağzında kıyı ovasına iner |
| Kıyı ovası ve kumsal | x ≈ +700 … +1 350 | Ova 10–50 m. Kumsal 80 m genişliğinde. Tanklar kumsaldan içeri, toplanma alanına sürer |
| **Kartal Koyu** | Kumsalın doğusu | İç bükey kıyı; kuzeyde ve güneyde iki burun. Ağız doğuya, açık denize açılır |

- **Şeritler:** Kıyıda z = −450, −150, +150, +450. Gemiler doğudan batıya, kıyıya dik gelir.
- **Uçuş sınırı (arcade):** Merkez (300, 0, 0), yarıçap 3 300 m.

## 5. Sistemler
### 5.1 Sahne (`Lab9SceneBuilder`)
- Bölüm 4 sahnesi `Assets/Scenes/Bolum5_KartalGecidi.unity`'ye kopyalanır. Var olan sahnenin üstüne yazılmaz; ayrıca bir "Rebuild" komutu var.
- Silinenler: Bölüm 4 dünyası (arazi, ray, tren, viyadük, istasyonlar, üs, bölgeler, görev, HUD, nehir).
- Kalanlar: uçak (arcade, makineli, bomba, nişan, motor dumanı ve sesi), kamera, güneş, Volume, ortam ışığı, savaş sesleri, duraklatma menüsü.
- Yeni dünya: arazi, deniz (görünür yüzey ve çarpıştırıcı), filo, koruma uçuşları, görev, HUD, GPS.
- Build Settings'in sonuna eklenir. Ana menü: "GÖREVE BAŞLA" Bölüm 5'i açar, "Bölüm Seç" listesine 8. satır eklenir (Lab 8'deki düzen).

### 5.2 Arazi (`Lab9Arazi`)
- Unity Terrain, 8 km, 1 025² örnek. Taban y = −60, böylece koyun dibi su altında kalır.
- Bölüm 4'ün dokuları kullanılır: çimen, kaya, uçurum, çakıl. Kumsal ve deniz dibi için yeni kum katmanı gelir.
- Ağaç ve kayalar Bölüm 4'ün prefab'larıdır.

### 5.3 Deniz
- Görünür yüzey: 8 km'lik düzlem, parlak koyu mavi-yeşil Lit malzeme.
- Çarpıştırıcı: y = 0'da ince bir kutu, "Arazi" katmanında. Sonuçları:
  - Mermi ve bomba suya çarpar.
  - Yer koruması ve çakılma, denizi zemin sayar.
  - Gemiler suyun üstünde yüzer.

### 5.4 Çıkarma gemisi (`CikarmaGemisi`, prefab)
- **Model:** İlkellerden bir tank çıkarma gemisi (LCT benzeri).
  - 46 × 10 m gövde, açık güverte, kıçta köprü üstü ve baca.
  - Pruvada menteşeli rampa.
  - İnsan, bayrak ya da arma yok.
- **Yük:** Güvertede 3 tank (Bölüm 4'ün tank modeli, silahsız `CikarmaTanki`).
- **Durumlar:** Yolda → Yanaştı (rampa iner, 3 s) → İndiriyor (5 s'de bir tank) → Ayrılıyor (rampa kalkar, geri çekilir, açık denize gider) → Gitti.
  - Canı biterse: Durdu (4 s'de durur, duman ve ateş, yana yatıp 2 m oturur; kalan tanklar güvertede kalır).
- **Hız:** 7 m/s, yanaşırken yavaşlar. Arkasında köpük izi bırakır.
- **Can ve hasar:** Can 240, taraf düşman. Makineli mermisi 10, bomba 100 hasar verir.

### 5.5 Tank (`CikarmaTanki`)
- Rampadan kumsala iner, toplanma alanındaki yerine sürer (5 m/s, araziye oturur). Kumsala değdiği an "karaya çıktı" sayılır.
- Silahsızdır: föy yalnız inişi sayıyor.

### 5.6 Filo (`CikarmaFilosu`)
- 4 şerit; başlangıç uzaklıkları 1 000 / 700 / 1 300 / 1 600 m. Toplam 12 gemi.
- Bir şeridin gemisi durdurulunca ya da indirmesini bitirince, 6 s sonra yenisi kıyıdan 1 400 m açıkta belirir.
- **Sayaçlar:** gelen, durdurulan, boşaltan, karadaki tank.
- **Olaylar:** `TankIndi`, `GemiDurdu`, `Bitti`.

### 5.7 Koruma uçuşları (`KorumaUcuslari`, `DusmanUcagi` koruma kipi)
- **Koruma kipi:** Uçak, koyun merkezi çevresinde (yarıçap 800 m, yükseklik 350 m) devriye uçar. Oyuncu koyun 2 400 m yakınına girince ona saldırır.
  - Varsayılan kapalıdır; Bölüm 4 değişmez.
- **Uçak sayısı:** Aynı anda 2 uçak, toplam 6. Vurulan uçağın yerine 30 s sonra yenisi gelir.
- **Değerler:** Bölüm 4'ün düşman uçağı prefab'ı aynen kullanılır (can 80; 2 hasarlık mermiler).

### 5.8 Görev (`KartalGecidiGorevi`)
- **Akış:** Savunma → Tamam.
- **Başarısızlıklar:** Kıyı düştü (12 tank), Vuruldun, Çakıldın. 4 s sonra yeniden başlar; R ile hemen.
- **Duyurular:** "KARTAL GEÇİDİ" (geçide girişte), "KARTAL KOYU — çıkarmayı durdur" (koya çıkışta), gemi durduruldu, tank karaya çıktı.

### 5.9 HUD (`Bolum5Hud`) ve GPS (`Bolum5Gps`)
- **Sol üst:**
  - UÇAK %
  - KARADA TANK x / 12
  - GEMİ gelen / 12 · durdurulan
  - MERMİ · BOMBA
  - GÖREV
- **Uyarılar:** "GEMİ KIYIDA — TANK İNİYOR", "ÇOK YÜKSEK — bomba", "MERMİ YOK".
- **İşaretler:**
  - Gemiler (uzaklık ve durum), düşman uçakları, seçili hedef.
  - Ekran dışındaki en yakın gemi için kenar oku.
- **Panel:** "BÖLÜM TAMAM" ya da "KIYI DÜŞTÜ" (karadaki tank, durdurulan gemi, düşürülen uçak, kalan cephane, süre).
- **GPS:** Deniz mavi, kumsal açık renk. Üstünde gemiler, uçaklar, karadaki tanklar, oyuncu ve hedef rotası.

### 5.10 Ortak kodda değişiklikler (geriye uyumlu)
- **`Susturulabilir`:**
  - Yeni tür `Gemi`; enum'un sonuna eklenir, kayıtlı değerler değişmez.
  - "Serbest hedef" kaydı: bölgeye bağlı olmayan hedefler. Varsayılanı kapalı.
- **`Bolum4Nisan`:** Serbest hedefleri de aday sayar. Gemi hem bomba hedefidir (güdüm, otomatik bırakma) hem makineli hedefidir. Tank yalnız bomba hedefi kalır.
- **`DusmanUcagi`:** Koruma kipi (varsayılan kapalı). Çekilme yönü harita merkezine göre ayarlanabilir.
- **`SavasSesleri`:** Serbest düşman uçaklarının atış sesleri.
- **`Lab8Dusmanlar`:** Yardımcı fonksiyonlar `public` yapılır; davranış aynı.

## 6. Değerler (INFERRED)
| Değer | Başlangıç |
|---|---|
| Gemi | 240 can; 7 m/s; rampa 3 s; tank aralığı 5 s; yeni gemi 6 s sonra, 1 400 m açıktan |
| Uçak | 100 can; 220 km/sa; 1 200 mermi; **6 bomba**; bomba tavanı yerden 300 m (Bölüm 4 kuralı) |
| Koruma uçağı | Aynı anda 2, toplam 6, yenisi 30 s sonra |
| Kıyı düşme sınırı | 12 tank (föy) |

Yardımsız oynayan biri için de ilk gemi yaklaşık 100 s sonra yanaşır. Geçitten koya uçuş yaklaşık 45 s sürer.

## 7. Testler
| TEST | Tür | Ölçülen |
|---|---|---|
| TEST-901 | EditMode | Sahne var, Build Settings'te; Bölüm 4 ve önceki sahneler MD5 ile aynı; Bölüm 4 dünyası yok; uçak, makineli, nişan, menü var |
| TEST-902 | EditMode | Başlangıç → koy düz çizgisini sırt keser; geçit hattı boyunca arazi uçuş yolunun altında; geçidin iki yanı 400 m'den yüksek; başlangıç geçidin batısında, koy doğusunda |
| TEST-903 | EditMode + PlayMode | 4 şerit, 250 m'den fazla aralıklı; oyun başında 4 gemi denizde, yolda |
| TEST-904 | PlayMode | Yanaşan gemi 3 tank indirir; tanklar kumsalda, karada; sayaç 3 |
| TEST-905 | PlayMode | İndirirken vurulan gemi durur; kalan tank inmez; sayaç artmaz |
| TEST-906 | PlayMode | Durdurulan geminin şeridine yenisi gelir ve tank indirir |
| TEST-907 | PlayMode | Koruma uçağı oyuncu yokken koyda devriye uçar; oyuncu gelince ona saldırır, ateş eder, can düşer |
| TEST-908 | PlayMode | HUD satırında "KARADA TANK x / 12" ve sayı doğru |
| TEST-909 | PlayMode | 12 tank → "KIYI DÜŞTÜ", yeniden yükleme |
| TEST-910 | PlayMode | 12 gemi gelip sonuçlanınca, karada <12 tank → "BÖLÜM TAMAM" |
| TEST-911 | EditMode | Gemi prefab'ında yalnız ilkel parçalar ve 3 tank var; sahnede adı insanı çağrıştıran nesne/model yok |
| TEST-912 | PlayMode | Tam tur: yardımlar ve komut dosyalı pilot ile 12 geminin hepsi geliyor, bölüm tamamlanıyor |

Görsel kayıtlar `Logs/lab9/gorsel/` klasörüne gider. Ölçümler `Logs/lab9/*.txt` dosyalarına yazılır.

## 8. Uygulama sırası (tek oturum)
1. Ortak kod değişiklikleri.
2. Yeni runtime sınıfları.
3. Harita, arazi, varlıklar, sahne kurucusu.
4. Derleme, sahneyi kurma.
5. EditMode ve PlayMode testleri (eski lab testleri dahil).
6. Görsel kayıt, build, belgeler.

## 9. Teslim
Föyde teslim kuralı yok. Hafta 8'in betiği (`teslim_h8.ps1`) genelleştirilip Bölüm 5 için de kullanılabilir. Zip'i oluşturmak kullanıcıya kalır.
