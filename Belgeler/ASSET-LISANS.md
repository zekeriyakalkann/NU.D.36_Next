# Asset lisansı ve kaynak kaydı (Lab 4 arayüz görselleri)

## Güncel durum (2026-10-20): özgün Blender üretimi
On arayüz görseli bu projede Blender 5.2.2 LTS ile **özgün olarak modellenip render edildi**. İnternet görselleri kullanılmıyor; klasörleri çalışma alanından kaldırıldı. Aşağıdaki "Görsel başına kayıt" ve "Kullanıcıdan beklenen bilgiler" bölümleri bu kararla geçersizdir (tarihsel kayıt).

| Dosya (`Assets/UI/`) | Üretim | Kaynak dosyalar | Araç ve sürüm | Tarih | Üçüncü taraf içerik | Durum |
|---|---|---|---|---|---|---|
| hud-cerceve.png | Blender geometrisi, ortografik render, saydam film | `Blender/UI/ui_hud-cerceve.blend`, `Tools/Blender/ui/build_ui_assets.py` | Blender 5.2.2 LTS, Eevee | 2026-10-20 | Yazı tipi: Blender ile gelen Inter (SIL OFL). Başka yok | Özgün |
| gosterge-kadranlari.png | aynı | `ui_gosterge-kadranlari.blend`, aynı betik | Blender 5.2.2, Eevee | 2026-10-20 | Inter | Özgün |
| gosterge-ibre.png | aynı | `ui_gosterge-ibre.blend`, aynı betik | Blender 5.2.2, Eevee | 2026-10-20 | yok | Özgün |
| gaz-kolu-plakasi.png | aynı | `ui_gaz-kolu-plakasi.blend`, aynı betik | Blender 5.2.2, Eevee | 2026-10-20 | Inter | Özgün |
| gaz-kolu-topuz.png | aynı | `ui_gaz-kolu-topuz.blend`, aynı betik | Blender 5.2.2, Eevee | 2026-10-20 | yok | Özgün |
| pusula-seridi.png | aynı, dikişsiz şerit | `ui_pusula-seridi.blend`, aynı betik | Blender 5.2.2, Cycles (CPU) | 2026-10-20 | Inter | Özgün |
| hedef-isaretleri.png | aynı | `ui_hedef-isaretleri.blend`, aynı betik | Blender 5.2.2, Eevee | 2026-10-20 | yok | Özgün |
| dugme-seti.png | aynı | `ui_dugme-seti.blend`, aynı betik | Blender 5.2.2, Eevee | 2026-10-20 | yok | Özgün |
| puan-tablosu.png | aynı | `ui_puan-tablosu.blend`, aynı betik | Blender 5.2.2, Eevee | 2026-10-20 | yok | Özgün |
| menu-arkaplan.png | Blender sahnesi, uçak modeli `Exports/NU_D_36.fbx` içinden okundu | `ui_menu-arkaplan.blend` (uçaksız), `Tools/Blender/ui/build_menu_background.py` | Blender 5.2.2, Eevee, Nishita gökyüzü | 2026-10-20 | NU.D.36 uçak modeli (projenin kendi varlığı) | Özgün |

Güncelleme 2026-10-20 (ikinci tur): `gosterge-ibre`, `gosterge-kadranlari` ve `menu-arkaplan` aynı betiklerle, ayarlar değiştirilerek yeniden üretildi (ibre kuyruğu kısaldı, kadran yazıları aşağı alındı, menü sol üçte biri koyulaştırıldı). Önceki sürümler `Exports/Lab4_UI_backup_2026-10-20_*` altında yedekli. Kaynak ve yöntem değişmedi.

Lisans: görsellerde üçüncü taraf pikseli yok; haklar proje sahibine aittir. Bu yargı betiklerden ve render yönteminden çıkarılmıştır, hukuki görüş değildir. Yazı tipi Inter, SIL Open Font License ile Blender dağıtımındadır. Prompt veya yapay zekâ üretimi kullanılmadı.

---
Aşağıdaki bölümler tarihseldir.

Güncelleme: 2026-10-19.

## Durum
**On görselin hiçbiri üretilmedi.** Bu oturumda görsel üretim aracı yoktu (kullanılabilir araç listesinde görsel üreten bir araç veya model yok) ve görsellerin tam prompt metinleri bu oturuma verilmedi. Hiçbir görsel uydurulmadı, yer tutucu dosya konmadı; `Assets/UI/` içinde henüz görsel yok. Aşağıdaki tablo üretim tamamlandıkça doldurulacak. Model adı, tarih veya araç bilgisi uydurulmaz.

## Kayıt tablosu
Her satır için: dosya, araç/model, üretim tarihi, tam prompt, son işlem, son boyut, saydamlık, Unity import ayarları.

| # | Dosya (`Assets/UI/`) | Beklenen boyut | Beklenen saydamlık | Araç / model | Tarih | Prompt | Son işlem | Gerçek boyut | Gerçek saydamlık | Import |
|---|---|---|---|---|---|---|---|---|---|---|
| 01 | hud-cerceve.png | 1920×1080 | saydam | — (üretilmedi) | — | — | — | — | — | — |
| 02 | gosterge-kadranlari.png | 1024×512 | belirtilmemiş | — (üretilmedi) | — | — | — | — | — | — |
| 03 | gosterge-ibre.png | 512×512 | saydam (kadran/arka plan yok) | — (üretilmedi) | — | — | — | — | — | — |
| 04 | gaz-kolu-plakasi.png | 256×1024 | belirtilmemiş | — (üretilmedi) | — | — | — | — | — | — |
| 05 | gaz-kolu-topuz.png | 512×512 | saydam (arka plan yok) | — (üretilmedi) | — | — | — | — | — | — |
| 06 | pusula-seridi.png | 1536×128 | belirtilmemiş | — (üretilmedi) | — | — | — | — | — | — (Repeat gerekli) |
| 07 | hedef-isaretleri.png | 1024×1024 | saydam | — (üretilmedi) | — | — | — | — | — | — |
| 08 | dugme-seti.png | 768×1024 | belirtilmemiş | — (üretilmedi) | — | — | — | — | — | — |
| 09 | puan-tablosu.png | 1024×1280 | saydam | — (üretilmedi) | — | — | — | — | — | — |
| 10 | menu-arkaplan.png | 1920×1080 | opak | — (üretilmedi) | — | — | — | — | — | — |

## Hazır olan import kuralı
`NU36.Editor.Lab4UiTexturePostprocessor`, `Assets/UI/` içindeki bu on dosya içe aktarıldığında şunu uygular: Texture Type Sprite (2D and UI), Single, sıkıştırma yok, mip map kapalı, Wrap Clamp (`pusula-seridi.png` için Repeat), Alpha Is Transparency açık, NPOT ölçekleme yok. Başka dokular ve proje genel ayarları değişmez. Bu kural dosyalar gelmeden denenmedi; gerçek bir içe aktarma ile doğrulanacak.

## Lisans
Görseller üretildiğinde, üretim aracının kullanım koşulları ve çıktı hakları buraya yazılacak. Şu an kayıtlı bir görsel olmadığı için lisans bilgisi yok.

## Güncelleme 2026-10-20: gönderilen görseller (henüz içe aktarılmadı)
`NU_D_36_Pictures/` klasöründe on PNG bulundu (Golden Hour Seaside Airfield, Vintage Aviation HUD Icon Set, Vintage Aviation Scoreboard Panel, Vintage Steampunk Aircraft Dials, Vintage Steampunk Cockpit HUD Frame, Vintage Steampunk Compass HUD Strip, Vintage Steampunk Gauge Needle, Vintage Steampunk UI Control Sprite Sheet, Weathered Steampunk Control Panel, Worn Vintage Red Throttle Lever). Bu dosyalar için **kaynak, araç, model, tarih, prompt ve lisans bilgisi çalışma alanında yok**; hiçbiri uydurulmadı. Bu nedenle dağıtım veya teslim için uygun sayılmaz. Teknik inceleme ve olası eşleştirme: `Documentation/Lab4_Phase3_Assets.md` (son bölüm). Yukarıdaki kayıt tablosu bilerek değiştirilmedi.

## Görsel başına kayıt (2026-10-20)
Bu alanların hiçbiri çalışma alanında bulunmuyor; hepsi UNKNOWN. Dağıtım ve teslimat izni verilmiş sayılmaz.

| Kaynak dosya | Olası hedef | Kaynak | Üretim aracı | Model ve sürüm | Tarih | Kullanım koşulları | Dağıtım/teslimat izni | Durum |
|---|---|---|---|---|---|---|---|---|
| Golden Hour Seaside Airfield.png | menu-arkaplan.png | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Vintage Aviation HUD Icon Set.png | hedef-isaretleri.png (olası) | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Vintage Aviation Scoreboard Panel.png | puan-tablosu.png (olası) | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Vintage Steampunk Aircraft Dials.png | gosterge-kadranlari.png | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Vintage Steampunk Cockpit HUD Frame.png | hud-cerceve.png | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Vintage Steampunk Compass HUD Strip.png | pusula-seridi.png (uygun değil) | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Vintage Steampunk Gauge Needle.png | gosterge-ibre.png | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Vintage Steampunk UI Control Sprite Sheet.png | dugme-seti.png (olası) | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Weathered Steampunk Control Panel.png | gaz-kolu-plakasi.png (tahmin) | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |
| Worn Vintage Red Throttle Lever.png | gaz-kolu-topuz.png | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | UNKNOWN | Teslime hazır değil |

`Exports/Lab4_Phase3_Working/` içindeki çalışma kopyaları bu görsellerden türetilmiştir ve aynı bilinmeyen koşullara tabidir.

## Kullanıcıdan beklenen bilgiler (toplu liste)
1. On görselin her biri için: kaynak, üretim aracı, model ve sürüm, tarih, kullanım koşulları, ödev teslimi ve dağıtım izni.
2. Bu on görselin Faz 3 görselleri olduğunun ve olası eşleştirmenin onayı.
3. Pusula: yeniden üretim mi; kayan mı statik mi (Faz 4 metni).
4. Uyumsuz görseller (hedef işaretleri, düğme seti, puan tablosu, gaz plakası): yeniden üretim mi, mevcut düzenle kırpma ve dilimleme mi.
5. Föyün on prompt metni (varsa).
