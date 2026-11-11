# VARLIKLAR — Bölüm 4 · Kurşun Dağları

Hafta 8 föyü, adım 01: Bölüm 4'te kullanılan her hazır varlık (asset) için bir satır. Hazır varlıkların hepsi `Assets/Bolum4` içindedir. İndirme tarihi: 11.11.2026.

| Varlık | Proje içindeki yeri | Kaynak bağlantısı | Lisans | Değiştirildi mi |
|---|---|---|---|---|
| Lokomotif (kömür vagonuyla) ve 4 yük vagonu: `Locomotive_Front`, `Locomotive_CoalTender`, `Locomotive_Wagon`, `CargoTrain_Container`, `CargoTrain_CoalContainer` | `Bolum4/Modeller/Tren` | Quaternius, "Modular Train": https://quaternius.com/packs/modulartrain.html | CC0 1.0 | Evet: ×1,10 ölçeklendi (lokomotif 15 m). Lokomotif ile kömür vagonu tek araçta birleştirildi. Malzeme renkleri açıldı (orijinali gün ışığında neredeyse siyahtı). Geometri değişmedi |
| Ray: `RailwayTrack_Straight` | `Bolum4/Modeller/Ray` | Quaternius, "Modular Train": https://quaternius.com/packs/modulartrain.html | CC0 1.0 | Evet: Ray açıklığı 1,435 m olacak biçimde ölçeklendi (×1,139). Düz parça, ray hattının eğrisi boyunca bükülerek döşendi (`Bolum4/Meshler/Ray_*`) |
| Tank: `Tank` | `Bolum4/Modeller/Tank` | Quaternius, "Animated Tanks": https://quaternius.com/packs/animatedtanks.html | CC0 1.0 | Evet: 7 m gövde olacak biçimde ölçeklendi; kule ve namlu, kodla dönen bir pivota taşındı; animasyonlar alınmadı. Geometri değişmedi |
| Düşman uçağı: "Aeroplane" (`DusmanUcagi`, dokusuyla) | `Bolum4/Modeller/DusmanUcagi` | Poly Pizza, yazar **Gilang Romadhan**: https://poly.pizza/m/9VeIc0cybp4 | CC BY 3.0. Yazar: Gilang Romadhan | Evet: GLB biçiminden Blender'la FBX'e çevrildi (`Tools/Blender/bolum4/glb_to_fbx.py`) ve dosya adı değiştirildi. 11 m kanat açıklığına ölçeklendi; dokusu yeni bir malzemeye bağlandı. Geometri değişmedi |
| Çam ağaçları (`PineTree_1–3`), kayalar (`Rock_1`, `Rock_2`, `Rock_Moss_1`, `Rock_Moss_3`) | `Bolum4/Modeller/Doga` | Quaternius, "Ultimate Nature Pack": https://quaternius.com/packs/ultimatenature.html | CC0 1.0 | Evet: Ölçeklendi ve proje malzemeleri (`Doga_*`) verildi. Arazi ağacı olarak dağıtıldı |
| Arazi dokuları: `cliff_side` (dik yamaç), `aerial_rocks_02` (kayalık), `aerial_grass_rock` (düzlük), `gravel_road` (ray yatağı); 2K, Diffuse ve Normal (GL) | `Bolum4/Dokular` | Poly Haven: https://polyhaven.com/a/cliff_side, https://polyhaven.com/a/aerial_rocks_02, https://polyhaven.com/a/aerial_grass_rock, https://polyhaven.com/a/gravel_road | CC0 | Hayır: dosyalar aynen; arazi katmanında renk tonu verildi |

## Hazır olmayan öğeler (kendi yapımımız)
- **Arazinin biçimi:** Dağların, vadinin, boğazın ve düzlüklerin yükseklikleri ray hattına göre kodla üretilir (`Scripts/Editor/Lab8Arazi.cs`). Yüzey dokuları, ağaçlar ve kayalar yukarıdaki hazır varlıklardır.
- **Yapılar:** Kalkış istasyonu, güvenli bölge deposu, üs (pist, hangarlar, kule) ve viyadük Unity'nin temel şekilleriyle kodla kurulur (`Scripts/Editor/Lab8Yapilar.cs`).
- **Oyuncunun uçağı:** NU.D.36, projenin kendi modelidir.
- **Uçaksavar mevzisi:** Hafta 7'deki kendi mevzi modelimizdir.
- **Sesler:** Motor, atış ve patlama sesleri hazır dosya değil, kodla üretilir (`Scripts/Runtime/MotorSesi.cs`, `SesBankasi.cs`).

## Denetim
- **Arma, bayrak, devlet sembolü:** Yok. Dokusu olan tek model düşman uçağı; dokusu (`DusmanUcagi_Doku.png`) düz yeşil gövdeden ve sarı şeritlerden oluşuyor. Tren, tank ve doğa modelleri düz renk malzemeli ve üzerlerinde işaret yok (11.11.2026, doku ve model görüntülerine bakılarak denetlendi; görüntüler `Logs/lab8/gorsel/yon_*.png`). Son gözle denetimi kullanıcı yapacak.
- **İnsan figürü:** Modellerin parça listesinde sürücü ya da pilot figürü yok (`Logs/lab8/model_inceleme.txt`). Düşman uçağı tek parçalı bir mesh; kabini kapalı ve dokusu koyu renkte. Kabinde figür olup olmadığı yakın plan görüntüde denetlenecek (Faz 4).
