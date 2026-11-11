# Lab 6 (Hafta 6): Kâğıt Görev — Bölüm 3 tasarımı ve gri kutu

Tarih: 2026-11-03. Kaynak: konuşmaya yapıştırılan "Laboratuvar Föyü: Kâğıt Görev" metni. Föyün kâğıt kiti (kural kartı, ısınma haritası, Bölüm 3 eki, gözlem formu, yineleme kaydı), tarayıcıdaki Kâğıt Sortie oyunu ve GDD şablonu (Word) çalışma alanında **yok**. Bu yüzden aşağıdaki kâğıt tasarım bir **öneridir (v0.1)**. Kitin 4. sayfasındaki sabit kurallarla (hareket, tur sayısı, can, bomba) karşılaştırılıp gerekirse düzeltilmeli.

Durum değerleri: IMPLEMENTED, VERIFIED, PARTIALLY VERIFIED, NOT TESTED, BLOCKED, KULLANICI (eşle kâğıt üzerinde yapılacak iş).

## 1. Föy adımları ve durum
| Adım | İş | Durum |
|---|---|---|
| 01 Isınma | Kâğıt motoru, 2 × 4 tur, tarayıcıda "3 · Gece" kipi | KULLANICI (kit ve tarayıcı oyunu bende yok) |
| 02 Tasarım | Harita v0.1, tek kural, iki yol, görev sayfası önerisi (§2) | IMPLEMENTED: veri `Bolum3Harita.cs`, çıktı `Exports/Lab6/bolum3_harita_v0.1.png`; tasarım kuralları testli (`Lab6Tests`) |
| 03 Sessiz gözlemci | Eşe oynatma, ≥ 6 satırlık form, 3 soru | KULLANICI (§3'te test sorusu önerisi; basılı form: `Exports/Lab6/kagit/bolum3_kagit_formlari.html`) |
| 04 Tek değişiklik | v0.2, yeniden oynatma | KULLANICI (§3'te aday değişiklikler; yineleme kaydı aynı HTML'de) |
| 05 Gri kutu | `Bolum3_GriKutu` sahnesi, ölçüler föydeki gibi, uçuş ve süre ölçümü | VERIFIED (§4) |
| 06 Laboratuvar GDD'si | Ölçülmüş sayılarla taslak: `Lab6_GDD_Taslagi.md` | IMPLEMENTED (taslak; Word şablonuna aktarmak KULLANICI) |
| Bonus | — | Yapılmadı |

## 2. Bölüm 3 haritası v0.1 (öneri)
Izgara 12 × 9 kare (A–L batıdan doğuya, 1–9 kuzeyden güneye), 1 kare = 200 m. Basılabilir harita: `Exports/Lab6/bolum3_harita_v0.1.png`. Elle yeniden tasarım için boş ızgara: `Exports/Lab6/bolum3_bos_harita.png`.

| Öğe | Kareler | Not |
|---|---|---|
| Pist (3 kare) | B8, C8, D8 | Başlangıç C8, Alçak, burun KD (ısınma kuralı) |
| Nehir | I1–I4, H5–J5, J6–J9 | Köprünün altında 3 kare genişliyor |
| Köprü (3 kare) | H5, I5, J5 | Nehrin üstünde, 25 m |
| Tepeler (120 m) | E4, F7, G7 | F7–G7 sırtı güney yolunu saklıyor |
| Işıldaklar (yön) | I1 F4 (GD, 135°), I2 K5 (B, 270°), I3 H7 (K, 0°) | Işın 6 kare; I2 köprü boyunca bakıyor |
| Uçaksavarlar | U1 G4, U2 K4, U3 I7 | Menzil 4 kare (Ø 1600 m) |
| Ses dinleyici (S) | H3 | Menzil 5 kare (Ø 2000 m) |
| Tek kural: **Bulut** | E5, F5, G5 | "E5, F5 ve G5 bulut karesidir: Orta irtifadaki uçağı bu 3 karede ışıldak yakalayamaz." |

**İki yol**
- **Yol 1, tepe arkası (alçak):** C8 → D8 → E8 → F8 → G8 → H8 → H7 → H6 → H5 (8 hamle). Satır 8, F7–G7 sırtı yüzünden U1'den görünmüyor. Bedeli şu: son bacak U3'ün dibinden ve I3'ün üstünden köprüye çıkıyor.
- **Yol 2, açık:** C8 → D7 → E6 → F5 → G5 → H5 (5 hamle). Kısa ve açık bir yol; U1'in menzilinden geçiyor. Bulut kuralı yüzünden Orta irtifada I1 ile I2'nin ışınları uçağı yakalayamıyor, ama Orta irtifa dalış zamanı ister.

Tasarım niyeti şu: kısa yol bulutla ışıktan korunur ama uçaksavardan korunmaz; uzun yol tepeyle göze karşı korunur ama süre yer. İki yol da föydeki "üç farklı gizlilik, üç farklı bedel" fikrine bağlı.

**Görev sayfası önerisi** (kitin 4. sayfasıyla karşılaştır; sayılar INFERRED)
- Tek cümle: "Gece, köprüyü şafaktan önce yık ve piste dön."
- Gece/son tur: 12. tur şafak.
- Can 3, bomba 2, başlangıç C8 Alçak burun KD.

## 3. Test ve yineleme önerileri (Adım 03–04)
- **Test sorusu (v0.1):** "Oyuncu köprüye ilk denemede açık yolu (Yol 2) mu seçiyor?" Cevap evet/hayır.
- **Gözlem formu:** Tur başına bir satır yaz (ne yaptı, ne dedi); yorum sütunu boş kalsın. Sonunda üç soruyu sor.
- **Tek değişiklik adayları** (gözleme göre yalnız birini seç):
  - Bulutu Yol 2'den çek (E5–G5 → E3–G3).
  - I2'nin başlangıç yönünü 270°'den 225°'ye çevir.
  - U1'i G4'ten G3'e taşı.

  Değişikliği ve gerekçesini yineleme kaydına yaz, sürümü 0.2 yap. Harita verisi tek yerde durduğu için (`Bolum3Harita.cs`) aynı değişiklik gri kutuya da tek satırla taşınır.

## 4. Gri kutu (Adım 05)
`NU36 → Lab 6 → Create Bolum3_GriKutu Scene` (`Lab6SceneBuilder.Create`), Rota05'in kopyasıdır ("Save As"). Rota05 ve önceki sahneler bayt bayt aynı kaldı (MD5 testi).

| Föy | Gri kutuda | Test |
|---|---|---|
| GriKutu (3000, 0, 0); Plane Scale (240, 1, 180) = 2400 × 1800 m | aynen | `Ground_At3000_2400x1800_LayerArazi` |
| Nehir mavi kutu (200, 2, 200) | 11 kutu | `RiverBridgeHills_…` |
| Köprü 3 sarı kutu (200, 20, 60), 25 m | merkezleri 25 m'de | aynı |
| Tepe gri küp (200, 120, 200) | zemine oturuyor | aynı; `Hill120m_CutsAlcak30m_NotOrta200m` (Play) |
| Işıldak beyaz silindir; ışın (8, 8, 1200) yarı saydam, bir ucu ışıldakta, ok yönünde | aynen; ışının çarpıştırıcısı yok | `Searchlights_…` |
| Uçaksavar kırmızı; menzil (1600, 0.1, 1600) yarı saydam | aynen | `Guns_…` |
| Ses dinleyici mor; Ø 2000 m halka | halka mesh'i | aynı |
| Pist mavi kutu | koyu mavi, 3 kare | — |
| Zemin, tepeler, köprü → Arazi | aynen | testler |
| Uçak pistin üstünde | C8, 30 m (Alçak), burun KD | `Aircraft_…` |
| Grid Size 50 | Scene penceresinin snap ayarı (sahneye kaydedilmez) | KULLANICI |

Ek olarak:
- **Bulut kuralı:** Bulut karelerinde Orta irtifada (200 m) yarı saydam beyaz kutular var.
- **Kronometre:** Play ile başlar; köprüye varışı ve piste dönüşü tur karşılığıyla HUD'a yazar. R tuşu sahneyi yeniden başlatır.
- **Bölüm 2 düzeni kapalı:** Arazi, tepeler, mevziler ve seviye bu sahnede kapatıldı (silinmedi).
- **Sis:** Bütün harita görünsün diye sis 1800–4000 m'ye, kamera uzaklığı 5000 m'ye çekildi.

**Süre ölçümü** (`Logs/lab6/sure_olcumu.txt`): Gerçek uçuş modeliyle (gaz 0,6 ≈ 66 m/s) uçuldu; testteki otopilot burnu en çok 30°/s döndürdü.

| Yol | İrtifa | Köprü | Piste dönüş | Tur karşılığı | Kâğıt (1 kare = 1 tur varsayımı) | Ekran / kâğıt |
|---|---|---|---|---|---|---|
| Yol 1 tepe arkası | 60 m | 22,6 s | 49,5 s | 3,3 tur | 16 hamle = 16 tur = 240 s | 0,21 |
| Yol 2 açık | 200 m | 15,7 s | 41,0 s | 2,7 tur | 10 hamle = 10 tur = 150 s | 0,27 |

**Fark:** Ekran kâğıttan 4–5 kat hızlı. 1 kare/tur ve 15 s/tur, kâğıtta 13 m/s'lik bir hız demek; uçak ise 66 m/s uçuyor. Kâğıttaki hareket kuralı kitte (bende yok); kartlar turda birden çok kare yürütüyorsa fark küçülür.

Bu ölçüm hakkında iki not:
- **Yol 1 irtifası:** Köprü 25 m merkezli olduğu için üst yüzeyi 35 m'de. Bu yüzden Yol 1 testte 60 m'den uçuldu; 30 m'de uçak köprüye çarpardı. Bu da kâğıt → Unity farklarından biri (bkz. GDD değişiklik günlüğü).
- **Elle uçuş:** Kronometreyle elle uçuş senin işin. Player: `Builds/Windows_Bolum3_GriKutu/NU.D.36.exe`.

## 5. Sınırlar
- Kit, tarayıcı oyunu ve GDD şablonu yok: hareket kuralları, son tur, can ve bomba sayıları kâğıt tasarımda öneri olarak duruyor.
- Gri kutuda gece, ışık ve köprü çöküşü yok; bunlar 7. haftanın işi (föy). Işıldaklar ve uçaksavarlar sadece kutu, ateş etmiyor.
- Hız kadranının yüzü hâlâ 0–50 (Lab 5 notu).

## 6. 7. hafta (2026-11-10)
Bu harita v0.1, Rota07'de gece seviyesi olarak büyüdü (`Lab7_Implementation.md`). Her öğe kâğıttaki karesinde duruyor (test edildi). v0.2 değişikliği `Bolum3Harita.cs`'de yapılırsa gri kutu ve gece seviyesi aynı veriden yeniden kurulur.

