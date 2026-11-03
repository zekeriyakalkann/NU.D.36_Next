# NU.D.36 — Laboratuvar GDD'si (taslak, 2026-11-03)

> **Bu bir taslak.** Ders GDD şablonu (Word) çalışma alanında yok, o yüzden şablonun 1, 2, 4, 5 ve 10 numaralı başlıklarının adlarını bilmiyorum. Aşağıdaki bölümleri şablondaki karşılıklarına taşı. Sayıların hepsi projenin kendisinden alındı (sahne değerleri, testler, ölçüm dosyaları); kaynakları köşeli parantezde. Föy 4–6 sayfa istiyor, gereksiz gördüğün tabloları kısalt.

## Oyunun özeti
NU.D.36, 1940 dönemi bir sınır bölgesinde geçen tek kişilik bir uçuş oyunu. Oyuncu bir çift kanatlıyla keşif yapıyor, ardından bombalama görevlerine çıkıyor. Düşman radarsızdır: ses dinleyici, göz ve ışıldakla arar. Oyuncunun gizlenme araçları da bunlara karşılık gelir: tepe arkası, motoru susturmak, bulut ve gece. Her bölüm bir fizik fikrini oyun mekaniğine çevirir.

**Çekirdek döngü:** kalk → hedefi bul/vur → hayatta kal → üsse dön → puan/ikmal → sonraki bölüm.

## Kontroller
W/S burun, A/D yatış, Q/E sapma, ↑/↓ gaz, Space bomba, R yeniden başlat. [Ucus eylem haritası, BombaYuku, SeviyeSonuEkrani]

## Bölüm 1 — Çakıl Burnu: keşif döngüsü (Rota04Hud)
| Kural | Değer [kaynak] |
|---|---|
| Hız | 8–42 m/s, ivme 6 m/s², tırmanışta yavaşlar, dalışta hızlanır [Rota04Hud: UcusKontrol] |
| Dönüş | Yatışla koordineli dönüş, en çok 65° yatış ve 45°/s [Lab4FlightSceneBuilder] |
| Hedef | 3 keşif hedefi, üsten 424–430 m uzakta, her biri 100 puan [Lab4SceneBuilder] |
| Süre | 150 s [Rota04Hud: GorevYoneticisi] |
| İniş | Hız ≤ 16 m/s, yunuslama hatası ≤ 18°, yatış ≤ 27°, arazi eğimi ≤ 20°; aşılırsa çakılma [InisAlgilayici] |
| Kilit | Hurricane eşiği projede 300 puan; föyün ekonomisi 400 (420 üzerinden). Fark bilerek bırakıldı [D-042] |

## Bölüm 2 — Sınır Hattı: mühimmat avı (Rota05)
| Kural | Değer [kaynak] |
|---|---|
| Uçak | Hurricane ölçeğinde: 30–90 m/s (başlangıç 66), burun 30°/s ≈ 85 m/s'de 4,5 g [Lab5SceneBuilder] |
| Hedef | 4 mühimmat mevzii, can 100, mevzi başına 100 puan + başarıda kalan saniye [MuhimmatMevzii, SeviyeYoneticisi] |
| Yük / süre / ikmal | 2 bomba; 180 s; üste ikmal: dolu yük ve +90 s, sadece yük eksikse [SeviyeYoneticisi] |
| Bomba | Yarı örtük Euler, patlama 25 m ve 100 hasar; 120 m altından bırakılırsa uçak kendi parçasından 40 hasar alır [Bomba] |
| Bırakma penceresi | Dalış 45–70°, yerden 120–260 m; menzil tablosuyla örtüşüyor (h 150 m: 30°→198, 45°→128, 60°→77, 70°→49 m) [Lab5FormulTests] |
| Uçaksavar (3) | Görüş konisi 120° yarı açı, görüş 1500 m, ateş menzili 1000 m, 0,3 s'de bir, 8 hasar, mermi 260 m/s, ölüm bandı 15–460 m, band dışında 6° saçılma [UcaksavarMevzii] |
| Görüş hattı | Göz 3 m'de, Arazi + Yapi katmanına ışın; 8 mevzi kare başına ≈ 0,012 ms [Logs/lab5/olcum_gorus.txt] |
| Ses dinleyici | 343 m/s, 3000 m'ye kadar duyar, gaz 0,15 altında duymaz; ölçülen sapma 600 m'de, 85 m/s ile 148,7 m (hesap 149 m) [Lab5FormulTests] |
| Arazi | 3 tepe: 120 / 170 / 230 m (Tepe 3) [Lab5SceneBuilder] |

## Bölüm 3 — Gece Köprüsü (tasarım, kâğıt v0.1)
- **Harita:** 12 × 9 kare, 1 kare = 200 m. Pist B8–D8; nehir I1–J9; köprü H5–J5 (25 m).
- **Tehditler:** 3 ışıldak (F4 → GD, K5 → B, H7 → K; ışın 6 kare), 3 uçaksavar (G4, K4, I7; menzil 4 kare), ses dinleyici H3 (5 kare).
- **Tepeler:** E4, F7, G7 (120 m: Alçak'ı keser, Orta'yı kesmez).
- **Tek kural, Bulut:** "E5, F5 ve G5 bulut karesidir: Orta irtifadaki uçağı bu 3 karede ışıldak yakalayamaz."
- **İki yol:** Yol 1 tepe arkası, alçak, 8 hamle. Yol 2 açık, bulut içinden, 5 hamle. Ayrıntı: `Lab6_Bolum3_Tasarim.md`.
- **Gri kutu süreleri** (piste dönüş): Yol 1 49,5 s (3,3 tur), Yol 2 41,0 s (2,7 tur) [Logs/lab6/sure_olcumu.txt].
- **Kâğıt testleri:** v0.1 test sorusu, gözlem satırları, v0.2 değişikliği → *eşle oynadıktan sonra doldur*.

## Değişiklik günlüğü: kâğıt → Unity
| Kâğıtta | Unity gri kutuda | Neden |
|---|---|---|
| 1 kare | 200 m | Föy ölçeği |
| 1 tur = 15 s, 1 kare/tur (varsayım) ≈ 13 m/s | Uçak 66 m/s: ekran süresi kâğıdın 0,21–0,27'si | Kâğıt tur sayısı ekranda 4–5 kat kısa; tur başına kare sayısı kitle doğrulanmalı |
| Alçak = 30 m | Köprünün üst yüzü 35 m: köprüye Alçak'ta yaklaşan uçak çarpar; Yol 1 ölçümü 60 m'den | Köprü 25 m merkezli, 20 m kalın |
| Işıldak oku | 1200 m sabit, yarı saydam kutu | Gece ve dönen ışık 7. haftanın işi |
| Bulut kareleri | Orta irtifada (200 m) yarı saydam kutular, çarpıştırıcı yok | Kural görsel; ışığın yakalaması henüz yok |
| Uçaksavar ve ses menzili halkası | Yarı saydam disk (Ø 1600) ve halka (Ø 2000) | Föy |
| Harita sınırı | Plane'in dışı boş; sis 1800–4000 m | Bütün harita pistten görünsün diye |

## Ek A — Kâğıtlar
- Basılı harita v0.1: `Exports/Lab6/bolum3_harita_v0.1.png`; boş ızgara: `Exports/Lab6/bolum3_bos_harita.png`.
- Gri kutunun üstten görünüşü: `Logs/lab6/gorsel/01_ustten_plan.png`.
- *Kâğıtların fotoğraflarını (görev sayfası, gözlem formu, yineleme kaydı) buraya ekle.*
