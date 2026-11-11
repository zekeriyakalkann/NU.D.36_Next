using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using NU36.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NU36.Tests
{
    /// <summary>Lab 7 (Hafta 7): night-map rules (pure functions) and the Rota07 scene structure.</summary>
    public class Lab7Tests
    {
        Scene scene;

        [TearDown]
        public void Close()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        static string Md5(string path)
        {
            using var md5 = MD5.Create();
            return string.Concat(md5.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2")));
        }

        GameObject[] Open()
        {
            scene = EditorSceneManager.OpenScene(Lab7SceneBuilder.ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene.GetRootGameObjects();
        }

        static T Tek<T>(GameObject[] roots) where T : Component =>
            roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();

        static T[] Hepsi<T>(GameObject[] roots) where T : Component =>
            roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        // ---- map ----------------------------------------------------------------------------------------------------------

        [Test]
        public void Runway_IsFlatAt22m_AlongItsWholeLength()
        {
            Vector3 a = GeceHaritasi.PistBasi, b = GeceHaritasi.PistSonu;
            Assert.That(Vector3.Distance(a, b), Is.EqualTo(GeceHaritasi.PistBoyu).Within(0.1f));
            for (int i = 0; i <= 20; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / 20f);
                foreach (float yan in new[] { -60f, 0f, 60f })
                {
                    Vector3 q = p + Vector3.Cross(Vector3.up, GeceHaritasi.PistEkseni) * yan;
                    Assert.That(GeceHaritasi.Yukseklik(q.x, q.z), Is.EqualTo(GeceHaritasi.PistYuksekligi).Within(0.05f), $"runway ground at {i}/20, {yan} m");
                }
            }
        }

        [Test]
        public void ApproachCorridor_ClearsA3DegreeGlidePath_BothEnds()
        {
            foreach (var (esik, yon) in new[] { (GeceHaritasi.PistSonu, GeceHaritasi.PistEkseni), (GeceHaritasi.PistBasi, -GeceHaritasi.PistEkseni) })
                for (float s = 200f; s <= 3000f; s += 100f)
                {
                    Vector3 p = esik + yon * s;
                    float sus = GeceHaritasi.PistYuksekligi + s * Mathf.Tan(3f * Mathf.Deg2Rad);
                    Assert.Less(GeceHaritasi.Yukseklik(p.x, p.z), sus - 8f, $"terrain {s} m from the threshold is below the 3° path with 8 m to spare");
                }
        }

        [Test]
        public void PaperMap_EveryElementStandsOnItsSquare_OneSquareIs200m()
        {
            // Bridge H5–J5: 3 squares long, over the water, ends on the banks under the 25 m deck.
            Vector3 k = GeceHaritasi.KopruMerkezi;
            Assert.AreEqual("I5", GeceHaritasi.KareAdi(k.x, k.z));
            Assert.AreEqual(3f * Bolum3Harita.Kare, GeceHaritasi.KopruBoyu, "bridge = 3 squares");
            Assert.AreEqual(Bolum3Harita.KopruYuksekligi, GeceHaritasi.KopruGuvertesi, "deck 25 m over the water");
            foreach (string kare in Bolum3Harita.Kopru)
            {
                Vector3 c = GeceHaritasi.Kare(kare);
                Assert.Less(GeceHaritasi.Yukseklik(c.x, c.z), GeceHaritasi.SuSeviyesi, $"water under the bridge at {kare}");
            }
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 uc = k + GeceHaritasi.KopruEkseni * s * (GeceHaritasi.KopruBoyu / 2f);
                Assert.That(GeceHaritasi.Yukseklik(uc.x, uc.z), Is.InRange(GeceHaritasi.SuSeviyesi + 1f, GeceHaritasi.KopruGuvertesi - 5f), $"bridge end {s} on the bank");
            }
            // River squares hold water; the other elements stand on dry land on their own squares.
            foreach (string kare in Bolum3Harita.Nehir)
            {
                Vector3 c = GeceHaritasi.Kare(kare);
                Assert.Less(GeceHaritasi.Yukseklik(c.x, c.z), GeceHaritasi.SuSeviyesi, $"river at {kare}");
            }
            for (int i = 0; i < GeceHaritasi.IsildakSayisi; i++)
            {
                Vector3 kule = GeceHaritasi.KuleKonumu(i);
                Assert.AreEqual(Bolum3Harita.Isildaklar[i].kare, GeceHaritasi.KareAdi(kule.x, kule.z));
                Assert.Greater(kule.y, GeceHaritasi.SuSeviyesi + 2f, $"searchlight {i} on dry land");
            }
            for (int i = 0; i < Bolum3Harita.Ucaksavarlar.Length; i++)
            {
                Vector3 top = GeceHaritasi.UcaksavarKonumu(i);
                Assert.AreEqual(Bolum3Harita.Ucaksavarlar[i], GeceHaritasi.KareAdi(top.x, top.z));
                Assert.Greater(top.y, GeceHaritasi.SuSeviyesi + 2f, $"gun {i} on dry land");
            }
            Vector3 ses = GeceHaritasi.SesDinleyiciKonumu;
            Assert.AreEqual(Bolum3Harita.SesDinleyici, GeceHaritasi.KareAdi(ses.x, ses.z));
            // Ranges from the paper: beam 6 squares, gun Ø 8 squares, locator Ø 10 squares.
            Assert.AreEqual(1200f, GeceHaritasi.IsildakMenzili);
            Assert.AreEqual(800f, GeceHaritasi.UcaksavarYaricapi);
            Assert.AreEqual(1000f, GeceHaritasi.SesYaricapi);
        }

        [Test]
        public void PaperMap_Hills120m_CutAlcak_NotOrta_AndTheRunwayIsOnRow8()
        {
            foreach (string kare in Bolum3Harita.Tepeler)
            {
                Vector3 c = GeceHaritasi.Kare(kare);
                Assert.That(GeceHaritasi.TepeKatkisi(c.x, c.z), Is.EqualTo(120f).Within(1f), $"hill {kare} 120 m");
                float tepe = GeceHaritasi.Yukseklik(c.x, c.z);
                float cevre = GeceHaritasi.Yukseklik(c.x, c.z - 2f * Bolum3Harita.Kare);   // two squares south
                Assert.Greater(tepe, cevre + Bolum3Harita.Alcak, $"{kare}: cuts a low (30 m) flight");
                Assert.Less(tepe, cevre + Bolum3Harita.Orta, $"{kare}: an Orta (200 m) flight passes over");
            }
            // Runway: paper B8–D8 lie inside the landing zone (the return to base), row 8, east end on D8's east edge.
            Assert.AreEqual(GeceHaritasi.Kare("C8").z, GeceHaritasi.PistSonu.z, 0.01f);
            Assert.AreEqual(GeceHaritasi.Kare("D8").x + 100f, GeceHaritasi.PistSonu.x, 0.01f);
            Assert.AreEqual(90f, GeceHaritasi.PistYonu);
            foreach (string kare in Bolum3Harita.Pist)
            {
                Vector3 c = GeceHaritasi.Kare(kare);
                Assert.That(GeceHaritasi.Yukseklik(c.x, c.z), Is.EqualTo(GeceHaritasi.PistYuksekligi).Within(0.05f), $"runway on {kare}");
            }
            // Clouds of the single rule: E5–G5, Orta band.
            foreach (string kare in Bolum3Harita.Bulutlar)
            {
                Bounds b = GeceHaritasi.BulutKutusu(kare);
                Vector3 c = GeceHaritasi.Kare(kare);
                Assert.IsTrue(b.Contains(new Vector3(c.x, Bolum3Harita.Orta, c.z)), $"{kare}: Orta inside the cloud");
                Assert.IsFalse(b.Contains(new Vector3(c.x, Bolum3Harita.Alcak, c.z)), $"{kare}: Alçak under the cloud");
                Assert.AreEqual(Bolum3Harita.Kare, b.size.x, 0.01f);
            }
        }

        [Test]
        public void PaperSquareNames_RoundTrip_AndTheTargetIsWithinReach()
        {
            for (int s = 0; s < Bolum3Harita.Sutun; s++)
                for (int r = 0; r < Bolum3Harita.Satir; r++)
                {
                    string ad = $"{(char)('A' + s)}{r + 1}";
                    Vector3 c = GeceHaritasi.Kare(ad);
                    Assert.AreEqual(ad, GeceHaritasi.KareAdi(c.x + 90f, c.z - 90f));
                }
            Assert.IsNull(GeceHaritasi.KareAdi(5000f, 0f));
            float d = Vector3.Distance(GeceHaritasi.PistSonu, GeceHaritasi.KopruMerkezi);
            Assert.That(d, Is.InRange(600f, 2000f), $"bridge {d:F0} m from the runway end");
            Assert.Less(GeceHaritasi.KopruMerkezi.magnitude, 2800f, "inside the mountain ring");
        }

        [Test]
        public void LongTarget_OnlyThePiecesNearTheHitFall()
        {
            var kok = new GameObject("test");
            var parcalar = new Transform[12];
            for (int i = 0; i < parcalar.Length; i++)
            {
                parcalar[i] = new GameObject("p" + i).transform;
                parcalar[i].SetParent(kok.transform);
                parcalar[i].position = new Vector3(i * 50f, 25f, 0f);
            }
            try
            {
                Assert.AreEqual(12, Yikilabilir.Secilenler(parcalar, new Vector3(100f, 0f, 0f), 0f).Count, "radius 0 = everything falls");
                var secilen = Yikilabilir.Secilenler(parcalar, new Vector3(100f, 0f, 0f), 60f);
                CollectionAssert.AreEquivalent(new[] { parcalar[1], parcalar[2], parcalar[3] }, secilen);
                Assert.AreEqual(1, Yikilabilir.Secilenler(parcalar, new Vector3(1000f, 0f, 0f), 60f).Count, "at least the nearest one");
            }
            finally { Object.DestroyImmediate(kok); }
        }

        [Test]
        public void Dawn_Blend_StartsLate_EndsAtOne()
        {
            Assert.AreEqual(0f, Safak.AydinlikHesapla(0.5f, 0.72f));
            Assert.AreEqual(0f, Safak.AydinlikHesapla(0.72f, 0.72f));
            Assert.That(Safak.AydinlikHesapla(0.86f, 0.72f), Is.InRange(0.3f, 0.7f));
            Assert.AreEqual(1f, Safak.AydinlikHesapla(1f, 0.72f));
            Assert.AreEqual("8:00", GeceHud.SureYazisi(480f));
            Assert.AreEqual("0:05", GeceHud.SureYazisi(4.2f));
            Assert.AreEqual("--:--", GeceHud.SureYazisi(float.PositiveInfinity));
        }

        // ---- scene --------------------------------------------------------------------------------------------------------

        [Test]
        public void EarlierScenes_AreUnchanged()
        {
            Assert.AreEqual("fee5968598add072519e4abffc067647", Md5("Assets/Scenes/Rota02.unity"));
            Assert.AreEqual("8d210ec5e8591cda4b0f48473db09e96", Md5("Assets/Scenes/Rota03.unity"));
            Assert.AreEqual("af087d445491ebe41ba67b8854e84d5b", Md5("Assets/Scenes/Rota04.unity"));
            Assert.AreEqual("664d5637761682ac6303a6fb31f02c12", Md5("Assets/Scenes/Rota04Hud.unity"));
            Assert.AreEqual("4680fa01303df2298a168652cb9cdd73", Md5("Assets/Scenes/Rota05.unity"));
            Assert.AreEqual("2864ebcaf382e90c2ce17ec78a3bb590", Md5("Assets/Scenes/Bolum3_GriKutu.unity"));
        }

        [Test]
        public void Rota07_IsInBuildSettings_AfterTheGreyBox()
        {
            var yollar = EditorBuildSettings.scenes.Select(s => s.path).ToList();
            Assert.Greater(yollar.IndexOf(Lab7SceneBuilder.ScenePath), yollar.IndexOf(Lab6SceneBuilder.ScenePath));
            Assert.IsTrue(EditorBuildSettings.scenes.Single(s => s.path == Lab7SceneBuilder.ScenePath).enabled);
        }

        [Test]
        public void Targets_ThreeSearchlightTowers_OneBridge_OnLayerHedef_WithHealth()
        {
            var roots = Open();
            var hedefler = Hepsi<Yikilabilir>(roots);
            Assert.AreEqual(3, hedefler.Count(h => h.Tur == HedefTuru.Kule));
            Assert.AreEqual(1, hedefler.Count(h => h.Tur == HedefTuru.Kopru), "one bridge (worksheet: \"bir köprü\", the narrow target)");
            Yikilabilir kopru = hedefler.Single(h => h.Tur == HedefTuru.Kopru);
            Assert.AreEqual(5 * GeceHaritasi.KopruAcikligi, kopru.DusenParcalar.Length, "10 m truss panels");
            Assert.AreEqual(Lab7Yapilar.KopruCokmeYaricapi, kopru.CokmeYaricapi, "only the spans near the bomb fall");
            Assert.IsTrue(kopru.DusenParcalar.All(p => p.GetComponent<BoxCollider>() != null), "standing panels can be hit");
            int hedefK = LayerMask.NameToLayer("Hedef"), yapi = LayerMask.NameToLayer("Yapi");
            foreach (Yikilabilir h in hedefler)
            {
                Assert.AreEqual(hedefK, h.gameObject.layer, h.name);
                Assert.IsNotNull(h.GetComponent<Saglik>());
                var kutu = h.GetComponent<BoxCollider>();
                Assert.IsTrue(kutu != null && kutu.enabled && !kutu.isTrigger, $"{h.name}: target collider");
                Assert.Greater(h.DusenParcalar.Length, 0, $"{h.name}: falling pieces");
                Assert.IsTrue(h.DusenParcalar.All(p => p != null && p.gameObject.layer == yapi), $"{h.name}: pieces on Yapi");
                if (h.Tur == HedefTuru.Kule)
                {
                    Assert.IsNotNull(h.Isildak, $"{h.name}: searchlight");
                    Assert.IsNotNull(h.Isildak.Hedef, "searchlight watches the aircraft");
                    Assert.AreEqual(GeceHaritasi.IsildakMenzili, h.Isildak.Menzil, "paper beam 1200 m");
                    int i = System.Array.FindIndex(Bolum3Harita.Isildaklar, x => h.name.EndsWith(x.kare));
                    Assert.GreaterOrEqual(i, 0, h.name);
                    Assert.AreEqual(Bolum3Harita.Isildaklar[i].yon, h.Isildak.TaramaYonu, "paper starting direction");
                    Assert.IsNotNull(h.Isildak.Kulak, "turns to what the sound locator hears");
                }
            }
        }

        [Test]
        public void Aircraft_AerodynamicModel_OnTheRunway_WithLightsBombsAndCrashProbes()
        {
            var roots = Open();
            var ucus = Tek<UcusKontrol>(roots);
            Assert.IsTrue(ucus.Aerodinamik, "aerodynamic flight model on");
            Vector3 p = ucus.transform.position;
            Assert.That(GeceHaritasi.Yukseklik(p.x, p.z), Is.EqualTo(GeceHaritasi.PistYuksekligi).Within(0.05f), "on the runway");
            Assert.That(ucus.transform.eulerAngles.y, Is.EqualTo(GeceHaritasi.PistYonu).Within(0.1f), "lined up for take-off (090°)");
            var so = new SerializedObject(ucus);
            Assert.AreEqual(0f, so.FindProperty("baslangicHizi").floatValue, "standing still");
            Assert.IsTrue(new SerializedObject(ucus.GetComponent<InisAlgilayici>()).FindProperty("yerdeBasla").boolValue);
            Assert.Greater(ucus.GetComponent<InisAlgilayici>().EnFazlaDususHizi, 0f, "sink rate checked at touchdown");
            Assert.IsNotNull(ucus.GetComponent<ZeminBaglayici>());
            var carp = ucus.GetComponent<CarpismaAlgilayici>();
            Assert.AreEqual(LayerMask.GetMask("Yapi", "Hedef"), carp.Engeller.value);
            var yuk = ucus.GetComponent<BombaYuku>();
            Assert.IsNotNull(yuk.BombaOn, "bomb prefab assigned");
            Assert.AreEqual(Lab7SceneBuilder.BombaKapasitesi, yuk.Kapasite);
            Assert.IsNotNull(yuk.BombaOn.GetComponent<Bomba>());
            var isik = ucus.GetComponent<UcakIsiklari>();
            Assert.IsNotNull(isik.InisFeneri, "landing light");
            Assert.AreEqual(LightType.Spot, isik.InisFeneri.type);
            Assert.Greater(Vector3.Dot(isik.InisFeneri.transform.forward, ucus.transform.forward), 0.95f, "the landing light points ahead");
        }

        [Test]
        public void Guns_HaveRounds_AndTheNightRule()
        {
            var roots = Open();
            var toplar = Hepsi<UcaksavarMevzii>(roots);
            Assert.AreEqual(3, toplar.Length);
            foreach (UcaksavarMevzii t in toplar)
            {
                var so = new SerializedObject(t);
                Assert.IsNotNull(so.FindProperty("mermiOn").objectReferenceValue, $"{t.name}: rounds");
                Assert.AreEqual(3, t.Isildaklar.Length, $"{t.name}: linked to the three searchlights");
                Assert.IsNotNull(t.Ucak);
                Assert.IsNotNull(t.Kulak, $"{t.name}: fires at the heard point");
                var mermi = (Mermi)so.FindProperty("mermiOn").objectReferenceValue;
                Assert.IsNotNull(mermi.GetComponent<TrailRenderer>(), "tracer rounds");
                Assert.AreEqual(GeceHaritasi.UcaksavarYaricapi, so.FindProperty("menzil").floatValue, "paper range Ø 1600 m");
            }
        }

        [Test]
        public void Mission_Hud_Airfield_AreWired()
        {
            var roots = Open();
            var g = Tek<GeceGorevi>(roots);
            var so = new SerializedObject(g);
            foreach (string ad in new[] { "yuk", "ucakSagligi", "algilayici", "ucus", "inisBolgesi", "pistIsiklari" })
                Assert.IsNotNull(so.FindProperty(ad).objectReferenceValue, ad);
            Assert.AreEqual(4, g.Hedefler.Length);
            Assert.IsNotNull(g.AnaHedef, "the bridge is the target");
            Assert.AreEqual(HedefTuru.Kopru, g.AnaHedef.Tur);
            Assert.AreEqual(Lab7SceneBuilder.SafakSuresi, g.SafakSuresi, "dawn");
            var safak = new SerializedObject(Tek<Safak>(roots));
            Assert.AreSame(g, safak.FindProperty("gorev").objectReferenceValue);
            Assert.IsNotNull(safak.FindProperty("ay").objectReferenceValue);
            Assert.IsNotNull(Tek<GeceSonuEkrani>(roots).Gorev);
            Assert.AreEqual(Lab7SceneBuilder.SceneName, Tek<GeceSonuEkrani>(roots).Sahne);
            var hud = new SerializedObject(Tek<GeceHud>(roots));
            foreach (string ad in new[] { "gorev", "kamera", "isiklar", "kulak", "hedefMetni", "gorevMetni", "bildirimMetni", "ucusMetni", "uyariMetni", "isaretKatmani", "yazi" })
                Assert.IsNotNull(hud.FindProperty(ad).objectReferenceValue, "HUD " + ad);
            var ikmal = Tek<IkmalAlani>(roots);
            Assert.AreSame(g, ikmal.Gorev);
            Assert.IsTrue(ikmal.GetComponent<BoxCollider>().isTrigger);
            var bolge = Tek<InisBolgesi>(roots);
            Assert.IsTrue(bolge.Iceride(GeceHaritasi.InisBolgesiMerkezi + Vector3.up * 2f), "zone centre");
            Assert.IsTrue(bolge.Iceride(GeceHaritasi.PistSonu - GeceHaritasi.PistEkseni * 20f + Vector3.up * 2f), "just after the 27 threshold");
            Assert.IsFalse(bolge.Iceride(GeceHaritasi.PistBasi + Vector3.up * 2f), "the far end is not in the zone");
        }

        [Test]
        public void Night_FogSkyLightsAndPostProcessing()
        {
            var roots = Open();
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(FogMode.ExponentialSquared, RenderSettings.fogMode);
            Assert.That(RenderSettings.fogDensity, Is.EqualTo(Lab7SceneBuilder.SisYogunlugu));
            Assert.Less(RenderSettings.fogColor.maxColorComponent, 0.2f, "dark fog");
            Assert.AreEqual("NU36/GeceGokyuzu", RenderSettings.skybox.shader.name);
            Assert.IsTrue(RenderSettings.skybox.HasProperty("_Safak"), "the sky can show the dawn");
            Light ay = Hepsi<Light>(roots).Single(l => l.type == LightType.Directional);
            Assert.Less(ay.intensity, 1f, "moonlight, not sunlight");
            Assert.Greater(ay.color.b, ay.color.r, "bluish");
            var vol = Tek<Volume>(roots);
            Assert.IsTrue(vol.isGlobal);
            Assert.IsTrue(vol.sharedProfile.TryGet(out Bloom bloom) && bloom.intensity.value > 0f, "bloom makes the lamps glow");
            Assert.IsNotNull(Tek<GeceOrtami>(roots));
            var cam = roots.Single(r => r.name == "Main Camera").GetComponent<Camera>();
            var ek = cam.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsNotNull(ek, "URP camera data");
            Assert.IsTrue(ek.renderPostProcessing);
            Assert.Greater(Hepsi<Isildak>(roots).Length, 2);
            Assert.IsTrue(Hepsi<Light>(roots).Count(l => l.type == LightType.Spot && l.range >= 1000f) == 3, "three searchlight spots");
        }

        [Test]
        public void SoundLocator_H3_And_Clouds_E5G5()
        {
            var roots = Open();
            var kulak = Tek<SesDinleyici>(roots);
            Assert.AreEqual(GeceHaritasi.SesYaricapi, kulak.DuymaMenzili, "paper ring Ø 2000 m");
            Assert.AreEqual(Bolum3Harita.SesDinleyici, GeceHaritasi.KareAdi(kulak.transform.position.x, kulak.transform.position.z));
            Assert.AreSame(Tek<UcusKontrol>(roots), kulak.Ucak);
            foreach (Isildak i in Hepsi<Isildak>(roots))
                Assert.AreSame(kulak, i.Kulak);
            var bulutlar = Hepsi<Bulut>(roots);
            CollectionAssert.AreEquivalent(Bolum3Harita.Bulutlar, bulutlar.Select(b => b.Kare));
            foreach (Bulut b in bulutlar)
            {
                Assert.IsTrue(b.GetComponentInChildren<ParticleSystem>() != null, $"{b.Kare}: visible cloud");
                Vector3 c = GeceHaritasi.Kare(b.Kare);
                Assert.IsTrue(b.Alan.Contains(new Vector3(c.x, Bolum3Harita.Orta, c.z)));
            }
        }

        [Test]
        public void Terrain_And_Water_AreGround()
        {
            var roots = Open();
            var arazi = Tek<GeceArazisi>(roots);
            Assert.AreEqual(LayerMask.NameToLayer("Arazi"), arazi.gameObject.layer);
            var su = roots.Single(r => r.name == "Nehir Suyu");
            var kutu = su.GetComponentInChildren<BoxCollider>();
            Assert.AreEqual(LayerMask.NameToLayer("Arazi"), kutu.gameObject.layer, "the water surface counts as ground (ditching = crash)");
            Assert.That(kutu.bounds.max.y, Is.EqualTo(GeceHaritasi.SuSeviyesi).Within(0.01f));
        }
    }
}
