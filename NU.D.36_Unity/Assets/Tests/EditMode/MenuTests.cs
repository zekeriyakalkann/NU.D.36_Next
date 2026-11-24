using System.Linq;
using NUnit.Framework;
using NU36.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NU36.Tests
{
    /// <summary>
    /// Main menu, pause menu and the Rota07 airspeed/throttle gauges (2026-11-10): pure rules, the AnaMenu scene, the Rota07
    /// wiring and the dial face import.
    /// </summary>
    public class MenuTests
    {
        Scene scene;

        [TearDown]
        public void Close()
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        GameObject[] Open(string path)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene.GetRootGameObjects();
        }

        static T Tek<T>(GameObject[] roots) where T : Component =>
            roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();

        // ------------------------------------------------------------ pure rules

        [Test]
        public void ButtonSheetRows_TopToBottom_NormalHoverPressedDisabled()
        {
            Assert.AreEqual(new Rect(0f, 0.75f, 1f, 0.25f), MenuDugmesi.Satir(MenuDugmesi.Normal));
            Assert.AreEqual(new Rect(0f, 0.5f, 1f, 0.25f), MenuDugmesi.Satir(MenuDugmesi.Uzerinde));
            Assert.AreEqual(new Rect(0f, 0.25f, 1f, 0.25f), MenuDugmesi.Satir(MenuDugmesi.Basili));
            Assert.AreEqual(new Rect(0f, 0f, 1f, 0.25f), MenuDugmesi.Satir(MenuDugmesi.Pasif));
        }

        [Test]
        public void TurkishUpperCase_DottedAndDotlessI()
        {
            Assert.AreEqual("HAFTA 5 · SINIR HATTI", MenuArayuzu.Buyuk("Hafta 5 · Sınır Hattı"));
            Assert.AreEqual("HAFTA 6 · GRİ KUTU", MenuArayuzu.Buyuk("Hafta 6 · Gri Kutu"));
            Assert.AreEqual("GECE GÖREVİ", MenuArayuzu.Buyuk("Gece Görevi"));
        }

        [Test]
        public void Settings_Cycle_Volume_Quality_AndNames()
        {
            Assert.AreEqual(0.75f, OyunAyarlari.SonrakiSes(1f));
            Assert.AreEqual(0f, OyunAyarlari.SonrakiSes(0.25f));
            Assert.AreEqual(1f, OyunAyarlari.SonrakiSes(0f));
            Assert.AreEqual(0.5f, OyunAyarlari.SonrakiSes(0.7f), "nearest step is taken as the current one");
            Assert.AreEqual(1, OyunAyarlari.SonrakiKalite(0, 2));
            Assert.AreEqual(0, OyunAyarlari.SonrakiKalite(1, 2));
            Assert.AreEqual(0, OyunAyarlari.SonrakiKalite(0, 0));
            string[] adlar = { "Mobile", "PC" };
            Assert.AreEqual("DÜŞÜK", OyunAyarlari.KaliteAdi(0, adlar));
            Assert.AreEqual("YÜKSEK", OyunAyarlari.KaliteAdi(1, adlar));
            Assert.AreEqual("?", OyunAyarlari.KaliteAdi(2, adlar));
            CollectionAssert.AreEqual(new[] { "Mobile", "PC" }, QualitySettings.names, "the project's quality levels");
        }

        [Test]
        public void GaugeReadouts_And_DialAngles()
        {
            Assert.AreEqual("0 km/sa", GeceHud.HizYazisi(0f));
            Assert.AreEqual("180 km/sa", GeceHud.HizYazisi(50f));
            Assert.AreEqual("0 km/sa", GeceHud.HizYazisi(-3f));
            Assert.AreEqual("GAZ %0", GeceHud.GazYazisi(0f));
            Assert.AreEqual("GAZ %75", GeceHud.GazYazisi(0.75f));
            Assert.AreEqual("GAZ %100", GeceHud.GazYazisi(1.4f));
            // The 0–500 km/h face: 250 km/h points straight up, 0 at -135°, 500 at +135° (clockwise), beyond stays at the end.
            float azami = Lab7Arayuz.KadranAzamiMs;
            Assert.AreEqual(0f, HudHesap.IbreZ(250f / 3.6f, azami), 1e-3f);
            Assert.AreEqual(135f, HudHesap.IbreZ(0f, azami), 1e-3f);
            Assert.AreEqual(-135f, HudHesap.IbreZ(500f / 3.6f, azami), 1e-3f);
            Assert.AreEqual(-135f, HudHesap.IbreZ(200f, azami), 1e-3f);
            // Take-off (~176 km/h, Lab 7 measurement) is inside the green band that starts at 150 km/h.
            Assert.Greater(HudHesap.KadranAcisi(176f / 3.6f, azami), HudHesap.KadranAcisi(150f / 3.6f, azami));
        }

        // ------------------------------------------------------------ scenes

        [Test]
        public void AnaMenu_Scene_InBuild_Wired_AndEveryChapterSceneIsInTheBuild()
        {
            var yollar = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            Assert.Contains(MenuSceneBuilder.ScenePath, yollar);
            Assert.AreEqual("Assets/Scenes/Rota02.unity", EditorBuildSettings.scenes[0].path, "earlier Build Settings order kept");

            GameObject[] roots = Open(MenuSceneBuilder.ScenePath);
            var menu = Tek<AnaMenu>(roots);
            Assert.IsNotNull(menu.Arkaplan, "background");
            Assert.IsNotNull(menu.DugmeSeti, "button sheet");
            // Hafta 8 (Lab 8 phase 3, D-118): the list gained Bölüm 4 as its 7th entry.
            // Hafta 9 (Lab 9, D-136): GÖREVE BAŞLA opens Bölüm 5; the list gained it as its 8th entry.
            Assert.AreEqual(Lab9SceneBuilder.SceneName, menu.GorevSahnesi);
            Assert.AreEqual(8, menu.Bolumler.Count);
            Assert.AreEqual(Lab8SceneBuilder.SceneName, menu.Bolumler[6].sahne);
            Assert.AreEqual(Lab9SceneBuilder.SceneName, menu.Bolumler[7].sahne);
            foreach (AnaMenu.Bolum b in menu.Bolumler)
                Assert.Contains($"Assets/Scenes/{b.sahne}.unity", yollar, b.ad);
            Assert.IsNotNull(Tek<Camera>(roots));
            Assert.IsFalse(roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).Any(c => c == null), "missing script");
        }

        [Test]
        public void Rota07_HasAirspeedDial_ThrottleLever_PauseMenu()
        {
            GameObject[] roots = Open(Lab7SceneBuilder.ScenePath);
            var ucus = Tek<UcusKontrol>(roots);
            var hud = new SerializedObject(Tek<GeceHud>(roots));
            foreach (string ad in new[] { "gostergeler", "hizMetni", "gazMetni" })
                Assert.IsNotNull(hud.FindProperty(ad).objectReferenceValue, "HUD " + ad);

            var hiz = Tek<HudGosterge>(roots);
            Assert.AreSame(ucus, hiz.Ucus);
            Assert.AreEqual(HudKaynak.Hiz, hiz.Kaynak);
            Assert.AreEqual(500f / 3.6f, hiz.Azami, 1e-3f);
            Assert.IsNotNull(hiz.Ibre);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<Sprite>(Lab7Arayuz.KadranYolu), hiz.GetComponent<Image>().sprite);

            var gaz = Tek<HudGazGostergesi>(roots);
            Assert.AreSame(ucus, gaz.Ucus);
            Assert.IsNotNull(gaz.Kol);
            Assert.Less(gaz.AltY, gaz.UstY);
            Assert.AreEqual(gaz.AltY, gaz.Kol.anchoredPosition.y, 1e-3f, "knob starts at 0 % (engine idle on the runway)");

            var menu = Tek<DuraklatmaMenusu>(roots);
            Assert.IsNotNull(menu.DugmeSeti);
            Assert.AreEqual(MenuSceneBuilder.SceneName, menu.AnaMenuSahnesi);
            Assert.AreEqual(MenuSceneBuilder.SceneName, Tek<GeceSonuEkrani>(roots).MenuSahnesi);
            Assert.IsFalse(roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).Any(c => c == null), "missing script");
        }

        [Test]
        public void DialFace_IsA512Sprite_WithoutMipmaps()
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(Lab7Arayuz.KadranYolu);
            Assert.IsNotNull(imp);
            Assert.AreEqual(TextureImporterType.Sprite, imp.textureType);
            Assert.IsFalse(imp.mipmapEnabled);
            Assert.IsTrue(imp.DoesSourceTextureHaveAlpha());
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Lab7Arayuz.KadranYolu);
            Assert.AreEqual(512, tex.width);
            Assert.AreEqual(512, tex.height);
        }
    }
}
